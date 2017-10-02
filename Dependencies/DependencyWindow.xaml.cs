using System;
using System.IO;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.ClrPh;
using System.ComponentModel;
using System.Windows.Input;
using System.Diagnostics;

public class RelayCommand : ICommand
{
    #region Fields 
    readonly Action<object> _execute;
    readonly Predicate<object> _canExecute;
    #endregion // Fields 
    #region Constructors 
    public RelayCommand(Action<object> execute) : this(execute, null) { }
    public RelayCommand(Action<object> execute, Predicate<object> canExecute)
    {
        if (execute == null)
            throw new ArgumentNullException("execute");
        _execute = execute; _canExecute = canExecute;
    }
    #endregion // Constructors 
    #region ICommand Members 
    [DebuggerStepThrough]
    public bool CanExecute(object parameter)
    {
        return _canExecute == null ? true : _canExecute(parameter);
    }
    public event EventHandler CanExecuteChanged
    {
        add { CommandManager.RequerySuggested += value; }
        remove { CommandManager.RequerySuggested -= value; }
    }
    public void Execute(object parameter) { _execute(parameter); }
    #endregion // ICommand Members 
}


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


public struct TreeViewItemContext
{
    public TreeViewItemContext(TreeViewItemContext other)
    {
        
        this.PeProperties = other.PeProperties;
        this.ImportProperties = other.ImportProperties; ;

        this.ModuleName = other.ModuleName;
        this.PeFilePath = other.PeFilePath;
        this.IsApiSet = other.IsApiSet;
        this.ApiSetModuleName = other.ApiSetModuleName;

        this.PeExports = other.PeExports;
        this.PeImports = other.PeImports;
    }


    // union-like
    public PE PeProperties; // null if not found
    public PeImportDll ImportProperties;

    public string ModuleName;
    public string PeFilePath; // null if not found
    public bool IsApiSet;
    public string ApiSetModuleName;

    public List<PeExport> PeExports; // null if not found
    public List<PeImportDll> PeImports; // null if not found
}

public struct DependencyNodeContext
{
    public DisplayModuleInfo ModuleInfo;
    public bool IsDummy;
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

        public ModuleTreeViewItem(ModuleTreeViewItem Other)
        {
            Dependencies.Properties.Settings.Default.PropertyChanged += this.ModuleTreeViewItem_PropertyChanged;

            this.DataContext = new TreeViewItemContext( (TreeViewItemContext) Other.DataContext );
        }

        public virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string GetTreeNodeHeaderName(bool FullPath)
        {
            return ((DependencyNodeContext)this.DataContext).ModuleInfo.ModuleName;
        }

        public string ModuleFilePath
        {
            get
            {
                return ((DependencyNodeContext) this.DataContext).ModuleInfo.Filepath;
            }
        }

        public bool IsModuleDelayLoad
        {
            get
            {
                return ((DependencyNodeContext)this.DataContext).ModuleInfo.DelayLoad;
            }
        }


        private void ModuleTreeViewItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "FullPath")
            {
                this.Header = (object) GetTreeNodeHeaderName(Dependencies.Properties.Settings.Default.FullPath);
            }
        }

        #region Commands 
        public RelayCommand OpenPeviewerCommand
        {
            get
            {
                if (_OpenPeviewerCommand == null)
                {
                    _OpenPeviewerCommand = new RelayCommand((param) => this.OpenPeviewer((object)param));
                }

                return _OpenPeviewerCommand;
            }
        }

        public bool OpenPeviewer(object Context)
        {
            string programPath = @".\peview.exe";
            Process PeviewerProcess = new Process();

            if (Context == null)
            {
                return false;
            }

            if (!File.Exists(programPath))
            {
                System.Windows.MessageBox.Show("peview.exe file could not be found !");
                return false;
            }

            string Filepath = ((TreeViewItemContext)Context).PeProperties.Filepath;
            if (Filepath == null)
            {
                return false;
            }

            PeviewerProcess.StartInfo.FileName = programPath;
            PeviewerProcess.StartInfo.Arguments = Filepath;
            return PeviewerProcess.Start();
        }

        public RelayCommand OpenNewAppCommand
        {
            get
            {
                if (_OpenPeviewerCommand == null)
                {
                    _OpenNewAppCommand = new RelayCommand((param) =>
                    {
                        string Filepath = ((TreeViewItemContext)param).PeProperties.Filepath;
                        if (Filepath == null)
                        {
                            return;
                        }

                        Process PeviewerProcess = new Process();
                        PeviewerProcess.StartInfo.FileName = System.Windows.Forms.Application.ExecutablePath;
                        PeviewerProcess.StartInfo.Arguments = Filepath;
                        PeviewerProcess.Start();
                    });
                }

                return _OpenNewAppCommand;
            }
        }
        #endregion // Commands 

        private RelayCommand _OpenPeviewerCommand;
        private RelayCommand _OpenNewAppCommand;
    }



    public class ModuleCacheKey : Tuple<string, string>
    {
        public ModuleCacheKey(string Name, string Filepath)
        :base(Name, Filepath)
        {
        }
    }

    public class ModulesCache : Dictionary<ModuleCacheKey, DisplayModuleInfo>
    {

    }

    /// <summary>
    /// Logique d'interaction pour DependencyWindow.xaml
    /// </summary>
    public partial class DependencyWindow : TabItem 
    {
        PE Pe;
        string RootFolder;
        PhSymbolProvider SymPrv;
        SxsEntries SxsEntriesCache;
        ApiSetSchema ApiSetmapCache;
        ModulesCache ProcessedModulesCache;

        /// <summary>
        /// Background processing of a single PE file.
        /// It can be lengthy since there are disk access (and misses).
        /// </summary>
        /// <param name="NewTreeContexts"> This variable is passed as reference to be updated since this function is run in a separate thread. </param>
        /// <param name="newPe"> Current PE file analyzed </param>
        private void ProcessPe(List<TreeViewItemContext> NewTreeContexts, PE newPe)
        {
            List<PeImportDll> PeImports = newPe.GetImports();
            foreach (PeImportDll DllImport in PeImports)
            {
                bool FoundApiSet = false;
                string ImportDllName = DllImport.Name;
                string ImportDllNameWithoutExtension = Path.GetFileNameWithoutExtension(ImportDllName);

                // Look for api set target 
                if (this.ApiSetmapCache.ContainsKey(ImportDllNameWithoutExtension))
                {
                    ApiSetTarget Targets = this.ApiSetmapCache[ImportDllNameWithoutExtension];
                    if (Targets.Count > 0)
                    {
                        FoundApiSet = true;
                        ImportDllName = Targets[0];
                    }
                }

                // Find Dll in "paths"
                String PeFilePath = FindPe.FindPeFromDefault(this.Pe, ImportDllName, this.SxsEntriesCache);
                PE ImportPe = (PeFilePath != null) ? new PE(PeFilePath) : null;


                TreeViewItemContext childTreeInfoContext = new TreeViewItemContext();
                childTreeInfoContext.PeProperties = ImportPe;
                childTreeInfoContext.ImportProperties = DllImport;
                childTreeInfoContext.PeFilePath = PeFilePath;
                childTreeInfoContext.ModuleName = DllImport.Name;
                childTreeInfoContext.IsApiSet = FoundApiSet;
                childTreeInfoContext.ApiSetModuleName = ImportDllName;

                NewTreeContexts.Add(childTreeInfoContext);
            }
        }

        private void ConstructDependencyTree(ModuleTreeViewItem RootNode, string FilePath, int RecursionLevel = 0)
        {
            ConstructDependencyTree(RootNode, new PE(FilePath), RecursionLevel);
        }

        private void ConstructDependencyTree(ModuleTreeViewItem RootNode, PE CurrentPE, int RecursionLevel = 0)
        {
            // "Closured" variables (it 's a scope hack really).
            List<Tuple<ModuleTreeViewItem, PE>> BacklogPeToProcess = new List<Tuple<ModuleTreeViewItem, PE>>();
            List<TreeViewItemContext> NewTreeContexts = new List<TreeViewItemContext>();

            BackgroundWorker bw = new BackgroundWorker();
            bw.WorkerReportsProgress = true; // useless here for now


            bw.DoWork += (sender, e) => {
                // Reduce cpu hogginess by imposing a sleep in order to get the
                // STA to redraw new nodes
                if (RecursionLevel > 0)
                {
                    System.Threading.Thread.Sleep(100);
                }

                ProcessPe(NewTreeContexts, CurrentPE);
            };


            bw.RunWorkerCompleted += (sender, e) =>
            { 
                List<ModuleTreeViewItem> PeWithDummyEntries = new List<ModuleTreeViewItem>();

                // Important !
                // 
                // This handler is executed in the STA (Single Thread Application)
                // which is authorized to manipulate UI elements. The BackgroundWorker is not.
                //

                foreach (TreeViewItemContext NewTreeContext in NewTreeContexts)
                {
                    ModuleTreeViewItem childTreeNode = new ModuleTreeViewItem();
                    DependencyNodeContext childTreeNodeContext = new DependencyNodeContext();
                    childTreeNodeContext.IsDummy = false;

                    string ModuleName = NewTreeContext.ModuleName;
                    string ModuleFilePath = NewTreeContext.PeFilePath;
                    ModuleCacheKey ModuleKey = new ModuleCacheKey(ModuleName, ModuleFilePath);

                    // Newly seen modules
                    if (!this.ProcessedModulesCache.ContainsKey(ModuleKey))
                    {
                        // Missing module "found"
                        if (NewTreeContext.PeFilePath == null)
                        {
                            this.ProcessedModulesCache[ModuleKey] = new NotFoundModuleInfo(ModuleName);
                        }
                        else
                        {
                            bool DelayLoad = (NewTreeContext.ImportProperties.Flags & 0x01) == 0x01; // TODO : Use proper macros

                            if (NewTreeContext.IsApiSet)
                            {
                                var ApiSetContractModule = new DisplayModuleInfo(NewTreeContext.ApiSetModuleName, NewTreeContext.PeProperties, DelayLoad);
                                var NewModule = new ApiSetModuleInfo(NewTreeContext.ModuleName, ref ApiSetContractModule);

                                this.ProcessedModulesCache[ModuleKey] = NewModule;
                            }
                            else
                            {
                                var NewModule = new DisplayModuleInfo(NewTreeContext.ModuleName, NewTreeContext.PeProperties, DelayLoad);
                                this.ProcessedModulesCache[ModuleKey] = NewModule;
                            }

                            // do not process twice the same PE in order to lessen memory pressure
                            BacklogPeToProcess.Add(new Tuple<ModuleTreeViewItem, PE>(childTreeNode, NewTreeContext.PeProperties));
                        }

                        // add it to the module list
                        this.ModulesList.Items.Add(this.ProcessedModulesCache[ModuleKey]);
                    }
                    else
                    {
                        // Since we uniquely process PE, for thoses who have already been "seen",
                        // we set a dummy entry in order to set the "[+]" icon next to the node.
                        // The dll dependencies are actually resolved on user double-click action
                        // We can't do the resolution in the same time as the tree construction since
                        // it's asynchronous (we would have to wait for all the background to finish and
                        // use another Async worker to resolve).

                        if ((NewTreeContext.PeProperties != null) && (NewTreeContext.PeProperties.GetImports().Count > 0))
                        {
                            ModuleTreeViewItem DummyEntry = new ModuleTreeViewItem();
                            DependencyNodeContext DummyContext = new DependencyNodeContext()
                            {
                                ModuleInfo = new NotFoundModuleInfo("Dummy"),
                                IsDummy = true
                            };

                            DummyEntry.DataContext = DummyContext;
                            DummyEntry.Header = "@Dummy : if you see this header, it's a bug.";
                            DummyEntry.IsExpanded = false;

                            childTreeNode.Items.Add(DummyEntry);
                            childTreeNode.Expanded += ResolveDummyEntries;
                        }
                    }

                    var Module = this.ProcessedModulesCache[ModuleKey];
                    childTreeNodeContext.ModuleInfo = Module;

                    // Add to tree view
                    childTreeNode.DataContext = childTreeNodeContext;
                    childTreeNode.Header = childTreeNode.GetTreeNodeHeaderName(Dependencies.Properties.Settings.Default.FullPath);
                    RootNode.Items.Add(childTreeNode);
                }


                // Process next batch of dll imports
                foreach (var NewPeNode in BacklogPeToProcess)
                {
                    ConstructDependencyTree(NewPeNode.Item1, NewPeNode.Item2, RecursionLevel + 1); // warning : recursive call
                }

            };

            bw.RunWorkerAsync();
        }

        
        public void ResolveDummyEntries(object sender, RoutedEventArgs e)
        {
            ModuleTreeViewItem NeedDummyPeNode = e.OriginalSource as ModuleTreeViewItem;
            DependencyNodeContext Context = (DependencyNodeContext)NeedDummyPeNode.DataContext;
            //TODO: Improve resolution predicate
            if (!Context.IsDummy)
            {
                return;
            }

            NeedDummyPeNode.Items.Clear();
            string Filepath = Context.ModuleInfo.Filepath;

            ConstructDependencyTree(NeedDummyPeNode, Filepath);     
        }

        public DependencyWindow(String FileName)
        {

            InitializeComponent();
            this.Header = Path.GetFileName(FileName);

            this.SymPrv = new PhSymbolProvider();
            

            this.Pe = new PE(FileName);
            this.RootFolder = Path.GetDirectoryName(FileName);
            this.SxsEntriesCache = SxsManifest.GetSxsEntries(this.Pe);
            this.ProcessedModulesCache = new ModulesCache();
            this.ApiSetmapCache = Phlib.GetApiSetSchema();

            this.ModulesList.Items.Clear();
            this.DllTreeView.Items.Clear();

            ModuleTreeViewItem treeNode = new ModuleTreeViewItem();
            DependencyNodeContext childTreeInfoContext = new DependencyNodeContext()
            {
                ModuleInfo = new DisplayModuleInfo(Path.GetFileName(FileName), this.Pe),
                IsDummy = false
            };

            treeNode.DataContext = childTreeInfoContext;
            treeNode.Header = treeNode.GetTreeNodeHeaderName(Dependencies.Properties.Settings.Default.FullPath);
            treeNode.IsExpanded = true;
            
            this.DllTreeView.Items.Add(treeNode);

      
            // Recursively construct tree of dll imports
            ConstructDependencyTree(treeNode, this.Pe);
        }

        #region Commands
        private void OnListViewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            System.Windows.Controls.ListView ListView = sender as System.Windows.Controls.ListView;
            bool CtrlKeyDown = Keyboard.IsKeyDown(System.Windows.Input.Key.LeftCtrl) || Keyboard.IsKeyDown(System.Windows.Input.Key.RightCtrl);

            Debug.WriteLine("Key Pressed : " + e.Key + ". Ctrl Key down : " + CtrlKeyDown);
            if ((e.Key == System.Windows.Input.Key.C) && CtrlKeyDown)
            {
                List<string> StrToCopy = new List<string>();
                foreach (object SelectItem in ListView.SelectedItems)
                {
                    if (ListView.Name == "ModulesList")
                    {
                        DisplayModuleInfo ModuleInfo = SelectItem as DisplayModuleInfo;
                        StrToCopy.Add(ModuleInfo.ModuleName);
                        
                    }
                    else if (ListView.Name == "ImportList")
                    {
                        DisplayPeImport PeInfo = SelectItem as DisplayPeImport;
                        StrToCopy.Add(PeInfo.Name);
                    }
                    else if (ListView.Name == "ExportList")
                    {
                        DisplayPeExport PeInfo = SelectItem as DisplayPeExport;
                        StrToCopy.Add(PeInfo.Name);
                    }
                }

                System.Windows.Clipboard.Clear();
                System.Windows.Clipboard.SetText(String.Join("\n", StrToCopy.ToArray()), System.Windows.TextDataFormat.Text);

            }
        }

        private void OnTreeViewSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            DependencyNodeContext childTreeContext = ((DependencyNodeContext)(this.DllTreeView.SelectedItem as ModuleTreeViewItem).DataContext);
            DisplayModuleInfo SelectedModule = childTreeContext.ModuleInfo;

            //PE SelectedPE = childTreeContext.PeProperties;

            this.ImportList.Items.Clear();
            this.ExportList.Items.Clear();

            // Selected Pe has not been found on disk
            if (SelectedModule == null)
                return;

            // Process imports and exports on first load
            //if (childTreeContext.PeExports == null) { childTreeContext.PeExports = SelectedPE.GetExports(); }
            //if (childTreeContext.PeImports == null) { childTreeContext.PeImports = SelectedPE.GetImports(); }



            foreach (PeImportDll DllImport in SelectedModule.Imports)
            {
                String PeFilePath = FindPe.FindPeFromDefault(this.Pe, DllImport.Name, this.SxsEntriesCache);

                foreach (PeImport Import in DllImport.ImportList)
                {
                    this.ImportList.Items.Add(new DisplayPeImport(Import, SymPrv, PeFilePath));
                }
            }

            foreach (PeExport Export in SelectedModule.Exports)
            {
                this.ExportList.Items.Add(new DisplayPeExport(Export, SymPrv));
            }

        }

        /// <summary>
        /// Reentrant version of Collapse/Expand Node
        /// </summary>
        /// <param name="Item"></param>
        /// <param name="ExpandNode"></param>
        private void CollapseOrExpandAllNodes(ModuleTreeViewItem Item, bool ExpandNode)
        {
            Item.IsExpanded = ExpandNode;
            foreach(ModuleTreeViewItem ChildItem in Item.Items)
            {
                CollapseOrExpandAllNodes(ChildItem, ExpandNode);
            }
        }

        private void ExpandAllNodes_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            // Expanding all nodes tends to slow down the application (massive allocations for node DataContext)
            // TODO : Reduce memory pressure by storing tree nodes data context in a HashSet and find an async trick
            // to improve the command responsiveness.
            System.Windows.Controls.TreeView TreeNode = sender as System.Windows.Controls.TreeView;
            CollapseOrExpandAllNodes((TreeNode.Items[0] as ModuleTreeViewItem), true);
        }

        private void CollapseAllNodes_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            System.Windows.Controls.TreeView TreeNode = sender as System.Windows.Controls.TreeView;
            CollapseOrExpandAllNodes((TreeNode.Items[0] as ModuleTreeViewItem), false);
        }

        private void DoFindModuleInList_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ModuleTreeViewItem Source = e.Source as ModuleTreeViewItem;
            String SelectedModuleName = Source.GetTreeNodeHeaderName(Dependencies.Properties.Settings.Default.FullPath);

            foreach (DisplayModuleInfo item in this.ModulesList.Items)
            {
                if (item.ModuleName == SelectedModuleName)
                {

                    this.ModulesList.SelectedItem = item;
                    this.ModulesList.ScrollIntoView(item);
                    return;
                }
            }
        }

        private void ExpandAllParentNode(ModuleTreeViewItem Item)
        {
            if (Item != null)
            {
                ExpandAllParentNode(Item.Parent as ModuleTreeViewItem);
                Item.IsExpanded = true;
            }
        }

        /// <summary>
        /// Reentrant version of Collapse/Expand Node
        /// </summary>
        /// <param name="Item"></param>
        /// <param name="ExpandNode"></param>
        private bool FindModuleInTree(ModuleTreeViewItem Item, DisplayModuleInfo Module)
        {
            
            if (Item.GetTreeNodeHeaderName(Dependencies.Properties.Settings.Default.FullPath) == Module.ModuleName)
            {
                ExpandAllParentNode(Item.Parent as ModuleTreeViewItem);
                Item.IsSelected = true;
                Item.BringIntoView();

                return true;
            }

            // BFS style search -> return the first matching node with the lowest "depth"
            foreach (ModuleTreeViewItem ChildItem in Item.Items)
            {
                if(ChildItem.GetTreeNodeHeaderName(Dependencies.Properties.Settings.Default.FullPath) == Module.ModuleName)
                {
                    ExpandAllParentNode(Item);
                    ChildItem.IsSelected = true;
                    ChildItem.BringIntoView();
                    return true;
                }
            }

            foreach (ModuleTreeViewItem ChildItem in Item.Items)
            {
                // early exit as soon as we find a matching node
                if (FindModuleInTree(ChildItem, Module))
                    return true;
            }

            return false;
        }

        private void DoFindModuleInTree_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DisplayModuleInfo item = this.ModulesList.SelectedItem as DisplayModuleInfo;
            ModuleTreeViewItem TreeRootItem = this.DllTreeView.Items[0] as ModuleTreeViewItem;
            FindModuleInTree(TreeRootItem, item);
        }

        private void ListViewSelectAll_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            System.Windows.Controls.ListView ListView = sender as System.Windows.Controls.ListView;
            ListView.SelectAll();
        }
        #endregion // Commands 

    }
}
