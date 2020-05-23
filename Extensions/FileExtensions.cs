using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using Ookii.Dialogs.Wpf;

using Shell32;
using Syroot.Windows.IO;

namespace OsuModeManager {
    public static class FileExtensions {

        #region Assembly Reflection        
        /// <summary> Returns the location of the ExecutingAssembly parsed as a <see cref="FileInfo"/>. See also: <seealso cref="Assembly.GetExecutingAssembly"/>. </summary>
        public static FileInfo GetExecutable() => new FileInfo(Assembly.GetExecutingAssembly().Location);

        /// <summary> Returns the <see cref="DirectoryInfo"/> containing the <see cref="FileInfo"/> returned from <see cref="GetExecutable"/>. </summary>
        public static DirectoryInfo GetExecutableLocation() => GetExecutable().Directory;
        #endregion

        #region FileInfo Extensions

        /// <summary> Gets the file version from the specified executable. </summary>
        /// <param name="FileInfo">The file.</param>
        /// <returns><see cref="string"/></returns>
        public static string GetFileVersion(this FileInfo FileInfo) => FileInfo.GetFileVersionInfo().FileVersion;

        /// <summary> Gets the product version from the specified executable. </summary>
        /// <param name="FileInfo">The file.</param>
        /// <returns><see cref="string"/></returns>
        public static string GetProductVersion(this FileInfo FileInfo) => FileInfo.GetFileVersionInfo().ProductVersion;

        /// <summary>Gets the <see cref="FileVersionInfo"/> from the specified <see cref="FileInfo"/>. </summary>
        /// <param name="FileInfo">The file.</param>
        /// <returns><see cref="FileVersionInfo"/></returns>
        public static FileVersionInfo GetFileVersionInfo(this FileInfo FileInfo) => FileVersionInfo.GetVersionInfo(FileInfo.FullName);

        /// <summary>
        /// Returns the FileInfo's name trimmed to its length minus the extension's length
        /// </summary>
        /// <param name="FileInfo">The file information.</param>
        /// <returns><see cref="FileInfo"/></returns>
        public static string NameWithoutExtension(this FileInfo FileInfo) => FileInfo.Name.Substring(0, FileInfo.Name.Length - FileInfo.Extension.Length);

        /// <summary>
        /// Returns a <see cref="FileInfo"/> relative to the specified <see cref="DirectoryInfo"/> utilising <see cref="TryParseFileInfo(string, out FileInfo)"/>
        /// </summary>
        /// <param name="DirectoryInfo">The directory information.</param>
        /// <param name="RelativeFileName">Name of the relative file.</param>
        /// <param name="File">The file.</param>
        /// <returns><see cref="FileInfo"/></returns>
        public static bool TryGetRelativeFile(this DirectoryInfo DirectoryInfo, string RelativeFileName, out FileInfo File) => TryParseFileInfo($"{DirectoryInfo.FullName}\\{RelativeFileName}", out File);

        #endregion

        #region Path Constructors

        #region Files

        /// <summary>
        /// Tries to parse the string as a <see cref="FileInfo"/>.
        /// </summary>
        /// <param name="FileName">Name of the file.</param>
        /// <param name="File">The file.</param>
        /// <returns><see cref="bool"/></returns>
        public static bool TryParseFileInfo(this string FileName, out FileInfo File) {
#pragma warning disable CA1031 // Do not catch general exception types
            try {
                File = new FileInfo(FileName);
                return true;
            } catch (ArgumentNullException) {
                //FileName is null.
                Debug.WriteLine("FileName is null.", "ArgumentNullException");

            } catch (SecurityException) {
                //The caller does not have the required permission.
                Debug.WriteLine("The caller does not have the required permission.", "SecurityException");

            } catch (ArgumentException) {
                //The file name is empty, contains only white spaces, or contains invalid characters.
                Debug.WriteLine("The file name is empty, contains only white spaces, or contains invalid characters.", "ArgumentException");

            } catch (UnauthorizedAccessException) {
                //Access to FileName is denied.
                Debug.WriteLine("Access to FileName is denied.", "UnauthorizedAccessException");

            } catch (PathTooLongException) {
                //The specified path, file name, or both exceed the system-defined maximum length.
                Debug.WriteLine("The specified path, file name, or both exceed the system-defined maximum length.", "PathTooLongException");
            } catch (NotSupportedException) {
                //FileName contains a colon (:) in the middle of the string.
                Debug.WriteLine("FileName contains a colon (:) in the middle of the string.", "NotSupportedException");
            }

            File = null;
            return false;
#pragma warning restore CA1031 // Do not catch general exception types
        }

        #endregion

        #region Directories
        /// <summary>Creates a <see cref="DirectoryInfo"/> from a given <see cref="KnownFolderType"/> by constructing a new <see cref="KnownFolder"/>. </summary>
        /// <param name="KnownFolderType">The known folder type.</param>
        /// <returns><see cref="DirectoryInfo"/></returns>
        public static DirectoryInfo GetDirectoryInfo(this KnownFolderType KnownFolderType) => new KnownFolder(KnownFolderType).GetDirectoryInfo();

        /// <summary>Creates a <see cref="DirectoryInfo"/> from a given <see cref="KnownFolder"/>. </summary>
        /// <param name="KnownFolder">The known folder.</param>
        /// <returns><see cref="DirectoryInfo"/></returns>
        public static DirectoryInfo GetDirectoryInfo(this KnownFolder KnownFolder) => TryParseDirectoryInfo(KnownFolder.Path, out DirectoryInfo DirectoryInfo) ? DirectoryInfo : null;


        /// <summary>Creates a <see cref="DirectoryInfo"/> from a given <see cref="Environment.SpecialFolder"/> by utilising <see cref="Environment.GetFolderPath(Environment.SpecialFolder)"/>. </summary>
        /// <param name="SpecialFolder">The special folder.</param>
        /// <returns><see cref="FileInfo"/></returns>
        public static DirectoryInfo GetDirectoryInfo(this Environment.SpecialFolder SpecialFolder) => new DirectoryInfo(Environment.GetFolderPath(SpecialFolder));

        /// <summary>Creates a <see cref="DirectoryInfo"/> from a given <see cref="Environment.SpecialFolder"/> by utilising <see cref="Environment.GetFolderPath(Environment.SpecialFolder)"/>. </summary>
        /// <param name="SpecialFolder">The special folder.</param>
        /// <param name="Option">The option.</param>
        /// <returns><see cref="DirectoryInfo"/></returns>
        public static DirectoryInfo GetDirectoryInfo(this Environment.SpecialFolder SpecialFolder, Environment.SpecialFolderOption Option) => new DirectoryInfo(Environment.GetFolderPath(SpecialFolder, Option));

        /// <summary> Tries to parse the string as a <see cref="DirectoryInfo"/>. </summary>
        /// <param name="DirectoryName">Name of the directory.</param>
        /// <param name="Directory">The directory.</param>
        /// <returns><see cref="bool"/></returns>
        public static bool TryParseDirectoryInfo(this string DirectoryName, out DirectoryInfo Directory) {
#pragma warning disable CA1031 // Do not catch general exception types
            try {
                Directory = new DirectoryInfo(DirectoryName);
                return true;
            } catch (ArgumentNullException) { //DirectoryName is null.
                Debug.WriteLine("DirectoryName is null.", "ArgumentNullException");

            } catch (SecurityException) { //The caller does not have the required permission.
                Debug.WriteLine("The caller does not have the required permission.", "SecurityException");

            } catch (ArgumentException) { //The Directory name is empty, contains only white spaces, or contains invalid characters.
                Debug.WriteLine("The Directory name is empty, contains only white spaces, or contains invalid characters.", "ArgumentException");

            } catch (UnauthorizedAccessException) { //Access to DirectoryName is denied.
                Debug.WriteLine("Access to DirectoryName is denied.", "UnauthorizedAccessException");

            } catch (PathTooLongException) { //The specified path, Directory name, or both exceed the system-defined maximum length.
                Debug.WriteLine("The specified path, Directory name, or both exceed the system-defined maximum length.", "PathTooLongException");
            } catch (NotSupportedException) { //DirectoryName contains a colon (:) in the middle of the string.
                Debug.WriteLine("DirectoryName contains a colon (:) in the middle of the string.", "NotSupportedException");
            }

            Directory = null;
            return false;
#pragma warning restore CA1031 // Do not catch general exception types
        }

        #endregion

        #endregion

        #region User Path Dialogs

        #region DirectoryInfo
        /// <summary>
        /// Opens a <see cref="VistaFolderBrowserDialog"/> to allow the user to select a directory, and returns true if a directory was successfully selected.
        /// </summary>
        /// <param name="Result">The result.</param>
        /// <param name="StartFolder">The start folder.</param>
        /// <param name="Description">The description.</param>
        /// <param name="UseDescriptionForTitles">if set to <c>true</c> [use description for titles].</param>
        /// <param name="ShowNewFolderButton">if set to <c>true</c> [show new folder button].</param>
        /// <param name="SelectedPath">The selected path.</param>

        public static bool TryGetUserDirectory(out DirectoryInfo Result, Environment.SpecialFolder StartFolder = Environment.SpecialFolder.LocalApplicationData, string Description = "", bool UseDescriptionForTitles = true, bool ShowNewFolderButton = true, string SelectedPath = default) {
            Result = GetUserDirectory(StartFolder, Description, UseDescriptionForTitles, ShowNewFolderButton, SelectedPath);
            return Result != null;
        }

        /// <summary>
        /// Opens a <see cref="VistaFolderBrowserDialog"/> to allow the user to select a directory.
        /// </summary>
        /// <param name="StartFolder">The start folder.</param>
        /// <param name="Description">The description.</param>
        /// <param name="UseDescriptionForTitles">if set to <c>true</c> [use description for titles].</param>
        /// <param name="ShowNewFolderButton">if set to <c>true</c> [show new folder button].</param>
        /// <param name="SelectedPath">The selected path.</param>

        public static DirectoryInfo GetUserDirectory(Environment.SpecialFolder StartFolder = Environment.SpecialFolder.LocalApplicationData, string Description = "", bool UseDescriptionForTitles = true, bool ShowNewFolderButton = true, string SelectedPath = default) {
            VistaFolderBrowserDialog FolderBrowser = new VistaFolderBrowserDialog {
                ShowNewFolderButton = ShowNewFolderButton,
                RootFolder = StartFolder,
                UseDescriptionForTitle = UseDescriptionForTitles,
                Description = Description
            };

            if (!SelectedPath.IsNullOrEmpty()) {
                FolderBrowser.SelectedPath = SelectedPath;
            }

            if (FolderBrowser.ShowDialog() == true && TryParseDirectoryInfo(FolderBrowser.SelectedPath, out DirectoryInfo Result)) {
                return Result;
            }

            return null;
        }

        #endregion

        #region FileInfo

        /// <summary>
        /// Opens a <see cref="VistaOpenFileDialog"/> to allow the user to select a file, and returns true if a file was successfully selected.
        /// </summary>
        /// <param name="Result">The result.</param>
        /// <param name="Title">The title.</param>
        /// <param name="Filter">The filter.</param>
        /// <param name="InitialDirectory">The initial directory.</param>
        /// <param name="StartFile">The start file.</param>

        public static bool TryGetUserFile(out FileInfo Result, string Title = "Pick a file", string Filter = "Any File (*.*)|*.*", DirectoryInfo InitialDirectory = null, FileInfo StartFile = null) {
            Result = GetUserFile(Title, Filter, InitialDirectory, StartFile);
            return Result != null;
        }

        /// <summary>
        /// Opens a <see cref="VistaOpenFileDialog"/> to allow the user to select a file.
        /// </summary>
        /// <param name="Title">The title.</param>
        /// <param name="Filter">The filter.</param>
        /// <param name="InitialDirectory">The initial directory.</param>
        /// <param name="StartFile">The start file.</param>

        public static FileInfo GetUserFile(string Title = "Pick a file", string Filter = "Any File (*.*)|*.*", DirectoryInfo InitialDirectory = null, FileInfo StartFile = null) {
            VistaOpenFileDialog FileBrowser = new VistaOpenFileDialog {
                ReadOnlyChecked = true,
                ShowReadOnly = true,
                CheckFileExists = true,
                CheckPathExists = true,
                Filter = Filter,
                FilterIndex = 0,
                InitialDirectory = InitialDirectory?.FullName ?? Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                RestoreDirectory = true,
                Title = Title
            };

            if (StartFile != null) {
                FileBrowser.FileName = StartFile.FullName;
            }

            if (FileBrowser.ShowDialog() == true && TryParseFileInfo(FileBrowser.FileName, out FileInfo Result)) {
                return Result;
            }

            return null;
        }
        
        #endregion

        #endregion

        #region Existance Checking

        /// <summary> Checks that the DirectoryInfo is not equal to null, and that it exists. </summary>
        /// <param name="DirectoryInfo">The directory information.</param>

        public static bool Exists(this DirectoryInfo DirectoryInfo) => DirectoryInfo != null && DirectoryInfo.Exists;

        /// <summary> Checks that the FileInfo is not equal to null, and that it exists. </summary>
        /// <param name="FileInfo">The file.</param>

        public static bool Exists(this FileInfo FileInfo) => FileInfo != null && FileInfo.Exists;

        #endregion

        #region FileInfo Writing
        /// <summary> Writes all text. </summary>
        /// <param name="FileInfo">The file.</param>
        /// <param name="Contents">The contents.</param>
        public static void WriteAllText(this FileInfo FileInfo, string Contents) => File.WriteAllText(FileInfo.FullName, Contents);

        /// <summary> Writes all lines. </summary>
        /// <param name="FileInfo">The file.</param>
        /// <param name="Contents">The contents.</param>
        public static void WriteAllLines(this FileInfo FileInfo, IEnumerable<string> Contents) => File.WriteAllLines(FileInfo.FullName, Contents);

        /// <summary> Writes all bytes. </summary>
        /// <param name="FileInfo">The file.</param>
        /// <param name="Bytes">The bytes.</param>
        public static void WriteAllBytes(this FileInfo FileInfo, byte[] Bytes) => File.WriteAllBytes(FileInfo.FullName, Bytes);
        #endregion

        #region FileInfo Reading
        public static string[] ReadAllLines(this FileInfo FileInfo) => File.ReadAllLines(FileInfo.FullName);
        
        public static string[] ReadAllLines(this FileInfo FileInfo, System.Text.Encoding Encoding) => File.ReadAllLines(FileInfo.FullName, Encoding);

        public static string ReadAllText(this FileInfo FileInfo) => File.ReadAllText(FileInfo.FullName);

        public static string ReadAllText(this FileInfo FileInfo, System.Text.Encoding Encoding) => File.ReadAllText(FileInfo.FullName, Encoding);

        public static byte[] ReadAllBytes(this FileInfo FileInfo) => File.ReadAllBytes(FileInfo.FullName);
        #endregion
        
        #region Recycling

        /// <summary> Wraps Shell32.dll functions. See also: <seealso cref="Shell32.Shell"/>. </summary>
        public static Shell Shell = new Shell();

        /// <summary> Represents the 'Recycling Bin' NameSpace from <see cref="Shell"/>. </summary>
        public static Folder RecyclingBin = Shell.NameSpace(ShellSpecialFolderConstants.ssfBITBUCKET); //Recycling Bin
        
        /// <summary>
        /// Sends the specified file to the recycling bin (<see cref="RecyclingBin"/>).
        /// </summary>
        /// <param name="FileInfo">The file.</param>
        public static void Recycle(this FileInfo FileInfo) => RecyclingBin.MoveHere(FileInfo.FullName);

        #region Junk Bytes RNG
        /// <summary> The <see cref="RNGCryptoServiceProvider"/> utilised by <see cref="DumpJunkBytes(FileInfo)"/>.</summary>
        public static RNGCryptoServiceProvider RNGCryptoProvider = new RNGCryptoServiceProvider();

        /// <summary>
        /// Dumps junk bytes into the specified FileInfo, preserving the file's length.
        /// </summary>
        /// <param name="FileInfo">The file.</param>
        public static void DumpJunkBytes(this FileInfo FileInfo) {
            byte[] JunkBytes = new byte[FileInfo.Length];
            RNGCryptoProvider.GetNonZeroBytes(JunkBytes);
            FileInfo.WriteAllBytes(JunkBytes);
        }

        /// <summary> Shreds the specified file, recycling if specified. </summary>
        /// <param name="FileInfo">The file.</param>
        /// <param name="Recycle">If set to <c>true</c> <see cref="Recycle(FileInfo)"/>.</param>
        public static void Shred(this FileInfo FileInfo, bool Recycle = true) {
            FileInfo.DumpJunkBytes();
            if (Recycle) {
                FileInfo.Recycle();
            } else {
                FileInfo.Delete();
            }
        }
        #endregion

        #endregion

        #region Explorer        
        /// <summary>The Windows Explorer executable given from the <see cref="Environment.SpecialFolder.Windows"/> path. </summary>
        public static FileInfo Explorer = Environment.SpecialFolder.Windows.GetDirectoryInfo().TryGetRelativeFile("explorer.exe", out FileInfo File) ? File : null;

        #region Select (FileInfo)

        /// <summary> Returns a new process that will select the specified <see cref="FileInfo"/> in the Windows Explorer instance provided by <see cref="Explorer"/>. </summary>
        /// <param name="FileInfo">The file.</param>
        /// <returns><see cref="Process"/></returns>
        public static Process GetSelectInExplorerProcess(this FileInfo FileInfo) => new Process {
            StartInfo = new ProcessStartInfo {
                FileName = Explorer.FullName,
                Arguments = $"/select,\"{FileInfo.FullName}\""
            }
        };

        /// <summary> Selects the given <see cref="FileInfo"/> in the Windows Explorer instance provided by <see cref="Explorer"/> utilising <see cref="GetSelectInExplorerProcess(FileInfo)"/>. </summary>
        /// <param name="FileInfo">The file.</param>
        public static void SelectInExplorer(this FileInfo FileInfo) => FileInfo.GetSelectInExplorerProcess().Start();

        #endregion

        #region Select (DirectoryInfo)

        /// <summary> Returns a new process that will select the specified <see cref="DirectoryInfo"/> in the Windows Explorer instance provided by <see cref="Explorer"/>. </summary>
        /// <param name="DirectoryInfo">The directory.</param>
        /// <returns><see cref="Process"/></returns>
        public static Process GetSelectInExplorerProcess(this DirectoryInfo DirectoryInfo) => new Process {
            StartInfo = new ProcessStartInfo {
                FileName = Explorer.FullName,
                Arguments = $"/select,\"{DirectoryInfo.FullName}\""
            }
        };

        /// <summary> Selects the given <see cref="DirectoryInfo"/> in the Windows Explorer instance provided by <see cref="Explorer"/> utilising <see cref="GetSelectInExplorerProcess(DirectoryInfo)"/>. </summary>
        /// <param name="DirectoryInfo">The directory.</param>
        public static void SelectInExplorer(this DirectoryInfo DirectoryInfo) => DirectoryInfo.GetSelectInExplorerProcess().Start();

        #endregion

        #region Open (DirectoryInfo)
        /// <summary> Returns a new process that will open the specified <see cref="DirectoryInfo"/> in the Windows Explorer instance provided by <see cref="Explorer"/>. </summary>
        /// <param name="DirectoryInfo">The directory.</param>
        /// <returns><see cref="Process"/></returns>
        public static Process GetOpenInExplorerProcess(this DirectoryInfo DirectoryInfo) => new Process {
            StartInfo = new ProcessStartInfo {
                FileName = Explorer.FullName,
                Arguments = $"\"{DirectoryInfo.FullName}\""
            }
        };

        /// <summary> Opens the given <see cref="DirectoryInfo"/> in the Windows Explorer instance provided by <see cref="Explorer"/> utilising <see cref="GetOpenInExplorerProcess(DirectoryInfo)"/>. </summary>
        /// <param name="DirectoryInfo">The directory.</param>
        public static void OpenInExplorer(this DirectoryInfo DirectoryInfo) => DirectoryInfo.GetOpenInExplorerProcess().Start();
        #endregion

        #endregion
    }
}
