using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

using Octokit;

namespace OsuModeManager {
    public partial class UpdateWindow {
        public MainWindow MainWindow;

        public const string OriginalTitle = "%%COUNT%% Update%%S%% Available";

        public ObservableCollection<Gamemode> DisplayGamemodes { get; private set; } = new ObservableCollection<Gamemode>();
        public Dictionary<Gamemode, Release> GamemodeReleases;

        public UpdateWindow(MainWindow MainWindow, Dictionary<Gamemode, Release> Updates) {
            InitializeComponent();
            this.MainWindow = MainWindow;
            if (Updates != null && Updates.Count > 0) {
                GamemodeReleases = Updates;

                UpdateSingleButton.IsEnabled = false;
                Title = OriginalTitle.Replace("%%COUNT%%", GamemodeReleases.Count.ToString("N0")).Replace("%%S%%", GamemodeReleases.Count != 1 ? "s" : string.Empty);
                ConfirmCount.Content = GamemodeReleases.Count;
                ConfirmGrammar.Content = Title.Substring(Title.IndexOf(' ') + 1);
                ConfirmButton.Visibility = Visibility.Visible;
            } else {
                Title = OriginalTitle.Replace("%%COUNT%%", "0").Replace("%%S%%", "s");
                CloseButton.Visibility = Visibility.Visible;
            }
        }

        void UpdateList_SelectionChanged(object Sender, SelectionChangedEventArgs E) => UpdateSingleButton.IsEnabled = UpdateList.SelectedIndex >= 0;

        async void UpdateSingleButton_Click(object Sender, RoutedEventArgs E) {
            MainGrid.IsEnabled = false;
            int SelectedIndex = UpdateList.SelectedIndex;
            if (SelectedIndex >= 0) {
                await Update(SelectedIndex);
                if (FilesRecycled) {
                    FilesRecycled = false;
                    Process.Start(FileExtensions.Explorer.FullName, "shell:RecycleBinFolder");
                }
            }

            Dispatcher.Invoke(() => {
                if (GamemodeReleases.Count <= 0) {
                    CloseButton.Visibility = Visibility.Visible;
                }
                MainGrid.IsEnabled = true;
            }, System.Windows.Threading.DispatcherPriority.Normal);
        }

        async void UpdateAllButton_Click(object Sender, RoutedEventArgs E) {
            MainGrid.IsEnabled = false;
            for (int G = GamemodeReleases.Count - 1; G >= 0; G--) {
                await Update(G);
                Dispatcher.Invoke(UpdateList.GetBindingExpression(ItemsControl.ItemsSourceProperty).UpdateTarget, System.Windows.Threading.DispatcherPriority.Normal);
            }

            if (FilesRecycled) {
                FilesRecycled = false;
                Process.Start(FileExtensions.Explorer.FullName, "shell:RecycleBinFolder");
            }

            Dispatcher.Invoke(() => {
                if (GamemodeReleases.Count <= 0) {
                    CloseButton.Visibility = Visibility.Visible;
                }
                MainGrid.IsEnabled = true;
            }, System.Windows.Threading.DispatcherPriority.Normal);
        }

        public async Task Update(int SelectedIndex) {
            Gamemode Gamemode = DisplayGamemodes[SelectedIndex];
            int CallerIndex = MainWindow?.Gamemodes.IndexOf(Gamemode) ?? -1;
            Release Release = GamemodeReleases[Gamemode];

            ReleaseAsset FoundAsset = null;
            foreach (ReleaseAsset Asset in Release.Assets.Where(Asset => Asset.Name.EndsWith(".dll"))) {
                FoundAsset = Asset;
            }

            if (FoundAsset == null) { return; }
            
            if (!await Update(MainWindow.GetCurrentLazerPath(), Gamemode, FoundAsset)) {
                return;
            }

            GamemodeReleases.Remove(Gamemode);
            DisplayGamemodes.RemoveAt(SelectedIndex);
            Title = OriginalTitle.Replace("%%COUNT%%", GamemodeReleases.Count.ToString("N0")).Replace("%%S%%", GamemodeReleases.Count != 1 ? "s" : string.Empty);

            if (CallerIndex >= 0) {
                Gamemode GamemodeClone = (Gamemode)Gamemode.Clone();
                GamemodeClone.UpdateStatus = UpdateStatus.UpToDate;
                GamemodeClone.GitHubTagVersion = Release.TagName ?? GamemodeClone.GitHubTagVersion;
                MainWindow.UpdateGamemode(CallerIndex, GamemodeClone, true);
            }
        }

        //Returns bool specifying whether or not to continue with the process
        public async Task<bool> Update(DirectoryInfo Destination, Gamemode Gamemode, ReleaseAsset Asset) {
            if (Destination == null || !Destination.Exists) {
                Dispatcher.Invoke(() => {
                    MessageBox.Show("Selected osu!lazer installation path is invalid.", Title, MessageBoxButton.OK, MessageBoxImage.Error);
                }, System.Windows.Threading.DispatcherPriority.Normal);
                MainWindow.UpdateLazerInstallationPath(false);
                return false;
            }
            Debug.WriteLine("Will Update " + Gamemode.RulesetFilename + " from " + Asset.BrowserDownloadUrl);

            FileInfo DestinationFile = Destination.TryGetRelativeFile(Gamemode.RulesetFilename, out FileInfo File) ? File : null;
            Debug.WriteLine("\tDestination: " + DestinationFile?.FullName);

            if (DestinationFile.Exists()) {
                RecycleFile(DestinationFile);
            }

            await DownloadFileAsync(new Uri(Asset.BrowserDownloadUrl), DestinationFile);
            return true;
        }

        public static async Task DownloadFileAsync(Uri DownloadUri, FileInfo Destination) {
            try {
                using (WebClient WebClient = new WebClient()) {
                    WebClient.Credentials = CredentialCache.DefaultNetworkCredentials;
                    await WebClient.DownloadFileTaskAsync(DownloadUri, Destination.FullName);
                }
#pragma warning disable CA1031 // Do not catch general exception types
            } catch (Exception) {
#pragma warning restore CA1031 // Do not catch general exception types
                Debug.WriteLine("Failed to download file: " + DownloadUri, "Error");
            }
        }

        public static async Task DownloadMultipleFilesAsync(List<(Uri DownloadUri, FileInfo Destination)> Downloads) => await Task.WhenAll(Downloads.Select(Download => DownloadFileAsync(Download.DownloadUri, Download.Destination)));

        public bool FilesRecycled;
        public void RecycleFile(FileInfo File) {
            File.Recycle();
            FilesRecycled = true; //Flag
        }

        void ConfirmButton_Click(object Sender, RoutedEventArgs E) {
            ConfirmButton.IsEnabled = false;
            ConfirmButton.Visibility = Visibility.Collapsed;
            foreach (Gamemode Gamemode in GamemodeReleases.Keys) {
                DisplayGamemodes.Add(Gamemode);
            }
        }

        void CloseButton_Click(object Sender, RoutedEventArgs E) => Close();

        void UpdateList_MouseDoubleClick(object Sender, System.Windows.Input.MouseButtonEventArgs E) {
            int SelectedIndex = UpdateList.SelectedIndex;
            if (SelectedIndex >= 0) {
                Release Release = GamemodeReleases[DisplayGamemodes[SelectedIndex]];
                _ = ReleaseWindow.ShowRelease(Release);
            }
        }
    }
}
