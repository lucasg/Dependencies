using System.Collections.Generic;

using Dependencies.ClrPh;

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

		public void SetImports(string ModuleFilepath, List<PeExport> Exports, List<PeImportDll> ParentImports, PhSymbolProvider SymPrv, DependencyWindow Dependencies)
		{
			this.Items.Clear();

			foreach (PeImportDll DllImport in ParentImports)
			{
				foreach (var Import in BinaryCache.LookupImports(DllImport, Exports))
				{
					this.Items.Add(new DisplayPeImport(Import.Item1, SymPrv, ModuleFilepath, Import.Item2));
				}
			}
		}

        public void SetRootImports(List<PeImportDll> Imports, PhSymbolProvider SymPrv, DependencyWindow Dependencies)
        {
            this.Items.Clear();

            foreach (PeImportDll DllImport in Imports)
            {

                PE ModuleImport = Dependencies.LoadImport(DllImport.Name, null, DllImport.IsDelayLoad() );
                string ModuleFilepath = (ModuleImport != null) ? ModuleImport.Filepath : null;

                foreach( var Import in BinaryCache.LookupImports(DllImport, ModuleFilepath))
                {
                    this.Items.Add(new DisplayPeImport(Import.Item1, SymPrv, ModuleFilepath, Import.Item2));
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
