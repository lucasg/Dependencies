using System;
using System.Windows;
using System.Windows.Data;
using System.Drawing;
using System.Windows.Media.Imaging;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

using Dependencies.ClrPh;

namespace Dependencies
{
	/// <summary>
    /// Summary description for ShellIcon.  Get a small or large Icon with an easy C# function call
    /// that returns a 32x32 or 16x16 System.Drawing.Icon depending on which function you call
    /// either GetSmallIcon(string fileName) or GetLargeIcon(string fileName)
    /// </summary>
    public static class ShellIcon
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct SHFILEINFO
        {
            public IntPtr hIcon;
            public IntPtr iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        };

        class Win32
        {
            public const uint SHGFI_ICON = 0x100;
            public const uint SHGFI_LARGEICON = 0x0; // 'Large icon
            public const uint SHGFI_SMALLICON = 0x1; // 'Small icon

            [DllImport("shell32.dll")]
            public static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);

            [DllImport("User32.dll")]
            public static extern int DestroyIcon(IntPtr hIcon);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool Wow64DisableWow64FsRedirection(ref IntPtr ptr);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool Wow64RevertWow64FsRedirection(IntPtr ptr);

            [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool IsWow64Process(
                [In] IntPtr processHandle,
                [Out, MarshalAs(UnmanagedType.Bool)] out bool wow64Process
            );
        }

        static ShellIcon()
        {

        }

        public static Icon GetSmallIcon(string fileName)
        {
            return GetIcon(fileName, Win32.SHGFI_SMALLICON);
        }

        public static Icon GetLargeIcon(string fileName)
        {
            return GetIcon(fileName, Win32.SHGFI_LARGEICON);
        }

        private static Icon GetIcon(string fileName, uint flags)
        {
            bool bIsWow64;
            SHFILEINFO shinfo = new SHFILEINFO();

            // Check if calling process is 32-bit
            SafeProcessHandle processHandle = System.Diagnostics.Process.GetCurrentProcess().SafeHandle;
            if (!Win32.IsWow64Process(processHandle.DangerousGetHandle(), out bIsWow64))
            {
                return null;
            }
            
            // Force ignoring folder redirection
            IntPtr OldRedirectionValue = new IntPtr();
            if (bIsWow64)
            {
                Win32.Wow64DisableWow64FsRedirection(ref OldRedirectionValue);
            }
        
            IntPtr hImgSmall = Win32.SHGetFileInfo(fileName, 0, ref shinfo, (uint)Marshal.SizeOf(shinfo), Win32.SHGFI_ICON | flags);

            if (bIsWow64)
            {
                Win32.Wow64RevertWow64FsRedirection(OldRedirectionValue);
            }

            if (hImgSmall == (IntPtr) 0x00)
            {
                return null;
            }
                   
            Icon icon = (Icon)System.Drawing.Icon.FromHandle(shinfo.hIcon).Clone();
            Win32.DestroyIcon(shinfo.hIcon);
            return icon;
        }
    }

    public class ImageToHeaderConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string Filepath = (string) value;
            Icon icon = ShellIcon.GetSmallIcon(Filepath);

            if (NativeFile.Exists(Filepath) && (icon != null))
            {
                return System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                            icon.Handle,
                            new Int32Rect(0, 0, icon.Width, icon.Height),
                            BitmapSizeOptions.FromEmptyOptions()); 
            }

            return "Images/Question.png";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class AlternateImageToHeaderConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            ModuleFlag Flags = (ModuleFlag)value;

			if ((string) parameter == "Overlay")
			{
				// ext-ms api are considered optional
				if (((Flags & ModuleFlag.NotFound) != 0) && ((Flags & ModuleFlag.ApiSetExt) == 0))
				{
					return "Images/Invalid.png";
				}

				return null;
			}

            bool DelayLoadModule = (Flags & ModuleFlag.DelayLoad) != 0;
            if (DelayLoadModule)
            {
                return "Images/Hourglass.png";
            }

            bool ClrAssembly = (Flags & ModuleFlag.ClrReference) != 0;
            if (ClrAssembly)
            {
                return "Images/Reference.png";
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}