#region Copyright (C) 2017-2020  Starflash Studios
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License (Version 3.0)
// as published by the Free Software Foundation.
// 
// More information can be found here: https://www.gnu.org/licenses/gpl-3.0.en.html
#endregion

#region Using Directives

using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Octokit;
using OsuModeManager.Extensions;

#endregion

namespace OsuModeManager.Windows {
    public partial class GamemodeEditor  {
        public TaskCompletionSource<Gamemode> Result;
        public Gamemode ResultantGamemode;

        public GamemodeEditor() {
            InitializeComponent();

            Task.Run(async () => {
                List<string> KnownURLs = await GetKnownModeURLs(MainWindow.Client);
                Dispatcher.Invoke(() => {
                    foreach (string KnownURL in KnownURLs) {
                        Debug.WriteLine($"\tGot: {KnownURL}");
                        KnownModes.Items.Add(KnownURL);
                    }
                });
            });
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

            TextBoxGitHubURL.Text = $"https://github.com/{CurrentGamemode.GitHubUser}/{CurrentGamemode.GitHubRepo}/";
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

        void SaveButton_Click(object Sender, RoutedEventArgs E) {
            string User = TextBoxGitHubUser.Text;
            string Repo = TextBoxGitHubRepo.Text;
            string Version = TextBoxTagVersion.Text;
            string RulesetFile = TextBoxRulsesetFilename.Text;

            if (User.IsNullOrEmpty() || Repo.IsNullOrEmpty() || Version.IsNullOrEmpty() || RulesetFile.IsNullOrEmpty()) { return; }

            ResultantGamemode = new Gamemode(User, Repo, Version, RulesetFile, ResultantGamemode.UpdateStatus);

            Result?.TrySetResult(ResultantGamemode);
        }

        #pragma warning disable IDE1006 // Naming Styles
        const string GitHubURLDecoder = @"^(.*?:\/\/)?(.*?\..*?\/)?(?<User>.*?)\/(?<Repo>.*?)(\/.*?)?$";
        #pragma warning restore IDE1006 // Naming Styles

        void TextBoxGitHubURL_TextChanged(object Sender, TextChangedEventArgs E) {
            string URL = TextBoxGitHubURL.Text;
            DecodeGitHubURL(URL, out string User, out bool UserS, out string Repo, out bool RepoS);
            if (UserS) {
                TextBoxGitHubUser.Text = User;
            }

            if (RepoS) {
                TextBoxGitHubRepo.Text = Repo;
            }

            GetLatestButton.IsEnabled = UserS && RepoS;
        }

        public static void DecodeGitHubURL(string URL, out string User, out bool UserS, out string Repo, out bool RepoS) {
            User = Repo = string.Empty;
            UserS = RepoS = false;

            if (!URL.IsNullOrEmpty()) {
                Match DecodedURL = Regex.Match(URL, GitHubURLDecoder);

                Group UserGroup = DecodedURL.Groups["User"];
                if (UserGroup.Success) {
                    User = UserGroup.Value;
                    UserS = true;
                }

                Group RepoGroup = DecodedURL.Groups["Repo"];
                if (RepoGroup.Success) {
                    Repo = RepoGroup.Value;
                    RepoS = true;
                }
            }
        }

        async void GetLatestButton_Click(object Sender, RoutedEventArgs E) => await GetLatest();

        async Task GetLatest() {
            string User = TextBoxGitHubUser.Text;
            string Repo = TextBoxGitHubRepo.Text;
            if ((await MainWindow.Client.Repository.Release.GetAll(User, Repo)).TryGetFirst(out Release Release)) {
                Dispatcher.Invoke(() => TextBoxTagVersion.Text = Release.TagName, DispatcherPriority.Normal);
                foreach (ReleaseAsset Asset in Release.Assets.Where(Asset => Asset.Name.ToLowerInvariant().EndsWith(".dll"))) {
                    Dispatcher.Invoke(() => TextBoxRulsesetFilename.Text = Asset.Name, DispatcherPriority.Normal);
                    ResultantGamemode.UpdateStatus = UpdateStatus.UpToDate;
                    break;
                }
            }
        }

        void MetroWindow_Closing(object Sender, CancelEventArgs E) => Result?.TrySetResult(ResultantGamemode);

        void TextBox_InvalidateUpdateCheck(object Sender, TextChangedEventArgs E) => ResultantGamemode.UpdateStatus = UpdateStatus.Unchecked;

        #pragma warning disable IDE1006 // Naming Styles
        //const string KnownNameRegex = @"^.*?NAME ?: ?(?<NAME>.+?)$";
        const string KnownURLRegex = @"^.*?\**?URL\**? ?: ?\**?(?<URL>.+?)$";
        //const string KnownStatusRegex = @"^.*?STATUS ?: ?(?<STATUS>.+?)$";
        #pragma warning restore IDE1006 // Naming Styles

        static async Task<List<string>> GetKnownModeURLs(GitHubClient Client) {
            List<string> URLs = new List<string>();
            IReadOnlyList<IssueComment> Comments = await Client.Issue.Comment.GetAllForIssue("ppy", "osu", 5852);
            //Regex NameR = new Regex(KnownNameRegex);
            Regex URLR = new Regex(KnownURLRegex);
            //Regex StatusR = new Regex(KnownStatusRegex);
            foreach (IssueComment C in Comments) {
                //if (L.Default) { continue; }
                string[] D = C.Body.GetLines();

                //string Name = "Unknown";
                string URL = string.Empty;
                //string Status = "Unknown";

                // ReSharper disable once LoopCanBePartlyConvertedToQuery
                Debug.WriteLine($"[{C.Id}]-----------");
                Debug.WriteLine($"\tDescription: '{C.Body}'");
                foreach (string DL in D) {
                    //Match NM = NameR.Match(DL);
                    //Group N = NM.Groups["NAME"];
                    //if (NM.Success && N.Success) { Name = N.Value; }

                    Debug.WriteLine($"\t\t'{DL}'");
                    Match UM = URLR.Match(DL);
                    Group UG = UM.Groups["URL"];
                    if (UM.Success && UG.Success) {
                        DecodeGitHubURL(UG.Value, out string U, out bool US, out string R, out bool UR);
                        if (!US) { Debug.WriteLine($"\tPost {C} ({C.Id}) has no valid github user."); }
                        if (!UR) { Debug.WriteLine($"\tPost {C} ({C.Id}) has no valid github repo."); }

                        URLs.Add($"{U}/{R}");
                        break;
                    }

                    //Match SM = StatusR.Match(DL);
                    //Group S = NM.Groups["STATUS"];
                    //if (SM.Success && S.Success) { Status = S.Value; }
                }

                //if (Name.IsNullOrEmpty()) { Debug.WriteLine($"Post {L} ({L.Id}) has no gamemode name."); }
                if (URL.IsNullOrEmpty()) {
                    Debug.WriteLine($"Post {C} ({C.Id}) has no gamemode URL.");
                } else {
                    URLs.Add(URL);
                }
                //if (Status.IsNullOrEmpty()) { Debug.WriteLine($"Post {L} ({L.Id}) has no gamemode status."); }
            }

            return URLs;
        }

        void KnownModes_SelectionChanged(object Sender, SelectionChangedEventArgs E) {
            string KnownURL = (Sender as ListView)?.SelectedItem as string;
            if (string.IsNullOrEmpty(KnownURL)) { return; }
            TextBoxGitHubURL.Text = KnownURL;
            GetLatestButton_Click(GetLatestButton, null);
            KnownModes.SelectedIndex = - 1;
        }
    }
}
