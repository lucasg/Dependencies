using System.Windows;
using System.Windows.Forms;

namespace Dependencies
{
    public partial class UserSettings : Window
    {
        public UserSettings()
        {
            InitializeComponent();
        }

        private void OnPeviewerPathSettingChange(object sender, RoutedEventArgs e)
        {
            string programPath = Dependencies.Properties.Settings.Default.PeViewerPath;

            OpenFileDialog InputFileNameDlg = new OpenFileDialog()
            {
                Filter = "exe files (*.exe, *.dll)| *.exe;*.dll; | All files (*.*)|*.*",
                FilterIndex = 0,
                RestoreDirectory = true,
                InitialDirectory = System.IO.Path.GetDirectoryName(programPath)
            };


            if (InputFileNameDlg.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;

            Dependencies.Properties.Settings.Default.PeViewerPath = InputFileNameDlg.FileName;
        }
        
    }
}
