using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml.Media.Imaging;

namespace ShutdownTimer.Services
{
    public static class IconHelper
    {
        // ============ ExtractIconEx（exe/dll） ============
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern uint ExtractIconEx(string szFileName, int nIconIndex,
            IntPtr[]? phiconLarge, IntPtr[]? phiconSmall, uint nIcons);

        // ============ SHGetFileInfo（任意文件） ============
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes,
            ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        private const uint SHGFI_ICON = 0x100;
        private const uint SHGFI_LARGEICON = 0x0;

        // ============ GDI / User32 ============
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetIconInfo(IntPtr hIcon, out ICONINFO piconinfo);

        [DllImport("gdi32.dll")]
        private static extern int GetObject(IntPtr hgdiobj, int cbBuffer, ref BITMAP lpvObject);

        [DllImport("gdi32.dll")]
        private static extern int GetDIBits(IntPtr hdc, IntPtr hbm, uint start, uint cLines,
            byte[] lpvBits, ref BITMAPINFOHEADER lpbmi, uint usage);

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        private const uint DIB_RGB_COLORS = 0;
        private const int BI_RGB = 0;

        [StructLayout(LayoutKind.Sequential)]
        private struct ICONINFO
        {
            public bool fIcon;
            public int xHotspot;
            public int yHotspot;
            public IntPtr hbmMask;
            public IntPtr hbmColor;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BITMAP
        {
            public int bmType;
            public int bmWidth;
            public int bmHeight;
            public int bmWidthBytes;
            public ushort bmPlanes;
            public ushort bmBitsPixel;
            public IntPtr bmBits;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BITMAPINFOHEADER
        {
            public int biSize;
            public int biWidth;
            public int biHeight;
            public ushort biPlanes;
            public ushort biBitCount;
            public int biCompression;
            public int biSizeImage;
            public int biXPelsPerMeter;
            public int biYPelsPerMeter;
            public int biClrUsed;
            public int biClrImportant;
        }

        // ==================== 对外 API ====================

        public static BitmapImage? LoadIconFromAnyFile(string path)
        {
            var icoPath = ExtractIconFromAnyFileToTempIco(path);
            if (icoPath == null) return null;

            try
            {
                return new BitmapImage(new Uri(icoPath));
            }
            catch
            {
                return null;
            }
        }

        public static string? ExtractIconFromAnyFileToTempIco(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return null;

            var ext = Path.GetExtension(path).ToLowerInvariant();

            if (ext == ".exe" || ext == ".dll")
                return ExtractIconToTempIco(path);

            return ExtractShellIconToTempIco(path);
        }

        public static string? ExtractIconToTempIco(string exePath)
        {
            if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath))
                return null;

            var largeIcons = new IntPtr[1];
            try
            {
                uint n = ExtractIconEx(exePath, 0, largeIcons, null, 1);
                if (n == 0 || largeIcons[0] == IntPtr.Zero)
                    return null;

                return SaveHIconAsIco(largeIcons[0]);
            }
            catch
            {
                return null;
            }
            finally
            {
                if (largeIcons[0] != IntPtr.Zero)
                    DestroyIcon(largeIcons[0]);
            }
        }

        private static string? ExtractShellIconToTempIco(string path)
        {
            var info = new SHFILEINFO();
            try
            {
                SHGetFileInfo(path, 0, ref info,
                    (uint)Marshal.SizeOf<SHFILEINFO>(),
                    SHGFI_ICON | SHGFI_LARGEICON);

                if (info.hIcon == IntPtr.Zero) return null;

                return SaveHIconAsIco(info.hIcon);
            }
            catch
            {
                return null;
            }
            finally
            {
                if (info.hIcon != IntPtr.Zero)
                    DestroyIcon(info.hIcon);
            }
        }

        // ==================== HICON → .ico 文件 ====================

        private static string? SaveHIconAsIco(IntPtr hIcon)
        {
            if (!GetIconInfo(hIcon, out var info)) return null;

            try
            {
                var bmColor = new BITMAP();
                if (GetObject(info.hbmColor, Marshal.SizeOf<BITMAP>(), ref bmColor) == 0)
                    return null;

                int width = bmColor.bmWidth;
                int height = bmColor.bmHeight;
                if (width <= 0 || height <= 0) return null;

                // ---- 颜色位图（GetDIBits 输出为 bottom-up，ICO 也要求 bottom-up，直接使用） ----
                var bmiColor = new BITMAPINFOHEADER
                {
                    biSize = Marshal.SizeOf<BITMAPINFOHEADER>(),
                    biWidth = width,
                    biHeight = height,
                    biPlanes = 1,
                    biBitCount = 32,
                    biCompression = BI_RGB,
                    biSizeImage = width * height * 4
                };
                byte[] xorData = new byte[width * height * 4];

                IntPtr hdc = GetDC(IntPtr.Zero);
                try
                {
                    if (GetDIBits(hdc, info.hbmColor, 0, (uint)height, xorData,
                        ref bmiColor, DIB_RGB_COLORS) == 0)
                        return null;
                }
                finally
                {
                    ReleaseDC(IntPtr.Zero, hdc);
                }

                // ---- mask 位图（1bpp，行按 4 字节对齐） ----
                int maskRowBytes = ((width + 31) / 32) * 4;
                var bmiMask = new BITMAPINFOHEADER
                {
                    biSize = Marshal.SizeOf<BITMAPINFOHEADER>(),
                    biWidth = width,
                    biHeight = height,
                    biPlanes = 1,
                    biBitCount = 1,
                    biCompression = BI_RGB,
                    biSizeImage = maskRowBytes * height
                };
                byte[] andData = new byte[maskRowBytes * height];

                hdc = GetDC(IntPtr.Zero);
                try
                {
                    GetDIBits(hdc, info.hbmMask, 0, (uint)height, andData,
                        ref bmiMask, DIB_RGB_COLORS);
                }
                finally
                {
                    ReleaseDC(IntPtr.Zero, hdc);
                }

                // ---- 写 ICO 文件 ----
                int xorSize = xorData.Length;
                int andSize = andData.Length;
                int imageSize = 40 + xorSize + andSize;

                string icoPath = Path.Combine(Path.GetTempPath(),
                    "ShutdownTimer_icon_" + Guid.NewGuid().ToString("N") + ".ico");

                using (var fs = new FileStream(icoPath, FileMode.Create, FileAccess.Write))
                using (var bw = new BinaryWriter(fs))
                {
                    // ICONDIR
                    bw.Write((ushort)0);
                    bw.Write((ushort)1);
                    bw.Write((ushort)1);

                    // ICONDIRENTRY
                    bw.Write((byte)(width >= 256 ? 0 : width));
                    bw.Write((byte)(height >= 256 ? 0 : height));
                    bw.Write((byte)0);
                    bw.Write((byte)0);
                    bw.Write((ushort)1);
                    bw.Write((ushort)32);
                    bw.Write(imageSize);
                    bw.Write(22);

                    // BITMAPINFOHEADER
                    bw.Write(40);
                    bw.Write(width);
                    bw.Write(height * 2);  // ICO: XOR + AND 的高度之和
                    bw.Write((ushort)1);
                    bw.Write((ushort)32);
                    bw.Write(0);           // BI_RGB
                    bw.Write(xorSize + andSize);
                    bw.Write(0);
                    bw.Write(0);
                    bw.Write(0);
                    bw.Write(0);

                    bw.Write(xorData);
                    bw.Write(andData);
                }

                return icoPath;
            }
            finally
            {
                if (info.hbmColor != IntPtr.Zero) DeleteObject(info.hbmColor);
                if (info.hbmMask != IntPtr.Zero) DeleteObject(info.hbmMask);
            }
        }
    }
}