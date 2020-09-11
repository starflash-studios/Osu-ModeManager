using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Octokit;

namespace OsuModeManager {
    public partial class GamemodeEditor  {
        public TaskCompletionSource<Gamemode> Result;
        public Gamemode ResultantGamemode;

        public GamemodeEditor() {
            InitializeComponent();
        }

        public static async Task<Gamemode> GetGamemodeEditor(Gamemode CurrentGamemode = default) {
            Debug.WriteLine("Update Status: " + CurrentGamemode.UpdateStatus);
            GamemodeEditor Window = new GamemodeEditor();
            Window.Show();
            return await Window.GetGamemode(CurrentGamemode);
        }

        public async Task<Gamemode> GetGamemode(Gamemode CurrentGamemode = default) {
            if (Result != null) {
                Debug.WriteLine("Please do not request multiple gamemodes at once.", "Warning");
                return default;
            }

            if (CurrentGamemode == default) {
                CurrentGamemode = Gamemode.CreateInstance();
            }
            TextBoxGitHubURL.Text = GitHubURLEncoder.Replace("%GitHubUser%", CurrentGamemode.GitHubUser).Replace("%GitHubRepo%", CurrentGamemode.GitHubRepo);
            //TextBoxGitHubUser.Text = CurrentGamemode.GitHubUser;
            //TextBoxGitHubRepo.Text = CurrentGamemode.GitHubRepo;
            TextBoxTagVersion.Text = CurrentGamemode.GitHubTagVersion;
            TextBoxRulsesetFilename.Text = CurrentGamemode.RulesetFilename;

            ResultantGamemode = CurrentGamemode;

            //await GetLatest();

            Result = new TaskCompletionSource<Gamemode>();
            Gamemode ResultGamemode = await Result.Task;

            Close();
            return ResultGamemode;
        }

        void SaveButton_Click(object Sender, System.Windows.RoutedEventArgs E) {
            string User = TextBoxGitHubUser.Text;
            string Repo = TextBoxGitHubRepo.Text;
            string Version = TextBoxTagVersion.Text;
            string RulesetFile = TextBoxRulsesetFilename.Text;

            if (User.IsNullOrEmpty() || Repo.IsNullOrEmpty() || Version.IsNullOrEmpty() || RulesetFile.IsNullOrEmpty()) { return; }

            ResultantGamemode = new Gamemode(User, Repo, Version, RulesetFile, ResultantGamemode.UpdateStatus);

            Result?.TrySetResult(ResultantGamemode);
        }

        const string GitHubURLDecoder = @"http(s)?:\/\/github.com?(.*?)\/(?<GitHubUser>.*[0-9a-zA-Z])\/(?<GitHubRepo>.*[0-9a-zA-Z])";
        
        const string GitHubURLEncoder = @"https://github.com/%GitHubUser%/%GitHubRepo%/";

        void TextBoxGitHubURL_TextChanged(object Sender, System.Windows.Controls.TextChangedEventArgs E) {
            string URL = TextBoxGitHubURL.Text;
            if (URL.IsNullOrEmpty()) { return; }
            Match DecodedURL = Regex.Match(URL, GitHubURLDecoder);

            Group UserGroup = DecodedURL.Groups["GitHubUser"];
            if (UserGroup.Success) {
                TextBoxGitHubUser.Text = UserGroup.Value;
            }

            Group RepoGroup = DecodedURL.Groups["GitHubRepo"];
            if (RepoGroup.Success) {
                TextBoxGitHubRepo.Text = RepoGroup.Value;
            }

            GetLatestButton.IsEnabled = !TextBoxGitHubUser.Text.IsNullOrEmpty() && !TextBoxGitHubRepo.Text.IsNullOrEmpty();
        }

        async void GetLatestButton_Click(object Sender, System.Windows.RoutedEventArgs E) => await GetLatest();

        async Task GetLatest() {
            string User = TextBoxGitHubUser.Text;
            string Repo = TextBoxGitHubRepo.Text;
            if ((await MainWindow.Client.Repository.Release.GetAll(User, Repo)).TryGetFirst(out Release Release)) {
                Dispatcher.Invoke(() => TextBoxTagVersion.Text = Release.TagName, System.Windows.Threading.DispatcherPriority.Normal);
                foreach (ReleaseAsset Asset in Release.Assets.Where(Asset => Asset.Name.ToLowerInvariant().EndsWith(".dll"))) {
                    Dispatcher.Invoke(() => TextBoxRulsesetFilename.Text = Asset.Name, System.Windows.Threading.DispatcherPriority.Normal);
                    ResultantGamemode.UpdateStatus = UpdateStatus.UpToDate;
                    break;
                }
            }
        }

        void MetroWindow_Closing(object Sender, System.ComponentModel.CancelEventArgs E) => Result?.TrySetResult(ResultantGamemode);

        void TextBox_InvalidateUpdateCheck(object Sender, System.Windows.Controls.TextChangedEventArgs E) => ResultantGamemode.UpdateStatus = UpdateStatus.Unchecked;
    }
}
