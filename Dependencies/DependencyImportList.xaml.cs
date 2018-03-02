using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Diagnostics;
using System.Windows.Input;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.ClrPh;

namespace Dependencies
{
    /// <summary>
    /// DependencyImportList  Filterable ListView for displaying imports.
    /// @TODO(Make this a template user control in order to share it between Modeules, Imports and Exports)
    /// </summary>
    public partial class DependencyImportList : UserControl
    {

        public DependencyImportList()
        {
            InitializeComponent();
        }

        public void SetImports(List<PeImportDll> Imports, PhSymbolProvider SymPrv, DependencyWindow Dependencies)
        {
            this.ImportList.Items.Clear();

            foreach (PeImportDll DllImport in Imports)
            {

                PE ModuleImport = Dependencies.LoadImport(DllImport.Name, null, DllImport.IsDelayLoad() );
                string ModuleFilepath = (ModuleImport != null) ? ModuleImport.Filepath : null;

                foreach (PeImport Import in DllImport.ImportList)
                {
                    this.ImportList.Items.Add(new DisplayPeImport(Import, SymPrv, ModuleFilepath));
                }
            }

            this.SearchBar.RaiseFilterEvent();
        }

        #region events handlers
        private void OnListViewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            bool CtrlKeyDown = Keyboard.IsKeyDown(System.Windows.Input.Key.LeftCtrl) || Keyboard.IsKeyDown(System.Windows.Input.Key.RightCtrl);

            Debug.WriteLine("[DependencyImportList] Key Pressed : " + e.Key + ". Ctrl Key down : " + CtrlKeyDown);
            if ((e.Key == System.Windows.Input.Key.C) && CtrlKeyDown)
            {
                List<string> StrToCopy = new List<string>();
                foreach (object SelectItem in this.ImportList.SelectedItems)
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
