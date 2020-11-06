#region Copyright (C) 2017-2020  Starflash Studios
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License (Version 3.0)
// as published by the Free Software Foundation.
// 
// More information can be found here: https://www.gnu.org/licenses/gpl-3.0.en.html
#endregion

#region Using Directives

using System;
using System.IO;
using System.Runtime.InteropServices;

#endregion

namespace OsuModeManager {
    //See: https://stackoverflow.com/a/21953690/11519246/ for more information on how this system works
    public static class FoldersLib {
        static readonly string[] _FolderGuiDs = {
            "{56784854-C6CB-462B-8169-88E350ACB882}", // Contacts
            "{B4BFCC3A-DB2C-424C-B029-7FE99A87C641}", // Desktop
            "{FDD39AD0-238F-46AF-ADB4-6C85480369C7}", // Documents
            "{374DE290-123F-4565-9164-39C4925E467B}", // Downloads
            "{1777F761-68AD-4D8A-87BD-30B759FA33DD}", // Favourites
            "{BFB9D5E0-C6A9-404C-B2B2-AE6DB6AF4968}", // Links
            "{4BD8D571-6D19-48D3-BE97-422220080E43}", // Music
            "{33E28130-4E1E-4676-835A-98395C3BC3BB}", // Pictures
            "{4C5C32FF-BB9D-43B0-B5B4-2D72E54EAAA4}", // SavedGames
            "{7D1D3A04-DEBB-4115-95CF-2F29DA2920DA}", // SavedSearches
            "{18989B1D-99B5-455B-841C-AB7C74E4DDFC}" // Videos
        };

        public static string GetPath(Folders KnownFolder) => GetPath(KnownFolder, false);

        public static string GetPath(Folders KnownFolder, bool DefaultUser) => GetPath(KnownFolder, FolderFlags.DontVerify, DefaultUser);

        static string GetPath(Folders KnownFolder, FolderFlags Flags,
            bool DefaultUser) {
            int Result = SHGetFoldersPath(new Guid(_FolderGuiDs[(int)KnownFolder]), (uint)Flags, new IntPtr(DefaultUser ? - 1 : 0), out IntPtr OutPath);
            if (Result >= 0) {
                string Path = Marshal.PtrToStringUni(OutPath);
                Marshal.FreeCoTaskMem(OutPath);
                return Path;
            }

            throw new ExternalException("Unable to retrieve the known folder path. It may not be available on this system.", Result);
        }

        [DllImport("Shell32.dll")]
        static extern int SHGetFoldersPath([MarshalAs(UnmanagedType.LPStruct)] Guid Rfid, uint DWFlags, IntPtr HToken, out IntPtr PpszPath);

        [Flags]
        enum FolderFlags : uint {
            SimpleIDList = 0x00000100,
            NotParentRelative = 0x00000200,
            DefaultPath = 0x00000400,
            Init = 0x00000800,
            NoAlias = 0x00001000,
            DontUnexpand = 0x00002000,
            DontVerify = 0x00004000,
            Create = 0x00008000,
            NoAppcontainerRedirection = 0x00010000,
            AliasOnly = 0x80000000
        }

        #region QuickUse

        public static readonly DirectoryInfo Downloads = new DirectoryInfo(GetPath(Folders.Downloads));
        public static readonly DirectoryInfo Desktop = new DirectoryInfo(GetPath(Folders.Desktop));

        #endregion

    }

    public enum Folders {
        Contacts,
        Desktop,
        Documents,
        Downloads,
        Favorites,
        Links,
        Music,
        Pictures,
        SavedGames,
        SavedSearches,
        Videos
    }
}