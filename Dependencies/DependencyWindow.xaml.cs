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
    public string VirtualAddress { get { return String.Format("0x{0:08x}", PeInfo.virtualAddress); } }



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

            //this.Title = FileName;

            this.Pe = new PE(FileName);
            List<PeExport> PeExports = Pe.GetExports();
            List<PeImportDll> PeImports = this.Pe.GetImports();


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

            this.ModulesList.Items.Clear();
            this.DllTreeView.Items.Clear();
            TreeViewItem treeNode = new TreeViewItem();
            treeNode.Header = FileName;

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
                treeNode.Items.Add(childTreeNode);

                // 
                i++;
            }

            this.DllTreeView.Items.Add(treeNode);
        }
    }
}
