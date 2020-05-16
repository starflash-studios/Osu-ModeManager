using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Octokit;

namespace OsuModeManager {
    public struct Gamemode : IEquatable<Gamemode> {
        
        public string GitHubUser;
        public string GitHubRepo;
        public string GitHubTagVersion;
        public string RulesetFilename;

        public bool? UpdateRequired;

        public System.Windows.Visibility DisplayUpdate => UpdateRequired == true ? System.Windows.Visibility.Visible : System.Windows.Visibility.Hidden;
        public System.Windows.Visibility DisplayUpToDate => UpdateRequired == false ? System.Windows.Visibility.Visible : System.Windows.Visibility.Hidden;
        public string DisplayName => ToString();

        public static Gamemode CreateInstance(string GitHubUser = @"Altenhh", string GitHubRepo = "tau", string TagVersion = null, string RulesetFilename = null, bool? UpdateRequired = null) => new Gamemode(GitHubUser, GitHubRepo, TagVersion, RulesetFilename, UpdateRequired);

        public Gamemode(string GitHubUser = @"Altenhh", string GitHubRepo = "tau", string TagVersion = null, string RulesetFilename = "osu.Game.Rulesets.Tau.dll", bool? UpdateRequired = null) {
            this.GitHubUser = GitHubUser;
            this.GitHubRepo = GitHubRepo;
            this.GitHubTagVersion = TagVersion;
            this.RulesetFilename = RulesetFilename;
            this.UpdateRequired = UpdateRequired;
        }

        #region GitHub
        public async Task<(bool, Release)> TryGetLatestReleaseAsync() {
            IEnumerable<Release> Releases = await MainWindow.Client.Repository.Release.GetAll(GitHubUser, GitHubRepo);
            //Releases = Releases.Reverse();
            return Releases.TryGetFirst(out Release LatestRelease) ? (true, LatestRelease) : (false, null);
        }

        public static bool TryGetRulesetAsync(Release Release, out ReleaseAsset FoundAsset) {
            foreach (ReleaseAsset Asset in Release.Assets.Where(Asset => Asset.Name.ToLowerInvariant().EndsWith(".dll"))) {
                FoundAsset = Asset;
                return true;
            }
            FoundAsset = null;
            return false;
        } 

        public async Task<(bool, Release)> TryGetCurrentReleaseAsync() {
            // ReSharper disable once LoopCanBePartlyConvertedToQuery
            foreach (Release Release in await MainWindow.Client.Repository.Release.GetAll(GitHubUser, GitHubRepo)) {
                if (Release.TagName.Equals(GitHubTagVersion, StringComparison.InvariantCultureIgnoreCase)) {
                    return (true, Release);
                }
            }
            return (false, null);
        }

        public async Task<(bool?, Release)> CheckForUpdate() {
            (bool LatestSucess, Release Latest) = await TryGetLatestReleaseAsync();
            (bool CurrentSucess, Release Current) = await TryGetCurrentReleaseAsync();

            if (LatestSucess && CurrentSucess) {
                //Debug.WriteLine("Current: " + Current.TagName + " | Latest: " + Latest.TagName + " |== " + Current.TagName.Equals(Latest.TagName));
                UpdateRequired = !Latest.TagName.Equals(Current.TagName, StringComparison.InvariantCultureIgnoreCase);
            }
            return (UpdateRequired, Latest);
        }

        #endregion

        #region Single Importing/Exporting
        public const char CharData = '';
        public const char CharLine = '';

        public string ExportGamemode() => $"{GitHubUser}{CharData}{GitHubRepo}{CharData}{GitHubTagVersion}{CharData}{RulesetFilename}";

        public static Gamemode? ImportGamemode(string ImportSection) {
            string[] Sections = ImportSection?.Split(CharData);
            if (Sections.TryGetAt(0, out string User) && Sections.TryGetAt(1, out string Repo) && Sections.TryGetAt(2, out string Tag) && Sections.TryGetAt(3, out string RulesetFile)) {
                return CreateInstance(User, Repo, Tag, RulesetFile);
            }
            return null;
        }
        #endregion

        #region File Importing/Exporting
        public static string ExportGamemodes(Gamemode[] Gamemodes) {
            string Result = "";
            for (int G = 0; G < Gamemodes.Length; G++) {
                Result += (G > 0 ? CharLine.ToString() : "") + Gamemodes[G].ExportGamemode();
            }
            return Result;
        }

        public static IEnumerable<Gamemode> ImportGamemodes(FileInfo GamemodeFile) {
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach(string Section in File.ReadAllText(GamemodeFile.FullName).Split(CharLine)) {
                Gamemode? SectionResult = ImportGamemode(Section);
                if (SectionResult.HasValue) {
                    yield return SectionResult.Value;
                }
            }
        }
        #endregion

        #region Generic Overrides
        public override string ToString() => $"{RulesetFilename} ({GitHubTagVersion})";

        public override bool Equals(object Obj) => Obj is Gamemode Gamemode && Equals(Gamemode);

        public override int GetHashCode() {
            unchecked {
                // ReSharper disable NonReadonlyMemberInGetHashCode
                int HashCode = (GitHubUser != null ? GitHubUser.GetHashCode() : 0);
                HashCode = (HashCode * 397) ^ (GitHubRepo != null ? GitHubRepo.GetHashCode() : 0);
                HashCode = (HashCode * 397) ^ (GitHubTagVersion != null ? GitHubTagVersion.GetHashCode() : 0);
                HashCode = (HashCode * 397) ^ (RulesetFilename != null ? RulesetFilename.GetHashCode() : 0);
                // ReSharper restore NonReadonlyMemberInGetHashCode
                return HashCode;
            }
        }

        public bool Equals(Gamemode Other) =>
            GitHubUser == Other.GitHubUser &&
            GitHubRepo == Other.GitHubRepo &&
            GitHubTagVersion == Other.GitHubTagVersion &&
            RulesetFilename == Other.RulesetFilename;

        public static bool operator ==(Gamemode Left, Gamemode Right) => Left.Equals(Right);

        public static bool operator !=(Gamemode Left, Gamemode Right) => !(Left == Right);
        #endregion
    }
}
