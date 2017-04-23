using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ClrPh;

namespace Dependencies
{
    public class DisplayPeImport
    {
        public DisplayPeImport(int Index, PeImport PeImport)
        {
            Info.index = Index;
            Info.ordinal = PeImport.Ordinal;
            Info.hint = PeImport.Hint;
            Info.name = PeImport.Name;
            Info.moduleName = PeImport.ModuleName;
            Info.delayedImport = PeImport.DelayImport;

        }

        public int Index { get { return Info.index; } }
        public int Hint { get { return Info.hint; } }
        public int Ordinal { get { return Info.ordinal; } }
        public string Name { get { return Info.name; } }
        public string ModuleName { get { return Info.moduleName; } }
        public Boolean DelayImport { get { return Info.delayedImport; } }

        private
            PeImportInfo Info;
    }

    public struct PeImportInfo
    {
        public int index;
        public int ordinal;
        public int hint;
        public string name;
        public string moduleName;
        public Boolean delayedImport;
    }

    public class DisplayPeExport
    {
        public DisplayPeExport(int Index, PeExport PeExport)
        {
            PeInfo.index = Index;
            PeInfo.ordinal = PeExport.Ordinal;
            PeInfo.hint = /*PeExport.Hint*/ PeExport.Ordinal - 1;
            PeInfo.name = PeExport.Name;
            PeInfo.ForwardName = PeExport.ForwardedName;
            PeInfo.exportByOrdinal = PeExport.ExportByOrdinal;
            PeInfo.forwardedExport = PeExport.ForwardedName.Length > 0;
            PeInfo.virtualAddress = PeExport.VirtualAddress;
        }

        public int Index { get { return PeInfo.index; } }
        public int Hint { get { return PeInfo.hint; } }
        public int Ordinal { get { return PeInfo.ordinal; } }
        public string Name { get {

                if (PeInfo.forwardedExport)
                    return PeInfo.ForwardName;

                if (PeInfo.exportByOrdinal)
                    return String.Format("Ordinal_{0:d}", PeInfo.ordinal);

                return PeInfo.name;
        } }
        public string VirtualAddress { get { return String.Format("{0:x}", PeInfo.virtualAddress); } }
        


        private
            PeExportInfo PeInfo;
    }

    public struct PeExportInfo
    {
        public Boolean exportByOrdinal;
        public Boolean forwardedExport;
        public int index;
        public int ordinal;
        public int hint;
        public long virtualAddress;
        public string name;
        public string ForwardName;
    }
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void OpenItem_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog InputFileNameDlg = new OpenFileDialog();
            InputFileNameDlg.Filter = "exe files (*.exe, *.dll)| *.exe;*.dll; | All files (*.*)|*.*";
            InputFileNameDlg.FilterIndex = 0;
            InputFileNameDlg.RestoreDirectory = true;

            if (InputFileNameDlg.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;
            
            this.Pe = new PE(InputFileNameDlg.FileName);
            List<PeExport> PeExports = Pe.GetExports();
            List<PeImportDll> PeImports = this.Pe.GetImports();

            this.ImportList.Items.Clear();
            this.ExportList.Items.Clear();

            int i = 0;
            foreach (PeImportDll DllImport in PeImports) {
                foreach (PeImport Import in DllImport.ImportList)
                {
                    this.ImportList.Items.Add(new DisplayPeImport(i, Import));
                    i++;
                }
            }

            i = 0;
            foreach (PeExport Export in PeExports)
            {
                this.ExportList.Items.Add(new DisplayPeExport(i, Export));
                i++;
            }

            this.DllTreeView.Items.Clear();
            TreeViewItem treeNode = new TreeViewItem();
            treeNode.Header = InputFileNameDlg.FileName;

            foreach (PeImportDll DllImport in PeImports)
            {
                TreeViewItem childTreeNode = new TreeViewItem();
                childTreeNode.Header = DllImport.Name;

                treeNode.Items.Add(childTreeNode);
            }

            this.DllTreeView.Items.Add(treeNode);


        }


        PE Pe;
    }
}