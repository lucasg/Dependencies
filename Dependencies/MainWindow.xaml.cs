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
            Phlib.InitializePhLib();
            InitializeComponent();
            
        }

        private void OpenCommandBinding_Executed(object sender, RoutedEventArgs e)
        {
            OpenFileDialog InputFileNameDlg = new OpenFileDialog();
            InputFileNameDlg.Filter = "exe files (*.exe, *.dll)| *.exe;*.dll; | All files (*.*)|*.*";
            InputFileNameDlg.FilterIndex = 0;
            InputFileNameDlg.RestoreDirectory = true;

            if (InputFileNameDlg.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;

            DependencyWindow nw = new DependencyWindow(InputFileNameDlg.FileName);
            double ChildWith = Math.Min((double)nw.GetValue(WidthProperty), Container.ActualWidth);
            double ChildHeight = Math.Min((double)nw.GetValue(HeightProperty), Container.ActualHeight);

            Container.Children.Add(new MdiChild
            {
                Title = InputFileNameDlg.FileName,
                Content = nw,
                Width = ChildWith,
                Height = ChildHeight,
                //Margin = new System.Windows.Thickness(15,15,15,15)
                //Icon = new BitmapImage(new Uri("OriginalLogo.png", UriKind.Relative))
            });

            // Invalidate size in order to trigger resize for internal elements.
            nw.Width = double.NaN;
            nw.Height = double.NaN;

        }

        private void ExitCommandBinding_Executed(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }
        


    }
}