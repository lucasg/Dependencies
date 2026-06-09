using System;
using System.Diagnostics;
using System.Windows;
using System.Collections.Generic;

using Dependencies;
using Dependencies.ClrPh;

public class DisplayPeImport : SettingBindingHandler
{
    #region Constructors
    public DisplayPeImport(
        PeImport PeImport,
        PhSymbolProvider SymPrv,
        string ModuleFilePath,
        bool ImportFound
    )
    {
       Info.ordinal = PeImport.Ordinal;
       Info.hint = PeImport.Hint;
       Info.name = PeImport.Name;
       Info.moduleName = PeImport.ModuleName;
       Info.modulePath = ModuleFilePath;
       Info.importNotFound = !ImportFound;

       Tuple<CLRPH_DEMANGLER, string> DemanglingInfos = SymPrv.UndecorateName(PeImport.Name);
       Info.Demangler = Enum.GetName(typeof(CLRPH_DEMANGLER), DemanglingInfos.Item1); 
       Info.UndecoratedName = DemanglingInfos.Item2;
        
       if (ImportFound)
       { 
           Info.delayedImport = PeImport.DelayImport;
           Info.importAsCppName = (PeImport.Name.Length > 0 && PeImport.Name[0] == '?');
           Info.importByOrdinal = PeImport.ImportByOrdinal;
       }

        AddNewEventHandler("Undecorate", "Undecorate", "Name", this.GetDisplayName);
        AddNewEventHandler("FullPath", "FullPath", "ModuleName", this.GetPathDisplayName);
    }
	#endregion Constructors

	#region PublicAPI
	public override string ToString()
	{
		List<string> members = new List<string>() {
			Ordinal != null ? String.Format("{0} (0x{0:x08})", Ordinal) : "N/A",
			Hint != null ? String.Format("{0} (0x{0:x08})", Hint) : "N/A",
			Name,
			ModuleName,
			DelayImport.ToString(),
			Demangler
		};

		return String.Join(", ", members.ToArray());
	}


	public string IconUri
    {
        // @TODO(implement API lookup in order to test for API Export presence)
        get
        {
            string PathStrFormat = "Images/import_{0:s}_found.png";
            if (Info.importNotFound)
                PathStrFormat = "Images/import_{0:s}_not_found.png";


            if (Info.importByOrdinal)
                return String.Format(PathStrFormat, "ord");

            if (Info.importAsCppName)
                return String.Format(PathStrFormat, "cpp");

            return String.Format(PathStrFormat, "c");
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

    public ushort? Hint
    {
        get
        {
            if (Info.importByOrdinal)
                return null;

            return Info.hint;
        }
    }

    public ushort? Ordinal { get { if (Info.importByOrdinal) { return Info.ordinal; } return null; } }

    public string Name
    {
        get { return GetDisplayName(Dependencies.Properties.Settings.Default.Undecorate); }
    }

    public string ModuleName
    {
        get { return GetPathDisplayName(Dependencies.Properties.Settings.Default.FullPath); }
    }

    public string FilterName
    {
        get { return  String.Format("{0:s}:{1:s}", this.ModuleName, this.Name); }
    }

    public Boolean DelayImport { get { return Info.delayedImport; } }

    public string Demangler { get { return this.Info.Demangler; } }

    protected string GetDisplayName(bool UndecorateName)
    {
        if (Info.importByOrdinal)
            return String.Format("Ordinal_{0:d}", Info.ordinal);

        if ((UndecorateName) && (Info.UndecoratedName.Length > 0))
            return Info.UndecoratedName;
        
       
       return Info.name;
    }

    protected string GetPathDisplayName(bool FullPath)
    {
        if ((FullPath) && (Info.modulePath != null))
            return Info.modulePath;

        return Info.moduleName;
    }
    #endregion PublicAPI

    #region Commands 
    public RelayCommand QueryImportApi
    {
        get
        {
            if (_QueryImportApi == null)
            {
                _QueryImportApi = new RelayCommand((param) =>
                {
                    if ((param == null))
                    {
                        return;
                    }

                    string ExportName = (param as DisplayPeImport).Name;
                    if (ExportName == null)
                    {
                        return;
                    }

                    Process.Start(new ProcessStartInfo { FileName = @"https://docs.microsoft.com/search/?search=" + ExportName, UseShellExecute = true });
                });
            }

            return _QueryImportApi;
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


    private PeImportInfo Info;
    private RelayCommand _QueryImportApi;
    private RelayCommand _CopyValue;
}

public struct PeImportInfo
{
   public ushort ordinal;
   public ushort hint;

   public string name;
   public string moduleName;
   public string modulePath;

   public string UndecoratedName;
   public string Demangler;

   public Boolean delayedImport;
   public Boolean importByOrdinal;
   public Boolean importAsCppName;
   public Boolean importNotFound;

}

