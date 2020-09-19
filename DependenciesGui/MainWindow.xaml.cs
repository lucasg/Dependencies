using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Forms;
using System.Windows.Data;

using Dragablz;
using System.Windows.Shell;

namespace Dependencies
{
    /// <summary>
    /// We override the default Dragablz.IInterTabClient  in order to change
    /// it's behaviour on closing all tabs.
    /// </summary>
    public class DependenciesInterTabClient : DefaultInterTabClient
    {
        /// <summary>
        /// When closing all tabs on a particular MainWindow instances, we want
        /// to know if it's okay to close the application also. the MainWindow created
        /// by the "App" entry point is marked as "master" and is not closed,
        /// whereas all the others are.
        /// </summary>
        public override TabEmptiedResponse TabEmptiedHandler(TabablzControl tabControl, Window window)
        {
            MainWindow main = window as MainWindow;
            if (main.IsMaster)
            {
                //main.DefaultMessage.Visibility = Visibility.Visible;
                return TabEmptiedResponse.DoNothing;
            }
                

            return TabEmptiedResponse.CloseWindowOrLayoutBranch;
        }
    }

    /// <summary>
    /// MainWindow : container for displaying one or sereral DependencyWindow tree elements.
    /// </summary>
    public partial class MainWindow : Window
    {
        public static readonly RoutedUICommand OpenAboutCommand = new RoutedUICommand();
        public static readonly RoutedUICommand OpenUserSettingsCommand = new RoutedUICommand();
		public static readonly RoutedUICommand OpenCustomizeSearchFolderCommand = new RoutedUICommand();

		private readonly IInterTabClient _interTabClient = new DependenciesInterTabClient();

        private About AboutPage;
        private UserSettings UserSettings;
		private SearchFolder SearchFolder;

        private bool _Master;
		private bool _EnableSearchFolderCustomization;

		#region PublicAPI
		public MainWindow()
        {

            InitializeComponent();
            PopulateRecentFilesMenuItems();

            this.AboutPage = new About();
            this.UserSettings = new UserSettings();
			this.SearchFolder = null;

			// TODO : understand how to reliably bind in xaml
			this.TabControl.InterTabController.InterTabClient = DoNothingInterTabClient;
            this.TabControl.IsEmptyChanged += MainWindow_TabControlIsEmptyHandler;

            this._Master = false;
			this.DataContext = this;
        }

        /// <summary>
        /// Open a new depedency tree window on a given PE.
        /// </summary>
        /// <param name="Filename">File path to a PE to process.</param>
        public void OpenNewDependencyWindow(String Filename)
        {
            var newDependencyWindow = new DependencyWindow(Filename);
            newDependencyWindow.Header = new CustomHeaderViewModel { Header = Path.GetFileNameWithoutExtension(Filename) };

            this.TabControl.AddToSource(newDependencyWindow);
            this.TabControl.SelectedItem = newDependencyWindow;

            // Update recent files entries
            App.AddToRecentDocuments(Filename);
            PopulateRecentFilesMenuItems();
        }

        /// <summary>
        /// We override the default Dragablz.IInterTabClient  in order to change
        /// it's behaviour on closing all tabs.
        /// </summary>
        public IInterTabClient DoNothingInterTabClient
        {
            get { return _interTabClient; }
        }

		
		public bool EnableSearchFolderCustomization
		{
			// find a way to update the toggle based on the number of window opened without relying on
			// creating a custom IsEnabledChanged event
			get { return true;/*this.TabControl.SelectedItem != null;*/ }
		}

		/// <summary>
		/// When closing all tabs on a particular MainWindow instances, we want
		/// to know if it's okay to close the application also. the MainWindow created
		/// by the "App" entry point is marked as "master" and is not closed,
		/// whereas all the others are.
		/// </summary>
		public bool IsMaster
        {
            get { return _Master; }
            set { _Master = value; }
        }

        #endregion PublicAPI

        /// <summary>
        /// Populate "recent entries" menu items
        /// </summary>
        private void PopulateRecentFilesMenuItems()
        {

            System.Windows.Controls.MenuItem FileMenuItem = (System.Windows.Controls.MenuItem)this.MainMenu.Items[0];
            

            if (Properties.Settings.Default.RecentFiles.Count == 0) {
                return;
            }


            foreach (var RecentFilePath in Properties.Settings.Default.RecentFiles)
            {
                // Ignore empty dummy entries
                if (String.IsNullOrEmpty(RecentFilePath))
                {
                    continue;
                }

                AddRecentFilesMenuItem(RecentFilePath, Properties.Settings.Default.RecentFiles.IndexOf(RecentFilePath));
            }
        }

        private void AddRecentFilesMenuItem(string Filepath, int index)
        {

            System.Windows.Controls.MenuItem FileMenuItem = (System.Windows.Controls.MenuItem)this.MainMenu.Items[0];

            // Create new menu item
            System.Windows.Controls.MenuItem newRecentFileItem = new System.Windows.Controls.MenuItem();
            newRecentFileItem.Header = System.IO.Path.GetFileName(Filepath);
            newRecentFileItem.DataContext = Filepath;
            newRecentFileItem.Click += new RoutedEventHandler(RecentFileCommandBinding_Clicked);

            // check if the item already is in the list, diregarding others menu items
            if (index + 5 < FileMenuItem.Items.Count)
            {
                FileMenuItem.Items[3 + index] = newRecentFileItem;
            }
            else
            {
                // Add it to the list at the top
                FileMenuItem.Items.Insert(3, newRecentFileItem);
            }

        }

        #region Commands
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
            this.UserSettings.Owner = this;
            this.UserSettings.Show();
        }

		private void OpenCustomizeSearchFolderCommand_Executed(object sender, RoutedEventArgs e)
		{
			DependencyWindow SelectedItem = this.TabControl.SelectedItem as DependencyWindow;
			if (SelectedItem == null)
				return;

			if (this.SearchFolder != null)
			{
				this.SearchFolder.Close();
			}
			
			this.SearchFolder = new SearchFolder(SelectedItem);
			this.SearchFolder.Show();
		}


		private void RefreshCommandBinding_Executed(object sender, RoutedEventArgs e)
        {
			DependencyWindow SelectedItem = this.TabControl.SelectedItem as DependencyWindow;
			if (SelectedItem == null)
				return;

			SelectedItem.InitializeView();
        }
        #endregion Commands

        #region EventsHandler

        /// <summary>
        /// Application.Close event handler. Save user settings and close every child windows.
        /// </summary>
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            this.UserSettings.Close();
            this.AboutPage.Close();

			if (this.SearchFolder!= null)
			{
				this.SearchFolder.Close();
			}
			


			base.OnClosing(e);
        }

        /// <summary>
        /// file drag-and-drop event handler.
        /// </summary>
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

        private void MainWindow_TabControlIsEmptyHandler(object sender, RoutedPropertyChangedEventArgs<bool> e)
        {
            this.DefaultMessage.Visibility = (e.NewValue) ? Visibility.Visible : Visibility.Hidden;
            this._RefreshItem.IsEnabled = !e.NewValue;
        }
        #endregion EventsHandler
    }

    /// <summary>
    /// Converter to transform a boolean into a Visibility settings. 
    /// Why is this not part of the WPF standard lib ? Everybody ends up coding one in every project.
    /// </summary>
    public class BooleanToVisibilityConverter : IValueConverter
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