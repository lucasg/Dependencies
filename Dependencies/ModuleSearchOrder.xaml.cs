using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.ComponentModel;
using System.Windows.Data;


namespace Dependencies
{


    public class ModuleSearchResult
    {
        public string ModuleFilePath { get; set; }

        public ModuleSearchStrategy SearchStrategy { get; set; }
    }


    /// <summary>
    /// Interaction logic for ModuleSearchOrder.xaml
    /// </summary>
    public partial class ModuleSearchOrder : Window
    {
        public ModuleSearchOrder(ModulesCache LoadedModules)
        {
            InitializeComponent();
            List<ModuleSearchResult> items = new List<ModuleSearchResult>();

            foreach (var LoadedModule in LoadedModules.Values)
            {
                items.Add(new ModuleSearchResult() { ModuleFilePath = LoadedModule.ModuleName, SearchStrategy = LoadedModule.Location });
            }
            OrderedModules.ItemsSource = items;

            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(OrderedModules.ItemsSource);

            PropertyGroupDescription groupDescription = new PropertyGroupDescription("SearchStrategy");
            view.GroupDescriptions.Add(groupDescription);

            SortDescription sortDescription = new SortDescription("SearchStrategy", ListSortDirection.Ascending);
            view.SortDescriptions.Add(sortDescription);
        }
    }
}
