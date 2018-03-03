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
    [TemplatePart(Name = PART_SearchBar, Type = typeof(FilterControl))]
    public partial class DependencyCustomListView : ListView
    {

        private const string PART_SearchBar = "PART_SearchBar";
        public FilterControl SearchBar = null;

        public DependencyCustomListView()
        {
            this.KeyDown += new KeyEventHandler(OnListViewKeyDown);
        }


        public static readonly DependencyProperty SearchListFilterProperty = DependencyProperty.Register(
            "SearchListFilter", typeof(string), typeof(DependencyCustomListView), new PropertyMetadata(null));

        public string SearchListFilter
        {
            get { return (string)GetValue(SearchListFilterProperty); }
            set { SetValue(SearchListFilterProperty, value); }
        }


        public static readonly DependencyProperty CopyHandlerProperty = DependencyProperty.Register(
            "CopyHandler", typeof(Func<object, string>), typeof(DependencyCustomListView), new PropertyMetadata(null));

        public Func<object, string> CopyHandler
        {
            get { return (Func<object, string>)GetValue(CopyHandlerProperty); }
            set { SetValue(CopyHandlerProperty, value); }
        }

        
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            AttachToVisualTree();
        }

        
        private void AttachToVisualTree()
        {
            SearchBar = GetTemplateChild(PART_SearchBar) as FilterControl;
            SearchBar.TargetControl = this;
        }



        #region events handlers
        protected virtual void OnListViewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            System.Windows.Controls.ListView ListView = sender as System.Windows.Controls.ListView;
            bool CtrlKeyDown = Keyboard.IsKeyDown(System.Windows.Input.Key.LeftCtrl) || Keyboard.IsKeyDown(System.Windows.Input.Key.RightCtrl);

            Debug.WriteLine("[DependencyCustomListView] Key Pressed : " + e.Key + ". Ctrl Key down : " + CtrlKeyDown);
            if ((e.Key == System.Windows.Input.Key.C) && CtrlKeyDown)
            {
                List<string> StrToCopy = new List<string>();
                foreach (object SelectItem in ListView.SelectedItems)
                {
                    StrToCopy.Add(CopyHandler(SelectItem));
                }

                System.Windows.Clipboard.Clear();
                System.Windows.Clipboard.SetText(String.Join("\n", StrToCopy.ToArray()), System.Windows.TextDataFormat.Text);
                return;
            }

            else if ((e.Key == System.Windows.Input.Key.F) && CtrlKeyDown)
            {
                if (this.SearchBar != null)
                {
                    this.SearchBar.Visibility = System.Windows.Visibility.Visible;
                    this.SearchBar.Focus();
                }

                return;
            }

            else if (e.Key == Key.Escape)
            {
                if (this.SearchBar != null)
                {
                    this.SearchBar.Clear();
                }
            }
        }
        #endregion events handlers
        
    }
}
