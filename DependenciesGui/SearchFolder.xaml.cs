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
			PropertyChangedEventHandler handler = PropertyChanged;
			if (handler != null)
			{
				handler(this, new PropertyChangedEventArgs(info));
			}
		}

		public string Folder {
			get { return folder; }
			set { folder = value;  OnPropertyChanged("Folder"); }
		}

		public bool Dummy { get; set; }

		private string folder;
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
						Dummy = false
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
						Dummy = true
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
