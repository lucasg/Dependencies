using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.ClrPh;




public class DisplayPeImport
{
    public DisplayPeImport(
        /*_In_*/ PeImport PeImport,
        /*_In_*/ PhSymbolProvider SymPrv
    )
    {
       Info.ordinal = PeImport.Ordinal;
       Info.hint = PeImport.Hint;
       Info.name = PeImport.Name;
       Info.moduleName = PeImport.ModuleName;
       Info.delayedImport = PeImport.DelayImport;
       Info.importAsCppName = (PeImport.Name.Length > 0 && PeImport.Name[0] == '?');
       Info.importByOrdinal = PeImport.ImportByOrdinal;

        if (Info.importAsCppName)
            Info.UndecoratedName = SymPrv.UndecorateName(PeImport.Name);
        else
            Info.UndecoratedName = "";
    }

    public string IconUri
    {
        get
        {
            //if (Info.importNotFound)
            //     return "Images/import_err.gif";
            if (Info.importByOrdinal)
                return "Images/import_ord.gif";
            if (Info.importAsCppName)
                return "Images/import_cpp.gif";

            return "Images/import_c.gif";
        }
    }
    public int Type
    {
        get
        {
            //if (Info.importNotFound)
            //    return 1;
            if (Info.importByOrdinal)
                return 2;
            if (Info.importAsCppName)
                return 3;

            return 0;
        }
    }
    public int Hint { get { return Info.hint; } }
    public int? Ordinal { get { if (Info.importByOrdinal) { return Info.ordinal; } return null; } }

    public string Name { get {

            if (Info.UndecoratedName.Length > 0)
                return Info.UndecoratedName;

            if (Info.importByOrdinal)
                return String.Format("Ordinal_{0:d}", Info.ordinal);

            return Info.name;
    } }
   public string ModuleName { get { return Info.moduleName; } }
   public Boolean DelayImport { get { return Info.delayedImport; } }

   private
       PeImportInfo Info;
}

public struct PeImportInfo
{
   public int ordinal;
   public int hint;
   public string name;
   public string moduleName;
   public Boolean delayedImport;
   public Boolean importByOrdinal;
   public Boolean importAsCppName;
   public string UndecoratedName;
}

public class DisplayPeExport
{
   public DisplayPeExport(
        /*_In_*/ PeExport PeExport,
        /*_In_*/ PhSymbolProvider SymPrv
    )
    {
        PeInfo.ordinal = PeExport.Ordinal;
        PeInfo.hint = /*PeExport.Hint*/ PeExport.Ordinal - 1; // @TODO(add hints to exports)
        PeInfo.name = PeExport.Name;
        PeInfo.ForwardName = PeExport.ForwardedName;
        PeInfo.exportByOrdinal = PeExport.ExportByOrdinal;
        PeInfo.forwardedExport = PeExport.ForwardedName.Length > 0;
        PeInfo.exportAsCppName = (PeExport.Name.Length > 0 && PeExport.Name[0] == '?');
        PeInfo.virtualAddress = PeExport.VirtualAddress;

        if (PeInfo.exportAsCppName)
            PeInfo.UndecoratedName = SymPrv.UndecorateName(PeExport.Name);
        else
            PeInfo.UndecoratedName = "";
    }

    public string IconUri
    {
        get
        {
            if (PeInfo.forwardedExport)
                return "Images/export_forward.gif";
            if (PeInfo.exportByOrdinal)
                return "Images/export_ord.gif";
            if (PeInfo.exportAsCppName)
                return "Images/export_cpp.gif";
            
            return "Images/export_C.gif";
        }
    }

    public int Type
    {
        get
        {
            if (PeInfo.forwardedExport)
                return 1;
            if (PeInfo.exportByOrdinal)
                return 2;
            if (PeInfo.exportAsCppName)
                return 3;

            return 0;
        }
    }
    public int Hint { get { return PeInfo.hint; } }
    public int? Ordinal { get { if (PeInfo.exportByOrdinal) { return PeInfo.ordinal; } return null; } }
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
    public Boolean exportAsCppName;
    public Boolean forwardedExport;
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

public class DisplayErrorModuleInfo : DisplayModuleInfo
{
    public DisplayErrorModuleInfo(uint Index, PeImportDll Module)
    : base(Index, Module.Name)
    {
    }

    public override string Cpu { get { return ""; } }
    public override string Type { get { return ""; } }
    public override string Filesize { get { return ""; } }
    public override string ImageBase { get { return ""; } }
    public override string VirtualSize { get { return ""; } }
    public override string EntryPoint { get { return ""; } }
    public override string Subsystem { get { return ""; } }
    public override string SubsystemVersion { get { return ""; } }
    public override string Checksum { get { return ""; } }

}

public class DisplayModuleInfo
{
    public DisplayModuleInfo(uint index, string ModuleName)
    {
        Info.index = index;
        Info.Name = ModuleName;
    }

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

    public virtual uint Index { get { return Info.index; } }
    public virtual string Name { get { return Info.Name; } }
    public virtual string Cpu
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
    public virtual string Type
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
    public virtual string Filesize { get { return String.Format("0x{0:x8}", 0x00); } }
    public virtual string ImageBase { get { return String.Format("0x{0:x8}", Info.ImageBase); } }
    public virtual string VirtualSize { get { return String.Format("0x{0:x8}", Info.SizeOfImage); } }
    public virtual string EntryPoint { get { return String.Format("0x{0:x8}", Info.EntryPoint); } }
    public virtual string Subsystem { get { return String.Format("{0:x}", Info.Subsystem); } }
    public virtual string SubsystemVersion { get { return ""; } }
    public virtual string Checksum
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

public struct TreeViewItemContext
{
    public PE PeProperties;
    public PeImportDll ImportProperties;
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

                // Add to tree view
                TreeViewItem childTreeNode = new TreeViewItem();
                TreeViewItemContext childTreeContext = new TreeViewItemContext();
                childTreeContext.PeProperties = ImportPe;
                childTreeContext.ImportProperties = DllImport;

                childTreeNode.Header = DllImport.Name;
                childTreeNode.DataContext = childTreeContext;
                currentNode.Items.Add(childTreeNode);

                
                if (!this.ModulesFound.Contains(PeFilePath))
                {
                    if (!ImportPe.LoadSuccessful)
                        this.ModulesList.Items.Add(new DisplayErrorModuleInfo(0xdeadbeef, DllImport));
                    else
                    {
                        this.ModulesList.Items.Add(new DisplayModuleInfo(0xdeadbeef, DllImport, ImportPe.Properties));
                        this.ModulesFound.Add(PeFilePath);
                    }

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

            this.Pe = new PE(FileName);
            this.SymPrv = new PhSymbolProvider();
            this.ModulesFound = new HashSet<String>();

            this.ModulesList.Items.Clear();
            this.DllTreeView.Items.Clear();
            
            TreeViewItem treeNode = new TreeViewItem();
            TreeViewItemContext childTreeContext = new TreeViewItemContext();

            childTreeContext.PeProperties = this.Pe;
            childTreeContext.ImportProperties = null;

            treeNode.Header = FileName;
            treeNode.DataContext = childTreeContext;
            treeNode.IsExpanded = true;
            
            this.DllTreeView.Items.Add(treeNode);


            // Recursively construct tree of dll imports
            ProcessPe(0, treeNode, this.Pe);
            

        }

        private void OnTreeViewSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            TreeViewItemContext childTreeContext = (TreeViewItemContext) (this.DllTreeView.SelectedItem as TreeViewItem).DataContext;
            PE SelectedPE = childTreeContext.PeProperties;

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
