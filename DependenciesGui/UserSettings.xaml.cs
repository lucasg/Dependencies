using System;
using System.Windows;
using System.Windows.Forms;

namespace Dependencies
{
    public partial class UserSettings : Window
    {
        private string PeviewerPath;

        public UserSettings()
        {
            InitializeComponent();

            TreeBuildCombo.ItemsSource = Enum.GetValues(typeof(TreeBuildingBehaviour.DependencyTreeBehaviour));
            PeviewerPath = Dependencies.Properties.Settings.Default.PeViewerPath;
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

            PeviewerPath = InputFileNameDlg.FileName;
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void OnValidate(object sender, RoutedEventArgs e)
        {
            // Update defaults
            Dependencies.Properties.Settings.Default.PeViewerPath = PeviewerPath;

            if (TreeBuildCombo.SelectedItem != null)
            {
                Dependencies.Properties.Settings.Default.TreeBuildBehaviour = TreeBuildCombo.SelectedItem.ToString();
            }


            this.Close();
        }

    }

}
