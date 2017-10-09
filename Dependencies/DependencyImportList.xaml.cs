using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Diagnostics;
using System.Windows.Input;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;

namespace Dependencies
{
    
    public partial class DependencyImportList : UserControl
    {
        public ICollectionView ImportItemsView { get; set; }

        public DependencyImportList()
        {
            InitializeComponent();

            ImportItemsView = CollectionViewSource.GetDefaultView(this.ImportList.Items.SourceCollection);
        }

        private void OnListViewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            System.Windows.Controls.ListView ListView = sender as System.Windows.Controls.ListView;
            bool CtrlKeyDown = Keyboard.IsKeyDown(System.Windows.Input.Key.LeftCtrl) || Keyboard.IsKeyDown(System.Windows.Input.Key.RightCtrl);

            Debug.WriteLine("[DependencyImportList] Key Pressed : " + e.Key + ". Ctrl Key down : " + CtrlKeyDown);
            if ((e.Key == System.Windows.Input.Key.C) && CtrlKeyDown)
            {
                List<string> StrToCopy = new List<string>();
                foreach (object SelectItem in ListView.SelectedItems)
                {
                    DisplayPeImport PeInfo = SelectItem as DisplayPeImport;
                    StrToCopy.Add(PeInfo.Name);
                }

                System.Windows.Clipboard.Clear();
                System.Windows.Clipboard.SetText(String.Join("\n", StrToCopy.ToArray()), System.Windows.TextDataFormat.Text);
                return;
            }

            if ((e.Key == System.Windows.Input.Key.F) && CtrlKeyDown)
            {
                this.ImportSearchBar.Visibility = System.Windows.Visibility.Visible;
                this.ImportSearchFilter.Focus();
                return;
            }
        }

        private void OnTextBoxKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                // HACK : Reset filter before closing
                this.ImportSearchFilter.Text = null;
                this.ImportSearchFilter_OnTextChanged(this.ImportList, null);

                this.OnImportSearchClose(null, null);
                return;
            }
        }

        private void OnImportSearchClose(object sender, RoutedEventArgs e)
        {
            this.ImportSearchBar.Visibility = System.Windows.Visibility.Collapsed;
        }

        private bool ImportListUserFilter(object item)
        {
            if (String.IsNullOrEmpty(ImportSearchFilter.Text))
                return true;
            else
                return ((item as DisplayPeImport).Name.IndexOf(ImportSearchFilter.Text, StringComparison.OrdinalIgnoreCase) >= 0) ||
                       ((item as DisplayPeImport).ModuleName.IndexOf(ImportSearchFilter.Text, StringComparison.OrdinalIgnoreCase) >= 0);

        }

        private void ImportSearchFilter_OnTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            ImportItemsView.Filter = ImportListUserFilter;
            ImportItemsView.Refresh();
        }


        private void ListViewSelectAll_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            System.Windows.Controls.ListView ListView = sender as System.Windows.Controls.ListView;
            ListView.SelectAll();
        }
    }
}
