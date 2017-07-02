using System;
using System.IO;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.ClrPh;
using System.ComponentModel;


public class DefaultSettingsBindingHandler : INotifyPropertyChanged
{
    public delegate string CallbackEventHandler(bool settingValue);
    public struct EventHandlerInfo
    {
        public string Property;
        public string Settings;
        public string MemberBindingName;
        public CallbackEventHandler Handler;
    }

    public event PropertyChangedEventHandler PropertyChanged;
    private List<EventHandlerInfo> Handlers;

    public DefaultSettingsBindingHandler()
    {
        Dependencies.Properties.Settings.Default.PropertyChanged += this.Handler_PropertyChanged;
        Handlers = new List<EventHandlerInfo>();
    }

    public void AddNewEventHandler(string PropertyName, string SettingsName, string MemberBindingName, CallbackEventHandler Handler )
    {
        EventHandlerInfo info = new EventHandlerInfo();
        info.Property = PropertyName;
        info.Settings = SettingsName;
        info.MemberBindingName = MemberBindingName;
        info.Handler = Handler;

        Handlers.Add(info);
    }

    public virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void Handler_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        foreach (EventHandlerInfo Handler in Handlers.FindAll(x => x.Property == e.PropertyName))
        {
            Handler.Handler(((bool)Dependencies.Properties.Settings.Default[Handler.Settings]));
            OnPropertyChanged(Handler.MemberBindingName);
        }

        
    }
}


public class DisplayPeImport : DefaultSettingsBindingHandler
{

    public DisplayPeImport(
        /*_In_*/ PeImport PeImport,
        /*_In_*/ PhSymbolProvider SymPrv,
        /*_In_*/ string ModuleFilePath
    )
    {
       Info.ordinal = PeImport.Ordinal;
       Info.hint = PeImport.Hint;
       Info.name = PeImport.Name;
       Info.moduleName = PeImport.ModuleName;
       Info.modulePath = ModuleFilePath;
       Info.UndecoratedName = SymPrv.UndecorateName(PeImport.Name);

       Info.delayedImport = PeImport.DelayImport;
       Info.importAsCppName = (PeImport.Name.Length > 0 && PeImport.Name[0] == '?');
       Info.importByOrdinal = PeImport.ImportByOrdinal;
       Info.importNotFound = ModuleFilePath == null;


        AddNewEventHandler("Undecorate", "Undecorate", "Name", this.GetDisplayName);
        AddNewEventHandler("FullPath", "FullPath", "ModuleName", this.GetPathDisplayName);
    }


    public string IconUri
    {
        get
        {
            if (Info.importNotFound)
                 return "Images/import_err.gif";
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
            if (Info.importNotFound)
                return 1;
            if (Info.importByOrdinal)
                return 2;
            if (Info.importAsCppName)
                return 3;

            return 0;
        }
    }
    public int Hint { get { return Info.hint; } }
    public int? Ordinal { get { if (Info.importByOrdinal) { return Info.ordinal; } return null; } }

    public string Name
    {
        get { return GetDisplayName(Dependencies.Properties.Settings.Default.Undecorate); }
    }

    public string ModuleName
    {
        get { return GetPathDisplayName(Dependencies.Properties.Settings.Default.FullPath); }
    }

    public Boolean DelayImport { get { return Info.delayedImport; } }


    protected string GetDisplayName(bool UndecorateName)
    {
        
        if ((UndecorateName) && (Info.UndecoratedName.Length > 0))
            return Info.UndecoratedName;
        
        else if (Info.importByOrdinal)
            return String.Format("Ordinal_{0:d}", Info.ordinal);
       
       return Info.name;
    }

    protected string GetPathDisplayName(bool FullPath)
    {
        if ((FullPath) && (Info.modulePath != null))
            return Info.modulePath;

        return Info.moduleName;
    }

    


    private PeImportInfo Info;
}

public struct PeImportInfo
{
   public int ordinal;
   public int hint;

   public string name;
   public string moduleName;
   public string modulePath;
   public string UndecoratedName;

   public Boolean delayedImport;
   public Boolean importByOrdinal;
   public Boolean importAsCppName;
   public Boolean importNotFound;

}

public class DisplayPeExport : DefaultSettingsBindingHandler
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
        PeInfo.UndecoratedName = SymPrv.UndecorateName(PeExport.Name);

        AddNewEventHandler("Undecorate", "Undecorate", "Name", this.GetDisplayName);
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
        get { return GetDisplayName(Dependencies.Properties.Settings.Default.Undecorate); }
    }

    public string VirtualAddress { get { return String.Format("0x{0:x8}", PeInfo.virtualAddress); } }



    protected string GetDisplayName(bool Undecorate)
    { 
        if (PeInfo.forwardedExport)
            return PeInfo.ForwardName;

        if (PeInfo.exportByOrdinal)
            return String.Format("Ordinal_{0:d}", PeInfo.ordinal);


        if ((Undecorate) && (PeInfo.UndecoratedName.Length > 0))
            return PeInfo.UndecoratedName;

        return PeInfo.name;
        
    }


    private PeExportInfo PeInfo;
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
    public DisplayErrorModuleInfo(PeImportDll Module)
    : base(Module.Name)
    {
    }

    public override string Cpu { get { return ""; } }
    public override string Type { get { return ""; } }
    public override UInt32? Filesize { get { return null; } }
    public override UInt64? ImageBase { get { return null; } }
    public override int? VirtualSize { get { return null; } }
    public override UInt64? EntryPoint { get { return null; } }
    public override int? Subsystem { get { return null; } }
    public override string SubsystemVersion { get { return ""; } }
    public override string Checksum { get { return ""; } }

}

public class DisplayModuleInfo : DefaultSettingsBindingHandler
{
    public DisplayModuleInfo(string ModuleName)
    {
        Info.Name = ModuleName;
        Info.Filepath = "";

        AddNewEventHandler("FullPath", "FullPath", "Name", this.GetPathDisplayName);
    }

    public DisplayModuleInfo(PeImportDll Module, PE Pe)
    {   
        Info.Name = Module.Name;
        Info.Filepath = Pe.Filepath;

        Info.Machine = Pe.Properties.Machine;
        Info.Magic = Pe.Properties.Magic;

        Info.ImageBase = (UInt64) Pe.Properties.ImageBase;
        Info.SizeOfImage = Pe.Properties.SizeOfImage;
        Info.EntryPoint = (UInt64) Pe.Properties.EntryPoint;

        Info.Checksum = Pe.Properties.Checksum;
        Info.CorrectChecksum = Pe.Properties.CorrectChecksum;

        Info.Subsystem = Pe.Properties.Subsystem;
        Info.Characteristics = Pe.Properties.Characteristics;
        Info.DllCharacteristics = Pe.Properties.DllCharacteristics;

        Info.context.ImportProperties = Module;
        Info.context.PeProperties = Pe;

        AddNewEventHandler("FullPath", "FullPath", "ModuleName", this.GetPathDisplayName);
    }

    public virtual TreeViewItemContext Context { get { return Info.context; } }

    public string ModuleName
    {
        get { return GetPathDisplayName(Dependencies.Properties.Settings.Default.FullPath); }
    }

    //public virtual string Name { get { return Info.Name; } }
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
    public virtual UInt32? Filesize { get { return 0x00; } }
    public virtual UInt64? ImageBase { get { return Info.ImageBase; } }
    public virtual int? VirtualSize { get { return Info.SizeOfImage; } }
    public virtual UInt64? EntryPoint { get { return Info.EntryPoint; } }
    public virtual int? Subsystem { get { return Info.Subsystem; } }
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


    protected string GetPathDisplayName(bool FullPath)
    {
        if ((FullPath) && (Info.Filepath.Length > 0))
            return Info.Filepath;

        return Info.Name;
    }

    private ModuleInfo Info;
}


public struct ModuleInfo
{
    // @TODO(Hack: refactor correctly for image generation)
    public TreeViewItemContext context;

    public string Name;
    public string Filepath;

    public Int16 Machine;
    public Int16 Magic;

    public UInt64 ImageBase;
    public Int32 SizeOfImage;
    public UInt64 EntryPoint;


    public Int32 Checksum;
    public Boolean CorrectChecksum;

    public Int16 Subsystem;
    public Int16 Characteristics;
    public Int16 DllCharacteristics;
}

public struct TreeViewItemContext
{
    // union-like
    public PE PeProperties; // null if not found
    public PeImportDll ImportProperties;

    public string ModuleName;
    public string PeFilePath; // null if not found

    public List<PeExport> PeExports; // null if not found
    public List<PeImportDll> PeImports; // null if not found
}

namespace Dependencies
{
    public class ModuleTreeViewItem : TreeViewItem, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public ModuleTreeViewItem()
        {
            Dependencies.Properties.Settings.Default.PropertyChanged += this.ModuleTreeViewItem_PropertyChanged;
        }

        public virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string GetTreeNodeHeaderName(bool FullPath)
        {
            TreeViewItemContext Context = ((TreeViewItemContext)DataContext);

            if ((FullPath) && (Context.PeFilePath != null))
            {
                return Context.PeFilePath;
            }
            else
            {
                return Context.ModuleName;
            }
        }

    
        private void ModuleTreeViewItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "FullPath")
            {
                this.Header = (object) GetTreeNodeHeaderName(Dependencies.Properties.Settings.Default.FullPath);
            }
        }
    }



    /// <summary>
    /// Logique d'interaction pour DependencyWindow.xaml
    /// </summary>
    public partial class DependencyWindow : UserControl
    {
        PE Pe;
        string RootFolder;
        PhSymbolProvider SymPrv;
        HashSet<String> ModulesFound;
        HashSet<String> ModulesNotFound;


        public Boolean ProcessPe(int level,  TreeViewItem currentNode, PE newPe)
        {
          
            List<PeImportDll> PeImports = newPe.GetImports();
            List<Tuple<ModuleTreeViewItem, PE>> BacklogPeToProcess = new List<Tuple<ModuleTreeViewItem, PE>>();

            foreach (PeImportDll DllImport in PeImports)
            {
                ModuleTreeViewItem childTreeNode = new ModuleTreeViewItem();

                // Find Dll in "paths"
                String PeFilePath = FindPe.FindPeFromDefault(DllImport.Name, RootFolder, this.Pe.IsWow64Dll() );
                PE ImportPe = null;

                if (PeFilePath != null)
                {
                    ImportPe = new PE(PeFilePath);

                    if (!this.ModulesFound.Contains(PeFilePath))
                    {
                        
                        this.ModulesFound.Add(PeFilePath);

                        // do not process twice the same PE in order to lessen memory pressure
                        BacklogPeToProcess.Add(new Tuple<ModuleTreeViewItem, PE>(childTreeNode, ImportPe));

                        this.ModulesList.Items.Add(new DisplayModuleInfo(DllImport, ImportPe));
                    }       
                }
                else
                {
                    if (!this.ModulesNotFound.Contains(DllImport.Name))
                    {

                        this.ModulesNotFound.Add(DllImport.Name);

                    
                        this.ModulesList.Items.Add(new DisplayErrorModuleInfo(DllImport));
                    }
                }


                // Add to tree view
                TreeViewItemContext childTreeInfoContext = new TreeViewItemContext();
                childTreeInfoContext.PeProperties = ImportPe;
                childTreeInfoContext.ImportProperties = DllImport;
                childTreeInfoContext.PeFilePath = PeFilePath;
                childTreeInfoContext.ModuleName = DllImport.Name;

                childTreeNode.DataContext = childTreeInfoContext;
                childTreeNode.Header = childTreeNode.GetTreeNodeHeaderName(Dependencies.Properties.Settings.Default.FullPath);
                currentNode.Items.Add(childTreeNode);
            }


            // Process next batch of dll imports
            foreach (Tuple<ModuleTreeViewItem, PE> NewPeNode in BacklogPeToProcess)
            {
                ProcessPe(level+1, NewPeNode.Item1, NewPeNode.Item2); // warning : recursive call
            }

            return true;

        }

        public DependencyWindow(String FileName)
        {

            InitializeComponent();

            this.Pe = new PE(FileName);
            this.RootFolder = Path.GetDirectoryName(FileName);
            this.SymPrv = new PhSymbolProvider();
            this.ModulesFound = new HashSet<String>();
            this.ModulesNotFound = new HashSet<String>();

            this.ModulesList.Items.Clear();
            this.DllTreeView.Items.Clear();

            ModuleTreeViewItem treeNode = new ModuleTreeViewItem();
            TreeViewItemContext childTreeInfoContext = new TreeViewItemContext();

            childTreeInfoContext.PeProperties = this.Pe;
            childTreeInfoContext.ImportProperties = null;
            childTreeInfoContext.PeFilePath = this.Pe.Filepath;
            childTreeInfoContext.ModuleName = FileName;

            treeNode.DataContext = childTreeInfoContext;
            treeNode.Header = treeNode.GetTreeNodeHeaderName(Dependencies.Properties.Settings.Default.FullPath);
            treeNode.IsExpanded = true;
            
            this.DllTreeView.Items.Add(treeNode);


            // Recursively construct tree of dll imports
            ProcessPe(0, treeNode, this.Pe);
            

        }

        private void OnTreeViewSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            // TreeViewItemContext childTreeContext = ((TreeViewItemContext) (this.DllTreeView.SelectedItem as TreeViewItem).DataContext);
            TreeViewItemContext childTreeContext = ((TreeViewItemContext)(this.DllTreeView.SelectedItem as ModuleTreeViewItem).DataContext);

            PE SelectedPE = childTreeContext.PeProperties;

            this.ImportList.Items.Clear();
            this.ExportList.Items.Clear();

            // Selected Pe has not been found on disk
            if (SelectedPE == null)
                return;

            // Process imports and exports on first load
            if (childTreeContext.PeExports == null) { childTreeContext.PeExports = SelectedPE.GetExports(); }
            if (childTreeContext.PeImports == null) { childTreeContext.PeImports = SelectedPE.GetImports(); }

                
            
            foreach (PeImportDll DllImport in childTreeContext.PeImports)
            {
                String PeFilePath = FindPe.FindPeFromDefault(DllImport.Name, RootFolder, this.Pe.IsWow64Dll());

                foreach (PeImport Import in DllImport.ImportList)
                {
                    this.ImportList.Items.Add(new DisplayPeImport(Import, SymPrv, PeFilePath));
                }
            }

            foreach (PeExport Export in childTreeContext.PeExports)
            {
                this.ExportList.Items.Add(new DisplayPeExport(Export, SymPrv));
            }

        }
    }
}
