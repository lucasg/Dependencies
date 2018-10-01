using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ClrPh;

namespace Dependencies
{
    namespace Test
    { 
        class Program
        {
            static bool TestKnownInputs()
            {
                return true;
            }

            static bool TestFilepath(string Filepath)
            {
                PhSymbolProvider SymPrv = new PhSymbolProvider();

                PE Pe = new PE(Filepath);
                if (!Pe.Load())
                {
                    Console.Error.WriteLine("[x] Could not load file {0:s} as a PE", Filepath);
                    return false;
                }

                foreach (PeExport Export in Pe.GetExports())
                {
                    if (Export.Name.Length > 0)
                        Console.WriteLine("\t Export : {0:s} -> {1:s}", Export.Name, SymPrv.UndecorateName(Export.Name));
                }

                foreach (PeImportDll DllImport in Pe.GetImports())
                {
                    foreach (PeImport Import in DllImport.ImportList)
                    {
                        if (!Import.ImportByOrdinal)
                        {
                            Console.WriteLine("\t Import from {0:s} : {1:s} -> {2:s}", DllImport.Name, Import.Name, SymPrv.UndecorateName(Import.Name));
                        }
                    }
                }

                return true;
            }

            static void Main(string[] args)
            {
                // always the first call to make
                Phlib.InitializePhLib();

                if (args.Length == 0)
                {
                    TestKnownInputs();
                    return;
                }

                TestFilepath(args[0]);
            }
        }
    }
}
