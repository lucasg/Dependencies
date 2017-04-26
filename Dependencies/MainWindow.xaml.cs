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
using WPF.MDI;

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

        private void OpenItem_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog InputFileNameDlg = new OpenFileDialog();
            InputFileNameDlg.Filter = "exe files (*.exe, *.dll)| *.exe;*.dll; | All files (*.*)|*.*";
            InputFileNameDlg.FilterIndex = 0;
            InputFileNameDlg.RestoreDirectory = true;

            if (InputFileNameDlg.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;

           DependencyWindow nw = new DependencyWindow(InputFileNameDlg.FileName);
            
            Container.Children.Add(new MdiChild
            {
                Title = InputFileNameDlg.FileName,
                Content = nw,
                Width = 1000,
                Height = 600,
                //Icon = new BitmapImage(new Uri("OriginalLogo.png", UriKind.Relative))
            });

        }


        
    }
}