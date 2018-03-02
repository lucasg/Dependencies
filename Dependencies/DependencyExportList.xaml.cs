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
        public DependencyExportList()
        {
           InitializeComponent();
        }

        public void SetExports(List<PeExport> Exports, PhSymbolProvider SymPrv)
        {
            this.ExportList.Items.Clear();

            foreach (PeExport Export in Exports)
            {
                this.ExportList.Items.Add(new DisplayPeExport(Export, SymPrv));
            }
        }

        #region events handlers
        private void OnListViewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            bool CtrlKeyDown = Keyboard.IsKeyDown(System.Windows.Input.Key.LeftCtrl) || Keyboard.IsKeyDown(System.Windows.Input.Key.RightCtrl);

            Debug.WriteLine("[DependencyExportList] Key Pressed : " + e.Key + ". Ctrl Key down : " + CtrlKeyDown);
            if ((e.Key == System.Windows.Input.Key.C) && CtrlKeyDown)
            {
                List<string> StrToCopy = new List<string>();
                foreach (object SelectItem in this.ExportList.SelectedItems)
                {
                    DisplayPeExport PeInfo = SelectItem as DisplayPeExport;
                    StrToCopy.Add(PeInfo.Name);
                }

                System.Windows.Clipboard.Clear();
                System.Windows.Clipboard.SetText(String.Join("\n", StrToCopy.ToArray()), System.Windows.TextDataFormat.Text);
                return;
            }

            else if ((e.Key == System.Windows.Input.Key.F) && CtrlKeyDown)
            {
                this.SearchBar.Visibility = System.Windows.Visibility.Visible;
                this.SearchBar.Focus();
                return;
            }

            else if (e.Key == Key.Escape)
            {
                this.SearchBar.Clear();
            }
        }

        

        private void ListViewSelectAll_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            System.Windows.Controls.ListView ListView = sender as System.Windows.Controls.ListView;
            ListView.SelectAll();
        }
        #endregion events handlers

    }
}
