using System;
using System.ClrPh;

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