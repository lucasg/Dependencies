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
    /// </summary>
    public partial class DependencyExportList : DependencyCustomListView
    {
        public DependencyExportList()
        {
           InitializeComponent();
        }

        public void SetExports(List<PeExport> Exports, PhSymbolProvider SymPrv)
        {
            this.Items.Clear();

            foreach (PeExport Export in Exports)
            {
                this.Items.Add(new DisplayPeExport(Export, SymPrv));
            }
        }

        private string ExportCopyHandler(object SelectedItem)
        {
            if (SelectedItem == null)
            {
                return "";
            }

            return (SelectedItem as DisplayPeExport).Name;
        }
    }
}
