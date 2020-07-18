using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.ComponentModel;
using System.Windows.Forms;

namespace Dependencies
{
	public class SearchFolderItem : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		protected void OnPropertyChanged(string info)
		{
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
        }

		public string Folder {
			get { return folder; }
			set { folder = value;  OnPropertyChanged("Folder"); }
		}

        public Boolean IsEditable
        {
            get { return is_editable; }
            set { is_editable = value; OnPropertyChanged("IsEditable"); }
        }

        public bool Dummy { get; set; }

		private string folder;
        private Boolean is_editable;
    }

	/// <summary>
	/// Logique d'interaction pour SearchFolder.xaml
	/// </summary>
	public partial class SearchFolder : Window
	{
		private DependencyWindow _SelectedItem;
		private ObservableCollection<SearchFolderItem> _CustomSearchFolders;
		private string _working_directory;

		public SearchFolder(DependencyWindow SelectedItem)
		{
			_SelectedItem = SelectedItem;
			_working_directory = SelectedItem.RootFolder;
			_CustomSearchFolders = new ObservableCollection<SearchFolderItem>();
			foreach (var item in SelectedItem.CustomSearchFolders)
			{
				_CustomSearchFolders.Add(
					new SearchFolderItem()
					{
						Folder = item,
						Dummy = false,
                        IsEditable = false,
                    }
				);
			}

			EnsureSearchFolderHasDummyEntry();

			// bind window to observable collections
			this.DataContext = this; 
			InitializeComponent();
		}

		public ObservableCollection<SearchFolderItem> SearchFolders
		{
			get
			{
                return _CustomSearchFolders;
                //return (ObservableCollection < SearchFolderItem >) _CustomSearchFolders.Where(sf => !sf.Dummy);
			}
		}

		public string WorkingDirectory
		{
			get { return _working_directory; }
			set { _working_directory = value; this.WorkingDirectoryTextBox.Text = value;}
		}

		private void OnChangeSearchFolder(object sender, RoutedEventArgs e)
		{
			System.Windows.Controls.Button selectedButton = sender as System.Windows.Controls.Button;
			SearchFolderItem selectedItem = (SearchFolderItem) (selectedButton.DataContext as System.Windows.Controls.ListBoxItem).Content;

			FolderBrowserDialog InputFileNameDlg = new FolderBrowserDialog();
			if (InputFileNameDlg.ShowDialog() != System.Windows.Forms.DialogResult.OK)
				return;

			var selectedPath = InputFileNameDlg.SelectedPath;


			selectedItem.Folder = selectedPath;
			selectedItem.Dummy = false;
			

			EnsureSearchFolderHasDummyEntry();
		}

		private void OnRemoveSearchFolder(object sender, RoutedEventArgs e)
		{
			System.Windows.Controls.Button selectedButton = sender as System.Windows.Controls.Button;
			SearchFolderItem selectedItem = (SearchFolderItem)(selectedButton.DataContext as System.Windows.Controls.ListBoxItem).Content;

			_CustomSearchFolders.Remove(selectedItem);

			EnsureSearchFolderHasDummyEntry();
		}

		private void EnsureSearchFolderHasDummyEntry()
		{
			// add a dummy entry if necessary
			if (_CustomSearchFolders.Count == 0 || !_CustomSearchFolders.Last().Dummy)
			{
				_CustomSearchFolders.Add(
					new SearchFolderItem()
					{
						Folder = null,
						Dummy = true,
                        IsEditable= false,
                    }
				);
			}
        }

		private void OnBinaryWorkindDirectoryChange(object sender, RoutedEventArgs e)
		{
			FolderBrowserDialog InputFileNameDlg = new FolderBrowserDialog()
			{
				SelectedPath = WorkingDirectory
			};


			if (InputFileNameDlg.ShowDialog() != System.Windows.Forms.DialogResult.OK)
				return;

			WorkingDirectory = InputFileNameDlg.SelectedPath;
		}

		private void OnCancel(object sender, RoutedEventArgs e)
		{
			this.Close();
		}

		private void OnValidate(object sender, RoutedEventArgs e)
		{
			var nonDummySearchFolders = _CustomSearchFolders.ToList()
												.FindAll(item => !item.Dummy)
												.Select(i => i.Folder)
												.ToList();

			// do not launch analysis again if there is no modifications
			bool searchFoldersChanged = (_SelectedItem.CustomSearchFolders != nonDummySearchFolders) || (WorkingDirectory != _SelectedItem.WorkingDirectory);
			
			this.Close();

			if (searchFoldersChanged)
			{
				_SelectedItem.CustomSearchFolders = nonDummySearchFolders;
				_SelectedItem.WorkingDirectory = WorkingDirectory;

				// Force refresh
				_SelectedItem.InitializeView();
			}
		}

        private void OnNewEntry(object sender, RoutedEventArgs e)
        {
            EnsureSearchFolderHasDummyEntry();

            // The last entry is always a dummy one, so we can use it
            SearchFolderItem newItem = _CustomSearchFolders.Last();

            newItem.Dummy = false;
            newItem.IsEditable = true;
            newItem.PropertyChanged += SearchFolder_PropertyChanged;

           

            EnsureSearchFolderHasDummyEntry();
        }

        private void OnEditEntry(object sender, RoutedEventArgs e)
        {
            SearchFolderItem selectedItem = (SearchFolderItem)SearchFoldersList.SelectedItem;
            if (selectedItem == null)
            {
                return;
            }

            selectedItem.IsEditable = true;
            selectedItem.PropertyChanged += SearchFolder_PropertyChanged;

            foreach (var sfi in _CustomSearchFolders)
            {
                if (sfi != selectedItem)
                {
                    sfi.IsEditable = false;
                }
            }

            EnsureSearchFolderHasDummyEntry();
        }

        private void OnBrowseEntry(object sender, RoutedEventArgs e)
        {
            // Ask the user for a path
            FolderBrowserDialog InputFileNameDlg = new FolderBrowserDialog();
            if (InputFileNameDlg.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;

            var selectedPath = InputFileNameDlg.SelectedPath;

            // Either edit the selected item or add a new one 
            SearchFolderItem selectedItem = (SearchFolderItem) SearchFoldersList.SelectedItem;
            if (selectedItem == null || !selectedItem.IsEditable)
            {
                // The last entry is always a dummy one, so we can use it
                selectedItem = _CustomSearchFolders.Last();
            }


            selectedItem.Folder = selectedPath;
            selectedItem.Dummy = false;
            selectedItem.IsEditable = true;

            foreach (var sfi in _CustomSearchFolders)
            {
                if (sfi != selectedItem)
                {
                    sfi.IsEditable = false;
                }
            }

            EnsureSearchFolderHasDummyEntry();
        }

        private void OnDeleteEntry(object sender, RoutedEventArgs e)
        {
            SearchFolderItem selectedItem = (SearchFolderItem) SearchFoldersList.SelectedItem;
            if (selectedItem == null)
            {
                return;
            }

            _CustomSearchFolders.Remove(selectedItem);

            EnsureSearchFolderHasDummyEntry();
        }

        private void OnMoveUpEntry(object sender, RoutedEventArgs e)
        {
            SearchFolderItem selectedItem = (SearchFolderItem)SearchFoldersList.SelectedItem;
            if (selectedItem == null)
            {
                return;
            }

            // Dummy entry stays last
            if (selectedItem.Dummy)
            {
                return;
            }

            // No need to move up the first entry
            int index = _CustomSearchFolders.IndexOf(selectedItem);
            if (index == 0)
            {
                return;
            }
            _CustomSearchFolders.Remove(selectedItem);
            _CustomSearchFolders.Insert(index - 1, selectedItem);

            EnsureSearchFolderHasDummyEntry();
        }

        private void OnMoveDownEntry(object sender, RoutedEventArgs e)
        {
            SearchFolderItem selectedItem = (SearchFolderItem)SearchFoldersList.SelectedItem;
            if (selectedItem == null)
            {
                return;
            }

            // Dummy entry stays last
            if (selectedItem.Dummy)
            {
                return;
            }

            // No need to move down the last non-dummy entry
            int index = _CustomSearchFolders.IndexOf(selectedItem);
            if (index == _CustomSearchFolders.Count() - 2 )
            {
                return;
            }
            _CustomSearchFolders.Remove(selectedItem);
            _CustomSearchFolders.Insert(index + 1, selectedItem);

            EnsureSearchFolderHasDummyEntry();
        }

        private void SearchFolder_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender.GetType() == typeof(SearchFolderItem) && e.PropertyName == "Folder")
            {
                SearchFolderItem ChangedSearchFolder = (SearchFolderItem)sender;

                ChangedSearchFolder.IsEditable = false;
                ChangedSearchFolder.PropertyChanged -= SearchFolder_PropertyChanged;
                ChangeSearchFolder(ChangedSearchFolder);
            }
        }

        private void OnSearchFolderViewSelectedItemChanged(object sender, RoutedEventArgs e)
        {
            SearchFolderItem selectedItem = (SearchFolderItem)SearchFoldersList.SelectedItem;

            foreach(var sfi in _CustomSearchFolders)
            {
                if(sfi != selectedItem)
                {
                    sfi.IsEditable = false;
                }
            }
        }

        private void ChangeSearchFolder(SearchFolderItem Item)
        {

        }

        private void SearchFolder_DragOver(object sender, System.Windows.DragEventArgs e)
		{
			if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
			{
				e.Effects = System.Windows.DragDropEffects.Copy;
				var listbox = sender as System.Windows.Controls.ListBox;
				listbox.Background = new SolidColorBrush(Color.FromRgb(155, 155, 155));
			}
			else
			{
				e.Effects = System.Windows.DragDropEffects.None;
			}
		}

        private void SearchFoldersList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var listbox = sender as System.Windows.Controls.ListBox;

            SearchFolderItem selectedItem = (SearchFolderItem)listbox.SelectedItem;
            if (selectedItem == null)
            {
                return;
            }

            selectedItem.IsEditable = true;
            selectedItem.PropertyChanged += SearchFolder_PropertyChanged;

            EnsureSearchFolderHasDummyEntry();
        }


        private void SearchFolder_DragLeave(object sender, System.Windows.DragEventArgs e)
		{
			var listbox = sender as System.Windows.Controls.ListBox;
			listbox.Background = new SolidColorBrush(Color.FromRgb(226, 226, 226));
		}

		private void SearchFolder_Drop(object sender, System.Windows.DragEventArgs e)
		{
			if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
			{
				string[] folders = (string[]) e.Data.GetData(System.Windows.DataFormats.FileDrop);

				foreach (string FolderPath in folders)
				{
					if (Directory.Exists(FolderPath))
					{
						if (_CustomSearchFolders.Last().Dummy)
						{
							_CustomSearchFolders.Last().Folder = FolderPath;
							_CustomSearchFolders.Last().Dummy = false;
						}
						else
						{ 
							_CustomSearchFolders.Add(new SearchFolderItem()
							{
								Folder = FolderPath,
								Dummy = false
							});
						}

						EnsureSearchFolderHasDummyEntry();
					}
				}
			}

			var listbox = sender as System.Windows.Controls.ListBox;
			listbox.Background = new SolidColorBrush(Color.FromRgb(226, 226, 226));
		}

    }
}
