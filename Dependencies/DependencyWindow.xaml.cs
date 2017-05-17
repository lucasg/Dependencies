using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.ClrPh;




public class DisplayPeImport
{
    public DisplayPeImport(
        /*_In_*/ int Index,
        /*_In_*/ PeImport PeImport,
        /*_In_*/ PhSymbolProvider SymPrv
    )
    {
       Info.index = Index;
       Info.ordinal = PeImport.Ordinal;
       Info.hint = PeImport.Hint;
       Info.name = PeImport.Name;
       Info.moduleName = PeImport.ModuleName;
       Info.delayedImport = PeImport.DelayImport;

        if (PeImport.Name.Length > 0 && PeImport.Name[0] == '?')
            Info.UndecoratedName = SymPrv.UndecorateName(PeImport.Name);
        else
            Info.UndecoratedName = "";
    }

   public int Index { get { return Info.index; } }
   public int Hint { get { return Info.hint; } }
   public int Ordinal { get { return Info.ordinal; } }
   public string Name { get {

            if (Info.UndecoratedName.Length > 0)
                return Info.UndecoratedName;

            return Info.name;
    } }
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
   public string UndecoratedName;
}

public class DisplayPeExport
{
   public DisplayPeExport(
       /*_In_*/ int Index,
        /*_In_*/ PeExport PeExport,
        /*_In_*/ PhSymbolProvider SymPrv
    )
    {
        PeInfo.index = Index;
        PeInfo.ordinal = PeExport.Ordinal;
        PeInfo.hint = /*PeExport.Hint*/ PeExport.Ordinal - 1;
        PeInfo.name = PeExport.Name;
        PeInfo.ForwardName = PeExport.ForwardedName;
        PeInfo.exportByOrdinal = PeExport.ExportByOrdinal;
        PeInfo.forwardedExport = PeExport.ForwardedName.Length > 0;
        PeInfo.virtualAddress = PeExport.VirtualAddress;

        if (PeExport.Name.Length > 0 && PeExport.Name[0] == '?')
            PeInfo.UndecoratedName = SymPrv.UndecorateName(PeExport.Name);
        else
            PeInfo.UndecoratedName = "";
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


            if (PeInfo.UndecoratedName.Length > 0)
                return PeInfo.UndecoratedName;

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
    public string UndecoratedName;
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
    public DisplayModuleInfo(uint index, PeImportDll Module, PeProperties Properties)
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

    public uint Index { get { return Info.index; } }
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
    public string Filesize { get { return String.Format("0x{0:x8}", 0x00); } }
    public string ImageBase { get { return String.Format("0x{0:x8}", Info.ImageBase); } }
    public string VirtualSize { get { return String.Format("0x{0:x8}", Info.SizeOfImage); } }
    public string EntryPoint { get { return String.Format("0x{0:x8}", Info.EntryPoint); } }
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
    public uint index;
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
        PhSymbolProvider SymPrv;
        HashSet<String> ModulesFound;


        public Boolean ProcessPe(int level,  TreeViewItem currentNode, PE newPe)
        {
            
            List<PeImportDll> PeImports = newPe.GetImports();

            List<Tuple<TreeViewItem, PE>> BacklogPeToProcess = new List<Tuple<TreeViewItem, PE>>();
            foreach (PeImportDll DllImport in PeImports)
            {
                // Find Dll in "paths"
                String PeFilePath = "C:\\Windows\\System32\\" + DllImport.Name;
                PE ImportPe = new PE(PeFilePath);

                if (!ImportPe.LoadSuccessful)
                    continue;


                // Add to tree view
                TreeViewItem childTreeNode = new TreeViewItem();
                childTreeNode.Header = DllImport.Name;
                childTreeNode.DataContext = ImportPe;
                currentNode.Items.Add(childTreeNode);


                if (!this.ModulesFound.Contains(PeFilePath))
                {
                    this.ModulesList.Items.Add(new DisplayModuleInfo(0xdeadbeef, DllImport, ImportPe.Properties));
                    this.ModulesFound.Add(PeFilePath);

                    // do not process twice the same PE in order to lessen memory pressure
                    BacklogPeToProcess.Add(new Tuple<TreeViewItem, PE>(childTreeNode, ImportPe));
                }                    
            }


            // Process next batch of dll imports
            foreach (Tuple<TreeViewItem, PE> NewPeNode in BacklogPeToProcess)
            {
                ProcessPe(level+1, NewPeNode.Item1, NewPeNode.Item2); // warning : recursive call
            }

            return true;

        }

        public DependencyWindow(String FileName)
        {
            InitializeComponent();
            Width = double.NaN;
            Height = double.NaN;
            
            this.Pe = new PE(FileName);
            this.SymPrv = new PhSymbolProvider();
            this.ModulesFound = new HashSet<String>();

            this.ModulesList.Items.Clear();
            this.DllTreeView.Items.Clear();
            
            TreeViewItem treeNode = new TreeViewItem();
            treeNode.Header = FileName;
            treeNode.DataContext = this.Pe;
            treeNode.IsExpanded = true;

            this.DllTreeView.Items.Add(treeNode);


            // Recursively construct tree of dll imports
            ProcessPe(0, treeNode, this.Pe);
            

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
                    this.ImportList.Items.Add(new DisplayPeImport(i, Import, SymPrv));
                    i++;
                }
            }

            i = 0;
            foreach (PeExport Export in PeExports)
            {
                this.ExportList.Items.Add(new DisplayPeExport(i, Export, SymPrv));
                i++;
            }
        }
    }
}
