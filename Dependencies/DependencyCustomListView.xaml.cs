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
    public partial class DependencyCustomListView : UserControl
    {
        /// <summary>
        /// Routed command which can be used to close a tab.
        /// </summary>
        public static RoutedCommand CloseSearchCommand = new RoutedUICommand("Close", "Close", typeof(DependencyCustomListView));

        protected ICollectionView ItemsView { get; set; }
        protected Predicate<object> _ListUserFilter;

        public DependencyCustomListView()
        {
            CommandManager.RegisterClassCommandBinding(typeof(Button), new CommandBinding(CloseSearchCommand, CloseSearchClassHandler, CloseSearchCanExecuteClassHandler));
        }

        private static void CloseSearchCanExecuteClassHandler(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }
        private static void CloseSearchClassHandler(object sender, ExecutedRoutedEventArgs e)
        {
            //(sender as DependencyCustomListView).SearchBar.Visibility = System.Windows.Visibility.Collapsed;
        }

        public string SearchText {
            // get {return this.SearchFilter.Text;}
            get { return ""; }
        }

        // public void SetExports(List<PeExport> Exports, PhSymbolProvider SymPrv)
        // {
        //     this.ExportList.Items.Clear();

        //     foreach (PeExport Export in Exports)
        //     {
        //         this.ExportList.Items.Add(new DisplayPeExport(Export, SymPrv));
        //     }

        //     // Refresh search view
        //     ExportSearchFilter_OnTextChanged(null, null);
        // }

        #region events handlers
        protected void OnListViewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            System.Windows.Controls.ListView ListView = sender as System.Windows.Controls.ListView;

            bool CtrlKeyDown = Keyboard.IsKeyDown(System.Windows.Input.Key.LeftCtrl) || Keyboard.IsKeyDown(System.Windows.Input.Key.RightCtrl);

            Debug.WriteLine("[DependencyImportList] Key Pressed : " + e.Key + ". Ctrl Key down : " + CtrlKeyDown);
            if ((e.Key == System.Windows.Input.Key.C) && CtrlKeyDown)
            {
                List<string> StrToCopy = new List<string>();
                foreach (object SelectItem in ListView.SelectedItems)
                {
                    // DisplayPeExport PeInfo = SelectItem as DisplayPeExport;
                    // StrToCopy.Add(PeInfo.Name);
                }

                System.Windows.Clipboard.Clear();
                System.Windows.Clipboard.SetText(String.Join("\n", StrToCopy.ToArray()), System.Windows.TextDataFormat.Text);
                return;
            }

            if ((e.Key == System.Windows.Input.Key.F) && CtrlKeyDown)
            {
                // this.SearchBar.Visibility = System.Windows.Visibility.Visible;
                // this.SearchFilter.Focus();
                return;
            }
        }

        private void OnTextBoxKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                // @TODO(HACK : Reset filter before closing, otherwise we might block the user out of enabling search bar again)
                //this.SearchFilter.Text = null;
                // this.SearchFilter_OnTextChanged(this.ExportList, null);

                //this.OnSearchClose(null, null);
                return;
            }
        }
        #endregion events handlers

        #region search filter       
        
        //public ICommand OnSearchClose
        //{
        //    get { return }
        //}

        //public class CloseCommand : ICommand
        //{
        //    public bool CanExecute(object parameter)
        //    {
        //        return true;
        //    }
        //    public event EventHandler CanExecuteChanged;

        //    public void Execute(object parameter)
        //    {
        //        this.SearchBar.Visibility = System.Windows.Visibility.Collapsed;
        //    }
        //}

        //public void OnSearchClose(object sender, RoutedEventArgs e)
        //{
        //    DependencyCustomListView List = (sender as DependencyCustomListView);
        //    //this.SearchBar.Visibility = System.Windows.Visibility.Collapsed;
        //}

        // private bool ExportListUserFilter(object item)
        // {
        //     if (String.IsNullOrEmpty(ExportSearchFilter.Text))
        //         return true;
        //     else
        //         return ((item as DisplayPeExport).Name.IndexOf(SearchFilter.Text, StringComparison.OrdinalIgnoreCase) >= 0);
        // }

        public void SearchFilter_OnTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            this.ItemsView.Filter = this._ListUserFilter;
            this.ItemsView.Refresh();
        }
        #endregion search filter

        protected void ListViewSelectAll_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            System.Windows.Controls.ListView ListView = sender as System.Windows.Controls.ListView;
            ListView.SelectAll();
        }
        
    }
}
