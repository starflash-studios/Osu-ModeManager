using System;
using Octokit;

namespace OsuModeManager {
    public partial class ReleaseWindow {
        public ReleaseWindow() {
            InitializeComponent();
        }

        public static ReleaseWindow ShowRelease(Release Release) {
            ReleaseWindow ReleaseWindow = new ReleaseWindow();
            ReleaseWindow.DisplayRelease(Release);
            ReleaseWindow.Show();
            return ReleaseWindow;
        }

        public void DisplayRelease(Release Release) {
            Title = Release.ToString();
            ReleaseName.Content = Release.Name;
            ReleaseTag.Content = Release.TagName;

            foreach (string Line in Release.Body.Split(new[] { Environment.NewLine }, StringSplitOptions.None)) {
                ReleaseChangelog.AppendText(Line);
            }
        }
    }
}
