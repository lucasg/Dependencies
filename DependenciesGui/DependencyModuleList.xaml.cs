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

    public class ModuleCacheKey
    {
        public ModuleCacheKey(string _Name, string _Filepath, ModuleFlag _Flags = ModuleFlag.NoFlag)
        {
            Name = _Name;
            Filepath = _Filepath;
            Flags = _Flags;
        }

        public ModuleCacheKey(ImportContext import)
        {
            Name = import.ModuleName;
            Filepath = import.PeFilePath;
            Flags = import.Flags;
        }

        // mandatory since ModuleCacheKey is used as a dictionnary key
        public override int GetHashCode()
        {
            return Name.GetHashCode() ^ Filepath.GetHashCode() ^ Flags.GetHashCode();
        }

        public string Name;
        public string Filepath;
        public ModuleFlag Flags;
    }



    public class ModulesCache : Dictionary<ModuleCacheKey, DisplayModuleInfo>
    {

    }


    /// <summary>
    /// DependencyImportList  Filterable ListView for displaying modules.
    /// </summary>
    public partial class DependencyModuleList : DependencyCustomListView
    {
        //public ICollectionView ModulesItemsView { get; set; }

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

        }

        public void AddModule(DisplayModuleInfo NewModule)
        {
            // TODO : Find a way to properly bind commands instead of using this hack
            NewModule.DoFindModuleInTreeCommand = DoFindModuleInTreeCommand;
            NewModule.ConfigureSearchOrderCommand = ConfigureSearchOrderCommand;
          

            this.Items.Add(NewModule);
        }

        public event RoutedEventHandler SelectedModuleChanged
        {
            add { AddHandler(SelectedModuleChangedEvent, value); }
            remove { RemoveHandler(SelectedModuleChangedEvent, value); }
        }

        private void OnSelectedModuleChanged(object sender, MouseButtonEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(SelectedModuleChangedEvent));
        }

        private string ModuleCopyHandler(object SelectedItem)
        {
            if (SelectedItem == null)
            {
                return "";
            }

            return (SelectedItem as DisplayModuleInfo).ModuleName;
        }
    }
}
