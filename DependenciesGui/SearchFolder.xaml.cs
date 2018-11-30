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
using System.Windows.Shapes;
using System.Windows.Forms;

namespace Dependencies
{
	/// <summary>
	/// Logique d'interaction pour SearchFolder.xaml
	/// </summary>
	public partial class SearchFolder : Window
	{
		private DependencyWindow _SelectedItem;
		private ObservableCollection<string> _CustomSearchFolders;
		private string _working_directory;

		public SearchFolder(DependencyWindow SelectedItem)
		{
			_SelectedItem = SelectedItem;
			_working_directory = SelectedItem.RootFolder;
			_CustomSearchFolders = new ObservableCollection<string>(SelectedItem.CustomSearchFolders);
			
			// bind window to observable collections
			this.DataContext = this; 
			InitializeComponent();
		}

		public ObservableCollection<string> SearchFolders
		{
			get
			{
				return _CustomSearchFolders;
			}
		}

		public string WorkingDirectory
		{
			get { return _working_directory; }
		}

		private void OnBinaryWorkindDirectoryChange(object sender, RoutedEventArgs e)
		{
			OpenFileDialog InputFileNameDlg = new OpenFileDialog()
			{
				Filter = "exe files (*.exe, *.dll)| *.exe;*.dll; | All files (*.*)|*.*",
				FilterIndex = 0,
				RestoreDirectory = true,
				InitialDirectory = _working_directory
			};


			if (InputFileNameDlg.ShowDialog() != System.Windows.Forms.DialogResult.OK)
				return;

			_working_directory = InputFileNameDlg.FileName;
		}

		private void OnCancel(object sender, RoutedEventArgs e)
		{
			this.Close();
		}

		private void OnValidate(object sender, RoutedEventArgs e)
		{
			// do not launch analysis again if there is no modifications
			bool searchFoldersChanged = (_SelectedItem.CustomSearchFolders == _CustomSearchFolders.ToList());
			
			this.Close();

			if (searchFoldersChanged)
			{
				_SelectedItem.CustomSearchFolders = _CustomSearchFolders.ToList();

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
						_CustomSearchFolders.Add(FolderPath);
					}
				}
			}

			var listbox = sender as System.Windows.Controls.ListBox;
			listbox.Background = new SolidColorBrush(Color.FromRgb(226, 226, 226));
		}
	}
}
