using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.ComponentModel;

using Dependencies.ClrPh;

[Flags]
public enum PeTypes
{
    None = 0,
    IMAGE_FILE_EXECUTABLE_IMAGE = 0x02,
    IMAGE_FILE_DLL = 0x2000,
}

[Flags]
public enum ModuleFlag
{
    NoFlag = 0x00,

    DelayLoad = 0x01,
    ClrReference = 0x02,
    ApiSet = 0x04,
	ApiSetExt = 0x08,
	NotFound = 0x10,
    MissingImports = 0x20,
    ChildrenError = 0x40,
}

namespace Dependencies
{

    public struct ModuleInfo
    {
        // @TODO(Hack: refactor correctly for image generation)
        // public TreeViewItemContext context;

        public Int16 Machine;
        public Int16 Magic;

        public UInt64 ImageBase;
        public Int32 SizeOfImage;
        public UInt64 EntryPoint;


        public Int32 Checksum;
        public Boolean CorrectChecksum;

        public Int16 Subsystem;
        public Tuple<Int16, Int16> SubsystemVersion;

        public Int16 Characteristics;
        public Int16 DllCharacteristics;

        public UInt64 Filesize;
    }

	public class ApiSetNotFoundModuleInfo : NotFoundModuleInfo
	{
		public ApiSetNotFoundModuleInfo(string ApiSetModuleName, string NotFoundHostModule)
		:base(ApiSetModuleName)
		{
			_HostName = NotFoundHostModule;

			_Flags |= ModuleFlag.ApiSet;
			_Flags |= ModuleFlag.NotFound;
			if (ApiSetModuleName.StartsWith("ext-"))
			{
				_Flags |= ModuleFlag.ApiSetExt;
			}
		}

		public override string ModuleName { get { return String.Format("{0:s} -> {1:s}", this._Name, _HostName); } }

		private string _HostName;
	}

	public class NotFoundModuleInfo : DisplayModuleInfo
    {
        public NotFoundModuleInfo(string NotFoundModuleName)
        : base(NotFoundModuleName)
        {
			_Flags |= ModuleFlag.NotFound;
        }

        public override string Filepath { get { return _Name; } }
        public override List<PeImportDll> Imports { get { return new List<PeImportDll>(); } }
        public override List<PeExport> Exports { get { return new List<PeExport>(); } }

        public override string Cpu { get { return null; } }
        public override string Type { get { return null; } }
        public override UInt64? Filesize { get { return null; } }
        public override UInt64? ImageBase { get { return null; } }
        public override int? VirtualSize { get { return null; } }
        public override UInt64? EntryPoint { get { return null; } }
        public override int? Subsystem { get { return null; } }
        public override string SubsystemVersion { get { return null; } }
        public override int? Checksum { get { return null; } }
        public override bool? CorrectChecksum { get { return null; } }
        public override ModuleSearchStrategy Location { get { return ModuleSearchStrategy.NOT_FOUND; } }

    }


    public class ApiSetModuleInfo : DisplayModuleInfo
    {
        public ApiSetModuleInfo(string ApiSetModuleName, ref DisplayModuleInfo _UnderlyingModule)
        : base(ApiSetModuleName)
        {
            UnderlyingModule = _UnderlyingModule;

            _Flags = _UnderlyingModule.Flags;
            _Flags |= ModuleFlag.ApiSet;
            if (ApiSetModuleName.StartsWith("ext-"))
            {
                _Flags |= ModuleFlag.ApiSetExt;
            }
        }

        public override string ModuleName { get { return String.Format("{0:s} -> {1:s}", this._Name, UnderlyingModule.ModuleName);}}
        public override string Filepath { get { return UnderlyingModule.Filepath; } }

        public override List<PeImportDll> Imports { get { return UnderlyingModule.Imports; } }
        public override List<PeExport> Exports { get { return UnderlyingModule.Exports; } }


        public override string Cpu { get { return UnderlyingModule.Cpu; } }
        public override string Type { get { return UnderlyingModule.Type; } }
        public override UInt64? Filesize { get { return UnderlyingModule.Filesize; } }
        public override UInt64? ImageBase { get { return UnderlyingModule.ImageBase; } }
        public override int? VirtualSize { get { return UnderlyingModule.VirtualSize; } }
        public override UInt64? EntryPoint { get { return UnderlyingModule.EntryPoint; } }
        public override int? Subsystem { get { return UnderlyingModule.Subsystem; } }
        public override string SubsystemVersion { get { return UnderlyingModule.SubsystemVersion; } }
        public override int? Checksum { get { return UnderlyingModule.Checksum; } }
        public override bool? CorrectChecksum { get { return UnderlyingModule.CorrectChecksum; } }
        public override ModuleSearchStrategy Location { get { return ModuleSearchStrategy.ApiSetSchema; } }

        /// <summary>
        /// The pointed module which actually does implement the api set contract
        /// TODO : there might be more than one contract modules ?
        /// </summary>
        private DisplayModuleInfo UnderlyingModule;
    }

    public class DisplayModuleInfo : SettingBindingHandler, INotifyPropertyChanged
    {
        #region Constructors 
        public DisplayModuleInfo(string ModuleName)
        {

			_Name = ModuleName;
            _Filepath = null;
            _Flags = 0;

            AddNewEventHandler("FullPath", "FullPath", "ModuleName", this.GetPathDisplayName);
        }

        public DisplayModuleInfo(string ModuleName, PE Pe, ModuleSearchStrategy Location, ModuleFlag Flags = 0)
        {


			_Name = ModuleName;
            _Filepath = Pe.Filepath;
            _Flags = Flags;
            
            
            // Do not set this variables in order to 
            // lessen memory allocations
            _Imports = null;
            _Exports = null;
            _Location = Location;

            _Info = new ModuleInfo()
            {

                Machine = Pe.Properties.Machine,
                Magic = Pe.Properties.Magic,
                Filesize = Pe.Properties.FileSize,

                ImageBase = (UInt64)Pe.Properties.ImageBase,
                SizeOfImage = Pe.Properties.SizeOfImage,
                EntryPoint = (UInt64)Pe.Properties.EntryPoint,

                Checksum = Pe.Properties.Checksum,
                CorrectChecksum = Pe.Properties.CorrectChecksum,

                Subsystem = Pe.Properties.Subsystem,
                SubsystemVersion = Pe.Properties.SubsystemVersion,

                Characteristics = Pe.Properties.Characteristics,
                DllCharacteristics = Pe.Properties.DllCharacteristics,
            };

            AddNewEventHandler("FullPath", "FullPath", "ModuleName", this.GetPathDisplayName);
        }
        #endregion // Constructors 



        #region PublicAPI
        public virtual string ModuleName
        {
            get { return GetPathDisplayName(Dependencies.Properties.Settings.Default.FullPath); }
        }

        public virtual string Filepath {
            get { return _Filepath; }
        }

        public virtual bool DelayLoad
        {
            get { return (_Flags & ModuleFlag.DelayLoad) != 0;  }
        }

        public virtual ModuleFlag Flags
        {
            get { return _Flags; }
            set { _Flags = value; }
        }

        public virtual List<PeImportDll> Imports
        {
            get { 
            
                if (_Imports == null) {
                    _Imports = (System.Windows.Application.Current as App).LoadBinary(Filepath).GetImports();
                }
            
                return _Imports;  
            }
        }

        public virtual List<PeExport> Exports
        {
            get { 
            
                if (_Exports == null) {
                    _Exports = (System.Windows.Application.Current as App).LoadBinary(Filepath).GetExports();
                }

                return _Exports;
            }
        }


        protected string GetPathDisplayName(bool FullPath)
        {
            if ((FullPath) && (_Filepath != null))
                return _Filepath;

            return _Name;
        }

    
        public virtual string Cpu
        {
            get
            {
                int machine_id = _Info.Machine & 0xffff;
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

                    case 0xAA64:/*IMAGE_FILE_MACHINE_ARM64*/
                        return "ARM64";

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
                PeTypes Type = (PeTypes)_Info.Characteristics;

                if ((Type & PeTypes.IMAGE_FILE_DLL) != PeTypes.None)/* IMAGE_FILE_DLL */
                    TypeList.Add("Dll");

                if ((Type & PeTypes.IMAGE_FILE_EXECUTABLE_IMAGE) != PeTypes.None) /* IMAGE_FILE_EXECUTABLE_IMAGE */
                    TypeList.Add("Executable");



                return String.Join("; ", TypeList.ToArray());
            }
        }

		public bool HasErrors
		{
			get
			{
				return _ErrorImport;
			}
			set
			{
				_ErrorImport = value;
                OnPropertyChanged("HasErrors");

            }
		}


        public virtual UInt64? Filesize { get { return _Info.Filesize; } }
        public virtual UInt64? ImageBase { get { return _Info.ImageBase; } }
        public virtual int? VirtualSize { get { return _Info.SizeOfImage; } }
        public virtual UInt64? EntryPoint { get { return _Info.EntryPoint; } }
        public virtual int? Subsystem { get { return _Info.Subsystem; } }
        public virtual string SubsystemVersion { get { return String.Format("{0:d}.{1:d}" , _Info.SubsystemVersion.Item1, _Info.SubsystemVersion.Item2); } }
        public virtual int? Checksum { get { return _Info.Checksum; } }
        public virtual bool? CorrectChecksum { get { return _Info.CorrectChecksum; } }
        public virtual ModuleSearchStrategy Location { get { return _Location; } }

        public string Status
        {
            get
            {
                if ((this.Flags & ModuleFlag.NotFound) != 0)
                {
                    return String.Format("{0:s} module could not be found on disk", this.Filepath);
                }

                if ((this.Flags & ModuleFlag.MissingImports) != 0)
                {
                    return String.Format("{0:s} module has missing imports", this.Filepath);
                }

                if ((this.Flags & ModuleFlag.DelayLoad) != 0)
                {
                    return String.Format("{0:s} module is delay-load", this.Filepath);
                }

                if ((this.Flags & ModuleFlag.ChildrenError) != 0)
                {
                    return String.Format("{0:s} module has an erroneous child module", this.Filepath);
                }

                return String.Format("{0:s} module loaded correctly", this.Filepath); ;
            }
        }


        #endregion PublicAPI



        #region Commands 
        private RelayCommand _DoFindModuleInTreeCommand;
        private RelayCommand _ConfigureSearchOrderCommand;

        public RelayCommand DoFindModuleInTreeCommand
        {
            get { return _DoFindModuleInTreeCommand; }
            set { _DoFindModuleInTreeCommand = value; }
        }

        public RelayCommand ConfigureSearchOrderCommand
        {
            get { return _ConfigureSearchOrderCommand; }
            set { _ConfigureSearchOrderCommand = value; }
        }


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

        public bool OpenPeviewer(object Module)
        {
            string programPath = Dependencies.Properties.Settings.Default.PeViewerPath;
            Process PeviewerProcess = new Process();
        
            if ((Module == null))
            {
                return false;
            }

            if (!File.Exists(programPath))
            {
                MessageBox.Show("peview.exe file could not be found !");
                return false;
            }

            string Filepath = (Module as DisplayModuleInfo).GetPathDisplayName(true);
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
                        string Filepath = (param as DisplayModuleInfo).GetPathDisplayName(true);
                        if (Filepath == null)
                        {
                            return;
                        }

                        Process OtherDependenciesProcess = new Process();
                        OtherDependenciesProcess.StartInfo.FileName = Application.ExecutablePath;
                        OtherDependenciesProcess.StartInfo.Arguments = Filepath;
                        OtherDependenciesProcess.Start();
                    });
                }

                return _OpenNewAppCommand;
            }
        }

        public RelayCommand CopyValue
        {
            get
            {
                if (_CopyValue == null)
                {
                    _CopyValue = new RelayCommand((param) =>
                    {

                        if ((param == null))
                        {
                            return;
                        }

                        Clipboard.Clear();

                        try
                        {

                            Clipboard.SetText((string)param, TextDataFormat.Text);
                        }
                        catch { }
                    });
                }

                return _CopyValue;
            }
        }
        #endregion // Commands 


        /// <summary>
        /// Name : string to identify the module.
        /// This is the only "mandatory" data, the rest can be private.
        /// </summary>
        public string _Name;
        protected string _Filepath;
        protected ModuleFlag _Flags;

        private ModuleInfo _Info;
        private ModuleSearchStrategy _Location;
        private List<PeImportDll> _Imports;
        private List<PeExport> _Exports;
		private bool _ErrorImport;


        private RelayCommand _OpenPeviewerCommand;
        private RelayCommand _OpenNewAppCommand;
        private RelayCommand _CopyValue;
    }
}