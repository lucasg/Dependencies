using System;
using System.ClrPh;
using System.Diagnostics;
using System.Windows;
using Dependencies;

public class DisplayPeImport : SettingBindingHandler
{
    #region Constructors
    public DisplayPeImport(
        PeImport PeImport,
        PhSymbolProvider SymPrv,
        string ModuleFilePath
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
    #endregion Constructors

    #region PublicAPI
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

                    Process.Start(@"http://search.msdn.microsoft.com/search/default.aspx?query=" + ExportName);
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
                    Clipboard.SetText((string)param, TextDataFormat.Text);
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

