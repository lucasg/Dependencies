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
            String FileName = "C:\\Windows\\System32\\kernel32.dll";
            PE Pe = new PE(FileName);
            List<PeExport> Exports = Pe.GetExports();

            
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
            //List < PeExport > ExportList = Pe;
        }
    }
}
