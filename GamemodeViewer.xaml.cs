using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Octokit;

namespace OsuModeManager {
    public partial class GamemodeViewer  {
        public TaskCompletionSource<GitHubGamemode> Result;
        public GitHubGamemode ResultantGamemode;

        public GamemodeViewer() {
            InitializeComponent();
        }

        public static async Task<GitHubGamemode> GetGamemodeViewer(GitHubGamemode CurrentGamemode = default) {
            GamemodeViewer Window = new GamemodeViewer();
            Window.Show();
            return await Window.GetGamemode(CurrentGamemode);
        }

        public async Task<GitHubGamemode> GetGamemode(GitHubGamemode CurrentGamemode = default) {
            if (Result != null) {
                Debug.WriteLine("Please do not request multiple gamemodes at once.", "Warning");
                return default;
            }

            if (CurrentGamemode == default) { CurrentGamemode = GitHubGamemode.CreateInstance(); }
            TextBoxGitHubURL.Text = GitHubURLEncoder.Replace("%GitHubUser%", CurrentGamemode.GitHubUser).Replace("%GitHubRepo%", CurrentGamemode.GitHubRepo);
            //TextBoxGitHubUser.Text = CurrentGamemode.GitHubUser;
            //TextBoxGitHubRepo.Text = CurrentGamemode.GitHubRepo;
            TextBoxTagVersion.Text = CurrentGamemode.TagVersion;
            TextBoxRulsesetFilename.Text = CurrentGamemode.RulesetFilename;

            ResultantGamemode = CurrentGamemode;

            //await GetLatest();

            Result = new TaskCompletionSource<GitHubGamemode>();
            GitHubGamemode ResultGamemode = await Result.Task;

            Close();
            return ResultGamemode;
        }

        void SaveButton_Click(object Sender, System.Windows.RoutedEventArgs E) {
            string User = TextBoxGitHubUser.Text;
            string Repo = TextBoxGitHubRepo.Text;
            string Version = TextBoxTagVersion.Text;
            string RulesetFile = TextBoxRulsesetFilename.Text;

            if (User.IsNullOrEmpty() || Repo.IsNullOrEmpty() || Version.IsNullOrEmpty() || RulesetFile.IsNullOrEmpty()) { return; }

            ResultantGamemode = new GitHubGamemode(User, Repo, Version, RulesetFile);

            Result?.TrySetResult(ResultantGamemode);
        }

        const string GitHubURLDecoder = @"http(s)?:\/\/github.com?(.*?)\/(?<GitHubUser>.+?)\/(?<GitHubRepo>.+?)\/";
        
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

            if ((await MainWindow.GetRepositoryReleases(User, Repo)).TryGetFirst(out Release Release)) {
                Dispatcher.Invoke(() => TextBoxTagVersion.Text = Release.TagName, System.Windows.Threading.DispatcherPriority.Normal);
                foreach (ReleaseAsset Asset in Release.Assets.Where(Asset => Asset.Name.ToLowerInvariant().EndsWith(".dll"))) {
                    Dispatcher.Invoke(() => TextBoxRulsesetFilename.Text = Asset.Name, System.Windows.Threading.DispatcherPriority.Normal);
                    break;
                }
            }
        }

        void MetroWindow_Closing(object Sender, System.ComponentModel.CancelEventArgs E) => Result?.TrySetResult(ResultantGamemode);
    }
}
