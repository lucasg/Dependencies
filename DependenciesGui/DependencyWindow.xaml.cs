using System;
using System.IO;
using System.Collections.Generic;
using System.Windows;
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
    /// Dependency tree building behaviour.
    /// A full recursive dependency tree can be memory intensive, therefore the
    /// choice is left to the user to override the default behaviour.
    /// </summary>
    public class BinaryCacheOption : IValueConverter
    {
        [TypeConverter(typeof(EnumToStringUsingDescription))]
        public enum BinaryCacheOptionValue
        {
            [Description("No (faster, but locks dll until Dependencies is closed)")]
            No = 0,

            [Description("Yes (prevents file locking issues)")]
            Yes = 1
        }

        public static BinaryCacheOptionValue GetGlobalBehaviour()
        {
            return (BinaryCacheOptionValue)(new BinaryCacheOption()).Convert(
                Dependencies.Properties.Settings.Default.BinaryCacheOptionValue,
                null,// targetType
                null,// parameter
                null // System.Globalization.CultureInfo
            );
        }

        #region BinaryCacheOption.IValueConverter_contract
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool StrOption = (bool)value;

            switch (StrOption)
            {
                default:
                case true:
                    return BinaryCacheOptionValue.Yes;
                case false:
                    return BinaryCacheOptionValue.No;
            }

        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            BinaryCacheOptionValue Behaviour = (BinaryCacheOptionValue)(int)value;

            switch (Behaviour)
            {
                default:
                case BinaryCacheOptionValue.Yes:
                    return true;
                case BinaryCacheOptionValue.No:
                    return false;
            }
        }
        #endregion BinaryCacheOption.IValueConverter_contract
    }

    public class EnumToStringUsingDescription : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return (sourceType.Equals(typeof(Enum)));
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return (destinationType.Equals(typeof(String)));
        }

        public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
        {
            return base.ConvertFrom(context, culture, value);
        }

        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            if (!destinationType.Equals(typeof(String)))
            {
                throw new ArgumentException("Can only convert to string.", "destinationType");
            }

            if (!value.GetType().BaseType.Equals(typeof(Enum)))
            {
                throw new ArgumentException("Can only convert an instance of enum.", "value");
            }

            string name = value.ToString();
            object[] attrs =
                value.GetType().GetField(name).GetCustomAttributes(typeof(DescriptionAttribute), false);
            return (attrs.Length > 0) ? ((DescriptionAttribute)attrs[0]).Description : name;
        }
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
			_importsVerified = false;
			_Parent = null;
			Dependencies.Properties.Settings.Default.PropertyChanged += this.ModuleTreeViewItem_PropertyChanged;
		}

		public ModuleTreeViewItem(ModuleTreeViewItem Parent)
        {
			_importsVerified = false;
			_Parent = Parent;
			Dependencies.Properties.Settings.Default.PropertyChanged += this.ModuleTreeViewItem_PropertyChanged;
        }

        public ModuleTreeViewItem(ModuleTreeViewItem Other, ModuleTreeViewItem Parent)
        {
			_importsVerified = false;
			_Parent = Parent;
			this.DataContext = new DependencyNodeContext( (DependencyNodeContext) Other.DataContext );
			Dependencies.Properties.Settings.Default.PropertyChanged += this.ModuleTreeViewItem_PropertyChanged;
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

        public ModuleTreeViewItem ParentModule
		{
			get
			{
				return _Parent;
			}
		}


		public ModuleFlag Flags
        {
            get
            {
                return ModuleInfo.Flags;
            }
        }

        private bool _has_error;

		public bool HasErrors
		{
            get
            {
                if (!_importsVerified)
                {
                    _has_error = VerifyModuleImports();
                    _importsVerified = true;

                    // Update tooltip only once some basic checks are done
                    this.ToolTip = ModuleInfo.Status;
                }

                // propagate error for parent
                if (_has_error)
                {
                    ModuleTreeViewItem ParentModule = this.ParentModule;
                    if (ParentModule != null)
                    {
                        ParentModule.HasChildErrors = true;
                    }
                }

                return _has_error;
            }

            set
            {
                if (value == _has_error) return;
                _has_error = value;
                OnPropertyChanged("HasErrors");
            }
		}


        public string Tooltip
        {
            get
            {
                return ModuleInfo.Status;
            }
        }

        public bool HasChildErrors
        {
            get
            {
                return _has_child_errors;
            }
            set
            {
                if (value)
                {
                    ModuleInfo.Flags |= ModuleFlag.ChildrenError;
                }
                else
                {
                    ModuleInfo.Flags &= ~ModuleFlag.ChildrenError;
                }

                ToolTip = ModuleInfo.Status;
                _has_child_errors = true;
                OnPropertyChanged("HasChildErrors");

                // propagate error for parent
                ModuleTreeViewItem ParentModule = this.ParentModule;
                if (ParentModule != null)
                {
                    ParentModule.HasChildErrors = true;
                }
            }
        }

        public DisplayModuleInfo ModuleInfo
		{
			get
			{
				return (((DependencyNodeContext)this.DataContext).ModuleInfo.Target as DisplayModuleInfo);
			}
		}


		private bool VerifyModuleImports()
		{

            // current module has issues
            if ((Flags & (ModuleFlag.NotFound | ModuleFlag.MissingImports | ModuleFlag.ChildrenError)) != 0)
            {
                return true;
            }

            // no parent : it's probably the root item
            ModuleTreeViewItem ParentModule = this.ParentModule;
            if (ParentModule == null)
			{
				return false;
			}

            // Check we have any imports issues
            foreach (PeImportDll DllImport in ParentModule.ModuleInfo.Imports)
			{
				if (DllImport.Name != ModuleInfo._Name)
					continue;



				List<Tuple<PeImport, bool>> resolvedImports = BinaryCache.LookupImports(DllImport, ModuleInfo.Filepath);
				if (resolvedImports.Count == 0)
				{
                    return true;
                }

				foreach (var Import in resolvedImports)
				{
					if (!Import.Item2)
					{
                        return true;
                    }
				}
			}



            return false;
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

            PeviewerProcess.StartInfo.FileName = String.Format("\"{0:s}\"", programPath);
            PeviewerProcess.StartInfo.Arguments = String.Format("\"{0:s}\"", Filepath); 
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
                        OtherDependenciesProcess.StartInfo.Arguments = String.Format("\"{0:s}\"", Filepath);
                        OtherDependenciesProcess.Start();
                    });
                }

                return _OpenNewAppCommand;
            }
        }

        #endregion // Commands 

        private RelayCommand _OpenPeviewerCommand;
        private RelayCommand _OpenNewAppCommand;
		private ModuleTreeViewItem _Parent;
		private bool _importsVerified;
        private bool _has_child_errors;


    }


    /// <summary>
    /// Dependemcy tree analysis window for a given PE.
    /// </summary>
    public partial class DependencyWindow :  TabItem 
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
            if (!NativeFile.Exists(this.Filename))
            {
                MessageBox.Show(
                    String.Format("{0:s} is not present on the disk", this.Filename),
                    "Invalid PE",
                    MessageBoxButton.OK
                );

                return;
            }

            this.Pe = (Application.Current as App).LoadBinary(this.Filename);
			if (this.Pe == null || !this.Pe.LoadSuccessful)
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

        private ImportContext ResolveImport(PeImportDll DllImport)
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

            return ImportModule;
        }

        private void TriggerWarningOnAppvIsvImports(string DllImportName)
        {
            if (String.Compare(DllImportName, "AppvIsvSubsystems32.dll", StringComparison.OrdinalIgnoreCase) == 0 ||
                    String.Compare(DllImportName, "AppvIsvSubsystems64.dll", StringComparison.OrdinalIgnoreCase) == 0)
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
        }

        private void ProcessAppInitDlls(Dictionary<string, ImportContext> NewTreeContexts, PE AnalyzedPe, ImportContext ImportModule)
        {
            List<PeImportDll> PeImports = AnalyzedPe.GetImports();

            // only user32 triggers appinit dlls
            string User32Filepath = Path.Combine(FindPe.GetSystemPath(this.Pe), "user32.dll");
            if (ImportModule.PeFilePath != User32Filepath)
            {
                return;
            }

            string AppInitRegistryKey =
                (this.Pe.IsArm32Dll()) ?
                "SOFTWARE\\WowAA32Node\\Microsoft\\Windows NT\\CurrentVersion\\Windows" :
                (this.Pe.IsWow64Dll()) ?
                "SOFTWARE\\Wow6432Node\\Microsoft\\Windows NT\\CurrentVersion\\Windows" :
                "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Windows";

            // Opening registry values
            RegistryKey localKey = RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.LocalMachine, RegistryView.Registry64);
            localKey = localKey.OpenSubKey(AppInitRegistryKey);
            int LoadAppInitDlls = (int)localKey.GetValue("LoadAppInit_DLLs", 0);
            string AppInitDlls = (string)localKey.GetValue("AppInit_DLLs", "");
            if (LoadAppInitDlls == 0 || String.IsNullOrEmpty(AppInitDlls))
            {
                return;
            }
                
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

        private void ProcessClrImports(Dictionary<string, ImportContext> NewTreeContexts, PE AnalyzedPe, ImportContext ImportModule)
        {
            List<PeImportDll> PeImports = AnalyzedPe.GetImports();

            // only mscorre triggers clr parsing
            string User32Filepath = Path.Combine(FindPe.GetSystemPath(this.Pe), "mscoree.dll");
            if (ImportModule.PeFilePath != User32Filepath)
            {
                return;
            }

            var resolver = new DefaultAssemblyResolver();
            resolver.AddSearchDirectory(RootFolder);

            // Parse it via cecil
            AssemblyDefinition PeAssembly = null;
            try
            {
                PeAssembly = AssemblyDefinition.ReadAssembly(AnalyzedPe.Filepath);
            }
            catch (BadImageFormatException)
            {
                MessageBoxResult result = MessageBox.Show(
                        String.Format("Cecil could not correctly parse {0:s}, which can happens on .NET Core executables. CLR imports will be not shown", AnalyzedPe.Filepath),
                        "CLR parsing fail"
                ); 

                return;
            }

            foreach (var module in PeAssembly.Modules)
            {
                // Process CLR referenced assemblies
                foreach (var assembly in module.AssemblyReferences)
                {
                    AssemblyDefinition definition;
                    try
                    {
                        definition = resolver.Resolve(assembly);
                    }
                    catch (AssemblyResolutionException)
                    {
                        ImportContext AppInitImportModule = new ImportContext();
                        AppInitImportModule.PeFilePath = null;
                        AppInitImportModule.PeProperties = null;
                        AppInitImportModule.ModuleName = Path.GetFileName(assembly.Name);
                        AppInitImportModule.ApiSetModuleName = null;
                        AppInitImportModule.Flags = ModuleFlag.ClrReference;
                        AppInitImportModule.ModuleLocation = ModuleSearchStrategy.ClrAssembly;
                        AppInitImportModule.Flags |= ModuleFlag.NotFound;

                        if (!NewTreeContexts.ContainsKey(AppInitImportModule.ModuleName))
                        {
                            NewTreeContexts.Add(AppInitImportModule.ModuleName, AppInitImportModule);
                        }

                        continue;
                    }

                    foreach (var AssemblyModule in definition.Modules)
                    {
                        Debug.WriteLine("Referenced Assembling loading " + AssemblyModule.Name + " : " + AssemblyModule.FileName);

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

        /// <summary>
        /// Background processing of a single PE file.
        /// It can be lengthy since there are disk access (and misses).
        /// </summary>
        /// <param name="NewTreeContexts"> This variable is passed as reference to be updated since this function is run in a separate thread. </param>
        /// <param name="newPe"> Current PE file analyzed </param>
        private void ProcessPe(Dictionary<string, ImportContext> NewTreeContexts, PE newPe)
        {
            List<PeImportDll> PeImports = newPe.GetImports();

            foreach (PeImportDll DllImport in PeImports)
            {
                // Ignore already processed imports
                if (NewTreeContexts.ContainsKey(DllImport.Name))
                {
                    continue;
                }

                // Find Dll in "paths"
                ImportContext ImportModule = ResolveImport(DllImport);

                // add warning for appv isv applications 
                TriggerWarningOnAppvIsvImports(DllImport.Name);
				

                NewTreeContexts.Add(DllImport.Name, ImportModule);


                // AppInitDlls are triggered by user32.dll, so if the binary does not import user32.dll they are not loaded.
                ProcessAppInitDlls(NewTreeContexts, newPe, ImportModule);


                // if mscoree.dll is imported, it means the module is a C# assembly, and we can use Mono.Cecil to enumerate its references
                ProcessClrImports(NewTreeContexts, newPe, ImportModule);
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
                    ModuleTreeViewItem childTreeNode = new ModuleTreeViewItem(RootNode);
                    DependencyNodeContext childTreeNodeContext = new DependencyNodeContext();
                    childTreeNodeContext.IsDummy = false;

                    string ModuleName = NewTreeContext.ModuleName;
                    string ModuleFilePath = NewTreeContext.PeFilePath;
                    ModuleCacheKey ModuleKey = new ModuleCacheKey(NewTreeContext);

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


				// Process next batch of dll imports only if :
				//	1. Recursive tree building has been activated
				//  2. Recursion is not hitting the max depth level
				bool doProcessNextLevel = (SettingTreeBehaviour != TreeBuildingBehaviour.DependencyTreeBehaviour.ChildOnly) &&
										  (RecursionLevel < Dependencies.Properties.Settings.Default.TreeDepth);

				if (doProcessNextLevel)
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
            if (SelectedModule == null)
            {
                return;
            }

            // Selected Pe has not been found on disk : unvalidate current module
            SelectedModule.HasErrors = !NativeFile.Exists(SelectedModule.Filepath);
            if (SelectedModule.HasErrors)
            {
                // TODO : do a proper refresh instead of asking the user to do it
                System.Windows.MessageBox.Show(String.Format("We could not find {0:s} file on the disk anymore, please fix this problem and refresh the window via F5", SelectedModule.Filepath));
            }

            // Root Item : no parent
            ModuleTreeViewItem TreeRootItem = this.DllTreeView.Items[0] as ModuleTreeViewItem;
			ModuleTreeViewItem SelectedItem = this.DllTreeView.SelectedItem as ModuleTreeViewItem;
			if (SelectedItem == TreeRootItem)
			{
                // Selected Pe has not been found on disk : unvalidate current module
                if (SelectedModule.HasErrors)
                {
                    UpdateImportExportLists(null, null);
                }
                else
                {
                    SelectedModule.HasErrors = false;
                    UpdateImportExportLists(SelectedModule, null);
                }
    
				return;
			}

			// Tree Item
			DisplayModuleInfo parentModule = SelectedItem.ParentModule.ModuleInfo;
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
				this.ImportList.ResetAutoSortProperty();

				this.ExportList.SetExports(SelectedModule.Exports, SymPrv);
				this.ExportList.ResetAutoSortProperty();
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

            // Not found module, returning PE without update module list
            if (ModuleFilepath == null)
            {
                return ResolvedModule.Item2;
            }

            ModuleFlag ModuleFlags = ModuleFlag.NoFlag;
            if (DelayLoad)
                ModuleFlags |= ModuleFlag.DelayLoad;
            if (ResolvedModule.Item1 == ModuleSearchStrategy.ApiSetSchema)
                ModuleFlags |= ModuleFlag.ApiSet;

            ModuleCacheKey ModuleKey = new ModuleCacheKey(ModuleName, ModuleFilepath, ModuleFlags);
            if (!this.ProcessedModulesCache.ContainsKey(ModuleKey))
            {
                DisplayModuleInfo NewModule;

                // apiset resolution are a bit trickier
                if (ResolvedModule.Item1 == ModuleSearchStrategy.ApiSetSchema)
                {
                    var ApiSetContractModule = new DisplayModuleInfo(
                        BinaryCache.LookupApiSetLibrary(ModuleName),
                        ResolvedModule.Item2,
                        ResolvedModule.Item1,
                        ModuleFlags
                    );
                    NewModule = new ApiSetModuleInfo(ModuleName, ref ApiSetContractModule);
                }
                else
                {
                    NewModule = new DisplayModuleInfo(
                        ModuleName,
                        ResolvedModule.Item2,
                        ResolvedModule.Item1,
                        ModuleFlags
                    );
                    
                }

                this.ProcessedModulesCache[ModuleKey] = NewModule;

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

        private void CopyFilePath_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ModuleTreeViewItem Source = e.Source as ModuleTreeViewItem;
            String SelectedModuleName = Source.GetTreeNodeHeaderName(Dependencies.Properties.Settings.Default.FullPath);
            if (Source == null)
                return;

            Clipboard.SetText(SelectedModuleName);
        }

        private void OpenInExplorer_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ModuleTreeViewItem Source = e.Source as ModuleTreeViewItem;
            if (Source == null)
                return;

            String commandParameter = "/select,\"" + Source.ModuleFilePath + "\"";

            Process.Start("explorer.exe", commandParameter);
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
