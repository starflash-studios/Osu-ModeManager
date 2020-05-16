using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;

using Octokit;

using Shell32;

namespace OsuModeManager {
    public partial class UpdateWindow {
        public MainWindow MainWindow;

        public static Shell Shell = new Shell();
        public static Folder RecyclingBin = Shell.NameSpace(ShellSpecialFolderConstants.ssfBITBUCKET); //Recycling Bin

        public const string OriginalTitle = "%%COUNT%% Update%%S%% Available";

        public ObservableCollection<Gamemode> DisplayGamemodes { get; private set; } = new ObservableCollection<Gamemode>();
        public Dictionary<Gamemode, Release> GamemodeReleases;

        public UpdateWindow(MainWindow MainWindow, Dictionary<Gamemode, Release> Updates) {
            InitializeComponent();
            this.MainWindow = MainWindow;
            GamemodeReleases = Updates;

            UpdateSingleButton.IsEnabled = false;
            Title = OriginalTitle.Replace("%%COUNT%%", GamemodeReleases.Count.ToString("N0")).Replace("%%S%%", GamemodeReleases.Count != 1 ? "s" : string.Empty);
            ConfirmCount.Content = GamemodeReleases.Count;
            ConfirmGrammar.Content = Title.Substring(Title.IndexOf(' ') + 1);
            ConfirmButton.Visibility = Visibility.Visible;
        }

        void UpdateList_SelectionChanged(object Sender, SelectionChangedEventArgs E) => UpdateSingleButton.IsEnabled = UpdateList.SelectedIndex >= 0;

        void UpdateSingleButton_Click(object Sender, RoutedEventArgs E) {
            int SelectedIndex = UpdateList.SelectedIndex;
            if (SelectedIndex >= 0) {
                Update(SelectedIndex);
                if (FilesRecycled) {
                    FilesRecycled = false;
                    Process.Start("explorer.exe", "shell:RecycleBinFolder");
                }
            }
        }

        void UpdateAllButton_Click(object Sender, RoutedEventArgs E) {
            for (int G = GamemodeReleases.Count - 1; G >= 0; G--) {
                Update(G);
            }
            if (FilesRecycled) {
                FilesRecycled = false;
                Process.Start("explorer.exe", "shell:RecycleBinFolder");
            }
        }

        public void Update(int SelectedIndex) {
            Gamemode Gamemode = DisplayGamemodes[SelectedIndex];
            Release Release = GamemodeReleases[Gamemode];

            ReleaseAsset FoundAsset = null;
            foreach (ReleaseAsset Asset in Release.Assets.Where(Asset => Asset.Name.EndsWith(".dll"))) {
                FoundAsset = Asset;
            }

            if (FoundAsset == null) { return; }
            
            if (!Update(MainWindow.GetCurrentLazerPath(), Gamemode, FoundAsset)) { return; }

            GamemodeReleases.Remove(Gamemode);
            DisplayGamemodes.RemoveAt(SelectedIndex);
            Title = OriginalTitle.Replace("%%COUNT%%", GamemodeReleases.Count.ToString("N0")).Replace("%%S%%", GamemodeReleases.Count != 1 ? "s" : string.Empty);

            int CallerIndex = MainWindow.Gamemodes.IndexOf(Gamemode);
            if (CallerIndex >= 0) {
                MainWindow.Gamemodes[CallerIndex] = new Gamemode(Gamemode.GitHubUser, Gamemode.GitHubRepo, Release.TagName, Gamemode.RulesetFilename, false);
            }
        }

        //Returns bool specifying whether or not to continue with the process
        public bool Update(DirectoryInfo Destination, Gamemode Gamemode, ReleaseAsset Asset) {
            if (Destination == null || !Destination.Exists) {
                Dispatcher.Invoke(() => {
                    MessageBox.Show("Selected osu!lazer installation path is invalid.", Title, MessageBoxButton.OK, MessageBoxImage.Error);
                }, System.Windows.Threading.DispatcherPriority.Normal);
                MainWindow.UpdateLazerInstallationPath(false);
                return false;
            }
            Debug.WriteLine("Will Update " + Gamemode.RulesetFilename + " from " + Asset.BrowserDownloadUrl);

            FileInfo DestinationFile = new FileInfo($"{Destination.FullName}\\{Gamemode.RulesetFilename}");
            Debug.WriteLine("\tDestination: " + DestinationFile.FullName);

            if (DestinationFile.Exists) {
                RecycleFile(DestinationFile);
            }
            using (WebClient WebClient = new WebClient()) {
                WebClient.DownloadFileAsync(new Uri(Asset.BrowserDownloadUrl), DestinationFile.FullName);
            }
            return true;
        }

        public bool FilesRecycled;
        public void RecycleFile(FileInfo File) {
            RecyclingBin.MoveHere(File.FullName);
            FilesRecycled = true; //Flag
        }

        void ConfirmButton_Click(object Sender, RoutedEventArgs E) {
            ConfirmButton.IsEnabled = false;
            ConfirmButton.Visibility = Visibility.Collapsed;
            foreach (Gamemode Gamemode in GamemodeReleases.Keys) {
                DisplayGamemodes.Add(Gamemode);
            }
        }

        void UpdateList_MouseDoubleClick(object Sender, System.Windows.Input.MouseButtonEventArgs E) {
            int SelectedIndex = UpdateList.SelectedIndex;
            if (SelectedIndex >= 0) {
                Release Release = GamemodeReleases[DisplayGamemodes[SelectedIndex]];
                _ = ReleaseWindow.ShowRelease(Release);
            }
        }
    }
}
