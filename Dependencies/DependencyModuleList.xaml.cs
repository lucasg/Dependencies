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
    public class ModuleCacheKey : Tuple<string, string>
    {
        public ModuleCacheKey(string Name, string Filepath)
        : base(Name, Filepath)
        {
        }
    }

    public class ModulesCache : Dictionary<ModuleCacheKey, DisplayModuleInfo>
    {

    }


    /// <summary>
    /// DependencyImportList  Filterable ListView for displaying modules.
    /// @TODO(Make this a template user control in order to share it between Modeules, Imports and Exports)
    /// </summary>
    public partial class DependencyModuleList : UserControl
    {
        public ICollectionView ModulesItemsView { get; set; }

        public RelayCommand DoFindModuleInTreeCommand
        {
            get { return (RelayCommand) GetValue(DoFindModuleInTreeCommandProperty); }
            set { SetValue(DoFindModuleInTreeCommandProperty, value);}
        }

        public RelayCommand ConfigureSearchOrderCommand
        {
            get { return (RelayCommand)GetValue(ConfigureSearchOrderCommandProperty); }
            set { SetValue(ConfigureSearchOrderCommandProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DoFindModuleInTreeCommand.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DoFindModuleInTreeCommandProperty =
            DependencyProperty.Register("DoFindModuleInTreeCommand", typeof(RelayCommand), typeof(DependencyModuleList), new UIPropertyMetadata(null));

        public static readonly DependencyProperty ConfigureSearchOrderCommandProperty =
            DependencyProperty.Register("ConfigureSearchOrderCommand", typeof(RelayCommand), typeof(DependencyModuleList), new UIPropertyMetadata(null));

        public static readonly RoutedEvent SelectedModuleChangedEvent
            = EventManager.RegisterRoutedEvent("SelectedModuleChanged", RoutingStrategy.Direct, typeof(RoutedEventHandler), typeof(DependencyModuleList));


        public DependencyModuleList()
        {
            InitializeComponent();

            ModulesItemsView = CollectionViewSource.GetDefaultView(this.ModulesList.Items.SourceCollection);
        }

        public void AddModule(DisplayModuleInfo NewModule)
        {
            // TODO : Find a way to properly bind commands instead of using this hack
            NewModule.DoFindModuleInTreeCommand = DoFindModuleInTreeCommand;
            NewModule.ConfigureSearchOrderCommand = ConfigureSearchOrderCommand;

            this.ModulesList.Items.Add(NewModule);
        }


        #region public events
        public event RoutedEventHandler SelectedModuleChanged
        {
            add { AddHandler(SelectedModuleChangedEvent, value); }
            remove { RemoveHandler(SelectedModuleChangedEvent, value); }
        }

        private void OnSelectedModuleChanged(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(SelectedModuleChangedEvent));
        }
        #endregion public events

        #region events handlers
        private void OnListViewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            bool CtrlKeyDown = Keyboard.IsKeyDown(System.Windows.Input.Key.LeftCtrl) || Keyboard.IsKeyDown(System.Windows.Input.Key.RightCtrl);

            Debug.WriteLine("[DependencyModuleList] Key Pressed : " + e.Key + ". Ctrl Key down : " + CtrlKeyDown);
            if ((e.Key == System.Windows.Input.Key.C) && CtrlKeyDown)
            {
                List<string> StrToCopy = new List<string>();
                foreach (object SelectItem in this.ModulesList.SelectedItems)
                {
                    DisplayModuleInfo ModuleInfo = SelectItem as DisplayModuleInfo;
                    StrToCopy.Add(ModuleInfo.ModuleName);
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
