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
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

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

        private void OpenItem_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog InputFileNameDlg = new OpenFileDialog();
            InputFileNameDlg.Filter = "exe files (*.exe, *.dll)| *.exe; *.dll | All files (*.*)|*.*";
            InputFileNameDlg.FilterIndex = 0;
            InputFileNameDlg.RestoreDirectory = true;

            if (InputFileNameDlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                this.Pe = new PE(InputFileNameDlg.FileName);
                //List<PeExport> Exports = Pe.GetExports();
                List<PeImport> PeImports = this.Pe.GetImports();

                this.ImportList.Items.Clear();

                int i = 0;
                foreach (PeImport Import in PeImports)
                {
                    this.ImportList.Items.Add(new DisplayPeImport(i, Import));
                    i++;
                }
            }
        }


        PE Pe;
    }
}