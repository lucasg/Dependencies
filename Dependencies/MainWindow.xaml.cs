using System;
using System.Windows;
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
        public MainWindow()
        {
            Phlib.InitializePhLib();

            InitializeComponent();
            PopulateRecentFilesMenuItems(true);
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
            DependencyWindow nw = new DependencyWindow(Filename);
            this.DockPanel.AddToSource(nw);
            this.DockPanel.SelectedItem = nw;

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
            OpenFileDialog InputFileNameDlg = new OpenFileDialog();
            InputFileNameDlg.Filter = "exe files (*.exe, *.dll)| *.exe;*.dll; | All files (*.*)|*.*";
            InputFileNameDlg.FilterIndex = 0;
            InputFileNameDlg.RestoreDirectory = true;

            if (InputFileNameDlg.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;

            OpenNewDependencyWindow(InputFileNameDlg.FileName);

        }

        private void ExitCommandBinding_Executed(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            Properties.Settings.Default.Save();
            base.OnClosing(e);
        }

        
        public static Func<DependencyWindow> Factory
        {
            get
            {
                return
                    () =>
                    {
                        OpenFileDialog InputFileNameDlg = new OpenFileDialog();
                        InputFileNameDlg.Filter = "exe files (*.exe, *.dll)| *.exe;*.dll; | All files (*.*)|*.*";
                        InputFileNameDlg.FilterIndex = 0;
                        InputFileNameDlg.RestoreDirectory = true;

                        if (InputFileNameDlg.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                            return null;
                        var Filename = InputFileNameDlg.FileName;

                        DependencyWindow nw = new DependencyWindow(Filename);

                        // Update recent files entries
                        App.AddToRecentDocuments(Filename);

                        return nw;
                    };
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