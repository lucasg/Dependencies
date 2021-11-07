using System;
using System.Linq;
using System.Windows;
using System.Collections.Generic;
using System.Windows.Input;

using Dependencies.ClrPh;

namespace Dependencies
{
    /// <summary>
    /// DependencyImportList  Filterable ListView for displaying exports.
    /// </summary>
    public partial class DependencyExportList : DependencyCustomListView
    {
		public static readonly RoutedUICommand CopyValuesCommand = new RoutedUICommand();

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

            return (SelectedItem as DisplayPeExport).ToString();
        }

		private void ExportListCopySelectedValues(object sender, RoutedEventArgs e)
		{
			if (this.SelectedItems.Count == 0)
				return;

			List<DisplayPeExport> selectedExports = new List<DisplayPeExport>();
			foreach (var import in this.SelectedItems)
			{
				selectedExports.Add((import as DisplayPeExport));
			}

			string SelectedValues = String.Join("\n", selectedExports.Select(exp => exp.ToString()));

			Clipboard.Clear();

			// sometimes another process has "opened" the clipboard, so we need to wait for it
			try
			{
				Clipboard.SetText((string)SelectedValues, TextDataFormat.Text);
				return;
			}
			catch { }
		}

		public void ResetAutoSortProperty()
		{
			Wpf.Util.GridViewSort.RemoveSort(this.Items, this);
		}
	}
}
