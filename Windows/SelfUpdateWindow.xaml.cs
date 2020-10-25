#region Copyright (C) 2017-2020  Starflash Studios
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License (Version 3.0)
// as published by the Free Software Foundation.
// 
// More information can be found here: https://www.gnu.org/licenses/gpl-3.0.en.html
#endregion

#region Using Directives

using System;
using System.ComponentModel;
using System.Linq;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Octokit;
using OsuModeManager.Extensions;
using Syroot.Windows.IO;

#endregion

namespace OsuModeManager.Windows {
    public partial class SelfUpdateWindow {
        public static DirectoryInfo Downloads = KnownFolderType.DownloadsLocalized.GetDirectoryInfo();
        public const string GitHubCreator = "starflash-studios";
        public const string GitHubRepo = "Osu-ModeManager";

        static Release _LatestRelease;
        static Window _Caller;

        string _DownloadTotal;
        
        public SelfUpdateWindow() {
            InitializeComponent();
        }

        public static async Task CreateUpdateChecker(Window Caller = null) {
            Debug.WriteLine("Checking for application updates...");
            bool UpdateRequired = await CheckForUpdate();
            if (UpdateRequired) {
                Debug.WriteLine("\tAn update is available.");
                Caller?.Hide();
                SelfUpdateWindow SelfUpdateWindow = new SelfUpdateWindow();
                SelfUpdateWindow.Show();
                _Caller = Caller;

                SelfUpdateWindow.UpdateCurrentVersionLabel(MainWindow.GetCurrentApplicationVersionName());
                SelfUpdateWindow.UpdateLatestVersionLabel(_LatestRelease.TagName);
                SelfUpdateWindow.DownloadButton.Visibility = Visibility.Visible;
            } else {
                Debug.WriteLine("\tThis application is up to date. :D");
            }
        }

        public void UpdateCurrentVersionLabel(string CurrentVersion) => CurrentVersionLabel.Content = CurrentVersionLabel.Content.ToString().Replace(@"%%CVER%%", CurrentVersion);

        public void UpdateLatestVersionLabel(string LatestVersion) => LatestVersionLabel.Content = LatestVersionLabel.Content.ToString().Replace(@"%%LVER%%", LatestVersion);

        /// <summary>Checks for updates and returns true if one is required.</summary>
        /// <returns><see cref="bool"/></returns>
        public static async Task<bool> CheckForUpdate() {
            Version CurrentVersion = new Version(MainWindow.GetCurrentApplicationVersionName());

            if ((await MainWindow.Client.Repository.Release.GetAll(GitHubCreator, GitHubRepo)).TryGetFirst(out _LatestRelease, true)) {
                Version LatestVersion = new Version(_LatestRelease.TagName);
                Debug.WriteLine("Current: " + CurrentVersion + " | Latest: " + LatestVersion + " | Update? " + (CurrentVersion < LatestVersion));
                if (CurrentVersion < LatestVersion) {
                    return true;
                }
            }
            return false;
        }

        void DownloadButton_Click(object Sender, RoutedEventArgs E) {
            if (_LatestRelease != null && Downloads.TryGetRelativeFile("Release.zip", out FileInfo Destination)) {
                foreach (ReleaseAsset Asset in _LatestRelease.Assets.Where(Asset => Asset.Name.Equals("release.zip", StringComparison.InvariantCultureIgnoreCase))) {
                    Dispatcher.Invoke(() => {
                        DownloadButton.IsEnabled = false;
                        DownloadButton.Visibility = Visibility.Collapsed;
                    }, DispatcherPriority.Normal);

                    WebClient Client = new WebClient();
                    Client.DownloadProgressChanged += Client_DownloadProgressChanged;
                    Client.DownloadFileCompleted += Client_DownloadFileCompleted;
                    Client.DownloadFileAsync(new Uri(Asset.BrowserDownloadUrl), Destination.FullName);
                    return;
                }
            }
        }

        void Client_DownloadProgressChanged(object Sender, DownloadProgressChangedEventArgs E) {
            if (_DownloadTotal.IsNullOrEmpty()) {
                _DownloadTotal = SizeSuffix(E.TotalBytesToReceive);
                DownloadProgress.Maximum = E.TotalBytesToReceive;
                Title = "Downloading 'Release.zip'...";
            }

            DownloadProgress.Value = E.BytesReceived;
            DownloadProgressLabel.Content = SizeSuffix(E.BytesReceived) + " / " + _DownloadTotal;
            DownloadProgressLabelShadow.Content = DownloadProgressLabel.Content;
        }

        void Client_DownloadFileCompleted(object Sender, AsyncCompletedEventArgs E) {
            Debug.WriteLine("Download complete!");
            Downloads.OpenInExplorer();
            Close();
        }

        void MetroWindow_Closing(object Sender, CancelEventArgs E) => _Caller?.Show();

        static readonly string[] _SizeSuffixes =
            { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

        static string SizeSuffix(long Value) {
            if (Value < 0) { return "-" + SizeSuffix(-Value); }
            if (Value == 0) { return "0.00 bytes"; }

            int Mag = (int)Math.Log(Value, 1024);
            decimal AdjustedSize = (decimal)Value / (1L << (Mag * 10));

            if (Math.Round(AdjustedSize, 2) >= 1000) {
                Mag += 1;
                AdjustedSize /= 1024;
            }

            return $"{AdjustedSize:N2} {_SizeSuffixes[Mag]}";
        }
    }
}
