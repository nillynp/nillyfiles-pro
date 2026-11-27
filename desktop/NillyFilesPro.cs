// ============================================================================
// NillyFiles Pro — Ultra-Light Native Windows 10/11 Tabbed & Dual-Pane
//                  File Explorer (Zero Bloat · Zero Admin · 0 MB Idle RAM)
// created by Nilly graphics (Nillygraphics · Sotiris Christos) — https://nillygraphics.com/
// Strictly C# 5 compatible (.NET Framework 4.x csc.exe — 0 external DLLs)
// Features:
//  - <45ms instant startup · 0 MB RAM / 0% CPU when closed · 0 external DLLs
//  - Native Win32 FindFirstFileExW (LARGE_FETCH) + Virtual-Mode ListView (10k+ files in ms)
//  - Multi-Tab Navigation (Ctrl+T / Ctrl+W) + 1-Click Dual-Pane Split View (F3 / F5 / F6)
//  - Real Windows Shell Clipboard (CF_HDROP Copy/Cut/Paste across Explorer & Adobe apps)
//  - Safe Recycle Bin Deletion (SHFileOperation FOF_ALLOWUNDO) + Native Progress Copy/Move
//  - Greek accent-free instant folder filter (Ctrl+F), Drives usage bars, Favorites (★)
//  - Optional On-Demand Preview Panel (Alt+P) · Built-in Ultra-Light Specs Dialog (i)
//  - 100% Safe: NEVER uninstalls, patches, or alters Windows explorer.exe
// ============================================================================

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

[assembly: AssemblyTitle("NillyFiles Pro")]
[assembly: AssemblyDescription("Ultra-Light Native Windows 10/11 Tabbed & Dual-Pane File Explorer — created by Nilly graphics (Nillygraphics · Sotiris Christos)")]
[assembly: AssemblyCompany("Nilly graphics (Nillygraphics · Sotiris Christos) — https://nillygraphics.com/")]
[assembly: AssemblyProduct("NillyFiles Pro")]
[assembly: AssemblyCopyright("Copyright (c) 2026 Nilly graphics (Sotiris Christos) — https://nillygraphics.com/")]
[assembly: AssemblyTrademark("Nilly graphics / Nillygraphics — Sotiris Christos")]
[assembly: AssemblyVersion("1.2.4.0")]
[assembly: AssemblyFileVersion("1.2.4.0")]

namespace NillyFilesPro
{
    // ========================================================================
    // 1. WIN32 KERNEL / SHELL INTEROP (Zero External Dependencies)
    // ========================================================================
    internal static class Native
    {
        public static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);
        public const int FindExInfoBasic = 1;
        public const int FindExSearchNameMatch = 0;
        public const int FIND_FIRST_EX_LARGE_FETCH = 2;

        public const uint FILE_ATTRIBUTE_HIDDEN = 0x02;
        public const uint FILE_ATTRIBUTE_SYSTEM = 0x04;
        public const uint FILE_ATTRIBUTE_DIRECTORY = 0x10;
        public const uint FILE_ATTRIBUTE_REPARSE_POINT = 0x400;

        public const uint FO_MOVE = 0x0001;
        public const uint FO_COPY = 0x0002;
        public const uint FO_DELETE = 0x0003;
        public const ushort FOF_ALLOWUNDO = 0x0040;
        public const ushort FOF_NOCONFIRMATION = 0x0010;
        public const ushort FOF_NOCONFIRMMKDIR = 0x0200;

        public const uint SEE_MASK_INVOKEIDLIST = 0x0000000C;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct WIN32_FIND_DATA
        {
            public uint dwFileAttributes;
            public uint ftCreationTimeLow, ftCreationTimeHigh;
            public uint ftLastAccessTimeLow, ftLastAccessTimeHigh;
            public uint ftLastWriteTimeLow, ftLastWriteTimeHigh;
            public uint nFileSizeHigh;
            public uint nFileSizeLow;
            public uint dwReserved0;
            public uint dwReserved1;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string cFileName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
            public string cAlternateFileName;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct SHFILEOPSTRUCT
        {
            public IntPtr hwnd;
            public uint wFunc;
            [MarshalAs(UnmanagedType.LPWStr)] public string pFrom;
            [MarshalAs(UnmanagedType.LPWStr)] public string pTo;
            public ushort fFlags;
            [MarshalAs(UnmanagedType.Bool)] public bool fAnyOperationsAborted;
            public IntPtr hNameMappings;
            [MarshalAs(UnmanagedType.LPWStr)] public string lpszProgressTitle;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct SHELLEXECUTEINFO
        {
            public int cbSize;
            public uint fMask;
            public IntPtr hwnd;
            [MarshalAs(UnmanagedType.LPWStr)] public string lpVerb;
            [MarshalAs(UnmanagedType.LPWStr)] public string lpFile;
            [MarshalAs(UnmanagedType.LPWStr)] public string lpParameters;
            [MarshalAs(UnmanagedType.LPWStr)] public string lpDirectory;
            public int nShow;
            public IntPtr hInstApp;
            public IntPtr lpIDList;
            [MarshalAs(UnmanagedType.LPWStr)] public string lpClass;
            public IntPtr hkeyClass;
            public uint dwHotKey;
            public IntPtr hIcon;
            public IntPtr hProcess;
        }

        [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
        [DllImport("kernel32.dll")] private static extern IntPtr GetCurrentProcess();
        [DllImport("kernel32.dll")] private static extern bool SetProcessWorkingSetSize(IntPtr hProcess, IntPtr minSize, IntPtr maxSize);

        public static void TrimMemory()
        {
            try
            {
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Optimized, false);
                SetProcessWorkingSetSize(GetCurrentProcess(), (IntPtr)(-1), (IntPtr)(-1));
            }
            catch { }
        }
        [DllImport("dwmapi.dll")] private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int val, int sz);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr FindFirstFileExW(
            string lpFileName, int fInfoLevelId, out WIN32_FIND_DATA lpFindFileData,
            int fSearchOp, IntPtr lpSearchFilter, int dwAdditionalFlags);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool FindNextFileW(IntPtr hFindFile, out WIN32_FIND_DATA lpFindFileData);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool FindClose(IntPtr hFindFile);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        public static extern int SHFileOperation(ref SHFILEOPSTRUCT lpFileOp);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ShellExecuteEx(ref SHELLEXECUTEINFO lpExecInfo);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr CreateIconFromResourceEx(byte[] presbits, uint dwResSize, bool fIcon, uint dwVer, int cxDesired, int cyDesired, uint Flags);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, string lParam);

        public static void SetCueBanner(TextBox tb, string text)
        {
            if (tb == null || !tb.IsHandleCreated) return;
            try { SendMessage(tb.Handle, 0x1501, (IntPtr)1, text); } catch { }
        }

        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        public static extern int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string pszSubIdList);

        public static void SetDarkScrollbars(Control c, bool dark)
        {
            if (c == null || !c.IsHandleCreated) return;
            try { SetWindowTheme(c.Handle, dark ? "DarkMode_Explorer" : "Explorer", null); } catch { }
        }

        public static void SetDarkTitlebar(IntPtr hwnd, bool dark)
        {
            if (hwnd == IntPtr.Zero) return;
            try
            {
                int v = dark ? 1 : 0;
                if (DwmSetWindowAttribute(hwnd, 20, ref v, 4) != 0)
                    DwmSetWindowAttribute(hwnd, 19, ref v, 4);
            }
            catch { }
        }

        public static DateTime FileTimeToDateTime(uint low, uint high)
        {
            try
            {
                long ft = ((long)high << 32) | low;
                if (ft <= 0) return DateTime.MinValue;
                return DateTime.FromFileTime(ft);
            }
            catch { return DateTime.MinValue; }
        }

        public static bool ShellOperate(IntPtr hwnd, uint func, IList<string> sources, string destDir, bool toRecycleBin, bool silent)
        {
            if (sources == null || sources.Count == 0) return false;
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < sources.Count; i++)
            {
                if (!string.IsNullOrEmpty(sources[i]))
                {
                    sb.Append(sources[i]);
                    sb.Append('\0');
                }
            }
            sb.Append('\0');

            SHFILEOPSTRUCT op = new SHFILEOPSTRUCT();
            op.hwnd = hwnd;
            op.wFunc = func;
            op.pFrom = sb.ToString();
            op.pTo = string.IsNullOrEmpty(destDir) ? null : (destDir + "\0\0");
            ushort flags = FOF_NOCONFIRMMKDIR;
            if (toRecycleBin && func == FO_DELETE) flags |= FOF_ALLOWUNDO;
            if (silent) flags |= FOF_NOCONFIRMATION;
            op.fFlags = flags;
            int res = SHFileOperation(ref op);
            return res == 0 && !op.fAnyOperationsAborted;
        }

        public static void ShowProperties(IntPtr hwnd, string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            try
            {
                SHELLEXECUTEINFO info = new SHELLEXECUTEINFO();
                info.cbSize = Marshal.SizeOf(typeof(SHELLEXECUTEINFO));
                info.fMask = SEE_MASK_INVOKEIDLIST;
                info.hwnd = hwnd;
                info.lpVerb = "properties";
                info.lpFile = path;
                info.nShow = 5;
                ShellExecuteEx(ref info);
            }
            catch { }
        }

        public static Bitmap LoadCrispIconBitmap(string icoPath, int sz)
        {
            if (string.IsNullOrEmpty(icoPath) || !File.Exists(icoPath)) return null;
            try
            {
                byte[] data = File.ReadAllBytes(icoPath);
                if (data.Length < 22) return null;
                int count = BitConverter.ToUInt16(data, 4);
                int bestIdx = 0; int bestW = 0;
                for (int i = 0; i < count; i++)
                {
                    int off = 6 + i * 16;
                    if (off + 16 > data.Length) break;
                    int w = data[off]; if (w == 0) w = 256;
                    if (w > bestW) { bestW = w; bestIdx = i; }
                }
                int eOff = 6 + bestIdx * 16;
                int imgLen = BitConverter.ToInt32(data, eOff + 8);
                int imgOff = BitConverter.ToInt32(data, eOff + 12);
                Bitmap dst = new Bitmap(sz, sz, PixelFormat.Format32bppArgb);
                using (Graphics g = Graphics.FromImage(dst))
                {
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.Clear(Color.Transparent);
                    if (imgOff + 4 < data.Length && data[imgOff] == 0x89 && data[imgOff + 1] == 0x50)
                    {
                        using (MemoryStream ms = new MemoryStream(data, imgOff, imgLen))
                        using (Image src = Image.FromStream(ms))
                            g.DrawImage(src, 0, 0, sz, sz);
                    }
                    else
                    {
                        using (Icon ic = new Icon(icoPath, sz, sz))
                            g.DrawIcon(ic, new Rectangle(0, 0, sz, sz));
                    }
                }
                return dst;
            }
            catch { return null; }
        }

        public static void ApplyCrispTaskbarIcon(Form form, string icoPath)
        {
            if (form == null || !form.IsHandleCreated || string.IsNullOrEmpty(icoPath) || !File.Exists(icoPath)) return;
            try
            {
                byte[] data = File.ReadAllBytes(icoPath);
                IntPtr hBig = CreateHIcon(data, 48);
                IntPtr hSmall = CreateHIcon(data, 16);
                if (hBig != IntPtr.Zero) SendMessage(form.Handle, 0x0080, (IntPtr)1, hBig);
                if (hSmall != IntPtr.Zero) SendMessage(form.Handle, 0x0080, (IntPtr)0, hSmall);
            }
            catch { }
        }

        private static IntPtr CreateHIcon(byte[] icoData, int desired)
        {
            if (icoData == null || icoData.Length < 22) return IntPtr.Zero;
            int count = BitConverter.ToUInt16(icoData, 4);
            int bestIdx = 0; int bestDiff = int.MaxValue;
            for (int i = 0; i < count; i++)
            {
                int off = 6 + i * 16;
                if (off + 16 > icoData.Length) break;
                int w = icoData[off]; if (w == 0) w = 256;
                int diff = Math.Abs(w - desired);
                if (w >= desired && diff < bestDiff) { bestDiff = diff; bestIdx = i; }
                else if (bestDiff == int.MaxValue && diff < bestDiff) { bestDiff = diff; bestIdx = i; }
            }
            int eOff = 6 + bestIdx * 16;
            int len = BitConverter.ToInt32(icoData, eOff + 8);
            int imgOff = BitConverter.ToInt32(icoData, eOff + 12);
            if (len <= 0 || imgOff < 0 || imgOff + len > icoData.Length) return IntPtr.Zero;
            byte[] slice = new byte[len];
            Buffer.BlockCopy(icoData, imgOff, slice, 0, len);
            return CreateIconFromResourceEx(slice, (uint)len, true, 0x00030000, desired, desired, 0);
        }
    }

    // ========================================================================
    // 2. THEME & FORMAT HELPERS (Monokai Dark + Crisp Light)
    // ========================================================================
    internal sealed class Theme
    {
        public bool Dark;
        public Color BgMain, BgPanel, BgSurface, BgHover, BgSel, BgAltRow;
        public Color Border, FgMain, FgMuted, Accent, Cyan, Green, Pink, Purple;

        public static Theme Create(bool dark)
        {
            Theme t = new Theme();
            t.Dark = dark;
            if (dark)
            {
                t.BgMain = Color.FromArgb(29, 31, 32);
                t.BgPanel = Color.FromArgb(34, 36, 38);
                t.BgSurface = Color.FromArgb(39, 41, 43);
                t.BgHover = Color.FromArgb(48, 51, 54);
                t.BgSel = Color.FromArgb(56, 46, 34);
                t.BgAltRow = Color.FromArgb(26, 28, 29);
                t.Border = Color.FromArgb(54, 57, 61);
                t.FgMain = Color.FromArgb(242, 244, 247);
                t.FgMuted = Color.FromArgb(154, 160, 168);
                t.Accent = Color.FromArgb(252, 150, 31);
                t.Cyan = Color.FromArgb(102, 217, 239);
                t.Green = Color.FromArgb(166, 226, 46);
                t.Pink = Color.FromArgb(240, 104, 118);
                t.Purple = Color.FromArgb(174, 129, 255);
            }
            else
            {
                t.BgMain = Color.FromArgb(248, 249, 251);
                t.BgPanel = Color.FromArgb(238, 241, 246);
                t.BgSurface = Color.FromArgb(255, 255, 255);
                t.BgHover = Color.FromArgb(226, 232, 242);
                t.BgSel = Color.FromArgb(209, 228, 250);
                t.BgAltRow = Color.FromArgb(243, 245, 249);
                t.Border = Color.FromArgb(210, 216, 226);
                t.FgMain = Color.FromArgb(28, 32, 40);
                t.FgMuted = Color.FromArgb(98, 108, 125);
                t.Accent = Color.FromArgb(224, 118, 12);
                t.Cyan = Color.FromArgb(14, 132, 168);
                t.Green = Color.FromArgb(62, 145, 32);
                t.Pink = Color.FromArgb(204, 28, 92);
                t.Purple = Color.FromArgb(120, 74, 210);
            }
            return t;
        }
    }

    internal static class Fmt
    {
        public static string Bytes(long b)
        {
            if (b < 0) return "";
            if (b < 1024) return b.ToString() + " B";
            double d = b / 1024.0;
            if (d < 1024) return d.ToString("0.0") + " KB";
            d /= 1024.0;
            if (d < 1024) return d.ToString("0.0") + " MB";
            d /= 1024.0;
            if (d < 1024) return d.ToString("0.00") + " GB";
            d /= 1024.0;
            return d.ToString("0.00") + " TB";
        }

        public static string StripAccents(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            string norm = s.Normalize(NormalizationForm.FormD);
            StringBuilder sb = new StringBuilder(norm.Length);
            for (int i = 0; i < norm.Length; i++)
            {
                UnicodeCategory uc = CharUnicodeInfo.GetUnicodeCategory(norm[i]);
                if (uc != UnicodeCategory.NonSpacingMark) sb.Append(char.ToLowerInvariant(norm[i]));
            }
            return sb.ToString();
        }

        public static string TypeLabel(string ext, bool isDir)
        {
            if (isDir) return "Φάκελος";
            if (string.IsNullOrEmpty(ext)) return "Αρχείο";
            switch (ext)
            {
                case ".pdf": return "PDF Έγγραφο";
                case ".jpg": case ".jpeg": case ".png": case ".webp": case ".bmp": case ".gif": case ".tif": case ".tiff": case ".svg": case ".ico": case ".heic": return "Εικόνα " + ext.Substring(1).ToUpperInvariant();
                case ".psd": case ".ai": case ".eps": case ".indd": case ".cdr": return "Γραφιστικό " + ext.Substring(1).ToUpperInvariant();
                case ".mp4": case ".mkv": case ".mov": case ".avi": case ".webm": case ".wmv": return "Βίντεο " + ext.Substring(1).ToUpperInvariant();
                case ".mp3": case ".wav": case ".flac": case ".ogg": case ".m4a": return "Ήχος " + ext.Substring(1).ToUpperInvariant();
                case ".zip": case ".rar": case ".7z": case ".tar": case ".gz": return "Αρχείο Συμπίεσης";
                case ".doc": case ".docx": case ".rtf": case ".odt": return "Έγγραφο Word";
                case ".xls": case ".xlsx": case ".csv": case ".ods": return "Φύλλο Excel";
                case ".ppt": case ".pptx": return "Παρουσίαση";
                case ".txt": case ".md": case ".log": case ".ini": return "Κείμενο";
                case ".cs": case ".js": case ".ts": case ".html": case ".css": case ".json": case ".xml": case ".py": case ".ps1": case ".php": case ".sql": return "Κώδικας " + ext.Substring(1).ToUpperInvariant();
                case ".exe": case ".msi": case ".bat": case ".cmd": case ".vbs": return "Εφαρμογή";
                case ".lnk": return "Συντόμευση";
                default: return ext.Substring(1).ToUpperInvariant() + " Αρχείο";
            }
        }

        public static int BadgeKind(string ext, bool isDir)
        {
            // 0=Dir, 1=Image/Design, 2=PDF, 3=Code/Text, 4=Archive, 5=Media, 6=App/Exe, 7=Doc/Sheet, 8=Generic
            if (isDir) return 0;
            if (string.IsNullOrEmpty(ext)) return 8;
            switch (ext)
            {
                case ".jpg": case ".jpeg": case ".png": case ".webp": case ".bmp": case ".gif": case ".tif": case ".tiff": case ".svg": case ".ico": case ".heic": case ".psd": case ".ai": case ".eps": return 1;
                case ".pdf": return 2;
                case ".cs": case ".js": case ".ts": case ".html": case ".css": case ".json": case ".xml": case ".py": case ".ps1": case ".php": case ".sql": case ".txt": case ".md": case ".log": case ".ini": return 3;
                case ".zip": case ".rar": case ".7z": case ".tar": case ".gz": return 4;
                case ".mp4": case ".mkv": case ".mov": case ".avi": case ".webm": case ".mp3": case ".wav": case ".flac": case ".m4a": return 5;
                case ".exe": case ".msi": case ".bat": case ".cmd": case ".vbs": case ".lnk": return 6;
                case ".doc": case ".docx": case ".xls": case ".xlsx": case ".csv": case ".ppt": case ".pptx": return 7;
                default: return 8;
            }
        }

        public static bool IsImageExt(string ext)
        {
            return ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".bmp" || ext == ".gif" || ext == ".ico" || ext == ".tif" || ext == ".tiff";
        }

        public static bool IsTextExt(string ext)
        {
            return ext == ".txt" || ext == ".md" || ext == ".json" || ext == ".xml" || ext == ".csv" || ext == ".ini" ||
                   ext == ".log" || ext == ".cs" || ext == ".ps1" || ext == ".bat" || ext == ".cmd" || ext == ".html" ||
                   ext == ".css" || ext == ".js" || ext == ".ts" || ext == ".py" || ext == ".php" || ext == ".sql" || ext == ".svg" || ext == ".yml" || ext == ".yaml";
        }

        public static string FriendlyError(Exception ex)
        {
            if (ex == null) return "Άγνωστο σφάλμα.";
            string raw = (ex.Message ?? "").Trim();
            string low = raw.ToLowerInvariant();
            if (ex is UnauthorizedAccessException || low.Contains("access is denied") || low.Contains("access to the path"))
                return "Δεν υπάρχει δικαίωμα πρόσβασης σε αυτόν τον φάκελο ή αρχείο.";
            if (low.Contains("being used by another process") || low.Contains("sharing violation"))
                return "Το αρχείο ή ο φάκελος χρησιμοποιείται από άλλο πρόγραμμα. Κλείσε το και δοκίμασε ξανά.";
            if (ex is PathTooLongException || low.Contains("path too long"))
                return "Η διαδρομή είναι υπερβολικά μεγάλη για τα Windows.";
            if (low.Contains("already exists"))
                return "Υπάρχει ήδη αρχείο ή φάκελος με αυτό το όνομα.";
            if (low.Contains("no application is associated"))
                return "Δεν υπάρχει συνδεδεμένη εφαρμογή στα Windows για αυτόν τον τύπο αρχείου.";
            return raw;
        }
    }

    // ========================================================================
    // 3. DATA MODELS & FAST WIN32 DIRECTORY ENGINE
    // ========================================================================
    internal sealed class FileEntry
    {
        public string Name;
        public string FullPath;
        public bool IsDir;
        public bool IsParentUp;
        public bool IsHidden;
        public long Size;
        public DateTime Modified;
        public string Ext;
        public string TypeLabel;
        public int BadgeKind;
        public string NormName;
        public string SizeText;
        public string DateText;
    }

    internal sealed class TabState
    {
        public string Path;
        public List<string> History = new List<string>();
        public int HistoryIndex = -1;
        public string Filter = "";
        public int SortCol = 0;
        public bool SortAsc = true;

        public string DisplayTitle()
        {
            if (string.IsNullOrEmpty(Path)) return "Δίσκος";
            string trimmed = Path.TrimEnd('\\');
            if (trimmed.Length <= 2) return trimmed + "\\";
            string name = System.IO.Path.GetFileName(trimmed);
            return string.IsNullOrEmpty(name) ? Path : name;
        }
    }

    internal static class FastDir
    {
        public static List<FileEntry> Enumerate(string dirPath, bool showHidden, out int dirCount, out int fileCount, out long totalBytes, out string error)
        {
            List<FileEntry> list = new List<FileEntry>(128);
            dirCount = 0; fileCount = 0; totalBytes = 0; error = null;
            if (string.IsNullOrEmpty(dirPath) || !Directory.Exists(dirPath))
            {
                error = "Ο φάκελος δεν βρέθηκε.";
                return list;
            }

            string cleanDir = dirPath.EndsWith("\\") ? dirPath : (dirPath + "\\");
            string pattern = cleanDir + "*";
            Native.WIN32_FIND_DATA fd;
            IntPtr h = Native.FindFirstFileExW(pattern, Native.FindExInfoBasic, out fd, Native.FindExSearchNameMatch, IntPtr.Zero, Native.FIND_FIRST_EX_LARGE_FETCH);
            if (h == Native.INVALID_HANDLE_VALUE)
            {
                int err = Marshal.GetLastWin32Error();
                if (err == 5) error = "Δεν επιτρέπεται η πρόσβαση σε αυτόν τον φάκελο.";
                return list;
            }

            try
            {
                do
                {
                    string name = fd.cFileName;
                    if (name == "." || name == "..") continue;
                    bool isHidden = (fd.dwFileAttributes & (Native.FILE_ATTRIBUTE_HIDDEN | Native.FILE_ATTRIBUTE_SYSTEM)) != 0;
                    if (!showHidden && isHidden) continue;

                    bool isDir = (fd.dwFileAttributes & Native.FILE_ATTRIBUTE_DIRECTORY) != 0;
                    long sz = isDir ? -1L : (((long)fd.nFileSizeHigh << 32) | fd.nFileSizeLow);
                    DateTime mod = Native.FileTimeToDateTime(fd.ftLastWriteTimeLow, fd.ftLastWriteTimeHigh);
                    string ext = isDir ? "" : (Path.GetExtension(name) ?? "").ToLowerInvariant();

                    FileEntry e = new FileEntry();
                    e.Name = name;
                    e.FullPath = cleanDir + name;
                    e.IsDir = isDir;
                    e.IsHidden = isHidden;
                    e.Size = sz;
                    e.Modified = mod;
                    e.Ext = ext;
                    e.TypeLabel = Fmt.TypeLabel(ext, isDir);
                    e.BadgeKind = Fmt.BadgeKind(ext, isDir);
                    e.NormName = Fmt.StripAccents(name);
                    e.SizeText = isDir ? "" : Fmt.Bytes(sz);
                    e.DateText = mod > DateTime.MinValue ? mod.ToString("dd/MM/yyyy HH:mm") : "";

                    list.Add(e);
                    if (isDir) dirCount++;
                    else { fileCount++; totalBytes += sz; }
                } while (Native.FindNextFileW(h, out fd));
            }
            finally
            {
                Native.FindClose(h);
            }
            return list;
        }
    }

    internal sealed class DoubleBufferedListView : ListView
    {
        public DoubleBufferedListView()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            UpdateStyles();
        }
    }

    // ========================================================================
    // 4. SETTINGS PERSISTENCE (%LOCALAPPDATA%\NillyFilesPro\settings.ini)
    // ========================================================================
    internal sealed class AppSettings
    {
        public bool DarkTheme = true;
        public bool ShowHidden = false;
        public bool DualPane = false;
        public bool ShowPreview = false;
        public List<string> Favorites = new List<string>();

        private static string GetPath()
        {
            string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NillyFilesPro");
            return Path.Combine(dir, "settings.ini");
        }

        public static AppSettings Load()
        {
            AppSettings s = new AppSettings();
            try
            {
                string p = GetPath();
                if (!File.Exists(p)) return s;
                string[] lines = File.ReadAllLines(p, Encoding.UTF8);
                for (int i = 0; i < lines.Length; i++)
                {
                    string ln = lines[i].Trim();
                    int eq = ln.IndexOf('=');
                    if (eq <= 0) continue;
                    string k = ln.Substring(0, eq).Trim();
                    string v = ln.Substring(eq + 1).Trim();
                    if (k == "DarkTheme") s.DarkTheme = (v != "0");
                    else if (k == "ShowHidden") s.ShowHidden = (v == "1");
                    else if (k == "DualPane") s.DualPane = (v == "1");
                    else if (k == "ShowPreview") s.ShowPreview = (v == "1");
                    else if (k == "Fav" && !string.IsNullOrEmpty(v) && Directory.Exists(v) && !s.Favorites.Contains(v))
                        s.Favorites.Add(v);
                }
            }
            catch { }
            return s;
        }

        public void Save()
        {
            try
            {
                string p = GetPath();
                Directory.CreateDirectory(Path.GetDirectoryName(p));
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("DarkTheme=" + (DarkTheme ? "1" : "0"));
                sb.AppendLine("ShowHidden=" + (ShowHidden ? "1" : "0"));
                sb.AppendLine("DualPane=" + (DualPane ? "1" : "0"));
                sb.AppendLine("ShowPreview=" + (ShowPreview ? "1" : "0"));
                for (int i = 0; i < Favorites.Count; i++)
                    sb.AppendLine("Fav=" + Favorites[i]);
                File.WriteAllText(p, sb.ToString(), Encoding.UTF8);
            }
            catch { }
        }
    }

    // ========================================================================
    // 5. SHELL CLIPBOARD HELPER (100% Windows Explorer Interop)
    // ========================================================================
    internal static class ShellClipboard
    {
        public static void SetFiles(IList<string> paths, bool isCut)
        {
            if (paths == null || paths.Count == 0) return;
            StringCollection sc = new StringCollection();
            for (int i = 0; i < paths.Count; i++) sc.Add(paths[i]);
            DataObject data = new DataObject();
            data.SetFileDropList(sc);
            byte[] dropEffect = new byte[] { (byte)(isCut ? 2 : 5), 0, 0, 0 }; // 2=Move, 5=Copy|Link
            MemoryStream ms = new MemoryStream(dropEffect);
            data.SetData("Preferred DropEffect", ms);
            Clipboard.SetDataObject(data, true);
        }

        public static bool TryGetFiles(out List<string> paths, out bool isCut)
        {
            paths = new List<string>();
            isCut = false;
            try
            {
                IDataObject obj = Clipboard.GetDataObject();
                if (obj == null || !obj.GetDataPresent(DataFormats.FileDrop)) return false;
                string[] arr = obj.GetData(DataFormats.FileDrop) as string[];
                if (arr == null || arr.Length == 0) return false;
                for (int i = 0; i < arr.Length; i++)
                    if (!string.IsNullOrEmpty(arr[i])) paths.Add(arr[i]);

                if (obj.GetDataPresent("Preferred DropEffect"))
                {
                    MemoryStream ms = obj.GetData("Preferred DropEffect") as MemoryStream;
                    if (ms != null && ms.Length >= 1)
                    {
                        byte[] b = ms.ToArray();
                        isCut = (b[0] == 2);
                    }
                }
                return paths.Count > 0;
            }
            catch { return false; }
        }
    }

    // ========================================================================
    // 6. TABBED FILE PANE (Virtual ListView + Breadcrumb + Live Filter)
    // ========================================================================
    internal sealed class FilePane : Panel
    {
        private readonly MainForm _owner;
        private readonly Panel _activeBar;
        private readonly Panel _tabStrip;
        private readonly Panel _navBar;
        private readonly AkButton _btnBack, _btnFwd, _btnUp, _btnRefresh, _btnStar;
        private readonly TextBox _txtPath;
        private readonly TextBox _txtFilter;
        private readonly DoubleBufferedListView _lv;
        private readonly Label _lblFooter;
        private readonly ContextMenuStrip _ctx;

        private readonly List<TabState> _tabs = new List<TabState>();
        private int _activeTabIdx = 0;
        private List<FileEntry> _allEntries = new List<FileEntry>();
        private List<FileEntry> _visible = new List<FileEntry>();
        private int _dirCount, _fileCount;
        private long _totalBytes;
        private long _lastScanMs;
        private bool _isActivePane;
        private Theme _theme;
        private bool _suppressFilterEvent;

        private readonly Font _uiFont = new Font("Segoe UI", 9.2f, FontStyle.Regular);
        private readonly Font _boldFont = new Font("Segoe UI", 9.2f, FontStyle.Bold);
        private readonly Font _smallFont = new Font("Segoe UI", 8.2f, FontStyle.Regular);

        public string CurrentPath { get { return CurrentTab != null ? CurrentTab.Path : ""; } }
        public TabState CurrentTab { get { return (_activeTabIdx >= 0 && _activeTabIdx < _tabs.Count) ? _tabs[_activeTabIdx] : null; } }
        public int VisibleCount { get { return _visible.Count; } }
        public long LastScanMs { get { return _lastScanMs; } }

        public FilePane(MainForm owner, string initialPath)
        {
            _owner = owner;
            _theme = owner.CurrentTheme;
            Dock = DockStyle.Fill;
            Padding = new Padding(0);

            _activeBar = new Panel { Dock = DockStyle.Top, Height = 2 };
            _tabStrip = new Panel { Dock = DockStyle.Top, Height = 30 };
            _navBar = new Panel { Dock = DockStyle.Top, Height = 36, Padding = new Padding(6, 5, 6, 5) };

            _btnBack = MakeNavBtn(AkIcon.ChevLeft, "Πίσω (Alt+Left)", delegate { GoBack(); });
            _btnFwd = MakeNavBtn(AkIcon.ChevRight, "Εμπρός (Alt+Right)", delegate { GoForward(); });
            _btnUp = MakeNavBtn(AkIcon.Up, "Επάνω φάκελος (Backspace)", delegate { GoUp(); });
            _btnRefresh = MakeNavBtn(AkIcon.Refresh, "Ανανέωση (F5)", delegate { Reload(); });
            _btnStar = MakeNavBtn(AkIcon.Star, "Προσθήκη/Αφαίρεση από Αγαπημένα", delegate { ToggleFavorite(); });

            _txtPath = new TextBox
            {
                BorderStyle = BorderStyle.FixedSingle,
                Font = _uiFont,
                Height = 24
            };
            _txtPath.KeyDown += delegate(object s, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    string target = Environment.ExpandEnvironmentVariables(_txtPath.Text.Trim().Trim('"'));
                    if (Directory.Exists(target)) Navigate(target, true);
                    else if (File.Exists(target)) { Navigate(Path.GetDirectoryName(target), true); }
                    else _owner.SetStatus("Δεν βρέθηκε η διαδρομή: " + target);
                    _lv.Focus();
                }
                else if (e.KeyCode == Keys.Escape)
                {
                    e.SuppressKeyPress = true;
                    _txtPath.Text = CurrentPath;
                    _lv.Focus();
                }
            };

            _txtFilter = new TextBox
            {
                BorderStyle = BorderStyle.FixedSingle,
                Font = _uiFont,
                Width = 155,
                Height = 24
            };
            _txtFilter.TextChanged += delegate
            {
                if (_suppressFilterEvent) return;
                if (CurrentTab != null) CurrentTab.Filter = _txtFilter.Text;
                ApplyFilterAndSort();
            };
            _txtFilter.KeyDown += delegate(object s, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Escape) { _txtFilter.Text = ""; _lv.Focus(); e.SuppressKeyPress = true; }
                else if (e.KeyCode == Keys.Enter) { _lv.Focus(); e.SuppressKeyPress = true; }
            };
            _txtFilter.HandleCreated += delegate { Native.SetCueBanner(_txtFilter, "Φίλτρο (Ctrl+F)..."); };
            _owner.SetTooltip(_txtFilter, "Άμεσο φίλτρο αρχείων χωρίς τόνους (Ctrl+F)");

            _navBar.Controls.Add(_btnBack);
            _navBar.Controls.Add(_btnFwd);
            _navBar.Controls.Add(_btnUp);
            _navBar.Controls.Add(_btnRefresh);
            _navBar.Controls.Add(_btnStar);
            _navBar.Controls.Add(_txtPath);
            _navBar.Controls.Add(_txtFilter);
            _navBar.Resize += delegate { LayoutNavBar(); };

            _lv = new DoubleBufferedListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                VirtualMode = true,
                OwnerDraw = true,
                FullRowSelect = true,
                MultiSelect = true,
                HideSelection = false,
                BorderStyle = BorderStyle.None,
                Font = _uiFont,
                AllowDrop = true
            };
            _lv.Columns.Add("Όνομα", 340, HorizontalAlignment.Left);
            _lv.Columns.Add("Τροποποίηση", 132, HorizontalAlignment.Left);
            _lv.Columns.Add("Τύπος", 125, HorizontalAlignment.Left);
            _lv.Columns.Add("Μέγεθος", 92, HorizontalAlignment.Right);

            _lv.RetrieveVirtualItem += Lv_RetrieveVirtualItem;
            _lv.DrawColumnHeader += Lv_DrawColumnHeader;
            _lv.DrawSubItem += Lv_DrawSubItem;
            _lv.ColumnClick += Lv_ColumnClick;
            _lv.DoubleClick += delegate { OpenSelected(); };
            _lv.SelectedIndexChanged += delegate { UpdateFooterAndPreview(); };
            _lv.Enter += delegate { _owner.SetActivePane(this); };
            _lv.MouseDown += delegate { _owner.SetActivePane(this); };
            _lv.KeyDown += Lv_KeyDown;
            _lv.ItemDrag += Lv_ItemDrag;
            _lv.DragEnter += Lv_DragEnter;
            _lv.DragDrop += Lv_DragDrop;
            _lv.Resize += delegate { FitNameColumn(); };
            _lv.HandleCreated += delegate { Native.SetDarkScrollbars(_lv, _theme.Dark); };

            _ctx = BuildContextMenu();
            _lv.ContextMenuStrip = _ctx;

            _lblFooter = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 24,
                Font = _smallFont,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 8, 0)
            };

            Controls.Add(_lv);
            Controls.Add(_lblFooter);
            Controls.Add(_navBar);
            Controls.Add(_tabStrip);
            Controls.Add(_activeBar);

            AddNewTab(initialPath, true);
            ApplyTheme(_theme);
            LayoutNavBar();
        }

        private AkButton MakeNavBtn(AkIcon icon, string tip, EventHandler onClick)
        {
            AkButton b = new AkButton
            {
                Kind = AkKind.Secondary,
                Icon = icon,
                Text = "",
                Width = 28,
                Height = 26,
                Radius = 6,
                Font = _boldFont
            };
            b.Click += delegate(object s, EventArgs e) { _owner.SetActivePane(this); onClick(s, e); };
            _owner.SetTooltip(b, tip);
            return b;
        }

        private void LayoutNavBar()
        {
            int x = 6;
            int y = 5;
            AkButton[] btns = new AkButton[] { _btnBack, _btnFwd, _btnUp, _btnRefresh, _btnStar };
            for (int i = 0; i < btns.Length; i++)
            {
                btns[i].Location = new Point(x, y);
                x += 31;
            }
            int filterW = Math.Max(110, Math.Min(185, _navBar.Width / 4));
            _txtFilter.SetBounds(_navBar.Width - filterW - 6, y + 1, filterW, 24);
            int pathW = Math.Max(80, _txtFilter.Left - x - 8);
            _txtPath.SetBounds(x + 2, y + 1, pathW, 24);
        }

        public void SetActiveVisual(bool active)
        {
            _isActivePane = active;
            _activeBar.BackColor = active ? _theme.Accent : _theme.Border;
        }

        public void ApplyTheme(Theme t)
        {
            _theme = t;
            BackColor = t.BgMain;
            _activeBar.BackColor = _isActivePane ? t.Accent : t.Border;
            _tabStrip.BackColor = t.BgPanel;
            _navBar.BackColor = t.BgPanel;
            _lblFooter.BackColor = t.BgPanel;
            _lblFooter.ForeColor = t.FgMuted;

            AkButton[] btns = new AkButton[] { _btnBack, _btnFwd, _btnUp, _btnRefresh, _btnStar };
            for (int i = 0; i < btns.Length; i++)
            {
                btns[i].Dark = t.Dark;
                btns[i].Active = (btns[i] == _btnStar && _owner.IsFavorite(CurrentPath));
                btns[i].Invalidate();
            }

            _txtPath.BackColor = t.Dark ? Color.FromArgb(24, 26, 27) : Color.White;
            _txtPath.ForeColor = t.FgMain;
            _txtFilter.BackColor = t.Dark ? Color.FromArgb(24, 26, 27) : Color.White;
            _txtFilter.ForeColor = t.FgMain;
            _lv.BackColor = t.Dark ? Color.FromArgb(24, 26, 27) : Color.White;
            _lv.ForeColor = t.FgMain;
            Native.SetDarkScrollbars(_lv, t.Dark);
            _lv.Invalidate();
            RebuildTabButtons();
        }

        public void FocusAddress() { _txtPath.Focus(); _txtPath.SelectAll(); }
        public void FocusFilter() { _txtFilter.Focus(); _txtFilter.SelectAll(); }
        public void FocusList() { _lv.Focus(); }

        // ---- Tabs ----
        public void AddNewTab(string path, bool select)
        {
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
                path = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            TabState tab = new TabState();
            tab.Path = path;
            tab.History.Add(path);
            tab.HistoryIndex = 0;
            _tabs.Add(tab);
            if (select)
            {
                _activeTabIdx = _tabs.Count - 1;
                LoadCurrentTab();
            }
            RebuildTabButtons();
        }

        public void CloseCurrentTab()
        {
            if (_tabs.Count <= 1) return;
            _tabs.RemoveAt(_activeTabIdx);
            if (_activeTabIdx >= _tabs.Count) _activeTabIdx = _tabs.Count - 1;
            LoadCurrentTab();
            RebuildTabButtons();
        }

        private void RebuildTabButtons()
        {
            _tabStrip.SuspendLayout();
            while (_tabStrip.Controls.Count > 0)
            {
                Control c = _tabStrip.Controls[0];
                _tabStrip.Controls.RemoveAt(0);
                c.Dispose();
            }

            int x = 6;
            for (int i = 0; i < _tabs.Count; i++)
            {
                int idx = i;
                bool active = (i == _activeTabIdx);
                string title = _tabs[i].DisplayTitle();
                bool showX = (_tabs.Count > 1 && active);
                int maxChars = showX ? 14 : 16;
                if (title.Length > maxChars) title = title.Substring(0, maxChars - 1) + "…";
                string label = showX ? (title + "  ×") : title;

                AkButton tb = new AkButton
                {
                    Kind = AkKind.Tab,
                    Active = active,
                    Dark = _theme.Dark,
                    Text = label,
                    AutoSize = false,
                    Height = 26,
                    Width = Math.Max(84, Math.Min(178, TextRenderer.MeasureText(label, active ? _boldFont : _smallFont).Width + 24)),
                    Location = new Point(x, 3),
                    Font = active ? _boldFont : _smallFont
                };
                tb.MouseUp += delegate(object s, MouseEventArgs me)
                {
                    _owner.SetActivePane(this);
                    if (me.Button == MouseButtons.Middle && _tabs.Count > 1)
                    {
                        _tabs.RemoveAt(idx);
                        if (_activeTabIdx >= _tabs.Count) _activeTabIdx = _tabs.Count - 1;
                        LoadCurrentTab();
                        RebuildTabButtons();
                        return;
                    }
                    if (me.Button == MouseButtons.Left)
                    {
                        if (active && _tabs.Count > 1 && me.X >= tb.Width - 22)
                        {
                            CloseCurrentTab();
                            return;
                        }
                        if (_activeTabIdx != idx)
                        {
                            _activeTabIdx = idx;
                            LoadCurrentTab();
                            RebuildTabButtons();
                        }
                    }
                };
                _tabStrip.Controls.Add(tb);
                x += tb.Width + 4;
            }

            AkButton btnPlus = new AkButton
            {
                Kind = AkKind.Ghost,
                Icon = AkIcon.Plus,
                Dark = _theme.Dark,
                Text = "",
                Width = 26,
                Height = 26,
                Location = new Point(x, 3),
                Font = _boldFont
            };
            btnPlus.Click += delegate { _owner.SetActivePane(this); AddNewTab(CurrentPath, true); };
            _owner.SetTooltip(btnPlus, "Νέα καρτέλα (Ctrl+T)");
            _tabStrip.Controls.Add(btnPlus);

            _tabStrip.ResumeLayout();
        }

        // ---- Navigation ----
        public void Navigate(string path, bool pushHistory)
        {
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path)) return;
            TabState tab = CurrentTab;
            if (tab == null) return;
            if (pushHistory && !string.Equals(tab.Path, path, StringComparison.OrdinalIgnoreCase))
            {
                if (tab.HistoryIndex < tab.History.Count - 1)
                    tab.History.RemoveRange(tab.HistoryIndex + 1, tab.History.Count - tab.HistoryIndex - 1);
                tab.History.Add(path);
                tab.HistoryIndex = tab.History.Count - 1;
            }
            tab.Path = path;
            tab.Filter = "";
            LoadCurrentTab();
            RebuildTabButtons();
        }

        public void GoBack()
        {
            TabState tab = CurrentTab;
            if (tab == null || tab.HistoryIndex <= 0) return;
            tab.HistoryIndex--;
            tab.Path = tab.History[tab.HistoryIndex];
            tab.Filter = "";
            LoadCurrentTab();
            RebuildTabButtons();
        }

        public void GoForward()
        {
            TabState tab = CurrentTab;
            if (tab == null || tab.HistoryIndex >= tab.History.Count - 1) return;
            tab.HistoryIndex++;
            tab.Path = tab.History[tab.HistoryIndex];
            tab.Filter = "";
            LoadCurrentTab();
            RebuildTabButtons();
        }

        public void GoUp()
        {
            if (string.IsNullOrEmpty(CurrentPath)) return;
            DirectoryInfo parent = Directory.GetParent(CurrentPath.TrimEnd('\\'));
            if (parent != null && Directory.Exists(parent.FullName))
                Navigate(parent.FullName, true);
        }

        public void Reload()
        {
            LoadCurrentTab();
        }

        private void ToggleFavorite()
        {
            if (string.IsNullOrEmpty(CurrentPath)) return;
            _owner.ToggleFavorite(CurrentPath);
            UpdateStarButton();
        }

        private void UpdateStarButton()
        {
            bool fav = _owner.IsFavorite(CurrentPath);
            _btnStar.Active = fav;
            _btnStar.Invalidate();
        }

        private void LoadCurrentTab()
        {
            TabState tab = CurrentTab;
            if (tab == null) return;

            Stopwatch sw = Stopwatch.StartNew();
            string err;
            _allEntries = FastDir.Enumerate(tab.Path, _owner.ShowHidden, out _dirCount, out _fileCount, out _totalBytes, out err);
            sw.Stop();
            _lastScanMs = sw.ElapsedMilliseconds;

            _txtPath.Text = tab.Path;
            _suppressFilterEvent = true;
            _txtFilter.Text = tab.Filter ?? "";
            _suppressFilterEvent = false;

            _btnBack.Enabled = tab.HistoryIndex > 0;
            _btnFwd.Enabled = tab.HistoryIndex < tab.History.Count - 1;
            _btnUp.Enabled = Directory.GetParent(tab.Path.TrimEnd('\\')) != null;
            UpdateStarButton();

            ApplyFilterAndSort();
            if (!string.IsNullOrEmpty(err))
                _owner.SetStatus(err);
            else
                _owner.SetStatus(string.Format("{0} · {1:N0} στοιχεία ({2} ms)", tab.DisplayTitle(), _allEntries.Count, _lastScanMs));
        }

        private void ApplyFilterAndSort()
        {
            TabState tab = CurrentTab;
            if (tab == null) return;
            string q = Fmt.StripAccents((tab.Filter ?? "").Trim());

            _visible = new List<FileEntry>(_allEntries.Count);
            for (int i = 0; i < _allEntries.Count; i++)
            {
                FileEntry e = _allEntries[i];
                if (q.Length == 0 || e.NormName.IndexOf(q, StringComparison.Ordinal) >= 0)
                    _visible.Add(e);
            }

            int col = tab.SortCol;
            bool asc = tab.SortAsc;
            _visible.Sort(delegate(FileEntry a, FileEntry b)
            {
                if (a.IsParentUp != b.IsParentUp) return a.IsParentUp ? -1 : 1;
                if (a.IsDir != b.IsDir) return a.IsDir ? -1 : 1;
                int cmp = 0;
                if (col == 0) cmp = StringComparer.CurrentCultureIgnoreCase.Compare(a.Name, b.Name);
                else if (col == 1) cmp = DateTime.Compare(a.Modified, b.Modified);
                else if (col == 2) cmp = StringComparer.CurrentCultureIgnoreCase.Compare(a.TypeLabel, b.TypeLabel);
                else if (col == 3) cmp = a.Size.CompareTo(b.Size);
                if (cmp == 0) cmp = StringComparer.CurrentCultureIgnoreCase.Compare(a.Name, b.Name);
                return asc ? cmp : -cmp;
            });

            if (q.Length == 0 && !string.IsNullOrEmpty(tab.Path))
            {
                DirectoryInfo par = Directory.GetParent(tab.Path.TrimEnd('\\'));
                if (par != null)
                {
                    FileEntry up = new FileEntry();
                    up.Name = "..";
                    up.FullPath = par.FullName;
                    up.IsDir = true;
                    up.IsParentUp = true;
                    up.Size = -1;
                    up.TypeLabel = "Επάνω φάκελος";
                    up.BadgeKind = 9;
                    up.NormName = "..";
                    up.SizeText = "";
                    up.DateText = "";
                    _visible.Insert(0, up);
                }
            }

            _lv.BeginUpdate();
            _lv.SelectedIndices.Clear();
            _lv.VirtualListSize = _visible.Count;
            _lv.EndUpdate();
            _lv.Invalidate();
            FitNameColumn();
            UpdateFooterAndPreview();
        }

        private void FitNameColumn()
        {
            if (_lv.Columns.Count < 4) return;
            int cw = _lv.ClientSize.Width;
            if (cw <= 80) return;
            if (cw < 560)
            {
                _lv.Columns[1].Width = 104;
                _lv.Columns[2].Width = 82;
                _lv.Columns[3].Width = 74;
            }
            else
            {
                _lv.Columns[1].Width = 132;
                _lv.Columns[2].Width = 125;
                _lv.Columns[3].Width = 92;
            }
            int fixedW = _lv.Columns[1].Width + _lv.Columns[2].Width + _lv.Columns[3].Width + 22;
            _lv.Columns[0].Width = Math.Max(130, cw - fixedW);
        }

        private void UpdateFooterAndPreview()
        {
            int selCount = _lv.SelectedIndices.Count;
            if (selCount > 0)
            {
                long selBytes = 0;
                FileEntry first = null;
                foreach (int idx in _lv.SelectedIndices)
                {
                    if (idx >= 0 && idx < _visible.Count)
                    {
                        if (first == null) first = _visible[idx];
                        if (!_visible[idx].IsDir && _visible[idx].Size > 0) selBytes += _visible[idx].Size;
                    }
                }
                string sizeStr = selBytes > 0 ? (" · " + Fmt.Bytes(selBytes)) : "";
                _lblFooter.Text = string.Format("{0:N0} στοιχεία ({1:N0} φάκελοι, {2:N0} αρχεία)  |  Επιλεγμένα: {3:N0}{4}  ·  {5} ms",
                    _visible.Count, _dirCount, _fileCount, selCount, sizeStr, _lastScanMs);
                if (_isActivePane) _owner.UpdatePreview(first);
            }
            else
            {
                _lblFooter.Text = string.Format("{0:N0} στοιχεία ({1:N0} φάκελοι, {2:N0} αρχεία · {3})  ·  {4} ms",
                    _visible.Count, _dirCount, _fileCount, Fmt.Bytes(_totalBytes), _lastScanMs);
                if (_isActivePane) _owner.UpdatePreview(null);
            }
        }

        // ---- Virtual ListView Rendering ----
        private void Lv_RetrieveVirtualItem(object sender, RetrieveVirtualItemEventArgs e)
        {
            if (e.ItemIndex < 0 || e.ItemIndex >= _visible.Count)
            {
                e.Item = new ListViewItem(new string[] { "", "", "", "" });
                return;
            }
            FileEntry f = _visible[e.ItemIndex];
            e.Item = new ListViewItem(new string[] { f.Name, f.DateText, f.TypeLabel, f.SizeText });
        }

        private void Lv_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            using (SolidBrush br = new SolidBrush(_theme.BgPanel)) e.Graphics.FillRectangle(br, e.Bounds);
            using (Pen p = new Pen(_theme.Border))
            {
                e.Graphics.DrawLine(p, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
                e.Graphics.DrawLine(p, e.Bounds.Right - 1, e.Bounds.Top + 4, e.Bounds.Right - 1, e.Bounds.Bottom - 4);
            }
            string txt = e.Header.Text;
            TabState tab = CurrentTab;
            if (tab != null && tab.SortCol == e.ColumnIndex)
                txt += tab.SortAsc ? " ▲" : " ▼";
            Rectangle r = new Rectangle(e.Bounds.X + 6, e.Bounds.Y, Math.Max(4, e.Bounds.Width - 10), e.Bounds.Height);
            TextFormatFlags tf = TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis |
                                 (e.ColumnIndex == 3 ? TextFormatFlags.Right : TextFormatFlags.Left);
            TextRenderer.DrawText(e.Graphics, txt, _smallFont, r, _theme.FgMuted, tf);
        }

        private void Lv_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            if (e.ItemIndex < 0 || e.ItemIndex >= _visible.Count) return;
            FileEntry f = _visible[e.ItemIndex];
            bool sel = _lv.SelectedIndices.Contains(e.ItemIndex);
            Color bg = sel ? _theme.BgSel : ((e.ItemIndex % 2 == 0) ? _theme.BgMain : _theme.BgAltRow);
            using (SolidBrush br = new SolidBrush(bg)) e.Graphics.FillRectangle(br, e.Bounds);

            if (e.ColumnIndex == 0)
            {
                DrawMiniBadge(e.Graphics, f.BadgeKind, e.Bounds.X + 7, e.Bounds.Y + (e.Bounds.Height - 14) / 2);
                Rectangle textR = new Rectangle(e.Bounds.X + 27, e.Bounds.Y, Math.Max(4, e.Bounds.Width - 30), e.Bounds.Height);
                Color fg = f.IsHidden ? _theme.FgMuted : (f.IsDir ? _theme.FgMain : _theme.FgMain);
                TextRenderer.DrawText(e.Graphics, f.Name, f.IsDir ? _boldFont : _uiFont, textR, fg,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            }
            else
            {
                Rectangle textR = new Rectangle(e.Bounds.X + 6, e.Bounds.Y, Math.Max(4, e.Bounds.Width - 12), e.Bounds.Height);
                Color fg = (e.ColumnIndex == 3) ? _theme.Cyan : _theme.FgMuted;
                TextFormatFlags tf = TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix |
                                     (e.ColumnIndex == 3 ? TextFormatFlags.Right : TextFormatFlags.Left);
                TextRenderer.DrawText(e.Graphics, e.SubItem.Text, _smallFont, textR, fg, tf);
            }
        }

        private void DrawMiniBadge(Graphics g, int kind, int x, int y)
        {
            if (kind == 9) // Up-parent (..) crisp arrow glyph
            {
                using (Pen p = new Pen(_theme.Cyan, 1.8f))
                {
                    g.DrawLine(p, x + 7, y + 2, x + 7, y + 12);
                    g.DrawLine(p, x + 3, y + 6, x + 7, y + 2);
                    g.DrawLine(p, x + 11, y + 6, x + 7, y + 2);
                }
                return;
            }
            if (kind == 0) // Crisp Folder glyph
            {
                using (SolidBrush b = new SolidBrush(_theme.Accent))
                {
                    g.FillRectangle(b, x, y + 1, 6, 3);
                    g.FillRectangle(b, x, y + 3, 14, 10);
                }
                return;
            }
            Color c = _theme.FgMuted;
            if (kind == 1) c = _theme.Cyan;
            else if (kind == 2) c = _theme.Pink;
            else if (kind == 3) c = _theme.Green;
            else if (kind == 4) c = _theme.Purple;
            else if (kind == 5) c = _theme.Accent;
            else if (kind == 6) c = _theme.Pink;
            else if (kind == 7) c = _theme.Cyan;

            using (Pen p = new Pen(c, 1.4f))
                g.DrawRectangle(p, x + 2, y + 1, 10, 12);
            using (SolidBrush b = new SolidBrush(c))
                g.FillRectangle(b, x + 4, y + 5, 6, 2);
        }

        private void Lv_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            TabState tab = CurrentTab;
            if (tab == null) return;
            if (tab.SortCol == e.Column) tab.SortAsc = !tab.SortAsc;
            else { tab.SortCol = e.Column; tab.SortAsc = true; }
            ApplyFilterAndSort();
        }

        // ---- File Operations ----
        public List<FileEntry> GetSelectedEntries()
        {
            List<FileEntry> list = new List<FileEntry>();
            foreach (int idx in _lv.SelectedIndices)
                if (idx >= 0 && idx < _visible.Count) list.Add(_visible[idx]);
            return list;
        }

        public List<string> GetSelectedPaths()
        {
            List<FileEntry> entries = GetSelectedEntries();
            List<string> paths = new List<string>(entries.Count);
            for (int i = 0; i < entries.Count; i++)
                if (!entries[i].IsParentUp) paths.Add(entries[i].FullPath);
            return paths;
        }

        public void OpenSelected()
        {
            List<FileEntry> sel = GetSelectedEntries();
            if (sel.Count == 0) return;
            FileEntry first = sel[0];
            if (first.IsParentUp)
            {
                GoUp();
                return;
            }
            if (first.IsDir)
            {
                Navigate(first.FullPath, true);
                return;
            }
            try
            {
                Process.Start(new ProcessStartInfo(first.FullPath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                _owner.SetStatus("Αδυναμία ανοίγματος: " + Fmt.FriendlyError(ex));
            }
        }

        public void CopySelection(bool isCut)
        {
            List<string> paths = GetSelectedPaths();
            if (paths.Count == 0) return;
            ShellClipboard.SetFiles(paths, isCut);
            _owner.SetStatus(string.Format("✓ {0} {1} στοιχείο/α στο πρόχειρο (συμβατό με Windows Explorer).",
                isCut ? "Αποκοπή" : "Αντιγραφή", paths.Count));
        }

        public void PasteHere()
        {
            List<string> paths; bool isCut;
            if (!ShellClipboard.TryGetFiles(out paths, out isCut)) return;
            if (string.IsNullOrEmpty(CurrentPath) || !Directory.Exists(CurrentPath)) return;
            bool ok = Native.ShellOperate(_owner.Handle, isCut ? Native.FO_MOVE : Native.FO_COPY, paths, CurrentPath, false, false);
            if (ok)
            {
                _owner.ReloadAllPanes();
                _owner.SetStatus(string.Format("✓ {0} {1} στοιχείο/α.", isCut ? "Μετακινήθηκαν" : "Επικολλήθηκαν", paths.Count));
            }
        }

        public void DeleteSelected(bool permanent)
        {
            List<string> paths = GetSelectedPaths();
            if (paths.Count == 0) return;
            string msg = permanent
                ? string.Format("ΜΟΝΙΜΗ διαγραφή {0} στοιχείων (ΔΕΝ πάνε στον Κάδο Ανακύκλωσης);\n\n{1}", paths.Count, Path.GetFileName(paths[0]))
                : string.Format("Μεταφορά {0} στοιχείων στον Κάδο Ανακύκλωσης;\n\n{1}", paths.Count, Path.GetFileName(paths[0]));
            DialogResult dr = MessageBox.Show(_owner, msg, permanent ? "NillyFiles Pro — Μόνιμη Διαγραφή" : "NillyFiles Pro — Κάδος Ανακύκλωσης",
                MessageBoxButtons.YesNo, permanent ? MessageBoxIcon.Warning : MessageBoxIcon.Question);
            if (dr != DialogResult.Yes) return;

            bool ok = Native.ShellOperate(_owner.Handle, Native.FO_DELETE, paths, null, !permanent, true);
            if (ok)
            {
                _owner.ReloadAllPanes();
                _owner.SetStatus(permanent ? "✓ Διαγράφηκαν μόνιμα." : "✓ Μεταφέρθηκαν στον Κάδο Ανακύκλωσης.");
            }
        }

        public void RenameSelected()
        {
            List<FileEntry> sel = GetSelectedEntries();
            if (sel.Count != 1 || sel[0].IsParentUp) return;
            FileEntry f = sel[0];
            string newName = PromptText("Μετονομασία", "Νέο όνομα για: " + f.Name, f.Name, !f.IsDir);
            if (string.IsNullOrEmpty(newName) || newName == f.Name) return;
            try
            {
                string dest = Path.Combine(CurrentPath, newName);
                if (f.IsDir) Directory.Move(f.FullPath, dest);
                else File.Move(f.FullPath, dest);
                _owner.ReloadAllPanes();
                _owner.SetStatus("✓ Μετονομάστηκε σε: " + newName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(_owner, Fmt.FriendlyError(ex), "NillyFiles Pro — Μετονομασία", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        public void CreateNewFolder()
        {
            string name = PromptText("Νέος Φάκελος", "Όνομα νέου φακέλου:", "Νέος φάκελος", false);
            if (string.IsNullOrEmpty(name)) return;
            try
            {
                string full = Path.Combine(CurrentPath, name);
                Directory.CreateDirectory(full);
                _owner.ReloadAllPanes();
                _owner.SetStatus("✓ Δημιουργήθηκε ο φάκελος: " + name);
            }
            catch (Exception ex)
            {
                MessageBox.Show(_owner, Fmt.FriendlyError(ex), "NillyFiles Pro", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        public void CreateNewTextFile()
        {
            string name = PromptText("Νέο Αρχείο", "Όνομα νέου αρχείου:", "notes.txt", true);
            if (string.IsNullOrEmpty(name)) return;
            try
            {
                string full = Path.Combine(CurrentPath, name);
                if (!File.Exists(full)) File.WriteAllText(full, "", new UTF8Encoding(false));
                _owner.ReloadAllPanes();
                _owner.SetStatus("✓ Δημιουργήθηκε το αρχείο: " + name);
            }
            catch (Exception ex)
            {
                MessageBox.Show(_owner, Fmt.FriendlyError(ex), "NillyFiles Pro", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        public void CopySelectedPaths()
        {
            List<string> paths = GetSelectedPaths();
            string text = paths.Count > 0 ? string.Join(Environment.NewLine, paths.ToArray()) : CurrentPath;
            if (!string.IsNullOrEmpty(text))
            {
                try
                {
                    Clipboard.SetText(text);
                    _owner.SetStatus("✓ Αντιγράφηκε η διαδρομή στο πρόχειρο.");
                }
                catch { _owner.SetStatus("⚠ Το πρόχειρο είναι απασχολημένο. Δοκίμασε ξανά."); }
            }
        }

        public void ShowSelectedProperties()
        {
            List<string> paths = GetSelectedPaths();
            Native.ShowProperties(_owner.Handle, paths.Count > 0 ? paths[0] : CurrentPath);
        }

        private string PromptText(string title, string label, string initial, bool selectStemOnly)
        {
            using (Form dlg = new Form())
            {
                dlg.Text = "NillyFiles Pro — " + title;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.ClientSize = new Size(410, 128);
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;
                dlg.BackColor = _theme.BgPanel;
                dlg.ForeColor = _theme.FgMain;
                Native.SetDarkTitlebar(dlg.Handle, _theme.Dark);

                Label lbl = new Label { Text = label, Location = new Point(14, 14), AutoSize = true, Font = _uiFont, ForeColor = _theme.FgMuted };
                TextBox tb = new TextBox
                {
                    Text = initial,
                    Location = new Point(14, 40),
                    Width = 380,
                    Font = _uiFont,
                    BackColor = _theme.BgSurface,
                    ForeColor = _theme.FgMain,
                    BorderStyle = BorderStyle.FixedSingle
                };
                Button ok = new Button
                {
                    Text = "ΟΚ",
                    DialogResult = DialogResult.OK,
                    Location = new Point(226, 82),
                    Width = 80,
                    Height = 30,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = _theme.Accent,
                    ForeColor = Color.FromArgb(20, 22, 26),
                    Font = _boldFont
                };
                ok.FlatAppearance.BorderSize = 0;
                Button cancel = new Button
                {
                    Text = "Ακύρωση",
                    DialogResult = DialogResult.Cancel,
                    Location = new Point(314, 82),
                    Width = 80,
                    Height = 30,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = _theme.BgSurface,
                    ForeColor = _theme.FgMain,
                    Font = _uiFont
                };
                cancel.FlatAppearance.BorderSize = 0;

                dlg.Controls.AddRange(new Control[] { lbl, tb, ok, cancel });
                dlg.AcceptButton = ok;
                dlg.CancelButton = cancel;
                dlg.Shown += delegate
                {
                    tb.Focus();
                    if (selectStemOnly)
                    {
                        int dot = initial.LastIndexOf('.');
                        if (dot > 0) tb.Select(0, dot);
                        else tb.SelectAll();
                    }
                    else tb.SelectAll();
                };
                return dlg.ShowDialog(_owner) == DialogResult.OK ? tb.Text.Trim() : null;
            }
        }

        // ---- Keyboard & Drag-Drop & Context Menu ----
        private void Lv_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && !e.Alt) { OpenSelected(); e.Handled = true; }
            else if (e.KeyCode == Keys.Enter && e.Alt) { ShowSelectedProperties(); e.Handled = true; }
            else if (e.KeyCode == Keys.Back) { GoUp(); e.Handled = true; }
            else if (e.Alt && e.KeyCode == Keys.Left) { GoBack(); e.Handled = true; }
            else if (e.Alt && e.KeyCode == Keys.Right) { GoForward(); e.Handled = true; }
            else if (e.Alt && e.KeyCode == Keys.Up) { GoUp(); e.Handled = true; }
            else if (e.KeyCode == Keys.F2) { RenameSelected(); e.Handled = true; }
            else if (e.KeyCode == Keys.Delete) { DeleteSelected(e.Shift); e.Handled = true; }
            else if (e.Control && !e.Shift && e.KeyCode == Keys.C) { CopySelection(false); e.Handled = true; }
            else if (e.Control && e.Shift && e.KeyCode == Keys.C) { CopySelectedPaths(); e.Handled = true; }
            else if (e.Control && e.KeyCode == Keys.X) { CopySelection(true); e.Handled = true; }
            else if (e.Control && e.KeyCode == Keys.V) { PasteHere(); e.Handled = true; }
            else if (e.Control && e.KeyCode == Keys.A)
            {
                _lv.BeginUpdate();
                _lv.SelectedIndices.Clear();
                for (int i = 0; i < _visible.Count; i++)
                    if (!_visible[i].IsParentUp) _lv.SelectedIndices.Add(i);
                _lv.EndUpdate();
                e.Handled = true;
            }
            else if ((e.Control && e.Shift && e.KeyCode == Keys.N) || e.KeyCode == Keys.F7)
            {
                CreateNewFolder();
                e.Handled = true;
            }
        }

        private void Lv_ItemDrag(object sender, ItemDragEventArgs e)
        {
            List<string> paths = GetSelectedPaths();
            if (paths.Count == 0) return;
            DataObject data = new DataObject(DataFormats.FileDrop, paths.ToArray());
            DoDragDrop(data, DragDropEffects.Copy | DragDropEffects.Move);
        }

        private void Lv_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = ((e.KeyState & 8) == 8) ? DragDropEffects.Copy : DragDropEffects.Move;
            else
                e.Effect = DragDropEffects.None;
        }

        private void Lv_DragDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
            string[] dropped = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (dropped == null || dropped.Length == 0) return;
            // Avoid dropping onto the exact same directory
            string srcDir = Path.GetDirectoryName(dropped[0]);
            if (string.Equals(srcDir, CurrentPath.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase) && e.Effect == DragDropEffects.Move)
                return;
            uint op = (e.Effect == DragDropEffects.Copy) ? Native.FO_COPY : Native.FO_MOVE;
            if (Native.ShellOperate(_owner.Handle, op, dropped, CurrentPath, false, false))
                _owner.ReloadAllPanes();
        }

        private static string FindSiblingExe(string folder, string exeName)
        {
            try
            {
                string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"NillySuite\" + folder + @"\" + exeName);
                if (File.Exists(appData)) return appData;
                DirectoryInfo par = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\'));
                if (par != null)
                {
                    string sib = Path.Combine(Path.Combine(par.FullName, folder), exeName);
                    if (File.Exists(sib)) return sib;
                }
            }
            catch { }
            return null;
        }

        private static void LaunchSibling(string exe, string args)
        {
            try { Process.Start(new ProcessStartInfo(exe, args) { WorkingDirectory = Path.GetDirectoryName(exe), UseShellExecute = true }); } catch { }
        }

        private ContextMenuStrip BuildContextMenu()
        {
            ContextMenuStrip m = new ContextMenuStrip();
            ToolStripMenuItem miOpen = new ToolStripMenuItem("Άνοιγμα (Enter)", null, delegate { OpenSelected(); });
            ToolStripMenuItem miNewTab = new ToolStripMenuItem("Άνοιγμα φακέλου σε νέα καρτέλα", null, delegate
            {
                List<FileEntry> sel = GetSelectedEntries();
                if (sel.Count > 0 && sel[0].IsDir) AddNewTab(sel[0].FullPath, true);
            });
            ToolStripMenuItem miSuite = new ToolStripMenuItem("Nilly Suite");
            ToolStripMenuItem miCopy = new ToolStripMenuItem("Αντιγραφή (Ctrl+C)", null, delegate { CopySelection(false); });
            ToolStripMenuItem miCut = new ToolStripMenuItem("Αποκοπή (Ctrl+X)", null, delegate { CopySelection(true); });
            ToolStripMenuItem miPaste = new ToolStripMenuItem("Επικόλληση (Ctrl+V)", null, delegate { PasteHere(); });
            ToolStripMenuItem miCopyOther = new ToolStripMenuItem("Αντιγραφή στο άλλο παράθυρο (F5)", null, delegate { _owner.TransferToOtherPane(false); });
            ToolStripMenuItem miMoveOther = new ToolStripMenuItem("Μετακίνηση στο άλλο παράθυρο (F6)", null, delegate { _owner.TransferToOtherPane(true); });
            ToolStripMenuItem miRen = new ToolStripMenuItem("Μετονομασία (F2)", null, delegate { RenameSelected(); });
            ToolStripMenuItem miDel = new ToolStripMenuItem("Διαγραφή στον Κάδο (Del)", null, delegate { DeleteSelected(false); });
            ToolStripMenuItem miNewDir = new ToolStripMenuItem("Νέος Φάκελος (F7)", null, delegate { CreateNewFolder(); });
            ToolStripMenuItem miNewTxt = new ToolStripMenuItem("Νέο Αρχείο κειμένου", null, delegate { CreateNewTextFile(); });
            ToolStripMenuItem miCopyPath = new ToolStripMenuItem("Αντιγραφή διαδρομής (Ctrl+Shift+C)", null, delegate { CopySelectedPaths(); });
            ToolStripMenuItem miWinExp = new ToolStripMenuItem("Άνοιγμα σε Windows Explorer", null, delegate
            {
                List<string> paths = GetSelectedPaths();
                if (paths.Count > 0) Process.Start("explorer.exe", "/select,\"" + paths[0] + "\"");
                else Process.Start("explorer.exe", "\"" + CurrentPath + "\"");
            });
            ToolStripMenuItem miTerm = new ToolStripMenuItem("Άνοιγμα PowerShell εδώ", null, delegate
            {
                try { Process.Start(new ProcessStartInfo("powershell.exe", "-NoExit -Command \"Set-Location -LiteralPath '" + CurrentPath.Replace("'", "''") + "'\"") { UseShellExecute = true }); }
                catch { }
            });
            ToolStripMenuItem miProps = new ToolStripMenuItem("Ιδιότητες (Alt+Enter)", null, delegate { ShowSelectedProperties(); });

            m.Items.AddRange(new ToolStripItem[]
            {
                miOpen, miNewTab, miSuite, new ToolStripSeparator(),
                miCopy, miCut, miPaste, miCopyOther, miMoveOther, new ToolStripSeparator(),
                miRen, miDel, new ToolStripSeparator(),
                miNewDir, miNewTxt, new ToolStripSeparator(),
                miCopyPath, miWinExp, miTerm, new ToolStripSeparator(),
                miProps
            });

            m.Opening += delegate
            {
                List<FileEntry> sel = GetSelectedEntries();
                bool hasSel = sel.Count > 0;
                miOpen.Enabled = hasSel;
                miNewTab.Visible = (sel.Count == 1 && sel[0].IsDir);
                miCopy.Enabled = hasSel;
                miCut.Enabled = hasSel;
                List<string> cb; bool isCut;
                miPaste.Enabled = ShellClipboard.TryGetFiles(out cb, out isCut);
                miCopyOther.Visible = _owner.IsDualPane && hasSel;
                miMoveOther.Visible = _owner.IsDualPane && hasSel;
                miRen.Enabled = (sel.Count == 1);
                miDel.Enabled = hasSel;

                // Populate dynamic Nilly Suite submenu with ONLY installed sibling tools!
                miSuite.DropDownItems.Clear();
                string targetDir = (sel.Count == 1 && sel[0].IsDir) ? sel[0].FullPath : CurrentPath;
                FileEntry first = hasSel ? sel[0] : null;
                string zipExe = FindSiblingExe("NillyZipPro", "NillyZipPro.exe");
                if (zipExe != null && hasSel)
                {
                    if (sel.Count == 1 && !first.IsDir && (first.Ext == ".zip" || first.Ext == ".rar" || first.Ext == ".7z"))
                        miSuite.DropDownItems.Add("Αποσυμπίεση εδώ (NillyZip Pro)", null, delegate { LaunchSibling(zipExe, "--extract-here \"" + first.FullPath + "\""); });
                    miSuite.DropDownItems.Add("Συμπίεση σε ZIP (NillyZip Pro)", null, delegate
                    {
                        StringBuilder sb = new StringBuilder("--compress");
                        foreach (FileEntry fe in sel) sb.Append(" \"").Append(fe.FullPath).Append('"');
                        LaunchSibling(zipExe, sb.ToString());
                    });
                }
                string diskExe = FindSiblingExe("NillyDiskPro", "NillyDiskPro.exe");
                if (diskExe != null && !string.IsNullOrEmpty(targetDir))
                    miSuite.DropDownItems.Add("Ανάλυση Χώρου (NillyDisk Pro)", null, delegate { LaunchSibling(diskExe, "\"" + targetDir + "\""); });
                string searchExe = FindSiblingExe("NillySearchPro", "NillySearchPro.exe");
                if (searchExe != null && !string.IsNullOrEmpty(targetDir))
                    miSuite.DropDownItems.Add("Αναζήτηση εδώ (NillySearch Pro)", null, delegate { LaunchSibling(searchExe, "\"" + targetDir + "\""); });
                if (first != null && !first.IsDir)
                {
                    string codeExe = FindSiblingExe("NillyCodePro", "NillyCodePro.exe");
                    if (codeExe != null) miSuite.DropDownItems.Add("Επεξεργασία σε NillyCode Pro", null, delegate { LaunchSibling(codeExe, "\"" + first.FullPath + "\""); });
                    string noteExe = FindSiblingExe("NillyNotePro", "NillyNotePro.exe");
                    if (noteExe != null) miSuite.DropDownItems.Add("Άνοιγμα σε NillyNote Pro", null, delegate { LaunchSibling(noteExe, "\"" + first.FullPath + "\""); });
                    if (first.Ext == ".pdf")
                    {
                        string docExe = FindSiblingExe("NillyDocPro", "NillyDocPro.exe");
                        if (docExe != null) miSuite.DropDownItems.Add("Άνοιγμα σε NillyDoc Pro (PDF)", null, delegate { LaunchSibling(docExe, "\"" + first.FullPath + "\""); });
                    }
                    if (first.BadgeKind == 1)
                    {
                        string viewExe = FindSiblingExe("NillyViewPro", "NillyViewPro.exe");
                        if (viewExe != null) miSuite.DropDownItems.Add("Προβολή σε NillyView Pro", null, delegate { LaunchSibling(viewExe, "\"" + first.FullPath + "\""); });
                    }
                    if (first.BadgeKind == 5)
                    {
                        string mediaExe = FindSiblingExe("NillyMediaLite", "NillyMediaLite.exe");
                        if (mediaExe != null) miSuite.DropDownItems.Add("Αναπαραγωγή σε NillyMedia Lite", null, delegate { LaunchSibling(mediaExe, "\"" + first.FullPath + "\""); });
                    }
                    if (first.Ext == ".ttf" || first.Ext == ".otf")
                    {
                        string fontExe = FindSiblingExe("NillyFontPro", "NillyFontPro.exe");
                        if (fontExe != null) miSuite.DropDownItems.Add("Δοκιμή / Εγκατάσταση σε NillyFont Pro", null, delegate { LaunchSibling(fontExe, "\"" + first.FullPath + "\""); });
                    }
                }
                string batchExe = FindSiblingExe("NillyBatchPro", "NillyBatchPro.exe");
                if (batchExe != null && hasSel)
                {
                    miSuite.DropDownItems.Add("Μαζική επεξεργασία (NillyBatch Pro)", null, delegate
                    {
                        StringBuilder sb = new StringBuilder();
                        foreach (FileEntry fe in sel) sb.Append(" \"").Append(fe.FullPath).Append('"');
                        LaunchSibling(batchExe, sb.ToString().Trim());
                    });
                }
                miSuite.Visible = (miSuite.DropDownItems.Count > 0);
            };
            return m;
        }
    }

    // ========================================================================
    // 7. MAIN FORM (Header + Sidebar + Single/Dual Pane + On-Demand Preview + Info)
    // ========================================================================
    internal sealed class MainForm : Form
    {
        private readonly AppSettings _settings;
        private Theme _theme;
        private readonly ToolTip _tip = new ToolTip();

        private readonly Panel _header;
        private readonly PictureBox _logoBox;
        private readonly Label _lblTitle;
        private readonly Label _lblSub;
        private readonly FlowLayoutPanel _topActions;
        private readonly AkButton _btnDual, _btnHidden, _btnPreview, _btnTheme, _btnInfo;

        private readonly Panel _sidebar;
        private readonly FlowLayoutPanel _sideFlow;

        private readonly SplitContainer _splitPanes;
        private readonly FilePane _leftPane;
        private readonly FilePane _rightPane;
        private FilePane _activePane;

        private readonly Panel _previewPanel;
        private readonly PictureBox _prevImg;
        private readonly TextBox _prevText;
        private readonly Label _prevMeta;
        private readonly Button _prevBtnOpen;
        private FileEntry _previewEntry;

        private readonly Panel _statusBar;
        private readonly Label _lblStatus;
        private readonly LinkLabel _lnkBrand;

        private Bitmap _logoBmp;
        private readonly Font _fontTitle = new Font("Segoe UI", 10.5f, FontStyle.Bold);
        private readonly Font _fontUi = new Font("Segoe UI", 9.0f, FontStyle.Regular);
        private readonly Font _fontBold = new Font("Segoe UI", 9.0f, FontStyle.Bold);
        private readonly Font _fontSmall = new Font("Segoe UI", 8.2f, FontStyle.Regular);
        private readonly Font _fontDriveSub = new Font("Segoe UI", 7.4f, FontStyle.Regular);

        public Theme CurrentTheme { get { return _theme; } }
        public bool ShowHidden { get { return _settings.ShowHidden; } }
        public bool IsDualPane { get { return _settings.DualPane; } }
        public FilePane LeftPane { get { return _leftPane; } }

        public MainForm(string startPath, bool smokeMode)
        {
            _settings = smokeMode ? new AppSettings() : AppSettings.Load();
            if (!smokeMode)
            {
                try
                {
                    string suiteTheme = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"NillySuite\suite.theme");
                    string localIni = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"NillyFilesPro\settings.ini");
                    if (File.Exists(suiteTheme) && (!File.Exists(localIni) || File.GetLastWriteTimeUtc(suiteTheme) > File.GetLastWriteTimeUtc(localIni)))
                    {
                        string s = File.ReadAllText(suiteTheme, Encoding.ASCII).Trim();
                        if (s.Equals("light", StringComparison.OrdinalIgnoreCase)) _settings.DarkTheme = false;
                        else if (s.Equals("dark", StringComparison.OrdinalIgnoreCase)) _settings.DarkTheme = true;
                    }
                }
                catch { }
            }
            _theme = Theme.Create(_settings.DarkTheme);

            Text = "NillyFiles Pro — Ultra-Light File Explorer | Nilly graphics";
            Size = new Size(1180, 720);
            MinimumSize = new Size(680, 440);
            StartPosition = FormStartPosition.CenterScreen;

            string icoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "NillyFilesPro.ico");
            if (File.Exists(icoPath))
            {
                try { _logoBmp = Native.LoadCrispIconBitmap(icoPath, 26); } catch { }
                try { Icon = new Icon(icoPath); } catch { }
            }

            // ---- Top Header ----
            _header = new Panel { Dock = DockStyle.Top, Height = 48, Padding = new Padding(10, 0, 10, 0) };
            _logoBox = new PictureBox { Size = new Size(26, 26), Location = new Point(12, 11), SizeMode = PictureBoxSizeMode.Zoom, Image = _logoBmp };
            _lblTitle = new Label { Text = "NillyFiles Pro", AutoSize = true, Location = new Point(44, 6), Font = _fontTitle };
            _lblSub = new Label { Text = "Ultra-Light Explorer · 0 MB Idle RAM", AutoSize = true, Location = new Point(45, 27), Font = new Font("Segoe UI", 7.6f) };
            _header.Paint += delegate(object s, PaintEventArgs pe)
            {
                using (Pen p = new Pen(_theme.Border))
                    pe.Graphics.DrawLine(p, 0, _header.Height - 1, _header.Width, _header.Height - 1);
            };

            _topActions = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                WrapContents = false,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(0, 9, 0, 0)
            };

            _btnDual = MakeTopBtn("Διπλό (F3)", 106, AkIcon.Split, "Εναλλαγή Διπλού Παραθύρου (Dual-Pane F3)", delegate { ToggleDualPane(); });
            _btnHidden = MakeTopBtn("Κρυφά", 86, AkIcon.Filter, "Εμφάνιση/Απόκρυψη κρυφών αρχείων (Ctrl+H)", delegate { ToggleHidden(); });
            _btnPreview = MakeTopBtn("Προεπ.", 90, AkIcon.List, "Εναλλαγή πάνελ προεπισκόπησης (Alt+P)", delegate { TogglePreview(); });
            _btnTheme = MakeTopBtn("", 32, AkIcon.Sun, "Αλλαγή Dark / Light θέματος", delegate { ToggleTheme(); });
            _btnInfo = MakeTopBtn("", 32, AkIcon.Info, "Πληροφορίες Ultra-Light Specs & Συντομεύσεις", delegate { ShowInfoDialog(); });

            _topActions.Controls.AddRange(new Control[] { _btnDual, _btnHidden, _btnPreview, _btnTheme, _btnInfo });
            _header.Controls.AddRange(new Control[] { _logoBox, _lblTitle, _lblSub, _topActions });

            // ---- Left Sidebar (Quick Access + Favorites + Drives) ----
            _sidebar = new Panel { Dock = DockStyle.Left, Width = 204, Padding = new Padding(0, 4, 1, 4) };
            _sideFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(6, 4, 6, 8)
            };
            _sidebar.Controls.Add(_sideFlow);

            // ---- Right On-Demand Preview Panel ----
            _previewPanel = new Panel { Dock = DockStyle.Right, Width = 264, Padding = new Padding(10), Visible = _settings.ShowPreview };
            Label lblPrevHdr = new Label { Text = "ΠΡΟΕΠΙΣΚΟΠΗΣΗ", Dock = DockStyle.Top, Height = 22, Font = new Font("Segoe UI", 8.0f, FontStyle.Bold) };
            _prevImg = new PictureBox { Dock = DockStyle.Top, Height = 180, SizeMode = PictureBoxSizeMode.Zoom, Visible = false };
            _prevText = new TextBox
            {
                Dock = DockStyle.Top,
                Height = 180,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Consolas", 8.4f),
                Visible = false
            };
            _prevMeta = new Label { Dock = DockStyle.Fill, Font = _fontSmall, Padding = new Padding(0, 8, 0, 8) };
            _prevBtnOpen = new Button
            {
                Text = "Άνοιγμα αρχείου",
                Dock = DockStyle.Bottom,
                Height = 30,
                FlatStyle = FlatStyle.Flat,
                Font = _fontBold,
                Cursor = Cursors.Hand,
                Visible = false
            };
            _prevBtnOpen.FlatAppearance.BorderSize = 0;
            _prevBtnOpen.Click += delegate
            {
                if (_previewEntry != null)
                {
                    try { Process.Start(new ProcessStartInfo(_previewEntry.FullPath) { UseShellExecute = true }); } catch { }
                }
            };
            _previewPanel.Controls.Add(_prevMeta);
            _previewPanel.Controls.Add(_prevBtnOpen);
            _previewPanel.Controls.Add(_prevText);
            _previewPanel.Controls.Add(_prevImg);
            _previewPanel.Controls.Add(lblPrevHdr);

            // ---- Center Single / Dual Split Container ----
            string initLeft = (!string.IsNullOrEmpty(startPath) && Directory.Exists(startPath))
                ? startPath
                : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string initRight = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (!Directory.Exists(initRight)) initRight = initLeft;

            _splitPanes = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterWidth = 4,
                Panel2Collapsed = !_settings.DualPane
            };
            _leftPane = new FilePane(this, initLeft);
            _rightPane = new FilePane(this, initRight);
            _splitPanes.Panel1.Controls.Add(_leftPane);
            _splitPanes.Panel2.Controls.Add(_rightPane);
            _activePane = _leftPane;

            // ---- Bottom Status Bar ----
            _statusBar = new Panel { Dock = DockStyle.Bottom, Height = 26, Padding = new Padding(10, 0, 10, 0) };
            _lblStatus = new Label { Dock = DockStyle.Fill, Font = _fontSmall, TextAlign = ContentAlignment.MiddleLeft };
            _lnkBrand = new LinkLabel
            {
                Text = "created by Nilly graphics (Sotiris Christos) — nillygraphics.com",
                Dock = DockStyle.Right,
                AutoSize = true,
                Font = _fontSmall,
                TextAlign = ContentAlignment.MiddleRight,
                Padding = new Padding(8, 5, 2, 0),
                LinkBehavior = LinkBehavior.HoverUnderline
            };
            _lnkBrand.LinkClicked += delegate { OpenUrl("https://nillygraphics.com/"); };
            _statusBar.Controls.Add(_lblStatus);
            _statusBar.Controls.Add(_lnkBrand);

            // Dock order: Fill first, then sides, then top/bottom
            Controls.Add(_splitPanes);
            Controls.Add(_previewPanel);
            Controls.Add(_sidebar);
            Controls.Add(_header);
            Controls.Add(_statusBar);

            HandleCreated += delegate
            {
                Native.SetDarkTitlebar(Handle, _theme.Dark);
                Native.ApplyCrispTaskbarIcon(this, icoPath);
            };
            Shown += delegate { Native.TrimMemory(); };
            FormClosing += delegate
            {
                if (!smokeMode) _settings.Save();
            };

            Resize += delegate
            {
                if (_lblSub != null) _lblSub.Visible = (ClientSize.Width >= 740);
                if (_lnkBrand != null) _lnkBrand.Visible = (ClientSize.Width >= 720);
                if (_header != null) _header.Invalidate();
            };

            SetActivePane(_leftPane);
            BuildSidebar();
            ApplyThemeAll();
        }

        private AkButton MakeTopBtn(string text, int w, AkIcon icon, string tip, EventHandler click)
        {
            AkButton b = new AkButton
            {
                Kind = AkKind.Secondary,
                Icon = icon,
                Text = text,
                Width = w,
                Height = 29,
                Font = _fontBold,
                Margin = new Padding(5, 0, 0, 0)
            };
            b.Click += click;
            _tip.SetToolTip(b, tip);
            return b;
        }

        public void SetTooltip(Control c, string text) { _tip.SetToolTip(c, text); }
        public void SetStatus(string text) { if (_lblStatus != null) _lblStatus.Text = text; }

        public void SetActivePane(FilePane p)
        {
            if (p == null) return;
            _activePane = p;
            _leftPane.SetActiveVisual(_activePane == _leftPane);
            _rightPane.SetActiveVisual(_activePane == _rightPane);
        }

        public FilePane GetOtherPane()
        {
            return (_activePane == _leftPane) ? _rightPane : _leftPane;
        }

        public void ReloadAllPanes()
        {
            _leftPane.Reload();
            if (_settings.DualPane) _rightPane.Reload();
            BuildSidebar();
        }

        public void TransferToOtherPane(bool move)
        {
            if (!_settings.DualPane) return;
            FilePane src = _activePane;
            FilePane dst = GetOtherPane();
            List<string> paths = src.GetSelectedPaths();
            if (paths.Count == 0 || string.IsNullOrEmpty(dst.CurrentPath)) return;
            if (string.Equals(src.CurrentPath.TrimEnd('\\'), dst.CurrentPath.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
            {
                SetStatus("Τα δύο παράθυρα δείχνουν στον ίδιο φάκελο.");
                return;
            }
            bool ok = Native.ShellOperate(Handle, move ? Native.FO_MOVE : Native.FO_COPY, paths, dst.CurrentPath, false, false);
            if (ok)
            {
                ReloadAllPanes();
                SetStatus(string.Format("✓ {0} {1} στοιχείο/α στο {2}", move ? "Μετακινήθηκαν" : "Αντιγράφηκαν", paths.Count, dst.CurrentTab.DisplayTitle()));
            }
        }

        public bool IsFavorite(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            for (int i = 0; i < _settings.Favorites.Count; i++)
                if (string.Equals(_settings.Favorites[i], path, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        public void ToggleFavorite(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            for (int i = 0; i < _settings.Favorites.Count; i++)
            {
                if (string.Equals(_settings.Favorites[i], path, StringComparison.OrdinalIgnoreCase))
                {
                    _settings.Favorites.RemoveAt(i);
                    BuildSidebar();
                    SetStatus("Αφαιρέθηκε από τα Αγαπημένα.");
                    return;
                }
            }
            _settings.Favorites.Add(path);
            BuildSidebar();
            SetStatus("★ Προστέθηκε στα Αγαπημένα!");
        }

        public void ToggleDualPane()
        {
            _settings.DualPane = !_settings.DualPane;
            _splitPanes.Panel2Collapsed = !_settings.DualPane;
            if (_settings.DualPane)
            {
                try { _splitPanes.SplitterDistance = Math.Max(220, _splitPanes.Width / 2); } catch { }
                _rightPane.Reload();
            }
            else
            {
                SetActivePane(_leftPane);
            }
            StyleTopButtons();
            SetStatus(_settings.DualPane ? "Διπλό Παράθυρο ενεργό (Tab εναλλαγή · F5 Αντιγραφή · F6 Μετακίνηση)" : "Μονό Παράθυρο");
        }

        public void ToggleHidden()
        {
            _settings.ShowHidden = !_settings.ShowHidden;
            ReloadAllPanes();
            StyleTopButtons();
            SetStatus(_settings.ShowHidden ? "Εμφάνιση κρυφών αρχείων: ΕΝΕΡΓΗ" : "Εμφάνιση κρυφών αρχείων: ΑΝΕΝΕΡΓΗ");
        }

        public void TogglePreview()
        {
            _settings.ShowPreview = !_settings.ShowPreview;
            _previewPanel.Visible = _settings.ShowPreview;
            StyleTopButtons();
            if (_settings.ShowPreview)
            {
                List<FileEntry> sel = _activePane.GetSelectedEntries();
                UpdatePreview(sel.Count > 0 ? sel[0] : null);
            }
        }

        public void ToggleTheme()
        {
            _settings.DarkTheme = !_settings.DarkTheme;
            _theme = Theme.Create(_settings.DarkTheme);
            Native.SetDarkTitlebar(Handle, _theme.Dark);
            ApplyThemeAll();
        }

        private void ApplyThemeAll()
        {
            BackColor = _theme.BgMain;
            ForeColor = _theme.FgMain;
            _header.BackColor = _theme.BgPanel;
            _lblTitle.ForeColor = _theme.FgMain;
            _lblSub.ForeColor = _theme.Accent;
            _sidebar.BackColor = _theme.BgPanel;
            _sideFlow.BackColor = _theme.BgPanel;
            _splitPanes.BackColor = _theme.Border;
            _previewPanel.BackColor = _theme.BgPanel;
            _previewPanel.ForeColor = _theme.FgMain;
            _prevText.BackColor = _theme.BgSurface;
            _prevText.ForeColor = _theme.FgMain;
            _prevMeta.ForeColor = _theme.FgMuted;
            _prevBtnOpen.BackColor = _theme.Accent;
            _prevBtnOpen.ForeColor = Color.FromArgb(20, 22, 26);
            _statusBar.BackColor = _theme.BgPanel;
            _lblStatus.ForeColor = _theme.FgMuted;
            _lnkBrand.LinkColor = _theme.FgMuted;
            _lnkBrand.ActiveLinkColor = _theme.Accent;

            StyleTopButtons();
            _leftPane.ApplyTheme(_theme);
            _rightPane.ApplyTheme(_theme);
            BuildSidebar();
        }

        private void StyleTopButtons()
        {
            _btnDual.Active = _settings.DualPane;
            _btnHidden.Active = _settings.ShowHidden;
            _btnPreview.Active = _settings.ShowPreview;
            _btnTheme.Icon = _theme.Dark ? AkIcon.Sun : AkIcon.Moon;
            _btnInfo.Kind = AkKind.AccentOutline;
            AkButton[] all = new AkButton[] { _btnDual, _btnHidden, _btnPreview, _btnTheme, _btnInfo };
            for (int i = 0; i < all.Length; i++) { all[i].Dark = _theme.Dark; all[i].Invalidate(); }
            _header.Invalidate();
        }

        // ---- Sidebar: Quick Access + Favorites + Drives with Free-Space Bars ----
        private void BuildSidebar()
        {
            _sideFlow.SuspendLayout();
            while (_sideFlow.Controls.Count > 0)
            {
                Control c = _sideFlow.Controls[0];
                _sideFlow.Controls.RemoveAt(0);
                c.Dispose();
            }

            AddSideHeader("ΓΡΗΓΟΡΗ ΠΡΟΣΒΑΣΗ");
            string user = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            AddSideLink("⌂  Επιφάνεια", Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
            AddSideLink("↓  Λήψεις", Path.Combine(user, "Downloads"));
            AddSideLink("≡  Έγγραφα", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
            AddSideLink("▣  Εικόνες", Environment.GetFolderPath(Environment.SpecialFolder.MyPictures));
            AddSideLink("▶  Βίντεο", Environment.GetFolderPath(Environment.SpecialFolder.MyVideos));
            AddSideLink("♪  Μουσική", Environment.GetFolderPath(Environment.SpecialFolder.MyMusic));
            AddSideLink("●  Χρήστης", user);

            if (_settings.Favorites.Count > 0)
            {
                AddSideHeader("ΑΓΑΠΗΜΕΝΑ ★");
                for (int i = 0; i < _settings.Favorites.Count; i++)
                {
                    string fav = _settings.Favorites[i];
                    string nm = Path.GetFileName(fav.TrimEnd('\\'));
                    if (string.IsNullOrEmpty(nm)) nm = fav;
                    AddSideLink("★  " + nm, fav);
                }
            }

            AddSideHeader("ΔΙΣΚΟΙ");
            try
            {
                DriveInfo[] drives = DriveInfo.GetDrives();
                for (int i = 0; i < drives.Length; i++)
                {
                    DriveInfo d = drives[i];
                    if ((d.DriveType != DriveType.Fixed && d.DriveType != DriveType.Removable) || !d.IsReady) continue;
                    AddDriveCard(d);
                }
            }
            catch { }

            _sideFlow.ResumeLayout();
        }

        private void AddSideHeader(string text)
        {
            Label l = new Label
            {
                Text = text,
                Width = 178,
                Height = 22,
                Font = new Font("Segoe UI", 7.6f, FontStyle.Bold),
                ForeColor = _theme.FgMuted,
                Padding = new Padding(4, 6, 0, 0),
                Margin = new Padding(0, 4, 0, 2)
            };
            _sideFlow.Controls.Add(l);
        }

        private void AddSideLink(string label, string path)
        {
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path)) return;
            Button b = new Button
            {
                Text = label,
                Width = 180,
                Height = 27,
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = _fontUi,
                BackColor = _theme.BgPanel,
                ForeColor = _theme.FgMain,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 1, 0, 1),
                Padding = new Padding(6, 0, 4, 0),
                TabStop = false
            };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = _theme.BgHover;
            b.Click += delegate { _activePane.Navigate(path, true); _activePane.FocusList(); };
            _tip.SetToolTip(b, path);
            _sideFlow.Controls.Add(b);
        }

        private void AddDriveCard(DriveInfo d)
        {
            string root = d.RootDirectory.FullName;
            string vol = string.IsNullOrEmpty(d.VolumeLabel) ? "Τοπικός Δίσκος" : d.VolumeLabel;
            double totalGb = d.TotalSize / (1024.0 * 1024.0 * 1024.0);
            double freeGb = d.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0);
            double usedFrac = d.TotalSize > 0 ? Math.Max(0.0, Math.Min(1.0, (double)(d.TotalSize - d.AvailableFreeSpace) / d.TotalSize)) : 0.0;

            Panel card = new Panel
            {
                Width = 180,
                Height = 46,
                BackColor = _theme.BgSurface,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 2, 0, 3)
            };
            card.Paint += delegate(object s, PaintEventArgs e)
            {
                Graphics g = e.Graphics;
                using (Pen p = new Pen(_theme.Border)) g.DrawRectangle(p, 0, 0, card.Width - 1, card.Height - 1);
                TextRenderer.DrawText(g, string.Format("{0} ({1})", vol, root.TrimEnd('\\')), _fontBold,
                    new Rectangle(8, 4, card.Width - 16, 17), _theme.FgMain, TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
                TextRenderer.DrawText(g, string.Format("{0:0.0} GB ελεύθ. / {1:0.0} GB", freeGb, totalGb), _fontDriveSub,
                    new Rectangle(8, 20, card.Width - 16, 14), _theme.FgMuted, TextFormatFlags.Left | TextFormatFlags.EndEllipsis);

                Rectangle barBg = new Rectangle(8, 36, card.Width - 16, 4);
                using (SolidBrush brBg = new SolidBrush(_theme.BgPanel)) g.FillRectangle(brBg, barBg);
                Color barColor = usedFrac > 0.90 ? _theme.Pink : (usedFrac > 0.78 ? _theme.Accent : _theme.Cyan);
                int fillW = (int)Math.Round(barBg.Width * usedFrac);
                if (fillW > 0)
                    using (SolidBrush brFill = new SolidBrush(barColor)) g.FillRectangle(brFill, barBg.X, barBg.Y, fillW, barBg.Height);
            };
            card.Click += delegate { _activePane.Navigate(root, true); _activePane.FocusList(); };
            card.MouseEnter += delegate { card.BackColor = _theme.BgHover; };
            card.MouseLeave += delegate { card.BackColor = _theme.BgSurface; };
            _sideFlow.Controls.Add(card);
        }

        // ---- On-Demand Zero-Lock Preview Panel ----
        public void UpdatePreview(FileEntry entry)
        {
            _previewEntry = entry;
            if (!_settings.ShowPreview) return;

            if (_prevImg.Image != null)
            {
                Image old = _prevImg.Image;
                _prevImg.Image = null;
                old.Dispose();
            }
            _prevImg.Visible = false;
            _prevText.Visible = false;
            _prevBtnOpen.Visible = (entry != null && !entry.IsDir);

            if (entry == null)
            {
                _prevMeta.Text = "Επίλεξε ένα αρχείο για γρήγορη προεπισκόπηση.\n\n(0 MB overhead όταν είναι κλειστή)";
                return;
            }

            string extra = "";
            if (!entry.IsDir && Fmt.IsImageExt(entry.Ext) && entry.Size <= 25 * 1024 * 1024)
            {
                try
                {
                    byte[] raw = File.ReadAllBytes(entry.FullPath);
                    using (MemoryStream ms = new MemoryStream(raw))
                    using (Image img = Image.FromStream(ms))
                    {
                        extra = string.Format("Διαστάσεις: {0} × {1} px\n", img.Width, img.Height);
                        Bitmap thumb = new Bitmap(Math.Min(240, img.Width), Math.Min(180, img.Height));
                        using (Graphics g = Graphics.FromImage(thumb))
                        {
                            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            g.DrawImage(img, 0, 0, thumb.Width, thumb.Height);
                        }
                        _prevImg.Image = thumb;
                        _prevImg.Visible = true;
                    }
                }
                catch { }
            }
            else if (!entry.IsDir && Fmt.IsTextExt(entry.Ext))
            {
                try
                {
                    using (FileStream fs = new FileStream(entry.FullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        int cap = (int)Math.Min(4096, fs.Length);
                        byte[] buf = new byte[cap];
                        int n = fs.Read(buf, 0, cap);
                        _prevText.Text = Encoding.UTF8.GetString(buf, 0, n);
                        _prevText.Visible = true;
                    }
                }
                catch { }
            }

            _prevMeta.Text = string.Format("{0}\n\nΤύπος: {1}\n{2}Μέγεθος: {3}\nΤροποποίηση: {4}\n\nΔιαδρομή:\n{5}",
                entry.Name,
                entry.TypeLabel,
                extra,
                entry.IsDir ? "Φάκελος" : Fmt.Bytes(entry.Size),
                entry.DateText,
                entry.FullPath);
        }

        // ---- Ultra-Light Specs & Info Dialog ('i') ----
        public void ShowInfoDialog()
        {
            double exeKb = 0;
            try
            {
                string exePath = Assembly.GetExecutingAssembly().Location;
                if (File.Exists(exePath)) exeKb = new FileInfo(exePath).Length / 1024.0;
            }
            catch { }
            long ramMb = Process.GetCurrentProcess().WorkingSet64 / (1024 * 1024);

            using (Form dlg = new Form())
            {
                dlg.Text = "NillyFiles Pro — Ultra-Light Specs & Πληροφορίες";
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.ClientSize = new Size(510, 445);
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;
                dlg.BackColor = _theme.BgMain;
                dlg.ForeColor = _theme.FgMain;
                Native.SetDarkTitlebar(dlg.Handle, _theme.Dark);

                PictureBox pic = new PictureBox { Size = new Size(42, 42), Location = new Point(18, 16), SizeMode = PictureBoxSizeMode.Zoom, Image = _logoBmp };
                Label hdr = new Label { Text = "NillyFiles Pro v1.2.2", Location = new Point(70, 16), AutoSize = true, Font = new Font("Segoe UI", 13.0f, FontStyle.Bold), ForeColor = _theme.FgMain };
                Label sub = new Label { Text = "Ultra-Light Native Windows 10/11 Tabbed & Dual-Pane File Explorer", Location = new Point(72, 42), AutoSize = true, Font = _fontSmall, ForeColor = _theme.Accent, UseMnemonic = false };

                Panel card = new Panel { Location = new Point(18, 72), Size = new Size(474, 170), BackColor = _theme.BgPanel };
                Label specs = new Label
                {
                    Location = new Point(12, 10),
                    Size = new Size(450, 152),
                    Font = _fontUi,
                    ForeColor = _theme.FgMain,
                    Text = string.Format(
                        "• Μέγεθος EXE στον δίσκο:  {0:0.0} KB  (0 εξωτερικά DLL · 0 WebView2 · 0 Electron)\n" +
                        "• Τρέχουσα RAM ανοιχτό:  ~{1} MB  |  Κλειστό:  0 MB RAM & 0% CPU\n" +
                        "• Ταχύτητα:  Win32 FindFirstFileExW + Virtual ListView (10.000+ αρχεία σε ms)\n" +
                        "• Σύγκριση vs Win 11 Explorer:  Χωρίς XAML/WinUI lag, χωρίς cloud/AI hooks\n" +
                        "• 100% Ασφαλές:  ΔΕΝ απεγκαθιστά ούτε πειράζει το Windows explorer.exe\n" +
                        "• Διαγραφή:  Ασφαλής Κάδος Ανακύκλωσης (Recycle Bin) · Zero Admin",
                        exeKb, ramMb)
                };
                card.Controls.Add(specs);

                Label keysHdr = new Label { Text = "ΒΑΣΙΚΕΣ ΣΥΝΤΟΜΕΥΣΕΙΣ", Location = new Point(18, 254), AutoSize = true, Font = new Font("Segoe UI", 8.2f, FontStyle.Bold), ForeColor = _theme.Cyan };
                Label keysTxt = new Label
                {
                    Location = new Point(18, 274),
                    Size = new Size(474, 92),
                    Font = _fontSmall,
                    ForeColor = _theme.FgMuted,
                    Text = "Ctrl+T  Νέα καρτέλα    ·    Ctrl+W  Κλείσιμο καρτέλας    ·    F3  Διπλό παράθυρο (Dual-Pane)\n" +
                           "Tab  Εναλλαγή παραθύρου    ·    F5 / F6  Αντιγραφή / Μετακίνηση στο άλλο παράθυρο\n" +
                           "Ctrl+L  Διαδρομή    ·    Ctrl+F  Άμεσο φίλτρο    ·    F2  Μετονομασία    ·    F7  Νέος φάκελος\n" +
                           "Ctrl+H  Κρυφά αρχεία    ·    Alt+P  Προεπισκόπηση    ·    Alt+Enter  Ιδιότητες"
                };

                LinkLabel lnkSite = new LinkLabel { Text = "created by Nilly graphics (Sotiris Christos) — https://nillygraphics.com/", Location = new Point(18, 376), AutoSize = true, Font = _fontSmall, LinkColor = _theme.Accent };
                lnkSite.LinkClicked += delegate { OpenUrl("https://nillygraphics.com/"); };

                LinkLabel lnkGit = new LinkLabel { Text = "GitHub (MIT Open Source · 0€): https://github.com/nillynp/nillyfiles-pro", Location = new Point(18, 396), AutoSize = true, Font = _fontSmall, LinkColor = _theme.Cyan };
                lnkGit.LinkClicked += delegate { OpenUrl("https://github.com/nillynp/nillyfiles-pro"); };

                Button btnClose = new Button
                {
                    Text = "Κλείσιμο",
                    DialogResult = DialogResult.OK,
                    Location = new Point(404, 398),
                    Size = new Size(88, 30),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = _theme.Accent,
                    ForeColor = Color.FromArgb(20, 22, 26),
                    Font = _fontBold
                };
                btnClose.FlatAppearance.BorderSize = 0;

                dlg.Controls.AddRange(new Control[] { pic, hdr, sub, card, keysHdr, keysTxt, lnkSite, lnkGit, btnClose });
                dlg.AcceptButton = btnClose;
                dlg.CancelButton = btnClose;
                dlg.ShowDialog(this);
            }
        }

        private static void OpenUrl(string url)
        {
            try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); } catch { }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.T)) { _activePane.AddNewTab(_activePane.CurrentPath, true); return true; }
            if (keyData == (Keys.Control | Keys.W)) { _activePane.CloseCurrentTab(); return true; }
            if (keyData == (Keys.Control | Keys.L) || keyData == (Keys.Alt | Keys.D)) { _activePane.FocusAddress(); return true; }
            if (keyData == (Keys.Control | Keys.F)) { _activePane.FocusFilter(); return true; }
            if (keyData == (Keys.Control | Keys.H)) { ToggleHidden(); return true; }
            if (keyData == (Keys.Alt | Keys.P)) { TogglePreview(); return true; }
            if (keyData == Keys.F3) { ToggleDualPane(); return true; }
            if (keyData == Keys.F5)
            {
                if (_settings.DualPane && _activePane.GetSelectedPaths().Count > 0) TransferToOtherPane(false);
                else ReloadAllPanes();
                return true;
            }
            if (keyData == Keys.F6 && _settings.DualPane) { TransferToOtherPane(true); return true; }
            if (keyData == Keys.Tab && _settings.DualPane && !(ActiveControl is TextBox))
            {
                FilePane other = GetOtherPane();
                SetActivePane(other);
                other.FocusList();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_logoBmp != null) { _logoBmp.Dispose(); _logoBmp = null; }
                if (_prevImg != null && _prevImg.Image != null) { _prevImg.Image.Dispose(); _prevImg.Image = null; }
                _fontTitle.Dispose();
                _fontUi.Dispose();
                _fontBold.Dispose();
                _fontSmall.Dispose();
                _fontDriveSub.Dispose();
                _tip.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    // ========================================================================
    // 8. ENTRY POINT & AUTOMATED SMOKE TEST
    // ========================================================================
    internal static class Program
    {
        [STAThread]
        public static int Main(string[] args)
        {
            try { Native.SetProcessDPIAware(); } catch { }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (args != null && args.Length > 0 && args[0].Equals("--test-smoke", StringComparison.OrdinalIgnoreCase))
            {
                string logPath = args.Length > 1 ? args[1] : null;
                string pngPath = args.Length > 2 ? args[2] : null;
                string testDir = args.Length > 3 ? args[3] : null;
                return RunSmokeTest(logPath, pngPath, testDir);
            }

            string startPath = (args != null && args.Length > 0 && Directory.Exists(args[0])) ? args[0] : null;
            Application.Run(SuiteUpdater.Attach(new MainForm(startPath, false), "nillyfiles-pro", "NillyFiles-Pro", "Install_NillyFilesPro.ps1"));
            return 0;
        }

        private static int RunSmokeTest(string logPath, string pngPath, string testDir)
        {
            StringBuilder sb = new StringBuilder();
            try
            {
                string dir = (!string.IsNullOrEmpty(testDir) && Directory.Exists(testDir))
                    ? testDir
                    : Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                using (MainForm f = new MainForm(dir, true))
                {
                    f.Show();
                    Application.DoEvents();
                    long idleRamMb = Process.GetCurrentProcess().WorkingSet64 / (1024 * 1024);
                    int singleCount = f.LeftPane.VisibleCount;
                    long scanMs = f.LeftPane.LastScanMs;

                    if (!string.IsNullOrEmpty(pngPath))
                    {
                        using (Bitmap shot = new Bitmap(f.Width, f.Height))
                        {
                            f.DrawToBitmap(shot, new Rectangle(0, 0, f.Width, f.Height));
                            shot.Save(pngPath, ImageFormat.Png);
                        }
                    }

                    // Exercise Dual-Pane + Tab creation
                    f.ToggleDualPane();
                    f.LeftPane.AddNewTab(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), true);
                    Application.DoEvents();

                    if (!string.IsNullOrEmpty(pngPath))
                    {
                        string dualPng = Path.Combine(Path.GetDirectoryName(pngPath), Path.GetFileNameWithoutExtension(pngPath) + "_dual.png");
                        using (Bitmap shot2 = new Bitmap(f.Width, f.Height))
                        {
                            f.DrawToBitmap(shot2, new Rectangle(0, 0, f.Width, f.Height));
                            shot2.Save(dualPng, ImageFormat.Png);
                        }
                    }

                    double exeKb = new FileInfo(Assembly.GetExecutingAssembly().Location).Length / 1024.0;
                    bool pass = (singleCount > 0 && f.IsHandleCreated && f.IsDualPane && exeKb < 150.0);

                    sb.AppendLine("ITEM_COUNT=" + singleCount);
                    sb.AppendLine("SCAN_MS=" + scanMs);
                    sb.AppendLine("EXE_KB=" + exeKb.ToString("0.0"));
                    sb.AppendLine("IDLE_WORKING_SET_MB=" + idleRamMb);
                    sb.AppendLine("DUAL_PANE_OK=" + f.IsDualPane);
                    sb.AppendLine("WINDOW_OPENED_OK=" + f.IsHandleCreated);
                    sb.AppendLine("SMOKE_RESULT=" + (pass ? "PASS" : "FAIL"));
                    f.Close();
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine("SMOKE_RESULT=FAIL");
                sb.AppendLine(ex.ToString());
                if (!string.IsNullOrEmpty(logPath)) File.WriteAllText(logPath, sb.ToString(), Encoding.UTF8);
                return 1;
            }
            if (!string.IsNullOrEmpty(logPath)) File.WriteAllText(logPath, sb.ToString(), Encoding.UTF8);
            return 0;
        }
    }

    // ========================================================================
    // BUILT-IN ULTRA-LIGHT AUTO-UPDATER (1x/24h shared CDN check · SHA-256)
    // ========================================================================
    internal static class SuiteUpdater
    {
        private const string CATALOG_URL = "https://raw.githubusercontent.com/nillynp/nillyhub/main/catalog.json";

        public static T Attach<T>(T form, string repoName, string pkgPrefix, string installerScript) where T : System.Windows.Forms.Form
        {
            if (form == null) return null;
            form.Shown += delegate
            {
                System.Threading.Thread th = new System.Threading.Thread(delegate()
                {
                    try
                    {
                        System.Threading.Thread.Sleep(2200);
                        if (form.IsDisposed) return;
                        string curTag, newTag;
                        if (!CheckForUpdate(repoName, out curTag, out newTag)) return;
                        if (form.IsDisposed || !form.IsHandleCreated) return;
                        form.BeginInvoke((System.Action)delegate
                        {
                            if (form.IsDisposed || form.FormBorderStyle == System.Windows.Forms.FormBorderStyle.None) return;
                            ShowUpdateBanner(form, repoName, pkgPrefix, installerScript, curTag, newTag);
                        });
                    }
                    catch { }
                });
                th.IsBackground = true;
                th.Start();
            };
            return form;
        }

        private static void ShowUpdateBanner(System.Windows.Forms.Form form, string repoName, string pkgPrefix, string installerScript, string curTag, string newTag)
        {
            System.Drawing.Font fTxt = new System.Drawing.Font("Segoe UI", 8.5f, System.Drawing.FontStyle.Regular);
            System.Drawing.Font fBtn = new System.Drawing.Font("Segoe UI", 8.2f, System.Drawing.FontStyle.Bold);

            System.Windows.Forms.Panel bar = new System.Windows.Forms.Panel();
            bar.Dock = System.Windows.Forms.DockStyle.Top;
            bar.Height = 28;
            bar.BackColor = System.Drawing.Color.FromArgb(40, 29, 18);
            bar.Padding = new System.Windows.Forms.Padding(10, 0, 8, 1);
            bar.Paint += delegate(object s, System.Windows.Forms.PaintEventArgs e)
            {
                using (System.Drawing.Pen p = new System.Drawing.Pen(System.Drawing.Color.FromArgb(252, 150, 31)))
                    e.Graphics.DrawLine(p, 0, bar.Height - 1, bar.Width, bar.Height - 1);
            };

            System.Windows.Forms.Button btnClose = new System.Windows.Forms.Button();
            btnClose.Text = "×";
            btnClose.Dock = System.Windows.Forms.DockStyle.Right;
            btnClose.Width = 26;
            btnClose.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.BackColor = System.Drawing.Color.Transparent;
            btnClose.ForeColor = System.Drawing.Color.FromArgb(200, 170, 130);
            btnClose.Font = fBtn;
            btnClose.Cursor = System.Windows.Forms.Cursors.Hand;
            btnClose.TabStop = false;
            btnClose.Click += delegate { bar.Dispose(); };

            System.Windows.Forms.Button btnUpd = new System.Windows.Forms.Button();
            btnUpd.Text = "Ενημέρωση";
            btnUpd.Dock = System.Windows.Forms.DockStyle.Right;
            btnUpd.Width = 88;
            btnUpd.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btnUpd.FlatAppearance.BorderSize = 0;
            btnUpd.BackColor = System.Drawing.Color.FromArgb(252, 150, 31);
            btnUpd.ForeColor = System.Drawing.Color.FromArgb(20, 22, 26);
            btnUpd.Font = fBtn;
            btnUpd.Cursor = System.Windows.Forms.Cursors.Hand;
            btnUpd.TabStop = false;

            System.Windows.Forms.Label lbl = new System.Windows.Forms.Label();
            lbl.Dock = System.Windows.Forms.DockStyle.Fill;
            lbl.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            lbl.Font = fTxt;
            lbl.ForeColor = System.Drawing.Color.FromArgb(255, 185, 95);
            lbl.UseMnemonic = false;
            lbl.Text = "Νέα έκδοση " + newTag + " διαθέσιμη (έχεις " + curTag + ")";

            btnUpd.Click += delegate
            {
                btnUpd.Enabled = false;
                btnUpd.Text = "Λήψη...";
                System.Threading.Thread uth = new System.Threading.Thread(delegate()
                {
                    try
                    {
                        ApplySelfUpdate(repoName, pkgPrefix, installerScript,
                            delegate(string st)
                            {
                                try { if (!form.IsDisposed) form.BeginInvoke((System.Action)delegate { lbl.Text = st; }); } catch { }
                            },
                            delegate
                            {
                                try { if (!form.IsDisposed) form.BeginInvoke((System.Action)delegate { System.Windows.Forms.Application.Exit(); }); } catch { }
                            });
                    }
                    catch (System.Exception ex)
                    {
                        try
                        {
                            if (!form.IsDisposed) form.BeginInvoke((System.Action)delegate
                            {
                                lbl.Text = "Σφάλμα ενημέρωσης: " + ex.Message;
                                btnUpd.Text = "Ενημέρωση";
                                btnUpd.Enabled = true;
                            });
                        }
                        catch { }
                    }
                });
                uth.IsBackground = true;
                uth.Start();
            };

            bar.Disposed += delegate { fTxt.Dispose(); fBtn.Dispose(); };
            bar.Controls.Add(lbl);
            bar.Controls.Add(btnUpd);
            bar.Controls.Add(btnClose);
            form.Controls.Add(bar);
        }

        private static bool TryParseVer(string raw, out System.Version ver)
        {
            ver = null;
            if (string.IsNullOrEmpty(raw)) return false;
            string s = raw.Trim();
            if (s.StartsWith("v", System.StringComparison.OrdinalIgnoreCase)) s = s.Substring(1);
            string[] p = s.Split('.');
            if (p.Length < 2 || p.Length > 4) return false;
            int maj, min, pat = 0;
            if (!int.TryParse(p[0], System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out maj)) return false;
            if (!int.TryParse(p[1], System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out min)) return false;
            if (p.Length >= 3 && !int.TryParse(p[2], System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out pat)) return false;
            ver = new System.Version(maj, min, pat, 0);
            return true;
        }

        private static string FmtVer(System.Version v)
        {
            return v.Build > 0
                ? string.Format(System.Globalization.CultureInfo.InvariantCulture, "v{0}.{1}.{2}", v.Major, v.Minor, v.Build)
                : string.Format(System.Globalization.CultureInfo.InvariantCulture, "v{0}.{1}", v.Major, v.Minor);
        }

        public static bool CheckForUpdate(string repoName, out string curTag, out string newTag)
        {
            curTag = null; newTag = null;
            System.Version rawCur = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            System.Version curVer = new System.Version(rawCur.Major, rawCur.Minor, rawCur.Build >= 0 ? rawCur.Build : 0, 0);
            curTag = FmtVer(curVer);

            string suiteDir = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "NillySuite");
            string cachePath = System.IO.Path.Combine(suiteDir, ".nillyhub_catalog.json");
            string json = null;
            try
            {
                if (System.IO.File.Exists(cachePath))
                {
                    System.DateTime wt = System.IO.File.GetLastWriteTimeUtc(cachePath);
                    if ((System.DateTime.UtcNow - wt).TotalHours < 24.0)
                    {
                        string cached = System.IO.File.ReadAllText(cachePath, System.Text.Encoding.UTF8);
                        if (cached.IndexOf("\"latestVersion\"", System.StringComparison.Ordinal) >= 0) json = cached;
                    }
                }
            }
            catch { }

            if (string.IsNullOrEmpty(json))
            {
                json = HttpGetText(CATALOG_URL, 5000);
                if (string.IsNullOrEmpty(json) || json.Length > 256 * 1024) return false;
                try
                {
                    System.IO.Directory.CreateDirectory(suiteDir);
                    System.IO.File.WriteAllText(cachePath, json, new System.Text.UTF8Encoding(false));
                }
                catch { }
            }

            System.Text.RegularExpressions.Match m = System.Text.RegularExpressions.Regex.Match(
                json,
                "\"repo\"\\s*:\\s*\"" + System.Text.RegularExpressions.Regex.Escape(repoName) + "\"[^\\}]{0,500}?\"latestVersion\"\\s*:\\s*\"(v[0-9]+\\.[0-9]+(?:\\.[0-9]+)?)\"",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (!m.Success) return false;
            System.Version remVer;
            if (!TryParseVer(m.Groups[1].Value, out remVer) || remVer <= curVer) return false;
            newTag = FmtVer(remVer);
            return true;
        }

        private static string HttpGetText(string url, int timeoutMs)
        {
            try { System.Net.ServicePointManager.SecurityProtocol |= (System.Net.SecurityProtocolType)3072 | System.Net.SecurityProtocolType.Tls12; } catch { }
            System.Net.HttpWebRequest req = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(url);
            req.UserAgent = "NillySuite-Updater/1.0 (https://nillygraphics.com/)";
            req.Accept = "application/vnd.github+json, application/json";
            req.AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate;
            req.Timeout = timeoutMs;
            req.ReadWriteTimeout = timeoutMs;
            using (System.Net.HttpWebResponse resp = (System.Net.HttpWebResponse)req.GetResponse())
            using (System.IO.Stream st = resp.GetResponseStream())
            using (System.IO.StreamReader sr = new System.IO.StreamReader(st, System.Text.Encoding.UTF8))
                return sr.ReadToEnd();
        }

        private static string ExtractAssetBlock(string json, string assetName)
        {
            System.Text.RegularExpressions.Match m = System.Text.RegularExpressions.Regex.Match(
                json, "\"name\"\\s*:\\s*\"" + System.Text.RegularExpressions.Regex.Escape(assetName) + "\"");
            if (!m.Success) return null;
            int start = m.Index;
            int depth = 0;
            while (start > 0)
            {
                char c = json[start];
                if (c == '}') depth++;
                else if (c == '{') { if (depth == 0) break; depth--; }
                start--;
            }
            int end = start;
            depth = 0;
            bool inStr = false;
            for (int i = start; i < json.Length; i++)
            {
                char c = json[i];
                if (c == '"' && (i == 0 || json[i - 1] != '\\')) inStr = !inStr;
                if (inStr) continue;
                if (c == '{') depth++;
                else if (c == '}') { depth--; if (depth == 0) { end = i; break; } }
            }
            return end > start ? json.Substring(start, end - start + 1) : null;
        }

        public static void ApplySelfUpdate(string repoName, string pkgPrefix, string installerScript, System.Action<string> onStatus, System.Action onExit)
        {
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string appDir = System.IO.Path.GetDirectoryName(exePath);
            if (System.IO.Directory.Exists(System.IO.Path.Combine(appDir, ".git")))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://github.com/nillynp/" + repoName + "/releases/latest") { UseShellExecute = true });
                if (onStatus != null) onStatus("Άνοιξε το GitHub Release (φάκελος Git).");
                return;
            }

            if (onStatus != null) onStatus("Έλεγχος SHA-256 στο GitHub...");
            string relJson = HttpGetText("https://api.github.com/repos/nillynp/" + repoName + "/releases/latest", 12000);
            if (string.IsNullOrEmpty(relJson)) throw new System.IO.InvalidDataException("Κενή απόκριση GitHub.");
            if (System.Text.RegularExpressions.Regex.IsMatch(relJson, "\"draft\"\\s*:\\s*true") ||
                System.Text.RegularExpressions.Regex.IsMatch(relJson, "\"prerelease\"\\s*:\\s*true"))
                throw new System.IO.InvalidDataException("Μη σταθερή έκδοση.");

            System.Text.RegularExpressions.Match tm = System.Text.RegularExpressions.Regex.Match(relJson, "\"tag_name\"\\s*:\\s*\"(v[0-9]+\\.[0-9]+(?:\\.[0-9]+)?)\"");
            if (!tm.Success) throw new System.IO.InvalidDataException("Μη έγκυρο tag έκδοσης.");
            string tag = tm.Groups[1].Value;

            System.Version curRaw = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            System.Version curVer = new System.Version(curRaw.Major, curRaw.Minor, curRaw.Build >= 0 ? curRaw.Build : 0, 0);
            System.Version remVer;
            if (!TryParseVer(tag, out remVer) || remVer <= curVer)
                throw new System.InvalidOperationException("Έχεις ήδη την τελευταία έκδοση.");

            string zipName = pkgPrefix + "-" + tag + "-Portable.zip";
            string expectedUrl = "https://github.com/nillynp/" + repoName + "/releases/download/" + tag + "/" + zipName;
            string blk = ExtractAssetBlock(relJson, zipName);
            if (string.IsNullOrEmpty(blk)) throw new System.IO.FileNotFoundException("Δεν βρέθηκε το Portable ZIP.");
            if (!System.Text.RegularExpressions.Regex.IsMatch(blk, "\"state\"\\s*:\\s*\"uploaded\""))
                throw new System.IO.InvalidDataException("Το αρχείο δεν είναι έτοιμο.");
            System.Text.RegularExpressions.Match um = System.Text.RegularExpressions.Regex.Match(blk, "\"browser_download_url\"\\s*:\\s*\"([^\"]+)\"");
            if (!um.Success || !string.Equals(um.Groups[1].Value, expectedUrl, System.StringComparison.Ordinal))
                throw new System.IO.InvalidDataException("Μη έγκυρο URL λήψης.");
            System.Text.RegularExpressions.Match dm = System.Text.RegularExpressions.Regex.Match(blk, "\"digest\"\\s*:\\s*\"sha256:([0-9a-f]{64})\"");
            if (!dm.Success) throw new System.IO.InvalidDataException("Απουσιάζει SHA-256.");
            string expectedSha = dm.Groups[1].Value;

            string tmpDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "NillyUpd-" + System.Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(tmpDir);
            string zipFile = System.IO.Path.Combine(tmpDir, "pkg.zip");

            if (onStatus != null) onStatus("Λήψη " + tag + "...");
            try { System.Net.ServicePointManager.SecurityProtocol |= (System.Net.SecurityProtocolType)3072 | System.Net.SecurityProtocolType.Tls12; } catch { }
            System.Net.HttpWebRequest dreq = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(expectedUrl);
            dreq.UserAgent = "NillySuite-Updater/1.0 (https://nillygraphics.com/)";
            dreq.Timeout = 20000;
            dreq.ReadWriteTimeout = 30000;
            using (System.Net.HttpWebResponse dresp = (System.Net.HttpWebResponse)dreq.GetResponse())
            using (System.IO.Stream src = dresp.GetResponseStream())
            using (System.IO.FileStream dst = new System.IO.FileStream(zipFile, System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.None))
            {
                byte[] buf = new byte[16384];
                int n; long total = 0;
                while ((n = src.Read(buf, 0, buf.Length)) > 0)
                {
                    total += n;
                    if (total > 32L * 1024L * 1024L) throw new System.IO.InvalidDataException("Υπέρβαση μεγέθους ZIP.");
                    dst.Write(buf, 0, n);
                }
            }

            if (onStatus != null) onStatus("Επαλήθευση SHA-256...");
            string actualSha;
            using (System.Security.Cryptography.SHA256 sha = System.Security.Cryptography.SHA256.Create())
            using (System.IO.FileStream fs = System.IO.File.OpenRead(zipFile))
            {
                byte[] hash = sha.ComputeHash(fs);
                System.Text.StringBuilder sb = new System.Text.StringBuilder(64);
                for (int i = 0; i < hash.Length; i++) sb.Append(hash[i].ToString("x2", System.Globalization.CultureInfo.InvariantCulture));
                actualSha = sb.ToString();
            }
            if (!string.Equals(actualSha, expectedSha, System.StringComparison.OrdinalIgnoreCase))
            {
                try { System.IO.Directory.Delete(tmpDir, true); } catch { }
                throw new System.IO.InvalidDataException("Αποτυχία ελέγχου SHA-256.");
            }

            if (onStatus != null) onStatus("Εγκατάσταση " + tag + " & επανεκκίνηση...");
            string ps1 = System.IO.Path.Combine(tmpDir, "apply.ps1");
            string script =
                "param($myPid,$zip,$appDir,$instName,$exePath,$tag)\r\n" +
                "for($i=0;$i -lt 60;$i++){ if(-not (Get-Process -Id $myPid -ErrorAction SilentlyContinue)){break}; Start-Sleep -Milliseconds 150 }\r\n" +
                "Add-Type -AssemblyName System.IO.Compression.FileSystem\r\n" +
                "$za = [System.IO.Compression.ZipFile]::OpenRead($zip)\r\n" +
                "try {\r\n" +
                "  $pref = $null; $shared = $true\r\n" +
                "  foreach($e in $za.Entries){ $fn=$e.FullName -replace '\\\\','/'; $sl=$fn.IndexOf('/'); if($sl -le 0){$shared=$false;break}; $p=$fn.Substring(0,$sl+1); if(-not $pref){$pref=$p}elseif(-not $pref.Equals($p,[StringComparison]::OrdinalIgnoreCase)){$shared=$false;break} }\r\n" +
                "  $rootFull = [System.IO.Path]::GetFullPath($appDir).TrimEnd('\\') + '\\'\r\n" +
                "  foreach($e in $za.Entries){\r\n" +
                "    $rel = $e.FullName -replace '\\\\','/'\r\n" +
                "    if($shared -and $pref -and $rel.StartsWith($pref,[StringComparison]::OrdinalIgnoreCase)){ $rel=$rel.Substring($pref.Length) }\r\n" +
                "    if([string]::IsNullOrEmpty($rel) -or $rel.EndsWith('/') -or $rel.Contains(':')){ continue }\r\n" +
                "    $dst = [System.IO.Path]::GetFullPath((Join-Path $appDir ($rel -replace '/','\\')))\r\n" +
                "    if(-not $dst.StartsWith($rootFull,[StringComparison]::OrdinalIgnoreCase)){ continue }\r\n" +
                "    $par = Split-Path -Parent $dst; if($par){ New-Item -ItemType Directory -Force -Path $par | Out-Null }\r\n" +
                "    for($k=0;$k -lt 5;$k++){ try { [System.IO.Compression.ZipFileExtensions]::ExtractToFile($e,$dst,$true); break } catch { Start-Sleep -Milliseconds 250 } }\r\n" +
                "  }\r\n" +
                "} finally { $za.Dispose() }\r\n" +
                "$bak = $exePath + '.bak'\r\n" +
                "if(Test-Path $exePath){ Copy-Item -LiteralPath $exePath -Destination $bak -Force -ErrorAction SilentlyContinue; for($k=0;$k -lt 5;$k++){ try { Remove-Item -LiteralPath $exePath -Force -ErrorAction Stop; break } catch { Start-Sleep -Milliseconds 250 } } }\r\n" +
                "Get-ChildItem (Join-Path $appDir 'desktop') -Filter '*.cs' -ErrorAction SilentlyContinue | ForEach-Object { $_.LastWriteTimeUtc = [DateTime]::UtcNow }\r\n" +
                "if($instName -and (Test-Path (Join-Path $appDir $instName))){ & (Join-Path $appDir $instName) -Silent 2>$null | Out-Null }\r\n" +
                "if(-not (Test-Path $exePath) -and (Test-Path $bak)){ Move-Item -LiteralPath $bak -Destination $exePath -Force -ErrorAction SilentlyContinue } else { Remove-Item -LiteralPath $bak -Force -ErrorAction SilentlyContinue }\r\n" +
                "if($tag){ Set-Content -LiteralPath (Join-Path $appDir '.nilly_version') -Value $tag -Encoding UTF8 -ErrorAction SilentlyContinue }\r\n" +
                "if(Test-Path $exePath){ Start-Process -FilePath $exePath -WorkingDirectory $appDir }\r\n" +
                "Start-Sleep -Milliseconds 400\r\n" +
                "Remove-Item -LiteralPath (Split-Path $zip) -Recurse -Force -ErrorAction SilentlyContinue\r\n";
            System.IO.File.WriteAllText(ps1, script, new System.Text.UTF8Encoding(false));

            int myPid = System.Diagnostics.Process.GetCurrentProcess().Id;
            System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo(
                "powershell.exe",
                "-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File \"" + ps1 + "\" " +
                myPid + " \"" + zipFile + "\" \"" + appDir + "\" \"" + installerScript + "\" \"" + exePath + "\" \"" + tag + "\"");
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            System.Diagnostics.Process.Start(psi);
            if (onExit != null) onExit();
        }
    }

    // ========================================================================
    // ARIAKIT-INSPIRED MINIMAL VECTOR UI KIT (Zero DLLs · 1.5px Stroke Icons)
    // ========================================================================
    internal enum AkKind { Primary, Secondary, Ghost, Tab, AccentOutline, Danger }

    internal enum AkIcon
    {
        None, Refresh, Sun, Moon, Search, Pin, Info, Play, Pause, Stop,
        Plus, Minus, Close, Up, Down, Folder, Copy, Trash, Check, Swap,
        Download, Link, Split, Pipette, Bolt, List, Filter, Shield, Lock, Unlock,
        Edit, Save, External, Bold, Italic, Undo, ChevLeft, ChevRight, Star
    }

    internal static class AkDraw
    {
        public static System.Drawing.Drawing2D.GraphicsPath RoundRect(float x, float y, float w, float h, float r)
        {
            System.Drawing.Drawing2D.GraphicsPath p = new System.Drawing.Drawing2D.GraphicsPath();
            float d = System.Math.Min(r * 2f, System.Math.Min(w, h));
            if (d <= 0.5f) { p.AddRectangle(new System.Drawing.RectangleF(x, y, w, h)); return p; }
            p.AddArc(x, y, d, d, 180, 90);
            p.AddArc(x + w - d, y, d, d, 270, 90);
            p.AddArc(x + w - d, y + h - d, d, d, 0, 90);
            p.AddArc(x, y + h - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        public static void DrawIcon(System.Drawing.Graphics g, AkIcon icon, float cx, float cy, float sz, System.Drawing.Color color)
        {
            if (icon == AkIcon.None) return;
            System.Drawing.Drawing2D.SmoothingMode oldSm = g.SmoothingMode;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            float r = sz * 0.5f;
            using (System.Drawing.Pen pen = new System.Drawing.Pen(color, 1.5f))
            using (System.Drawing.SolidBrush br = new System.Drawing.SolidBrush(color))
            {
                pen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                pen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
                pen.LineJoin = System.Drawing.Drawing2D.LineJoin.Round;
                switch (icon)
                {
                    case AkIcon.Refresh:
                        g.DrawArc(pen, cx - r * 0.82f, cy - r * 0.82f, r * 1.64f, r * 1.64f, -55, 295);
                        g.DrawLine(pen, cx + r * 0.28f, cy - r * 0.85f, cx + r * 0.62f, cy - r * 0.52f);
                        g.DrawLine(pen, cx + r * 0.62f, cy - r * 0.52f, cx + r * 0.20f, cy - r * 0.42f);
                        break;
                    case AkIcon.Sun:
                        g.DrawEllipse(pen, cx - r * 0.42f, cy - r * 0.42f, r * 0.84f, r * 0.84f);
                        for (int i = 0; i < 8; i++)
                        {
                            double a = i * System.Math.PI / 4.0;
                            float x1 = cx + (float)(System.Math.Cos(a) * r * 0.68f);
                            float y1 = cy + (float)(System.Math.Sin(a) * r * 0.68f);
                            float x2 = cx + (float)(System.Math.Cos(a) * r * 0.94f);
                            float y2 = cy + (float)(System.Math.Sin(a) * r * 0.94f);
                            g.DrawLine(pen, x1, y1, x2, y2);
                        }
                        break;
                    case AkIcon.Moon:
                        g.DrawArc(pen, cx - r * 0.78f, cy - r * 0.78f, r * 1.56f, r * 1.56f, 40, 235);
                        g.DrawArc(pen, cx - r * 0.30f, cy - r * 0.92f, r * 1.32f, r * 1.32f, 115, 130);
                        break;
                    case AkIcon.Search:
                        g.DrawEllipse(pen, cx - r * 0.82f, cy - r * 0.82f, r * 1.24f, r * 1.24f);
                        g.DrawLine(pen, cx + r * 0.25f, cy + r * 0.25f, cx + r * 0.82f, cy + r * 0.82f);
                        break;
                    case AkIcon.Pin:
                        g.DrawLine(pen, cx - r * 0.48f, cy - r * 0.72f, cx + r * 0.48f, cy - r * 0.72f);
                        g.DrawLine(pen, cx - r * 0.34f, cy - r * 0.72f, cx - r * 0.56f, cy + r * 0.14f);
                        g.DrawLine(pen, cx + r * 0.34f, cy - r * 0.72f, cx + r * 0.56f, cy + r * 0.14f);
                        g.DrawLine(pen, cx - r * 0.66f, cy + r * 0.14f, cx + r * 0.66f, cy + r * 0.14f);
                        g.DrawLine(pen, cx, cy + r * 0.14f, cx, cy + r * 0.86f);
                        break;
                    case AkIcon.Info:
                        g.DrawEllipse(pen, cx - r * 0.85f, cy - r * 0.85f, r * 1.70f, r * 1.70f);
                        g.DrawLine(pen, cx, cy - r * 0.08f, cx, cy + r * 0.48f);
                        g.FillEllipse(br, cx - 1.1f, cy - r * 0.52f, 2.2f, 2.2f);
                        break;
                    case AkIcon.Play:
                        g.FillPolygon(br, new System.Drawing.PointF[] {
                            new System.Drawing.PointF(cx - r * 0.45f, cy - r * 0.68f),
                            new System.Drawing.PointF(cx + r * 0.68f, cy),
                            new System.Drawing.PointF(cx - r * 0.45f, cy + r * 0.68f)
                        });
                        break;
                    case AkIcon.Pause:
                        g.DrawLine(pen, cx - r * 0.36f, cy - r * 0.62f, cx - r * 0.36f, cy + r * 0.62f);
                        g.DrawLine(pen, cx + r * 0.36f, cy - r * 0.62f, cx + r * 0.36f, cy + r * 0.62f);
                        break;
                    case AkIcon.Stop:
                        g.FillRectangle(br, cx - r * 0.56f, cy - r * 0.56f, r * 1.12f, r * 1.12f);
                        break;
                    case AkIcon.Plus:
                        g.DrawLine(pen, cx - r * 0.68f, cy, cx + r * 0.68f, cy);
                        g.DrawLine(pen, cx, cy - r * 0.68f, cx, cy + r * 0.68f);
                        break;
                    case AkIcon.Minus:
                        g.DrawLine(pen, cx - r * 0.68f, cy, cx + r * 0.68f, cy);
                        break;
                    case AkIcon.Close:
                        g.DrawLine(pen, cx - r * 0.58f, cy - r * 0.58f, cx + r * 0.58f, cy + r * 0.58f);
                        g.DrawLine(pen, cx + r * 0.58f, cy - r * 0.58f, cx - r * 0.58f, cy + r * 0.58f);
                        break;
                    case AkIcon.Up:
                        g.DrawLine(pen, cx, cy + r * 0.68f, cx, cy - r * 0.64f);
                        g.DrawLine(pen, cx - r * 0.52f, cy - r * 0.14f, cx, cy - r * 0.66f);
                        g.DrawLine(pen, cx + r * 0.52f, cy - r * 0.14f, cx, cy - r * 0.66f);
                        break;
                    case AkIcon.Down:
                        g.DrawLine(pen, cx, cy - r * 0.68f, cx, cy + r * 0.64f);
                        g.DrawLine(pen, cx - r * 0.52f, cy + r * 0.14f, cx, cy + r * 0.66f);
                        g.DrawLine(pen, cx + r * 0.52f, cy + r * 0.14f, cx, cy + r * 0.66f);
                        break;
                    case AkIcon.Folder:
                        g.DrawPolygon(pen, new System.Drawing.PointF[] {
                            new System.Drawing.PointF(cx - r * 0.85f, cy - r * 0.56f),
                            new System.Drawing.PointF(cx - r * 0.22f, cy - r * 0.56f),
                            new System.Drawing.PointF(cx - r * 0.04f, cy - r * 0.34f),
                            new System.Drawing.PointF(cx + r * 0.85f, cy - r * 0.34f),
                            new System.Drawing.PointF(cx + r * 0.85f, cy + r * 0.64f),
                            new System.Drawing.PointF(cx - r * 0.85f, cy + r * 0.64f)
                        });
                        break;
                    case AkIcon.Copy:
                        g.DrawRectangle(pen, cx - r * 0.26f, cy - r * 0.26f, r * 1.02f, r * 1.02f);
                        g.DrawLine(pen, cx - r * 0.68f, cy + r * 0.30f, cx - r * 0.68f, cy - r * 0.68f);
                        g.DrawLine(pen, cx - r * 0.68f, cy - r * 0.68f, cx + r * 0.30f, cy - r * 0.68f);
                        break;
                    case AkIcon.Trash:
                        g.DrawLine(pen, cx - r * 0.76f, cy - r * 0.44f, cx + r * 0.76f, cy - r * 0.44f);
                        g.DrawLine(pen, cx - r * 0.28f, cy - r * 0.74f, cx + r * 0.28f, cy - r * 0.74f);
                        g.DrawRectangle(pen, cx - r * 0.54f, cy - r * 0.42f, r * 1.08f, r * 1.16f);
                        break;
                    case AkIcon.Check:
                        g.DrawLine(pen, cx - r * 0.68f, cy + r * 0.04f, cx - r * 0.14f, cy + r * 0.58f);
                        g.DrawLine(pen, cx - r * 0.14f, cy + r * 0.58f, cx + r * 0.74f, cy - r * 0.52f);
                        break;
                    case AkIcon.Swap:
                        g.DrawLine(pen, cx - r * 0.70f, cy - r * 0.32f, cx + r * 0.70f, cy - r * 0.32f);
                        g.DrawLine(pen, cx + r * 0.32f, cy - r * 0.68f, cx + r * 0.72f, cy - r * 0.32f);
                        g.DrawLine(pen, cx + r * 0.70f, cy + r * 0.34f, cx - r * 0.70f, cy + r * 0.34f);
                        g.DrawLine(pen, cx - r * 0.32f, cy + r * 0.70f, cx - r * 0.72f, cy + r * 0.34f);
                        break;
                    case AkIcon.Download:
                        g.DrawLine(pen, cx, cy - r * 0.72f, cx, cy + r * 0.28f);
                        g.DrawLine(pen, cx - r * 0.44f, cy - r * 0.12f, cx, cy + r * 0.32f);
                        g.DrawLine(pen, cx + r * 0.44f, cy - r * 0.12f, cx, cy + r * 0.32f);
                        g.DrawLine(pen, cx - r * 0.74f, cy + r * 0.72f, cx + r * 0.74f, cy + r * 0.72f);
                        break;
                    case AkIcon.Link:
                    case AkIcon.External:
                        g.DrawLine(pen, cx - r * 0.58f, cy + r * 0.58f, cx + r * 0.62f, cy - r * 0.62f);
                        g.DrawLine(pen, cx - r * 0.06f, cy - r * 0.64f, cx + r * 0.64f, cy - r * 0.64f);
                        g.DrawLine(pen, cx + r * 0.64f, cy - r * 0.64f, cx + r * 0.64f, cy + r * 0.06f);
                        break;
                    case AkIcon.Split:
                        g.DrawRectangle(pen, cx - r * 0.82f, cy - r * 0.64f, r * 1.64f, r * 1.28f);
                        g.DrawLine(pen, cx, cy - r * 0.64f, cx, cy + r * 0.64f);
                        break;
                    case AkIcon.Pipette:
                        g.DrawLine(pen, cx - r * 0.66f, cy + r * 0.66f, cx + r * 0.40f, cy - r * 0.40f);
                        g.DrawLine(pen, cx + r * 0.12f, cy - r * 0.68f, cx + r * 0.68f, cy - r * 0.12f);
                        break;
                    case AkIcon.Bolt:
                        g.DrawPolygon(pen, new System.Drawing.PointF[] {
                            new System.Drawing.PointF(cx + r * 0.14f, cy - r * 0.82f),
                            new System.Drawing.PointF(cx - r * 0.62f, cy + r * 0.08f),
                            new System.Drawing.PointF(cx - r * 0.04f, cy + r * 0.08f),
                            new System.Drawing.PointF(cx - r * 0.18f, cy + r * 0.82f),
                            new System.Drawing.PointF(cx + r * 0.62f, cy - r * 0.10f),
                            new System.Drawing.PointF(cx + r * 0.04f, cy - r * 0.10f)
                        });
                        break;
                    case AkIcon.List:
                        g.DrawLine(pen, cx - r * 0.72f, cy - r * 0.46f, cx + r * 0.72f, cy - r * 0.46f);
                        g.DrawLine(pen, cx - r * 0.72f, cy, cx + r * 0.72f, cy);
                        g.DrawLine(pen, cx - r * 0.72f, cy + r * 0.46f, cx + r * 0.72f, cy + r * 0.46f);
                        break;
                    case AkIcon.Filter:
                        g.DrawPolygon(pen, new System.Drawing.PointF[] {
                            new System.Drawing.PointF(cx - r * 0.78f, cy - r * 0.60f),
                            new System.Drawing.PointF(cx + r * 0.78f, cy - r * 0.60f),
                            new System.Drawing.PointF(cx + r * 0.16f, cy + r * 0.08f),
                            new System.Drawing.PointF(cx + r * 0.16f, cy + r * 0.68f),
                            new System.Drawing.PointF(cx - r * 0.16f, cy + r * 0.52f),
                            new System.Drawing.PointF(cx - r * 0.16f, cy + r * 0.08f)
                        });
                        break;
                    case AkIcon.Shield:
                        g.DrawPolygon(pen, new System.Drawing.PointF[] {
                            new System.Drawing.PointF(cx, cy - r * 0.82f),
                            new System.Drawing.PointF(cx + r * 0.72f, cy - r * 0.44f),
                            new System.Drawing.PointF(cx + r * 0.58f, cy + r * 0.34f),
                            new System.Drawing.PointF(cx, cy + r * 0.82f),
                            new System.Drawing.PointF(cx - r * 0.58f, cy + r * 0.34f),
                            new System.Drawing.PointF(cx - r * 0.72f, cy - r * 0.44f)
                        });
                        break;
                    case AkIcon.Lock:
                        g.DrawRectangle(pen, cx - r * 0.58f, cy - r * 0.08f, r * 1.16f, r * 0.84f);
                        g.DrawArc(pen, cx - r * 0.38f, cy - r * 0.76f, r * 0.76f, r * 0.84f, 180, 180);
                        break;
                    case AkIcon.Unlock:
                        g.DrawRectangle(pen, cx - r * 0.58f, cy - r * 0.08f, r * 1.16f, r * 0.84f);
                        g.DrawArc(pen, cx - r * 0.38f, cy - r * 0.76f, r * 0.76f, r * 0.84f, 180, 130);
                        break;
                    case AkIcon.Save:
                        g.DrawRectangle(pen, cx - r * 0.72f, cy - r * 0.72f, r * 1.44f, r * 1.44f);
                        g.DrawRectangle(pen, cx - r * 0.42f, cy - r * 0.72f, r * 0.72f, r * 0.48f);
                        g.DrawRectangle(pen, cx - r * 0.44f, cy + r * 0.14f, r * 0.88f, r * 0.58f);
                        break;
                    case AkIcon.Edit:
                        g.DrawLine(pen, cx - r * 0.62f, cy + r * 0.62f, cx + r * 0.56f, cy - r * 0.56f);
                        g.DrawLine(pen, cx + r * 0.24f, cy - r * 0.74f, cx + r * 0.74f, cy - r * 0.24f);
                        g.DrawLine(pen, cx - r * 0.72f, cy + r * 0.72f, cx - r * 0.34f, cy + r * 0.62f);
                        break;
                    case AkIcon.Undo:
                        g.DrawArc(pen, cx - r * 0.52f, cy - r * 0.34f, r * 1.24f, r * 1.04f, -135, 225);
                        g.DrawLine(pen, cx - r * 0.72f, cy - r * 0.22f, cx - r * 0.28f, cy - r * 0.62f);
                        g.DrawLine(pen, cx - r * 0.72f, cy - r * 0.22f, cx - r * 0.24f, cy + r * 0.10f);
                        break;
                    case AkIcon.ChevLeft:
                        g.DrawLine(pen, cx + r * 0.28f, cy - r * 0.58f, cx - r * 0.34f, cy);
                        g.DrawLine(pen, cx - r * 0.34f, cy, cx + r * 0.28f, cy + r * 0.58f);
                        break;
                    case AkIcon.ChevRight:
                        g.DrawLine(pen, cx - r * 0.28f, cy - r * 0.58f, cx + r * 0.34f, cy);
                        g.DrawLine(pen, cx + r * 0.34f, cy, cx - r * 0.28f, cy + r * 0.58f);
                        break;
                    case AkIcon.Star:
                        System.Drawing.PointF[] sp = new System.Drawing.PointF[10];
                        for (int k = 0; k < 10; k++)
                        {
                            double ang = -System.Math.PI / 2.0 + k * System.Math.PI / 5.0;
                            float rad = (k % 2 == 0) ? r * 0.85f : r * 0.38f;
                            sp[k] = new System.Drawing.PointF(cx + (float)(System.Math.Cos(ang) * rad), cy + (float)(System.Math.Sin(ang) * rad));
                        }
                        g.DrawPolygon(pen, sp);
                        break;
                }
            }
            g.SmoothingMode = oldSm;
        }
    }

    internal sealed class AkButton : System.Windows.Forms.Button
    {
        public AkKind Kind = AkKind.Secondary;
        public AkIcon Icon = AkIcon.None;
        public bool Active = false;
        public bool Dark = true;
        public int Radius = 6;
        private bool _hov = false;
        private bool _down = false;

        public AkButton()
        {
            SetStyle(System.Windows.Forms.ControlStyles.AllPaintingInWmPaint |
                     System.Windows.Forms.ControlStyles.OptimizedDoubleBuffer |
                     System.Windows.Forms.ControlStyles.UserPaint |
                     System.Windows.Forms.ControlStyles.ResizeRedraw, true);
            FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Cursor = System.Windows.Forms.Cursors.Hand;
            TabStop = false;
            UseMnemonic = false;
        }

        protected override void OnMouseEnter(System.EventArgs e) { _hov = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(System.EventArgs e) { _hov = false; _down = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(System.Windows.Forms.MouseEventArgs e) { if (e.Button == System.Windows.Forms.MouseButtons.Left) { _down = true; Invalidate(); } base.OnMouseDown(e); }
        protected override void OnMouseUp(System.Windows.Forms.MouseEventArgs e) { _down = false; Invalidate(); base.OnMouseUp(e); }

        protected override void OnPaint(System.Windows.Forms.PaintEventArgs e)
        {
            System.Drawing.Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            System.Drawing.Color parentBg = Parent != null ? Parent.BackColor : (Dark ? System.Drawing.Color.FromArgb(29, 31, 32) : System.Drawing.Color.FromArgb(248, 249, 250));
            g.Clear(parentBg);

            System.Drawing.Color accent = Dark ? System.Drawing.Color.FromArgb(252, 150, 31) : System.Drawing.Color.FromArgb(224, 118, 12);
            System.Drawing.Color accentHov = Dark ? System.Drawing.Color.FromArgb(255, 169, 64) : System.Drawing.Color.FromArgb(204, 102, 8);
            System.Drawing.Color surf = Dark ? System.Drawing.Color.FromArgb(39, 41, 43) : System.Drawing.Color.White;
            System.Drawing.Color surfHov = Dark ? System.Drawing.Color.FromArgb(50, 53, 56) : System.Drawing.Color.FromArgb(236, 239, 243);
            System.Drawing.Color border = Dark ? System.Drawing.Color.FromArgb(58, 62, 66) : System.Drawing.Color.FromArgb(210, 216, 224);
            System.Drawing.Color borderHov = Dark ? System.Drawing.Color.FromArgb(84, 89, 95) : System.Drawing.Color.FromArgb(176, 184, 196);
            System.Drawing.Color fgMain = Dark ? System.Drawing.Color.FromArgb(242, 244, 247) : System.Drawing.Color.FromArgb(26, 29, 33);
            System.Drawing.Color fgMuted = Dark ? System.Drawing.Color.FromArgb(154, 160, 168) : System.Drawing.Color.FromArgb(95, 104, 116);
            System.Drawing.Color fgDis = Dark ? System.Drawing.Color.FromArgb(92, 97, 104) : System.Drawing.Color.FromArgb(170, 178, 188);

            bool en = Enabled;
            System.Drawing.Color bg = parentBg;
            System.Drawing.Color bd = System.Drawing.Color.Transparent;
            System.Drawing.Color fg = en ? fgMain : fgDis;
            bool drawFill = false;

            if (Kind == AkKind.Primary)
            {
                drawFill = true;
                bg = en ? (_hov ? accentHov : accent) : (Dark ? System.Drawing.Color.FromArgb(56, 48, 38) : System.Drawing.Color.FromArgb(228, 224, 218));
                fg = en ? (Dark ? System.Drawing.Color.FromArgb(20, 21, 23) : System.Drawing.Color.White) : fgDis;
            }
            else if (Kind == AkKind.AccentOutline)
            {
                drawFill = true;
                bg = en ? (_hov ? (Dark ? System.Drawing.Color.FromArgb(52, 42, 30) : System.Drawing.Color.FromArgb(255, 243, 228)) : surf) : (Dark ? System.Drawing.Color.FromArgb(32, 34, 36) : System.Drawing.Color.FromArgb(244, 246, 248));
                bd = en ? accent : border;
                fg = en ? accent : fgDis;
            }
            else if (Kind == AkKind.Danger)
            {
                drawFill = true;
                System.Drawing.Color red = Dark ? System.Drawing.Color.FromArgb(240, 104, 118) : System.Drawing.Color.FromArgb(196, 40, 56);
                bg = en ? (_hov ? (Dark ? System.Drawing.Color.FromArgb(62, 30, 36) : System.Drawing.Color.FromArgb(254, 228, 231)) : (Dark ? System.Drawing.Color.FromArgb(46, 28, 32) : System.Drawing.Color.FromArgb(255, 240, 242))) : surf;
                bd = en ? (Dark ? System.Drawing.Color.FromArgb(116, 48, 58) : System.Drawing.Color.FromArgb(236, 154, 164)) : border;
                fg = en ? red : fgDis;
            }
            else if (Kind == AkKind.Tab)
            {
                drawFill = _hov && en;
                bg = surfHov;
                fg = en ? (Active ? fgMain : (_hov ? fgMain : fgMuted)) : fgDis;
            }
            else if (Kind == AkKind.Ghost)
            {
                drawFill = (Active || _hov) && en;
                bg = Active ? (Dark ? System.Drawing.Color.FromArgb(52, 42, 30) : System.Drawing.Color.FromArgb(255, 243, 228)) : surfHov;
                bd = Active ? accent : System.Drawing.Color.Transparent;
                fg = en ? (Active ? accent : (_hov ? fgMain : fgMuted)) : fgDis;
            }
            else // Secondary (Ariakit tactile elevated pill)
            {
                drawFill = true;
                bg = en ? (Active ? (Dark ? System.Drawing.Color.FromArgb(52, 42, 30) : System.Drawing.Color.FromArgb(255, 243, 228)) : (_hov ? surfHov : surf)) : (Dark ? System.Drawing.Color.FromArgb(32, 34, 36) : System.Drawing.Color.FromArgb(245, 247, 249));
                bd = en ? (Active ? accent : (_hov ? borderHov : border)) : (Dark ? System.Drawing.Color.FromArgb(44, 47, 50) : System.Drawing.Color.FromArgb(224, 228, 234));
                fg = en ? (Active ? accent : fgMain) : fgDis;
            }

            float ox = 0.5f, oy = (_down && en) ? 1.5f : 0.5f;
            float rw = Width - 1.5f, rh = Height - 1.5f - ((_down && en) ? 1f : 0f);
            if (drawFill || bd.A > 0)
            {
                using (System.Drawing.Drawing2D.GraphicsPath gp = AkDraw.RoundRect(ox, oy, rw, rh, Radius))
                {
                    if (drawFill) using (System.Drawing.SolidBrush br = new System.Drawing.SolidBrush(bg)) g.FillPath(br, gp);
                    if (bd.A > 0) using (System.Drawing.Pen p = new System.Drawing.Pen(bd, 1f)) g.DrawPath(p, gp);
                }
            }
            if (Kind == AkKind.Tab && Active)
            {
                using (System.Drawing.SolidBrush ab = new System.Drawing.SolidBrush(accent))
                    g.FillRectangle(ab, 6, Height - 3, System.Math.Max(8, Width - 12), 2.5f);
            }

            string txt = Text ?? "";
            float yShift = (_down && en) ? 1f : 0f;
            if (Icon != AkIcon.None && txt.Length == 0)
            {
                AkDraw.DrawIcon(g, Icon, Width * 0.5f, Height * 0.5f + yShift, 13.5f, fg);
            }
            else if (Icon != AkIcon.None)
            {
                System.Drawing.Size ts = System.Windows.Forms.TextRenderer.MeasureText(txt, Font, new System.Drawing.Size(Width, Height), System.Windows.Forms.TextFormatFlags.NoPrefix | System.Windows.Forms.TextFormatFlags.NoPadding);
                float iconW = 13f, gap = 6f;
                float totalW = iconW + gap + ts.Width;
                float startX = System.Math.Max(8f, (Width - totalW) * 0.5f);
                AkDraw.DrawIcon(g, Icon, startX + iconW * 0.5f, Height * 0.5f + yShift, 12.5f, fg);
                System.Drawing.Rectangle tr = new System.Drawing.Rectangle((int)(startX + iconW + gap), (int)yShift, System.Math.Max(10, Width - (int)(startX + iconW + gap) - 4), Height);
                System.Windows.Forms.TextRenderer.DrawText(g, txt, Font, tr, fg, System.Windows.Forms.TextFormatFlags.Left | System.Windows.Forms.TextFormatFlags.VerticalCenter | System.Windows.Forms.TextFormatFlags.NoPrefix | System.Windows.Forms.TextFormatFlags.EndEllipsis);
            }
            else
            {
                System.Drawing.Rectangle tr = new System.Drawing.Rectangle(4, (int)yShift, System.Math.Max(8, Width - 8), Height);
                System.Windows.Forms.TextRenderer.DrawText(g, txt, Font, tr, fg, System.Windows.Forms.TextFormatFlags.HorizontalCenter | System.Windows.Forms.TextFormatFlags.VerticalCenter | System.Windows.Forms.TextFormatFlags.NoPrefix | System.Windows.Forms.TextFormatFlags.EndEllipsis);
            }
        }
    }

}
