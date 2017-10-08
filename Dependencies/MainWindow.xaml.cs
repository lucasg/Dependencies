using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Forms;
using System.Windows.Data;
using System.ClrPh;

namespace Dependencies
{

    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static readonly RoutedUICommand OpenAboutCommand = new RoutedUICommand();
        public static readonly RoutedUICommand OpenUserSettingsCommand = new RoutedUICommand();

        private About AboutPage;
        private UserSettings UserSettings;

        public MainWindow()
        {
            Phlib.InitializePhLib();

            InitializeComponent();

            PopulateRecentFilesMenuItems(true);

            this.AboutPage = new About();
            this.UserSettings = new UserSettings();
        }


        // Populate "recent entries"
        private void PopulateRecentFilesMenuItems(bool InializeMenuEntries = false)
        { 

            System.Windows.Controls.MenuItem FileMenuItem = (System.Windows.Controls.MenuItem)this.MainMenu.Items[0];
            System.Windows.Controls.MenuItem RecentFilesItem = (System.Windows.Controls.MenuItem)FileMenuItem.Items[2];

            byte RecentFilesCount = (byte)Properties.Settings.Default.RecentFiles.Count;
            byte RecentFilesIndex = (byte)Properties.Settings.Default.RecentFilesIndex;

            byte Index = (byte)((RecentFilesIndex + RecentFilesCount - 1) % RecentFilesCount);
            int IndexEntry = 0;

            do
            {
                String RecentFilePath = Properties.Settings.Default.RecentFiles[Index];

                System.Windows.Controls.MenuItem newRecentFileItem = new System.Windows.Controls.MenuItem();
                newRecentFileItem.Header = System.IO.Path.GetFileName(RecentFilePath);
                newRecentFileItem.DataContext = RecentFilePath;
                newRecentFileItem.Click += new RoutedEventHandler(RecentFileCommandBinding_Clicked);

                // application initialization
                if (InializeMenuEntries)
                {
                    FileMenuItem.Items.Insert(3, newRecentFileItem);
                }
                else // update elem
                {
                    FileMenuItem.Items[FileMenuItem.Items.Count - 3 -  IndexEntry] = newRecentFileItem;
                }
                

                Index = (byte)((Index - 1 + RecentFilesCount) % RecentFilesCount);
                IndexEntry = IndexEntry + 1;

            } while (Index != Properties.Settings.Default.RecentFilesIndex);


        }

        public void OpenNewDependencyWindow(String Filename)
        {

            this.DependencyRootLayout.FloatingItems.Add(new DependencyWindow(Filename));


            // Update recent files entries
            App.AddToRecentDocuments(Filename);
            PopulateRecentFilesMenuItems();
        }

        private void RecentFileCommandBinding_Clicked(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.MenuItem RecentFile = sender as System.Windows.Controls.MenuItem;
            String RecentFilePath = RecentFile.DataContext as String;

            if (RecentFilePath.Length != 0 )
            {
                OpenNewDependencyWindow(RecentFilePath);
            }

        }

        private void OpenCommandBinding_Executed(object sender, RoutedEventArgs e)
        {
            OpenFileDialog InputFileNameDlg = new OpenFileDialog
            {
                Filter = "exe files (*.exe, *.dll)| *.exe;*.dll; | All files (*.*)|*.*",
                FilterIndex = 0,
                RestoreDirectory = true,
                
            };
            

            if (InputFileNameDlg.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;

            OpenNewDependencyWindow(InputFileNameDlg.FileName);

        }

        private void ExitCommandBinding_Executed(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        private void OpenAboutCommandBinding_Executed(object sender, RoutedEventArgs e)
        {
            this.AboutPage.Close();
            this.AboutPage = new About();
            this.AboutPage.Show();
        }

        private void OpenUserSettingsCommandBinding_Executed(object sender, RoutedEventArgs e)
        {
            this.UserSettings.Close();
            this.UserSettings = new UserSettings();
            this.UserSettings.Show();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            this.UserSettings.Close();
            this.AboutPage.Close();

            Properties.Settings.Default.Save();
            base.OnClosing(e);
        }

        private void MainWindow_Drop(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.Forms.DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(System.Windows.Forms.DataFormats.FileDrop);
                
                foreach (var file in files)
                {
                    OpenNewDependencyWindow(file);
                }
            }
        }
    }


    public class BooleanToVisbilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Boolean SettingValue = (Boolean) value;

            if (SettingValue)
                return Visibility.Visible;

            return Visibility.Collapsed;

        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}