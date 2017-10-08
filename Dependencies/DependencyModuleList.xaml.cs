using System;
using System.Windows;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Input;
using System.Diagnostics;
using System.ComponentModel;
using System.Windows.Data;

namespace Dependencies
{
    /// <summary>
    /// Logique d'interaction pour DependencyModuleList.xaml
    /// </summary>
    public partial class DependencyModuleList : UserControl
    {
        public ICollectionView ModulesItemsView { get; set; }

        public DependencyModuleList()
        {
            InitializeComponent();

            ModulesItemsView = CollectionViewSource.GetDefaultView(this.ModulesList.Items.SourceCollection);
        }

        public void AddModule(DisplayModuleInfo NewModule)
        {
            this.ModulesList.Items.Add(NewModule);
        }

        public void Clear()
        {
            this.ModulesList.Items.Clear();
        }

        public static readonly RoutedEvent SelectedModuleChangedEvent
            = EventManager.RegisterRoutedEvent("SelectedModuleChanged", RoutingStrategy.Direct, typeof(RoutedEventHandler), typeof(DependencyModuleList));

        public event RoutedEventHandler SelectedModuleChanged
        {
            add { AddHandler(SelectedModuleChangedEvent, value); }
            remove { RemoveHandler(SelectedModuleChangedEvent, value); }
        }

        private void OnSelectedModuleChanged(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(SelectedModuleChangedEvent));
        }


        private void OnListViewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            System.Windows.Controls.ListView ListView = sender as System.Windows.Controls.ListView;
            bool CtrlKeyDown = Keyboard.IsKeyDown(System.Windows.Input.Key.LeftCtrl) || Keyboard.IsKeyDown(System.Windows.Input.Key.RightCtrl);

            Debug.WriteLine("[DependencyModuleList] Key Pressed : " + e.Key + ". Ctrl Key down : " + CtrlKeyDown);
            if ((e.Key == System.Windows.Input.Key.C) && CtrlKeyDown)
            {
                List<string> StrToCopy = new List<string>();
                foreach (object SelectItem in ListView.SelectedItems)
                {
                    DisplayModuleInfo ModuleInfo = SelectItem as DisplayModuleInfo;
                    StrToCopy.Add(ModuleInfo.ModuleName);
                }

                System.Windows.Clipboard.Clear();
                System.Windows.Clipboard.SetText(String.Join("\n", StrToCopy.ToArray()), System.Windows.TextDataFormat.Text);
                return;
            }

            if ((e.Key == System.Windows.Input.Key.F) && CtrlKeyDown)
            {
                this.ModulesSearchBar.Visibility = System.Windows.Visibility.Visible;
                this.ModuleSearchFilter.Focus();
                return;
            }

        }

        private void OnTextBoxKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                // HACK : Reset filter before closing
                this.ModuleSearchFilter.Text = null;
                this.ModuleSearchFilter_OnTextChanged(this.ModulesList, null);

                this.OnModuleSearchClose(null, null);
                return;
            }
        }

        private void OnModuleSearchClose(object sender, RoutedEventArgs e)
        {
            this.ModulesSearchBar.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void ListViewSelectAll_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            System.Windows.Controls.ListView ListView = sender as System.Windows.Controls.ListView;
            ListView.SelectAll();
        }

        private bool ModulesListUserFilter(object item)
        {
            if (String.IsNullOrEmpty(ModuleSearchFilter.Text))
                return true;
            else
                return ((item as DisplayModuleInfo).ModuleName.IndexOf(ModuleSearchFilter.Text, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private void ModuleSearchFilter_OnTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            ModulesItemsView.Filter = ModulesListUserFilter;
            ModulesItemsView.Refresh();
        }
    }
}
