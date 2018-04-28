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
    /// </summary>
    public partial class DependencyImportList : DependencyCustomListView
    {

        public DependencyImportList()
        {
            InitializeComponent();
        }

        public void SetImports(List<PeImportDll> Imports, PhSymbolProvider SymPrv, DependencyWindow Dependencies)
        {
            this.Items.Clear();

            foreach (PeImportDll DllImport in Imports)
            {

                PE ModuleImport = Dependencies.LoadImport(DllImport.Name, null, DllImport.IsDelayLoad() );
                string ModuleFilepath = (ModuleImport != null) ? ModuleImport.Filepath : null;

                foreach (PeImport Import in DllImport.ImportList)
                {
                    this.Items.Add(new DisplayPeImport(Import, SymPrv, ModuleFilepath));
                }
            }
        }

        private string ImportCopyHandler(object SelectedItem)
        {
            if (SelectedItem == null)
            {
                return "";
            }

            return (SelectedItem as DisplayPeImport).Name;
        }
    }
}
