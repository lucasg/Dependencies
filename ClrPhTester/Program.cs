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
            List<PeImportDll> Imports = Pe.GetImports();


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
            foreach (PeImportDll DllImport in Imports)
            {
                Console.WriteLine("Import from module {0:s} :", DllImport.Name);

                foreach (PeImport Import in DllImport.ImportList)
                {
                    if (Import.ImportByOrdinal)
                    {
                        Console.Write("\t Ordinal_{0:d} ", Import.Ordinal);
                    }
                    else
                    {
                        Console.Write("\t Function {0:s}", Import.Name);
                    }
                    if (Import.DelayImport)                        
                        Console.WriteLine(" (Delay Import)");
                    else
                        Console.WriteLine("");
                }
            }

            Console.WriteLine("Import listing done");

        }
    }
}
