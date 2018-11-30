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

namespace Dependencies
{
	/// <summary>
	/// Logique d'interaction pour SearchFolder.xaml
	/// </summary>
	public partial class SearchFolder : Window
	{
		private DependencyWindow _SelectedItem;
		private ObservableCollection<string> _CustomSearchFolders;

		public SearchFolder(DependencyWindow SelectedItem)
		{
			_SelectedItem = SelectedItem;
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

		private void SearchFolder_DragOver(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				e.Effects = DragDropEffects.Copy;
				var listbox = sender as ListBox;
				listbox.Background = new SolidColorBrush(Color.FromRgb(155, 155, 155));
			}
			else
			{
				e.Effects = DragDropEffects.None;
			}
		}

		private void SearchFolder_DragLeave(object sender, DragEventArgs e)
		{
			var listbox = sender as ListBox;
			listbox.Background = new SolidColorBrush(Color.FromRgb(226, 226, 226));
		}

		private void SearchFolder_Drop(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				string[] folders = (string[]) e.Data.GetData(DataFormats.FileDrop);

				foreach (string FolderPath in folders)
				{
					if (Directory.Exists(FolderPath))
					{
						_CustomSearchFolders.Add(FolderPath);
					}
				}
			}

			var listbox = sender as ListBox;
			listbox.Background = new SolidColorBrush(Color.FromRgb(226, 226, 226));
		}
	}
}
