using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Input;
using System.Diagnostics;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.ClrPh;

namespace Dependencies
{
    /// <summary>
    /// DependencyImportList  Filterable ListView for displaying exports.
    /// @TODO(Make this a template user control in order to share it between Modeules, Imports and Exports)
    /// </summary>
    public partial class DependencyExportList : UserControl
    {
        public ICollectionView ExportItemsView { get; set; }

        public DependencyExportList()
        {
            InitializeComponent();

            ExportItemsView = CollectionViewSource.GetDefaultView(this.ExportList.Items.SourceCollection);
        }

        public void SetExports(List<PeExport> Exports, PhSymbolProvider SymPrv)
        {
            this.ExportList.Items.Clear();

            foreach (PeExport Export in Exports)
            {
                this.ExportList.Items.Add(new DisplayPeExport(Export, SymPrv));
            }

            // Refresh search view
            ExportSearchFilter_OnTextChanged(null, null);
        }

        #region events handlers
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
                    DisplayPeExport PeInfo = SelectItem as DisplayPeExport;
                    StrToCopy.Add(PeInfo.Name);
                }

                System.Windows.Clipboard.Clear();
                System.Windows.Clipboard.SetText(String.Join("\n", StrToCopy.ToArray()), System.Windows.TextDataFormat.Text);
                return;
            }

            if ((e.Key == System.Windows.Input.Key.F) && CtrlKeyDown)
            {
                this.ExportSearchBar.Visibility = System.Windows.Visibility.Visible;
                this.ExportSearchFilter.Focus();
                return;
            }
        }

        private void OnTextBoxKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                // @TODO(HACK : Reset filter before closing, otherwise we might block the user out of enabling search bar again)
                this.ExportSearchFilter.Text = null;
                this.ExportSearchFilter_OnTextChanged(this.ExportList, null);

                this.OnExportSearchClose(null, null);
                return;
            }
        }
        #endregion events handlers

        #region search filter        
        private void OnExportSearchClose(object sender, RoutedEventArgs e)
        {
            this.ExportSearchBar.Visibility = System.Windows.Visibility.Collapsed;
        }

        private bool ExportListUserFilter(object item)
        {
            if (String.IsNullOrEmpty(ExportSearchFilter.Text))
                return true;
            else
                return ((item as DisplayPeExport).Name.IndexOf(ExportSearchFilter.Text, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private void ExportSearchFilter_OnTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            ExportItemsView.Filter = ExportListUserFilter;
            ExportItemsView.Refresh();
        }
        #endregion search filter

        private void ListViewSelectAll_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            System.Windows.Controls.ListView ListView = sender as System.Windows.Controls.ListView;
            ListView.SelectAll();
        }
        
    }
}
