using System;
using System.IO;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using System.ComponentModel;
using System.Windows.Input;
using System.Diagnostics;
using System.Windows.Data;
using Microsoft.Win32;

using Mono.Cecil;
using Dependencies.ClrPh;

namespace Dependencies
{

    /// <summary>
    /// ImportContext : Describe an import module parsed from a PE.
    /// Only used during the dependency tree building phase
    /// </summary>
    public struct ImportContext
{
        // Import "identifier" 
        public string ModuleName;

        // Return how the module was found (NOT_FOUND otherwise)
        public ModuleSearchStrategy ModuleLocation;

        // If found, set the filepath and parsed PE, otherwise it's null
        public string PeFilePath;
        public PE PeProperties;

        // Some imports are from api sets
        public bool IsApiSet;
        public string ApiSetModuleName;

        // module flag attributes
        public ModuleFlag Flags;
    }


    /// <summary>
    /// Dependency tree building behaviour.
    /// A full recursive dependency tree can be memory intensive, therefore the
    /// choice is left to the user to override the default behaviour.
    /// </summary>
    public class TreeBuildingBehaviour : IValueConverter
    { 
        public enum DependencyTreeBehaviour
        {
            ChildOnly,
            RecursiveOnlyOnDirectImports,
            Recursive,

        }

        public static DependencyTreeBehaviour GetGlobalBehaviour()
        {
            return (DependencyTreeBehaviour) (new TreeBuildingBehaviour()).Convert(
                Dependencies.Properties.Settings.Default.TreeBuildBehaviour,
                null,// targetType
                null,// parameter
                null // System.Globalization.CultureInfo
            );
        }

        #region TreeBuildingBehaviour.IValueConverter_contract
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string StrBehaviour = (string)value;

            switch (StrBehaviour)
            {
                default:
                case "ChildOnly":
                    return DependencyTreeBehaviour.ChildOnly;
                case "RecursiveOnlyOnDirectImports":
                    return DependencyTreeBehaviour.RecursiveOnlyOnDirectImports;
                case "Recursive":
                    return DependencyTreeBehaviour.Recursive;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            DependencyTreeBehaviour Behaviour = (DependencyTreeBehaviour) value;

            switch (Behaviour)
            {
                default:
                case DependencyTreeBehaviour.ChildOnly:
                    return "ChildOnly";
                case DependencyTreeBehaviour.RecursiveOnlyOnDirectImports:
                    return "RecursiveOnlyOnDirectImports";
                case DependencyTreeBehaviour.Recursive:
                    return "Recursive";
            }
        }
        #endregion TreeBuildingBehaviour.IValueConverter_contract
    }

    /// <summary>
    /// User context of every dependency tree node.
    /// </summary>
    public struct DependencyNodeContext
    {
        public DependencyNodeContext(DependencyNodeContext other)
        {
            ModuleInfo = other.ModuleInfo;
            IsDummy = other.IsDummy;
        }

        /// <summary>
        /// We use a WeakReference to point towars a DisplayInfoModule
        /// in order to reduce memory allocations.
        /// </summary>
        public WeakReference ModuleInfo;

        /// <summary>
        /// Depending on the dependency tree behaviour, we may have to
        /// set up "dummy" nodes in order for the parent to display the ">" button.
        /// Those dummy are usually destroyed when their parents is expandend and imports resolved.
        /// </summary>
        public bool IsDummy;
    }

    /// <summary>
    /// Deprendency Tree custom node. It's DataContext is a DependencyNodeContext struct
    /// </summary>
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

            this.DataContext = new DependencyNodeContext( (DependencyNodeContext) Other.DataContext );
        }

        #region PropertyEventHandlers 
        public virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void ModuleTreeViewItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "FullPath")
            {
                this.Header = (object)GetTreeNodeHeaderName(Dependencies.Properties.Settings.Default.FullPath);
            }
        }
        #endregion PropertyEventHandlers

        #region Getters

        public string GetTreeNodeHeaderName(bool FullPath)
        {
            return (((DependencyNodeContext)this.DataContext).ModuleInfo.Target as DisplayModuleInfo).ModuleName;
        }

        public string ModuleFilePath
        {
            get
            {
                return (((DependencyNodeContext)this.DataContext).ModuleInfo.Target as DisplayModuleInfo).Filepath;
            }
        }

        public ModuleFlag Flags
        {
            get
            {
                return (((DependencyNodeContext)this.DataContext).ModuleInfo.Target as DisplayModuleInfo).Flags;
            }
        }

        #endregion Getters


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
            string programPath = Dependencies.Properties.Settings.Default.PeViewerPath;
            Process PeviewerProcess = new Process();

            if (Context == null)
            {
                return false;
            }

            if (!File.Exists(programPath))
            {
                System.Windows.MessageBox.Show(String.Format("{0:s} file could not be found !", programPath));
                return false;
            }

            string Filepath = ModuleFilePath;
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
                if (_OpenNewAppCommand == null)
                {
                    _OpenNewAppCommand = new RelayCommand((param) =>
                    {
                        string Filepath = ModuleFilePath;
                        if (Filepath == null)
                        {
                            return;
                        }

                        Process OtherDependenciesProcess = new Process();
                        OtherDependenciesProcess.StartInfo.FileName = System.Windows.Forms.Application.ExecutablePath;
                        OtherDependenciesProcess.StartInfo.Arguments = Filepath;
                        OtherDependenciesProcess.Start();
                    });
                }

                return _OpenNewAppCommand;
            }
        }

        #endregion // Commands 

        private RelayCommand _OpenPeviewerCommand;
        private RelayCommand _OpenNewAppCommand;
        
    }


    /// <summary>
    /// Dependemcy tree analysis window for a given PE.
    /// </summary>
    public partial class DependencyWindow : TabItem 
    { 

        PE Pe;
		public string RootFolder;
		public string WorkingDirectory;
		string Filename;
        PhSymbolProvider SymPrv;
        SxsEntries SxsEntriesCache;
        ApiSetSchema ApiSetmapCache;
        ModulesCache ProcessedModulesCache;
        DisplayModuleInfo _SelectedModule;
        bool _DisplayWarning;

		public List<string> CustomSearchFolders;

        #region PublicAPI
        public DependencyWindow(String Filename, List<string> CustomSearchFolders = null)
        {
            InitializeComponent();

			if (CustomSearchFolders != null)
			{
				this.CustomSearchFolders = CustomSearchFolders;
			}
			else
			{
				this.CustomSearchFolders = new List<string>();
			}
			
			this.Filename = Filename;
			this.WorkingDirectory = Path.GetDirectoryName(this.Filename);
			InitializeView();
        }

        public void InitializeView()
        {
            this.Pe = (Application.Current as App).LoadBinary(this.Filename);

            if (!this.Pe.LoadSuccessful)
            {
                MessageBox.Show(
                    String.Format("{0:s} is not a valid PE-COFF file", this.Filename),
                    "Invalid PE",
                    MessageBoxButton.OK
                );

                return;
            }

            this.SymPrv = new PhSymbolProvider();
            this.RootFolder = Path.GetDirectoryName(this.Filename);
            this.SxsEntriesCache = SxsManifest.GetSxsEntries(this.Pe);
            this.ProcessedModulesCache = new ModulesCache();
            this.ApiSetmapCache = Phlib.GetApiSetSchema();
            this._SelectedModule = null;
            this._DisplayWarning = false;

            // TODO : Find a way to properly bind commands instead of using this hack
            this.ModulesList.Items.Clear();
            this.ModulesList.DoFindModuleInTreeCommand = DoFindModuleInTree;
            this.ModulesList.ConfigureSearchOrderCommand = ConfigureSearchOrderCommand;

            var RootFilename = Path.GetFileName(this.Filename);
            var RootModule = new DisplayModuleInfo(RootFilename, this.Pe, ModuleSearchStrategy.ROOT);
            this.ProcessedModulesCache.Add(new ModuleCacheKey(RootFilename, this.Filename), RootModule);

            ModuleTreeViewItem treeNode = new ModuleTreeViewItem();
            DependencyNodeContext childTreeInfoContext = new DependencyNodeContext()
            {
                ModuleInfo = new WeakReference(RootModule),
                IsDummy = false
            };

            treeNode.DataContext = childTreeInfoContext;
            treeNode.Header = treeNode.GetTreeNodeHeaderName(Dependencies.Properties.Settings.Default.FullPath);
            treeNode.IsExpanded = true;

            this.DllTreeView.Items.Clear();
            this.DllTreeView.Items.Add(treeNode);

            // Recursively construct tree of dll imports
            ConstructDependencyTree(treeNode, this.Pe);
        }
        #endregion PublicAPI

        #region TreeConstruction
        /// <summary>
        /// Background processing of a single PE file.
        /// It can be lengthy since there are disk access (and misses).
        /// </summary>
        /// <param name="NewTreeContexts"> This variable is passed as reference to be updated since this function is run in a separate thread. </param>
        /// <param name="newPe"> Current PE file analyzed </param>
        private void ProcessPe(Dictionary<string, ImportContext> NewTreeContexts, PE newPe)
        {
            List<PeImportDll> PeImports = newPe.GetImports();

            Environment.SpecialFolder WindowsSystemFolder = (this.Pe.IsWow64Dll()) ?
                Environment.SpecialFolder.SystemX86 :
                Environment.SpecialFolder.System;
            string User32Filepath = Path.Combine(Environment.GetFolderPath(WindowsSystemFolder), "user32.dll");
            string MsCoreeFilepath = Path.Combine(Environment.GetFolderPath(WindowsSystemFolder), "mscoree.dll");

            foreach (PeImportDll DllImport in PeImports)
            {

                ImportContext ImportModule = new ImportContext();
                ImportModule.PeFilePath = null;
                ImportModule.PeProperties = null;
                ImportModule.ModuleName = DllImport.Name;
                ImportModule.ApiSetModuleName = null;
                ImportModule.Flags = 0;
                if (DllImport.IsDelayLoad())
                {
                    ImportModule.Flags |= ModuleFlag.DelayLoad;
                }

                if (NewTreeContexts.ContainsKey(DllImport.Name))
                { 
                    continue;
                }

				// Find Dll in "paths"
				Tuple<ModuleSearchStrategy, PE> ResolvedModule = BinaryCache.ResolveModule(
					this.Pe,
					DllImport.Name,
					this.SxsEntriesCache,
					this.CustomSearchFolders,
					this.WorkingDirectory
				);

                ImportModule.ModuleLocation = ResolvedModule.Item1;
				if (ImportModule.ModuleLocation != ModuleSearchStrategy.NOT_FOUND)
				{
					ImportModule.PeProperties = ResolvedModule.Item2;

                    if (ResolvedModule.Item2 != null)
                    {
                        ImportModule.PeFilePath = ResolvedModule.Item2.Filepath;
                        foreach (var Import in BinaryCache.LookupImports(DllImport, ImportModule.PeFilePath))
                        {
                            if (!Import.Item2)
                            {
                                ImportModule.Flags |= ModuleFlag.MissingImports;
                                break;
                            }

                        }
                    }
				}
				else
				{
					ImportModule.Flags |= ModuleFlag.NotFound;
				}

                // special case for apiset schema
                ImportModule.IsApiSet = (ImportModule.ModuleLocation == ModuleSearchStrategy.ApiSetSchema);
                if (ImportModule.IsApiSet)
				{
					ImportModule.Flags |= ModuleFlag.ApiSet;
					ImportModule.ApiSetModuleName = BinaryCache.LookupApiSetLibrary(DllImport.Name);

					if (DllImport.Name.StartsWith("ext-"))
					{
						ImportModule.Flags |= ModuleFlag.ApiSetExt;
					}
				}

				// add warning for appv isv applications 
				if (String.Compare(DllImport.Name, "AppvIsvSubsystems32.dll", StringComparison.OrdinalIgnoreCase) == 0 ||
                    String.Compare(DllImport.Name, "AppvIsvSubsystems64.dll", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    if (!this._DisplayWarning)
                    {
                        MessageBoxResult result = MessageBox.Show(
                        "This binary use the App-V containerization technology which fiddle with search directories and PATH env in ways Dependencies can't handle.\n\nFollowing results are probably not quite exact.",
                        "App-V ISV disclaimer"
                        );

                        this._DisplayWarning = true; // prevent the same warning window to popup several times
                    }

                }

                NewTreeContexts.Add(DllImport.Name, ImportModule);


                // AppInitDlls are triggered by user32.dll, so if the binary does not import user32.dll they are not loaded.
                if (ImportModule.PeFilePath == User32Filepath)
                {
                    string AppInitRegistryKey = (this.Pe.IsWow64Dll()) ?
                        "HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node\\Microsoft\\Windows NT\\CurrentVersion\\Windows" :
                        "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Windows";

                    int LoadAppInitDlls = (int)Registry.GetValue(AppInitRegistryKey, "LoadAppInit_DLLs", 0);
                    string AppInitDlls = (string)Registry.GetValue(AppInitRegistryKey, "AppInit_DLLs", "");

                    if ((LoadAppInitDlls != 0) && (AppInitDlls != ""))
                    {
                        // Extremely crude parser. TODO : Add support for quotes wrapped paths with spaces
                        foreach (var AppInitDll in AppInitDlls.Split(' '))
                        {
                            Debug.WriteLine("AppInit loading " + AppInitDll);

                            // Do not process twice the same imported module
                            if (null != PeImports.Find(module => module.Name == AppInitDll))
                            {
                                continue;
                            }

                            if (NewTreeContexts.ContainsKey(AppInitDll))
                            { 
                                continue;
                            }

                            ImportContext AppInitImportModule = new ImportContext();
                            AppInitImportModule.PeFilePath = null;
                            AppInitImportModule.PeProperties = null;
                            AppInitImportModule.ModuleName = AppInitDll;
                            AppInitImportModule.ApiSetModuleName = null;
                            AppInitImportModule.Flags = 0;
                            AppInitImportModule.ModuleLocation = ModuleSearchStrategy.AppInitDLL;

                            

                            Tuple<ModuleSearchStrategy, PE> ResolvedAppInitModule = BinaryCache.ResolveModule(
								this.Pe, 
								AppInitDll, 
								this.SxsEntriesCache,
								this.CustomSearchFolders,
								this.WorkingDirectory
							);
                            if (ResolvedAppInitModule.Item1 != ModuleSearchStrategy.NOT_FOUND)
                            {
                                AppInitImportModule.PeProperties = ResolvedAppInitModule.Item2;
                                AppInitImportModule.PeFilePath = ResolvedAppInitModule.Item2.Filepath;
                            }
							else
							{
								AppInitImportModule.Flags |= ModuleFlag.NotFound;
							}

                            NewTreeContexts.Add(AppInitDll, AppInitImportModule);
                        }
                    }
                }


                // if mscoree.dll is imported, it means the module is a C# assembly, and we can use Mono.Cecil to enumerate its references
                if (ImportModule.PeFilePath == MsCoreeFilepath)
                {
                    var resolver = new DefaultAssemblyResolver();
                    resolver.AddSearchDirectory(RootFolder);

                    AssemblyDefinition PeAssembly = AssemblyDefinition.ReadAssembly(newPe.Filepath);

                    foreach (var module in PeAssembly.Modules)
                    {
                        // Process CLR referenced assemblies
                        foreach (var assembly in module.AssemblyReferences)
                        {
                            AssemblyDefinition definition = resolver.Resolve(assembly);

                            foreach (var AssemblyModule in definition.Modules)
                            { 
                                Debug.WriteLine("Referenced Assembling loading " + AssemblyModule.Name + " : "+ AssemblyModule.FileName );

                                // Do not process twice the same imported module
                                if (null != PeImports.Find(mod => mod.Name == Path.GetFileName(AssemblyModule.FileName)))
                                {
                                    continue;
                                }

                                ImportContext AppInitImportModule = new ImportContext();
                                AppInitImportModule.PeFilePath = null;
                                AppInitImportModule.PeProperties = null;
                                AppInitImportModule.ModuleName = Path.GetFileName(AssemblyModule.FileName);
                                AppInitImportModule.ApiSetModuleName = null;
                                AppInitImportModule.Flags = ModuleFlag.ClrReference;
                                AppInitImportModule.ModuleLocation = ModuleSearchStrategy.ClrAssembly;

                                Tuple<ModuleSearchStrategy, PE> ResolvedAppInitModule = BinaryCache.ResolveModule(
									this.Pe, 
									AssemblyModule.FileName, 
									this.SxsEntriesCache,
									this.CustomSearchFolders,
									this.WorkingDirectory
								);
                                if (ResolvedAppInitModule.Item1 != ModuleSearchStrategy.NOT_FOUND)
                                {
                                    AppInitImportModule.PeProperties = ResolvedAppInitModule.Item2;
                                    AppInitImportModule.PeFilePath = ResolvedAppInitModule.Item2.Filepath;
                                }
								else
								{
									AppInitImportModule.Flags |= ModuleFlag.NotFound;
								}

                                if (!NewTreeContexts.ContainsKey(AppInitImportModule.ModuleName))
                                {
                                    NewTreeContexts.Add(AppInitImportModule.ModuleName, AppInitImportModule);
                                }
                            }

                        }

                        // Process unmanaged dlls for native calls
                        foreach (var UnmanagedModule in module.ModuleReferences)
                        {
                            // some clr dll have a reference to an "empty" dll
                            if (UnmanagedModule.Name.Length == 0)
                            {
                                continue;
                            }

                            Debug.WriteLine("Referenced module loading " + UnmanagedModule.Name);

                            // Do not process twice the same imported module
                            if (null != PeImports.Find(m => m.Name == UnmanagedModule.Name))
                            {
                                continue;
                            }

                                

                            ImportContext AppInitImportModule = new ImportContext();
                            AppInitImportModule.PeFilePath = null;
                            AppInitImportModule.PeProperties = null;
                            AppInitImportModule.ModuleName = UnmanagedModule.Name;
                            AppInitImportModule.ApiSetModuleName = null;
                            AppInitImportModule.Flags = ModuleFlag.ClrReference;
                            AppInitImportModule.ModuleLocation = ModuleSearchStrategy.ClrAssembly;

                            Tuple<ModuleSearchStrategy, PE> ResolvedAppInitModule = BinaryCache.ResolveModule(
								this.Pe, 
								UnmanagedModule.Name, 
								this.SxsEntriesCache,
								this.CustomSearchFolders,
								this.WorkingDirectory
							);
                            if (ResolvedAppInitModule.Item1 != ModuleSearchStrategy.NOT_FOUND)
                            {
                                AppInitImportModule.PeProperties = ResolvedAppInitModule.Item2;
                                AppInitImportModule.PeFilePath = ResolvedAppInitModule.Item2.Filepath;
                            }

                            if (!NewTreeContexts.ContainsKey(AppInitImportModule.ModuleName))
                            {
                                NewTreeContexts.Add(AppInitImportModule.ModuleName, AppInitImportModule);
                            }
                        }
                    }
                }
            }
        }

        private class BacklogImport : Tuple<ModuleTreeViewItem, string>
        {
            public BacklogImport(ModuleTreeViewItem Node, string Filepath)
            : base(Node, Filepath)
            {
            }
        }

        private void ConstructDependencyTree(ModuleTreeViewItem RootNode, string FilePath, int RecursionLevel = 0)
        {
            PE CurrentPE = (Application.Current as App).LoadBinary(FilePath);

            if (null == CurrentPE)
            {
                return;
            }

            ConstructDependencyTree(RootNode, CurrentPE, RecursionLevel);
        }

        private void ConstructDependencyTree(ModuleTreeViewItem RootNode, PE CurrentPE, int RecursionLevel = 0)
        {
            // "Closured" variables (it 's a scope hack really).
            Dictionary<string, ImportContext> NewTreeContexts = new Dictionary<string, ImportContext>();

            BackgroundWorker bw = new BackgroundWorker();
            bw.WorkerReportsProgress = true; // useless here for now


            bw.DoWork += (sender, e) => {

                ProcessPe(NewTreeContexts, CurrentPE);
            };


            bw.RunWorkerCompleted += (sender, e) =>
            {
                TreeBuildingBehaviour.DependencyTreeBehaviour SettingTreeBehaviour = Dependencies.TreeBuildingBehaviour.GetGlobalBehaviour();
                List<ModuleTreeViewItem> PeWithDummyEntries = new List<ModuleTreeViewItem>();
                List<BacklogImport> PEProcessingBacklog = new List<BacklogImport>();

                // Important !
                // 
                // This handler is executed in the STA (Single Thread Application)
                // which is authorized to manipulate UI elements. The BackgroundWorker is not.
                //

                foreach (ImportContext NewTreeContext in NewTreeContexts.Values)
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
                        if ((NewTreeContext.PeFilePath == null) || !NativeFile.Exists(NewTreeContext.PeFilePath)) 
                        {
							if (NewTreeContext.IsApiSet)
							{
								this.ProcessedModulesCache[ModuleKey] = new ApiSetNotFoundModuleInfo(ModuleName, NewTreeContext.ApiSetModuleName);
							}
							else
							{
								this.ProcessedModulesCache[ModuleKey] = new NotFoundModuleInfo(ModuleName);
							}
								
                        }
                        else
                        {


                            if (NewTreeContext.IsApiSet)
                            {
                                var ApiSetContractModule = new DisplayModuleInfo(NewTreeContext.ApiSetModuleName, NewTreeContext.PeProperties, NewTreeContext.ModuleLocation, NewTreeContext.Flags);
                                var NewModule = new ApiSetModuleInfo(NewTreeContext.ModuleName, ref ApiSetContractModule);

                                this.ProcessedModulesCache[ModuleKey] = NewModule;

                                if (SettingTreeBehaviour == TreeBuildingBehaviour.DependencyTreeBehaviour.Recursive)
                                {
                                    PEProcessingBacklog.Add(new BacklogImport(childTreeNode, ApiSetContractModule.ModuleName));
                                }
                            }
                            else
                            {
                                var NewModule = new DisplayModuleInfo(NewTreeContext.ModuleName, NewTreeContext.PeProperties, NewTreeContext.ModuleLocation, NewTreeContext.Flags);
                                this.ProcessedModulesCache[ModuleKey] = NewModule;

                                switch(SettingTreeBehaviour)
                                {
                                    case TreeBuildingBehaviour.DependencyTreeBehaviour.RecursiveOnlyOnDirectImports:
                                        if ((NewTreeContext.Flags & ModuleFlag.DelayLoad) == 0)
                                        {
                                            PEProcessingBacklog.Add(new BacklogImport(childTreeNode, NewModule.ModuleName));
                                        }
                                        break;

                                    case TreeBuildingBehaviour.DependencyTreeBehaviour.Recursive:
                                        PEProcessingBacklog.Add(new BacklogImport(childTreeNode, NewModule.ModuleName));
                                        break;
                                }
                            }
                        }

                        // add it to the module list
                        this.ModulesList.AddModule(this.ProcessedModulesCache[ModuleKey]);
                    }
                    
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
                            ModuleInfo = new WeakReference(new NotFoundModuleInfo("Dummy")),
                            IsDummy = true
                        };

                        DummyEntry.DataContext = DummyContext;
                        DummyEntry.Header = "@Dummy : if you see this header, it's a bug.";
                        DummyEntry.IsExpanded = false;

                        childTreeNode.Items.Add(DummyEntry);
                        childTreeNode.Expanded += ResolveDummyEntries;
                    }

                    // Add to tree view
                    childTreeNodeContext.ModuleInfo = new WeakReference(this.ProcessedModulesCache[ModuleKey]);
                    childTreeNode.DataContext = childTreeNodeContext;
                    childTreeNode.Header = childTreeNode.GetTreeNodeHeaderName(Dependencies.Properties.Settings.Default.FullPath);
                    RootNode.Items.Add(childTreeNode);
                }


                // Process next batch of dll imports
                if (SettingTreeBehaviour != TreeBuildingBehaviour.DependencyTreeBehaviour.ChildOnly)
                { 
                    foreach (var ImportNode in PEProcessingBacklog)
                    {
                        ConstructDependencyTree(ImportNode.Item1, ImportNode.Item2, RecursionLevel + 1); // warning : recursive call
                    }
                }


            };

            bw.RunWorkerAsync();
        }

        /// <summary>
        /// Resolve imports when the user expand the node.
        /// </summary>
        private void ResolveDummyEntries(object sender, RoutedEventArgs e)
        {
            ModuleTreeViewItem NeedDummyPeNode = e.OriginalSource as ModuleTreeViewItem;

            if (NeedDummyPeNode.Items.Count == 0)
            {
                return;
            }
            ModuleTreeViewItem MaybeDummyNode = (ModuleTreeViewItem) NeedDummyPeNode.Items[0];
            DependencyNodeContext Context = (DependencyNodeContext)MaybeDummyNode.DataContext;

            //TODO: Improve resolution predicate
            if (!Context.IsDummy)
            {
                return;
            }

            NeedDummyPeNode.Items.Clear();
            string Filepath = NeedDummyPeNode.ModuleFilePath;

            ConstructDependencyTree(NeedDummyPeNode, Filepath);     
        }

        #endregion TreeConstruction

        #region Commands
     
        private void OnModuleViewSelectedItemChanged(object sender, RoutedEventArgs e)
        {
            DisplayModuleInfo SelectedModule = (sender as DependencyModuleList).SelectedItem as DisplayModuleInfo;

            // Selected Pe has not been found on disk
            if (SelectedModule == null)
                return;
			
			// Display module as root (since we can't know which parent it's attached to)
			UpdateImportExportLists(SelectedModule, null);
        }

        private void OnTreeViewSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (this.DllTreeView.SelectedItem == null)
            {
                UpdateImportExportLists(null, null);
                return;
            }
			
			DependencyNodeContext childTreeContext = ((DependencyNodeContext)(this.DllTreeView.SelectedItem as ModuleTreeViewItem).DataContext);
            DisplayModuleInfo SelectedModule = childTreeContext.ModuleInfo.Target as DisplayModuleInfo;

			// Selected Pe has not been found on disk
			if (SelectedModule == null)
                return;

			// Root Item : no parent
			ModuleTreeViewItem TreeRootItem = this.DllTreeView.Items[0] as ModuleTreeViewItem;
			ModuleTreeViewItem SelectedItem = this.DllTreeView.SelectedItem as ModuleTreeViewItem;
			if (SelectedItem == TreeRootItem)
			{
				UpdateImportExportLists(SelectedModule, null);
				return;
			}

			// find parent. TODO : add parent ref to treeview context
			var parent = VisualTreeHelper.GetParent(SelectedItem as DependencyObject);
			while ((parent as TreeViewItem) == null)
			{
				parent = VisualTreeHelper.GetParent(parent);
			}

			DependencyNodeContext parentTreeContext = ((DependencyNodeContext)(parent as ModuleTreeViewItem).DataContext);
			DisplayModuleInfo parentModule = parentTreeContext.ModuleInfo.Target as DisplayModuleInfo;

			UpdateImportExportLists(SelectedModule, parentModule);
        }

        private void UpdateImportExportLists(DisplayModuleInfo SelectedModule, DisplayModuleInfo Parent)
        {
            if (SelectedModule == null)
            {
                this.ImportList.Items.Clear();
                this.ExportList.Items.Clear();
            }
            else
            {
				if (Parent == null) // root module
				{
					this.ImportList.SetRootImports(SelectedModule.Imports, SymPrv, this);
				}
				else
				{
					// Imports from the same dll are not necessarly sequential (see: HDDGuru\RawCopy.exe)
					var machingImports = Parent.Imports.FindAll(imp => imp.Name == SelectedModule._Name);
					this.ImportList.SetImports(SelectedModule.Filepath, SelectedModule.Exports, machingImports, SymPrv, this);
				}

				this.ExportList.SetExports(SelectedModule.Exports, SymPrv);
			}
        }

        public PE LoadImport(string ModuleName, DisplayModuleInfo CurrentModule = null, bool DelayLoad = false)
        {
            if (CurrentModule == null)
            {
                CurrentModule = this._SelectedModule;
            }

            Tuple<ModuleSearchStrategy, PE> ResolvedModule = BinaryCache.ResolveModule(
				this.Pe, 
				ModuleName, 
				this.SxsEntriesCache,
				this.CustomSearchFolders,
				this.WorkingDirectory
			);
            string ModuleFilepath = (ResolvedModule.Item2 != null) ? ResolvedModule.Item2.Filepath : null;

            ModuleCacheKey ModuleKey = new ModuleCacheKey(ModuleName, ModuleFilepath);
            if ( (ModuleFilepath != null) && !this.ProcessedModulesCache.ContainsKey(ModuleKey))
            {
                ModuleFlag DelayLoadFlag = (DelayLoad) ? ModuleFlag.DelayLoad : 0;

                if (ResolvedModule.Item1 == ModuleSearchStrategy.ApiSetSchema)
                {
                    var ApiSetContractModule = new DisplayModuleInfo(
                        BinaryCache.LookupApiSetLibrary(ModuleName),
                        ResolvedModule.Item2,
                        ResolvedModule.Item1,
                        DelayLoadFlag & ModuleFlag.ApiSet
                    );
                    var NewModule = new ApiSetModuleInfo(ModuleName, ref ApiSetContractModule);
                    this.ProcessedModulesCache[ModuleKey] = NewModule;
                }
                else
                {
                    var NewModule = new DisplayModuleInfo(
                        ModuleName,
                        ResolvedModule.Item2,
                        ResolvedModule.Item1,
                        DelayLoadFlag
                    );
                    this.ProcessedModulesCache[ModuleKey] = NewModule;
                }

                // add it to the module list
                this.ModulesList.AddModule(this.ProcessedModulesCache[ModuleKey]);
            }

            return ResolvedModule.Item2;
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
        private ModuleTreeViewItem FindModuleInTree(ModuleTreeViewItem Item, DisplayModuleInfo Module, bool Highlight=false)
        {
            
            if (Item.GetTreeNodeHeaderName(Dependencies.Properties.Settings.Default.FullPath) == Module.ModuleName)
            {
				if (Highlight)
				{ 
					ExpandAllParentNode(Item.Parent as ModuleTreeViewItem);
					Item.IsSelected = true;
					Item.BringIntoView();
					Item.Focus();
				}

				return Item;
            }

            // BFS style search -> return the first matching node with the lowest "depth"
            foreach (ModuleTreeViewItem ChildItem in Item.Items)
            {
                if(ChildItem.GetTreeNodeHeaderName(Dependencies.Properties.Settings.Default.FullPath) == Module.ModuleName)
                {
					if (Highlight)
					{
						ExpandAllParentNode(Item);
						ChildItem.IsSelected = true;
						ChildItem.BringIntoView();
						ChildItem.Focus();
					}

                    return Item;
                }
            }

            foreach (ModuleTreeViewItem ChildItem in Item.Items)
            {
				ModuleTreeViewItem matchingItem = FindModuleInTree(ChildItem, Module, Highlight);
				
				// early exit as soon as we find a matching node
				if (matchingItem != null)
                    return matchingItem;
            }

            return null;
        }

        
        public  RelayCommand DoFindModuleInTree
        {
            get
            {
                return new RelayCommand((param) =>
                {
                    DisplayModuleInfo SelectedModule = (param as DisplayModuleInfo);
                    ModuleTreeViewItem TreeRootItem = this.DllTreeView.Items[0] as ModuleTreeViewItem;

                    FindModuleInTree(TreeRootItem, SelectedModule, true);
                });
            }
        }

        public RelayCommand ConfigureSearchOrderCommand
        {
            get
            {
                return new RelayCommand((param) =>
                {
                    ModuleSearchOrder modalWindow = new ModuleSearchOrder(ProcessedModulesCache);
                    modalWindow.ShowDialog();
                });
            }
        }
        #endregion // Commands 

    }
}
