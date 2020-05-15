using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Octokit;

namespace OsuModeManager {
    public struct GitHubGamemode : IEquatable<GitHubGamemode> {
        
        public string GitHubUser;
        public string GitHubRepo;
        public string TagVersion;
        public string RulesetFilename;

        public static GitHubGamemode CreateInstance(string GitHubUser = @"Altenhh", string GitHubRepo = "tau", string TagVersion = null, string RulesetFilename = "osu.Game.Rulesets.Tau.dll") => new GitHubGamemode(GitHubUser, GitHubRepo, TagVersion, RulesetFilename);
        public GitHubGamemode(string GitHubUser = @"Altenhh", string GitHubRepo = "tau", string TagVersion = null, string RulesetFilename = "osu.Game.Rulesets.Tau.dll") {
            this.GitHubUser = GitHubUser;
            this.GitHubRepo = GitHubRepo;
            this.TagVersion = TagVersion;
            this.RulesetFilename = RulesetFilename;
        }

        #region GitHub
        public async Task<(bool, Release)> TryGetLatestReleaseAsync() {
            IEnumerable<Release> Releases = await MainWindow.GetRepositoryReleases(GitHubUser, GitHubRepo);
            Releases = Releases.Reverse();
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
                if (Release.TagName.Equals(TagVersion, StringComparison.InvariantCultureIgnoreCase)) {
                    Debug.WriteLine(Release.TagName + "==" + TagVersion);
                    return (true, Release);
                }
            }
            return (false, null);
        }

        public async Task<(bool?, Release)> CheckForUpdate() {
            (bool LatestSucess, Release Latest) = await TryGetLatestReleaseAsync();
            (bool CurrentSucess, Release Current) = await TryGetCurrentReleaseAsync();

            Debug.WriteLine("[ID Check; Latest: " + Latest.Id + " | Current: " + Current.Id + "]");
            return LatestSucess && CurrentSucess ? ((bool ?, Release))(!Latest.Id.Equals(Current.Id), Latest) : (null, Latest);
        }

        #endregion

        #region Single Importing/Exporting
        public const char CharData = '';
        public const char CharLine = '';

        public string ExportGamemode() => $"{GitHubUser}{CharData}{GitHubRepo}{CharData}{TagVersion}{CharData}{RulesetFilename}";

        public static GitHubGamemode? ImportGamemode(string ImportSection) {
            string[] Sections = ImportSection?.Split(CharData);
            if (Sections.TryGetAt(0, out string User) && Sections.TryGetAt(1, out string Repo) && Sections.TryGetAt(2, out string Tag) && Sections.TryGetAt(3, out string RulesetFile)) {
                return CreateInstance(User, Repo, Tag, RulesetFile);
            }
            return null;
        }
        #endregion

        #region File Importing/Exporting
        public static string ExportGamemodes(GitHubGamemode[] Gamemodes) {
            string Result = "";
            for (int G = 0; G < Gamemodes.Length; G++) {
                Result += (G > 0 ? CharLine.ToString() : "") + Gamemodes[G].ExportGamemode();
            }
            return Result;
        }

        public static IEnumerable<GitHubGamemode> ImportGamemodes(FileInfo GamemodeFile) {
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach(string Section in File.ReadAllText(GamemodeFile.FullName).Split(CharLine)) {
                GitHubGamemode? SectionResult = ImportGamemode(Section);
                if (SectionResult.HasValue) {
                    yield return SectionResult.Value;
                }
            }
        }
        #endregion

        #region Generic Overrides
        public override string ToString() => $"{RulesetFilename} ({TagVersion})";

        public override bool Equals(object Obj) => Obj is GitHubGamemode Gamemode && Equals(Gamemode);

        public override int GetHashCode() {
            unchecked {
                // ReSharper disable NonReadonlyMemberInGetHashCode
                int HashCode = (GitHubUser != null ? GitHubUser.GetHashCode() : 0);
                HashCode = (HashCode * 397) ^ (GitHubRepo != null ? GitHubRepo.GetHashCode() : 0);
                HashCode = (HashCode * 397) ^ (TagVersion != null ? TagVersion.GetHashCode() : 0);
                HashCode = (HashCode * 397) ^ (RulesetFilename != null ? RulesetFilename.GetHashCode() : 0);
                // ReSharper restore NonReadonlyMemberInGetHashCode
                return HashCode;
            }
        }

        public bool Equals(GitHubGamemode Other) =>
            GitHubUser == Other.GitHubUser &&
            GitHubRepo == Other.GitHubRepo &&
            TagVersion == Other.TagVersion &&
            RulesetFilename == Other.RulesetFilename;

        public static bool operator ==(GitHubGamemode Left, GitHubGamemode Right) => Left.Equals(Right);

        public static bool operator !=(GitHubGamemode Left, GitHubGamemode Right) => !(Left == Right);
        #endregion
    }
}
