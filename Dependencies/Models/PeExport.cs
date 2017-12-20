using System;
using System.ClrPh;
using System.Diagnostics;
using System.Windows;
using Dependencies;

public class DisplayPeExport : SettingBindingHandler
{
    # region Constructors
    public DisplayPeExport(
        PeExport PeExport,
        PhSymbolProvider SymPrv
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
    # endregion Constructors

    # region PublicAPI
    public string IconUri
    {
        get
        {
            if (PeInfo.forwardedExport)
                return "Images/export_forward.png";
            if (PeInfo.exportByOrdinal)
                return "Images/export_ord.png";
            if (PeInfo.exportAsCppName)
                return "Images/export_cpp.png";
            
            return "Images/export_C.png";
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
    # endregion PublicAPI

    #region Commands 
    public RelayCommand QueryExportApi
    {
        get
        {
            if (_QueryExportApi == null)
            {
                _QueryExportApi = new RelayCommand((param) =>
                {
                    if ((param == null))
                    {
                        return;
                    }

                    string ExportName = (param as DisplayPeExport).Name;
                    if (ExportName == null)
                    {
                        return;
                    }

                    Process.Start(@"http://search.msdn.microsoft.com/search/default.aspx?query=" + ExportName);
                });
            }

            return _QueryExportApi;
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


    private PeExportInfo PeInfo;
    private RelayCommand _QueryExportApi;
    private RelayCommand _CopyValue;
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