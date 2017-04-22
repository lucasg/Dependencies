using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ClrPh;

namespace ClrPhTester
{
    class Program
    {
        static void Main(string[] args)
        {
            Phlib.InitializePhLib();

            //String FileName = "F:\\Dev\\processhacker2\\TestBt\\ClangDll\\Release\\ClangDll.dll";
            String FileName = "C:\\Windows\\System32\\kernelbase.dll";
            PE Pe = new PE(FileName);
            List<PeExport> Exports = Pe.GetExports();
            List<PeImport> Imports = Pe.GetImports();


            Console.WriteLine("Export listing for file : {0}" , FileName);
            foreach(PeExport Export in Exports)
            {
                Console.WriteLine("Export {0:d} :", Export.Ordinal);
                Console.WriteLine("\t Name : {0:s}", Export.Name);
                Console.WriteLine("\t VA : 0x{0:X}", (int) Export.VirtualAddress);
                if (Export.ForwardedName.Length > 0)
                    Console.WriteLine("\t ForwardedName : {0:s}", Export.ForwardedName);
            }

            Console.WriteLine("Export listing done");

            Console.WriteLine("Import listing for file : {0}", FileName);
            foreach (PeImport Import in Imports)
            {
                if (Import.ImportByOrdinal)
                {
                    Console.WriteLine("Import {0:s} Ordinal_{1:d} :", Import.ModuleName, Import.Ordinal);
                }
                else
                {
                    Console.WriteLine("Import {0:s} Name {1:d} :", Import.ModuleName, Import.Name);
                }
                if (Import.DelayImport)
                    Console.WriteLine("\t Delay Import");

            }

            Console.WriteLine("Import listing done");

        }
    }
}
