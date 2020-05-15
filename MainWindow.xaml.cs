using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Octokit;

namespace OsuModeManager {
    public partial class MainWindow {
        public static DirectoryInfo LazerInstallationPath;
        public static GitHubClient Client;

        public MainWindow() {
            InitializeComponent();
            //Client = new GitHubClient(new ProductHeaderValue("osumodemanager", FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion), new Credentials();
            Client = new GitHubClient(new ProductHeaderValue("osumodemanager"));
            UpdateLazerInstallationPath();
            ToggleUpdateCheckButton();

            List<DirectoryInfo> AppVersions = LazerInstallationPath.GetDirectories("app-*").ToList();
            AppVersions.Reverse();

            foreach(DirectoryInfo AppVersion in AppVersions) {
                LazerVersionCombo.Items.Add(AppVersion);
            }
            LazerVersionCombo.SelectedIndex = 0;

            Debug.WriteLine("osu!lazer is installed at: " + LazerInstallationPath.FullName);
        }

        public static List<DirectoryInfo> GetLazerVersions() {
            List<DirectoryInfo> AppVersions = LazerInstallationPath.GetDirectories("app-*").ToList();
            AppVersions.Reverse();
            return AppVersions;
        }

        public static List<FileInfo> GetGamerules(DirectoryInfo LazerVersion) => LazerVersion.GetFiles("osu.Game.Rulesets.*.dll").ToList();

        public static void UpdateLazerInstallationPath(bool CheckResources = true) {
            if (!CheckResources || Properties.Settings.Default.OsuLazerInstallationPath.IsNullOrEmpty()) {
                DirectoryInfo NewInstallationPath = Extensions.GetDirectory(SelectedPath: Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\osulazer\\");

                if (NewInstallationPath == null || !NewInstallationPath.Exists) {
                    Environment.Exit(0);
                    return;
                }

                Properties.Settings.Default.OsuLazerInstallationPath = NewInstallationPath.FullName;
                Properties.Settings.Default.Save();

                LazerInstallationPath = NewInstallationPath;

            } else if (Properties.Settings.Default.OsuLazerInstallationPath.TryGetDirectory(out DirectoryInfo NewInstallationPath)) {
                LazerInstallationPath = NewInstallationPath;
            } else {
                Environment.Exit(0);
            }
        }

        #region File Locations
        public static FileInfo Executable() => new FileInfo(Assembly.GetExecutingAssembly().Location);

        public static DirectoryInfo ExecutableLocation() => Executable().Directory;

        public static FileInfo GamemodeSaveLocation() => new FileInfo($"{ExecutableLocation().FullName}\\Gamemodes.txt");
        #endregion

        #region GitHub Engine
        public static async Task<IReadOnlyList<Release>> GetRepositoryReleases(string Author, string Name) => await Client.Repository.Release.GetAll(Author, Name);

        #endregion

        #region Gamemode List Editing
        async void GamemodeListAdd_Click(object Sender, System.Windows.RoutedEventArgs E) {
            GamemodeList.Items.Add(await GamemodeViewer.GetGamemodeViewer());
            ToggleUpdateCheckButton();
        }

        async void GamemodeList_MouseDoubleClick(object Sender, System.Windows.Input.MouseButtonEventArgs E) {
            int SelectedIndex = GamemodeList.SelectedIndex;
            if (SelectedIndex >= 0) {
                GamemodeList.Items[SelectedIndex] = await GamemodeViewer.GetGamemodeViewer((GitHubGamemode)GamemodeList.Items[SelectedIndex]);
            }
        }

        void GamemodeListRemove_Click(object Snder, System.Windows.RoutedEventArgs E) {
            int SelectedIndex = GamemodeList.SelectedIndex;
            if (SelectedIndex >= 0) {
                GamemodeList.Items.RemoveAt(SelectedIndex);
                if (GamemodeList.Items.Count > 0) {
                    GamemodeList.SelectedIndex = (SelectedIndex - 1).Clamp(0);
                }

                ToggleUpdateCheckButton();
            }
        }
        #endregion

        public void ToggleUpdateCheckButton() => UpdateCheckButton.IsEnabled = (GamemodeList.Items?.Count ?? 0) > 0;

        async void UpdateCheckButton_Click(object Sender, System.Windows.RoutedEventArgs E) {
            Debug.WriteLine("Checking for updates...");
            foreach(GitHubGamemode Gamemode in GamemodeList.Items) {
                Debug.WriteLine("\tChecking: " + Gamemode);
                (bool? UpdateRequired, Release LatestRelease) = await Gamemode.CheckForUpdate();
                Debug.WriteLine("\t\tResult: " + UpdateRequired + "; " + LatestRelease.Name);
                switch (UpdateRequired) {
                    case true:
                        Debug.WriteLine("Update required for " + Gamemode + " | Newest release: " + LatestRelease.Name);
                        break;
                    case false:
                        Debug.WriteLine(Gamemode + " is up to date.");
                        break;
                    case null:
                        Debug.WriteLine($"An error occurred when checking {Gamemode} ({Gamemode.GitHubRepo} by {Gamemode.GitHubUser})");
                        break;
                }
            }
        }
    }
}
