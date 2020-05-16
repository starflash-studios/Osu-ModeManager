using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;

using Octokit;

using OsuModeManager.Properties;

namespace OsuModeManager {
    public partial class MainWindow {
        public static DirectoryInfo LazerInstallationPath;
        public static GitHubClient Client;

        public ObservableCollection<Gamemode> Gamemodes { get; private set; } = new ObservableCollection<Gamemode>();

        public List<Gamemode> LoadedGamemodes = new List<Gamemode>();

        public ObservableCollection<DirectoryInfo> LazerInstallations { get; private set; } = new ObservableCollection<DirectoryInfo>();

        public MainWindow() {
            UpdateLazerInstallationPath();
            InitializeComponent(); //Setup dependencies before initialisation

            Client = new GitHubClient(new ProductHeaderValue("Osu!ModeManager", FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion));

            //Create new OAuth token if none exists, or if the latest is older than 1 hour
            if (Settings.Default.LatestToken.IsNullOrEmpty() || DateTime.UtcNow - Settings.Default.LatestTokenCreationTime > TimeSpan.FromHours(1)) {
                AuthoriseButton.Visibility = Visibility.Visible;
            } else {
                Client.Credentials = new Credentials(Settings.Default.LatestToken);
            }
            //AuthoriseButton.Visibility = System.Windows.Visibility.Visible;

            LazerVersionCombo.SelectedIndex = 0;
            LoadGamemodes();

            Debug.WriteLine("osu!lazer is installed at: " + LazerInstallationPath.FullName);
        }

        public void UpdateLazerInstallationPath(bool CheckResources = true) {
            if (!CheckResources || !Settings.Default.OsuLazerInstallationPath.TryGetDirectory(out DirectoryInfo NewInstallationPath)) {
                NewInstallationPath = Extensions.GetDirectory(SelectedPath: $@"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\osulazer\\", Description: "Please locate your osu!lazer installation directory (generally C:\\Users\\[USER-NAME]\\AppData\\Local\\osulazer\\)");

                if (NewInstallationPath == null || !NewInstallationPath.Exists) {
                    Environment.Exit(0);
                    return;
                }
            }

            Settings.Default.OsuLazerInstallationPath = NewInstallationPath.FullName;
            Settings.Default.Save();

            LazerInstallationPath = NewInstallationPath;
            
            LazerInstallations = new ObservableCollection<DirectoryInfo>();
            foreach (DirectoryInfo LazerInstallation in GetLazerVersions()) {
                LazerInstallations.Add(LazerInstallation);
                Debug.WriteLine("Found Installation: " + LazerInstallation);
            }
        }

        #region File Locations
        public DirectoryInfo GetCurrentLazerPath() => (DirectoryInfo)LazerVersionCombo.SelectedItem;

        public static List<DirectoryInfo> GetLazerVersions() {
            List<DirectoryInfo> AppVersions = LazerInstallationPath?.GetDirectories("app-*").ToList() ?? new List<DirectoryInfo>();
            AppVersions.Reverse();
            return AppVersions;
        }

        //public static List<FileInfo> GetGamemodes(DirectoryInfo LazerVersion) => LazerVersion.GetFiles("osu.Game.Rulesets.*.dll").ToList();

        public const string GamemodesFile = "osu.Game.Rulesets.List.txt";
        public static FileInfo GetGamemodesFile() {
            FileInfo SearchFile = new FileInfo($"{LazerInstallationPath.FullName}\\{GamemodesFile}");
            if (!SearchFile.Exists) { SearchFile.Create(); }
            return SearchFile;
        }

        public DirectoryInfo GetSelectedLazerInstall() => LazerInstallations[LazerVersionCombo.SelectedIndex];

        public static FileInfo Executable() => new FileInfo(Assembly.GetExecutingAssembly().Location);

        public static DirectoryInfo ExecutableLocation() => Executable().Directory;

        public static FileInfo GamemodeSaveLocation() => new FileInfo($"{ExecutableLocation().FullName}\\Gamemodes.txt");
        #endregion

        #region Gamemode List Editing
        async void GamemodeListAdd_Click(object Sender, RoutedEventArgs E) {
            Gamemode NewGamemode = await GamemodeEditor.GetGamemodeEditor(default);
            Dispatcher.Invoke(() => {
                Gamemodes.Add(NewGamemode);

                ToggleUpdateCheckButton();
            }, DispatcherPriority.Normal);
        }

        async void GamemodeList_MouseDoubleClick(object Sender, System.Windows.Input.MouseButtonEventArgs E) {
            int SelectedIndex = GamemodeList.SelectedIndex;
            if (SelectedIndex >= 0) {
                Gamemode NewGamemode = await GamemodeEditor.GetGamemodeEditor(Gamemodes[SelectedIndex]);
                Gamemodes[SelectedIndex] = NewGamemode;
            }
        }

        void GamemodeListRemove_Click(object Sender, RoutedEventArgs E) {
            int SelectedIndex = GamemodeList.SelectedIndex;
            if (SelectedIndex >= 0) {
                Gamemodes.RemoveAt(SelectedIndex);
                if (Gamemodes.Count > 0) {
                    GamemodeList.SelectedIndex = (SelectedIndex - 1).Clamp(0);
                }

                ToggleUpdateCheckButton();
            }
        }

        void GamemodeListImport_Click(object Sender, RoutedEventArgs E) {
            FileInfo ImportFile = Extensions.GetFile(Filter: $"{GamemodesFile}|{GamemodesFile}|Any File (*.*)|*.*");
            if (ImportFile != null && ImportFile.Exists) {
                File.WriteAllText(GetGamemodesFile().FullName, File.ReadAllText(ImportFile.FullName));
                Dispatcher.Invoke(LoadGamemodes, DispatcherPriority.Normal);
            }
        }

        void GamemodeListSave_Click(object Sender, RoutedEventArgs E) => SaveGamemodes();
        #endregion

        #region Update Checking
        public void ToggleUpdateCheckButton() => UpdateCheckButton.IsEnabled = GamemodeList.Items.Count > 0;

        async void UpdateCheckButton_Click(object Sender, RoutedEventArgs E) {
            Debug.WriteLine("Checking for updates...");
            bool AnyUpdatesRequired = false;
            Dictionary<Gamemode, Release> Updates = new Dictionary<Gamemode, Release>();
            for (int G = 0; G < Gamemodes.Count; G++) {
                Gamemode Gamemode = Gamemodes[G];
                //Debug.WriteLine("\tChecking: " + Gamemode);
                (bool ? UpdateRequired, Release LatestRelease) = await Gamemode.CheckForUpdate();
                //Debug.WriteLine("\t\tResult: " + UpdateRequired + "; " + LatestRelease.Name);
                bool? ReflectedUpdate = null;
                switch (UpdateRequired) {
                    case true:
                        Debug.WriteLine("Update required for " + Gamemode + " | Newest release: " + LatestRelease.TagName);
                        AnyUpdatesRequired = true;
                        ReflectedUpdate = true;

                        Updates.Add(Gamemodes[G], LatestRelease);
                        break;
                    case false:
                        Debug.WriteLine(Gamemode + " is up to date.");
                        ReflectedUpdate = false;
                        break;
                    case null:
                        Debug.WriteLine($"An error occurred when checking {Gamemode} ({Gamemode.GitHubRepo} by {Gamemode.GitHubUser})");

                        ReflectedUpdate = null;
                        break;
                }

                Gamemodes.RemoveAt(G);
                Gamemodes.Insert(G, new Gamemode(Gamemode.GitHubUser, Gamemode.GitHubRepo, Gamemode.GitHubTagVersion, Gamemode.RulesetFilename, ReflectedUpdate));
            }

            if (AnyUpdatesRequired) {
                UpdateWindow UpdateWindow = new UpdateWindow(this, Updates);
                UpdateWindow.Show();
                UpdateWindow.Closing += (_, __) => Dispatcher.Invoke(() => MainGrid.IsEnabled = true, DispatcherPriority.Normal);
                MainGrid.IsEnabled = false;
            }

            SaveGamemodes();
        }
        #endregion

        //Call OAuth flow in OAuthWindow and get token back
        async void AuthoriseButton_Click(object Sender, RoutedEventArgs E) {
            AuthoriseButton.IsEnabled = false;
            OAuthWindow OAuthWindow = new OAuthWindow();
            OAuthWindow.Show();

            string Token = await OAuthWindow.GetOAuthToken(Client, Properties.Resources.ClientID, Properties.Resources.ClientSecret);

            OAuthWindow.Close();
            if (!Token.IsNullOrEmpty()) {
                Debug.WriteLine("Got Token: " + Token);
                Client.Credentials = new Credentials(Token);
                Settings.Default.LatestToken = Token;
                Settings.Default.LatestTokenCreationTime = DateTime.UtcNow;
                Settings.Default.Save();

                Dispatcher.Invoke(() => { //Called on OAuth Success
                    AuthoriseButton.Visibility = Visibility.Collapsed;
                }, DispatcherPriority.Normal);
                return;
            }

            Dispatcher.Invoke(() => { //Called on OAuth Failure
                AuthoriseButton.IsEnabled = true;
            }, DispatcherPriority.Normal);
        }

        #region Saving / Loading
        public void LoadGamemodes() {
            FileInfo LoadFile = GetGamemodesFile();
            if (LoadFile != null && LoadFile.Exists) {
                Gamemodes = new ObservableCollection<Gamemode>(Gamemode.ImportGamemodes(LoadFile));
                GamemodeList.GetBindingExpression(System.Windows.Controls.ItemsControl.ItemsSourceProperty).UpdateTarget();
            }
            LoadedGamemodes = Gamemodes.ToList();
            ToggleUpdateCheckButton();
        }

        public void SaveGamemodes() {
            FileInfo SaveFile = GetGamemodesFile();
            File.WriteAllText(SaveFile.FullName, Gamemode.ExportGamemodes(Gamemodes.ToArray()));
            LoadedGamemodes = Gamemodes.ToList();
            Debug.WriteLine("Saving...");
        }

        void MainWindowElement_Closing(object Sender, System.ComponentModel.CancelEventArgs E) {
            if (!Gamemodes.SequenceEqual(LoadedGamemodes)) {
                switch (MessageBox.Show("Unsaved changes! Exit anyways?", Title + "・Unsaved Changes", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning)) {
                    case MessageBoxResult.Yes:
                    case MessageBoxResult.OK:
                        break;
                    default:
                        E.Cancel = true;
                        break;
                }
            }
        }
        #endregion
    }
}
