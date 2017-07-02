using System;
using System.ClrPh;

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

