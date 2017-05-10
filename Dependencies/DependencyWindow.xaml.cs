using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.ClrPh;
using System.Drawing;
using System.Runtime.InteropServices;

public class DisplayPeImport
{
    public DisplayPeImport(int Index, PeImport PeImport)
    {
        Info.index = Index;
        Info.ordinal = PeImport.Ordinal;
        Info.hint = PeImport.Hint;
        Info.name = PeImport.Name;
        Info.moduleName = PeImport.ModuleName;
        Info.delayedImport = PeImport.DelayImport;

    }

    public int Index { get { return Info.index; } }
    public int Hint { get { return Info.hint; } }
    public int Ordinal { get { return Info.ordinal; } }
    public string Name { get { return Info.name; } }
    public string ModuleName { get { return Info.moduleName; } }
    public Boolean DelayImport { get { return Info.delayedImport; } }

    private
        PeImportInfo Info;
}

public struct PeImportInfo
{
    public int index;
    public int ordinal;
    public int hint;
    public string name;
    public string moduleName;
    public Boolean delayedImport;
}

public class DisplayPeExport
{
    public DisplayPeExport(int Index, PeExport PeExport)
    {
        PeInfo.index = Index;
        PeInfo.ordinal = PeExport.Ordinal;
        PeInfo.hint = /*PeExport.Hint*/ PeExport.Ordinal - 1;
        PeInfo.name = PeExport.Name;
        PeInfo.ForwardName = PeExport.ForwardedName;
        PeInfo.exportByOrdinal = PeExport.ExportByOrdinal;
        PeInfo.forwardedExport = PeExport.ForwardedName.Length > 0;
        PeInfo.virtualAddress = PeExport.VirtualAddress;
    }

    public int Index { get { return PeInfo.index; } }
    public int Hint { get { return PeInfo.hint; } }
    public int Ordinal { get { return PeInfo.ordinal; } }
    public string Name
    {
        get
        {

            if (PeInfo.forwardedExport)
                return PeInfo.ForwardName;

            if (PeInfo.exportByOrdinal)
                return String.Format("Ordinal_{0:d}", PeInfo.ordinal);

            return PeInfo.name;
        }
    }
    public string VirtualAddress { get { return String.Format("0x{0:x8}", PeInfo.virtualAddress); } }



    private
        PeExportInfo PeInfo;
}

public struct PeExportInfo
{
    public Boolean exportByOrdinal;
    public Boolean forwardedExport;
    public int index;
    public int ordinal;
    public int hint;
    public long virtualAddress;
    public string name;
    public string ForwardName;
}

[Flags]
public enum PeTypes
{
    None = 0,
    IMAGE_FILE_EXECUTABLE_IMAGE = 0x02,
    IMAGE_FILE_DLL = 0x2000,

}

public class DisplayModuleInfo
{
    public DisplayModuleInfo(int index, PeImportDll Module, PeProperties Properties)
    {
        Info.index = index;
        Info.Name = Module.Name;

        Info.Machine = Properties.Machine;
        Info.Magic = Properties.Magic;

        Info.ImageBase = Properties.ImageBase;
        Info.SizeOfImage = Properties.SizeOfImage;
        Info.EntryPoint = Properties.EntryPoint;

        Info.Checksum = Properties.Checksum;
        Info.CorrectChecksum = Properties.CorrectChecksum;

        Info.Subsystem = Properties.Subsystem;
        Info.Characteristics = Properties.Characteristics;
        Info.DllCharacteristics = Properties.DllCharacteristics;

    }

    public int Index { get { return Info.index; } }
    public string Name { get { return Info.Name; } }
    public string Cpu
    {
        get
        {
            int machine_id = Info.Machine & 0xffff;
            switch (machine_id)
            {
                case 0x014c: /*IMAGE_FILE_MACHINE_I386*/
                    return "i386";

                case 0x8664: /*IMAGE_FILE_MACHINE_AMD64*/
                    return "AMD64";

                case 0x0200:/*IMAGE_FILE_MACHINE_IA64*/
                    return "IA64";

                case 0x01c4:/*IMAGE_FILE_MACHINE_ARMNT*/
                    return "ARM Thumb-2";

                default:
                    return "Unknown";
            }
        }
    }
    public string Type
    {
        get
        {
            List<String> TypeList = new List<String>();
            PeTypes Type = (PeTypes)Info.Characteristics;

            if ((Type & PeTypes.IMAGE_FILE_DLL) != PeTypes.None)/* IMAGE_FILE_DLL */
                TypeList.Add("Dll");

            if ((Type & PeTypes.IMAGE_FILE_EXECUTABLE_IMAGE) != PeTypes.None) /* IMAGE_FILE_EXECUTABLE_IMAGE */
                TypeList.Add("Executable");



            return String.Join("; ", TypeList.ToArray());
        }
    }
    public string Filesize { get { return String.Format("0x{0:08x}", 0x00); } }
    public string ImageBase { get { return String.Format("0x{0:08x}", Info.ImageBase); } }
    public string VirtualSize { get { return String.Format("0x{0:08x}", Info.SizeOfImage); } }
    public string EntryPoint { get { return String.Format("0x{0:08x}", Info.EntryPoint); } }
    public string Subsystem { get { return String.Format("{0:x}", Info.Subsystem); } }
    public string SubsystemVersion { get { return ""; } }
    public string Checksum
    {
        get
        {
            if (Info.CorrectChecksum)
                return String.Format("{0:x} (correct)", Info.Subsystem);
            else
                return String.Format("{0:x} (incorrect)", Info.Subsystem);
        }
    }

    private
        ModuleInfo Info;
}


public struct ModuleInfo
{
    public int index;
    public string Name;
    public Int16 Machine;
    public Int16 Magic;

    public IntPtr ImageBase;
    public Int32 SizeOfImage;
    public IntPtr EntryPoint;


    public Int32 Checksum;
    public Boolean CorrectChecksum;

    public Int16 Subsystem;
    public Int16 Characteristics;
    public Int16 DllCharacteristics;
}

namespace Dependencies
{
    /// <summary> Summary description for ExtractIcon.</summary>
    public class ExtractIcon
    {
        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern int SHGetFileInfo(
          string pszPath,
          int dwFileAttributes,
          out SHFILEINFO psfi,
          uint cbfileInfo,
          SHGFI uFlags);

        /// <summary>Maximal Length of unmanaged Windows-Path-strings</summary>
        private const int MAX_PATH = 260;
        /// <summary>Maximal Length of unmanaged Typename</summary>
        private const int MAX_TYPE = 80;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct SHFILEINFO
        {
            public SHFILEINFO(bool b)
            {
                hIcon = IntPtr.Zero;
                iIcon = 0;
                dwAttributes = 0;
                szDisplayName = "";
                szTypeName = "";
            }
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_PATH)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_TYPE)]
            public string szTypeName;
        };

        private ExtractIcon()
        {
        }

        [Flags]
        enum SHGFI : int
        {
            /// <summary>get icon</summary>
            Icon = 0x000000100,
            /// <summary>get display name</summary>
            DisplayName = 0x000000200,
            /// <summary>get type name</summary>
            TypeName = 0x000000400,
            /// <summary>get attributes</summary>
            Attributes = 0x000000800,
            /// <summary>get icon location</summary>
            IconLocation = 0x000001000,
            /// <summary>return exe type</summary>
            ExeType = 0x000002000,
            /// <summary>get system icon index</summary>
            SysIconIndex = 0x000004000,
            /// <summary>put a link overlay on icon</summary>
            LinkOverlay = 0x000008000,
            /// <summary>show icon in selected state</summary>
            Selected = 0x000010000,
            /// <summary>get only specified attributes</summary>
            Attr_Specified = 0x000020000,
            /// <summary>get large icon</summary>
            LargeIcon = 0x000000000,
            /// <summary>get small icon</summary>
            SmallIcon = 0x000000001,
            /// <summary>get open icon</summary>
            OpenIcon = 0x000000002,
            /// <summary>get shell size icon</summary>
            ShellIconSize = 0x000000004,
            /// <summary>pszPath is a pidl</summary>
            PIDL = 0x000000008,
            /// <summary>use passed dwFileAttribute</summary>
            UseFileAttributes = 0x000000010,
            /// <summary>apply the appropriate overlays</summary>
            AddOverlays = 0x000000020,
            /// <summary>Get the index of the overlay in the upper 8 bits of the iIcon</summary>
            OverlayIndex = 0x000000040,
        }

        /// <summary>
        /// Get the associated Icon for a file or application, this method always returns
        /// an icon.  If the strPath is invalid or there is no idonc the default icon is returned
        /// </summary>
        /// <param name="strPath">full path to the file</param>
        /// <param name="bSmall">if true, the 16x16 icon is returned otherwise the 32x32</param>
        /// <returns></returns>
        public static Icon GetIcon(string strPath, bool bSmall)
        {
            SHFILEINFO info = new SHFILEINFO(true);
            int cbFileInfo = Marshal.SizeOf(info);
            SHGFI flags;
            if (bSmall)
                flags = SHGFI.Icon | SHGFI.SmallIcon | SHGFI.UseFileAttributes;
            else
                flags = SHGFI.Icon | SHGFI.LargeIcon | SHGFI.UseFileAttributes;

            SHGetFileInfo(strPath, 256, out info, (uint)cbFileInfo, flags);
            return Icon.FromHandle(info.hIcon);
        }
    }

    public class ImageToHeaderConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string Filename = (string)value;

            if (Filename == null)
                return null;

            Icon icon = ExtractIcon.GetIcon(Filename, true);

            if (icon != null)
            {
               
                return System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                            icon.Handle,
                            new Int32Rect(0, 0, icon.Width, icon.Height),
                            BitmapSizeOptions.FromEmptyOptions()); 
            }
            
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Logique d'interaction pour DependencyWindow.xaml
    /// </summary>
    public partial class DependencyWindow : UserControl
    {
        PE Pe;


        public DependencyWindow(String FileName)
        {
            InitializeComponent();
            Width = double.NaN;
            Height = double.NaN;

            
            int i = 0;
            this.Pe = new PE(FileName);
            List<PeImportDll> PeImports = this.Pe.GetImports();

            this.ModulesList.Items.Clear();
            this.DllTreeView.Items.Clear();
            TreeViewItem treeNode = new TreeViewItem();
            treeNode.Header = FileName;
            treeNode.DataContext = this.Pe;

            i = 0;
            foreach (PeImportDll DllImport in PeImports)
            {
                // Find Dll in "paths"
                PE ImportPe = new PE("C:\\Windows\\System32\\" + DllImport.Name);

                if (ImportPe.LoadSuccessful)
                {
                    this.ModulesList.Items.Add(new DisplayModuleInfo(i, DllImport, ImportPe.Properties));
                }


                // Add to tree view
                TreeViewItem childTreeNode = new TreeViewItem();
                childTreeNode.Header = DllImport.Name;
                childTreeNode.DataContext = ImportPe;
                treeNode.Items.Add(childTreeNode);
                

                // 
                i++;
            }

            this.DllTreeView.Items.Add(treeNode);
        }

        private void OnTreeViewSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            PE SelectedPE = (PE) ((TreeViewItem) this.DllTreeView.SelectedItem).DataContext;
            List<PeExport> PeExports = SelectedPE.GetExports();
            List<PeImportDll> PeImports = SelectedPE.GetImports();


            this.ImportList.Items.Clear();
            this.ExportList.Items.Clear();

            int i = 0;
            foreach (PeImportDll DllImport in PeImports)
            {
                foreach (PeImport Import in DllImport.ImportList)
                {
                    this.ImportList.Items.Add(new DisplayPeImport(i, Import));
                    i++;
                }
            }

            i = 0;
            foreach (PeExport Export in PeExports)
            {
                this.ExportList.Items.Add(new DisplayPeExport(i, Export));
                i++;
            }
        }
    }
}
