#region Copyright (C) 2017-2020  Starflash Studios
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License (Version 3.0)
// as published by the Free Software Foundation.
// 
// More information can be found here: https://www.gnu.org/licenses/gpl-3.0.en.html
#endregion

#region Using Directives

using System;
using Octokit;

#endregion

namespace OsuModeManager.Windows {
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
