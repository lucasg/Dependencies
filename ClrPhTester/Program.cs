using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using System.IO;
using System.ClrPh;

namespace ClrPhTester
{
    class Program
    {
        static void PrintKnownDlls()
        {
            Console.WriteLine("64-bit KnownDlls : ");
            foreach (String KnownDll in Phlib.GetKnownDlls(false))
            {
                Console.WriteLine("\t{0:s}", KnownDll);
            }
            Console.WriteLine("");

            Console.WriteLine("32-bit KnownDlls : ");
            foreach (String KnownDll in Phlib.GetKnownDlls(true))
            {
                Console.WriteLine("\t{0:s}", KnownDll);
            }
            Console.WriteLine("");
        }



        static void PrintApplicationManifest(PE Application)
        {
            String PeManifest = Application.GetManifest();
            if (PeManifest.Length == 0)
            {
                return;
            }

            // Use a memory stream to correctly handle BOM encoding for manifest resource
            using (var stream = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(PeManifest)))
            {
                XDocument XmlManifest = XDocument.Load(stream);
                XNamespace Namespace = XmlManifest.Root.GetDefaultNamespace();
                Console.WriteLine(XmlManifest);

                // Extracting assemblyIdentity
                String DependencyNodeName = String.Format("{{{0}}}dependency", Namespace);
                String AssemblyIdentityNodeName = String.Format("{{{0}}}assemblyIdentity", Namespace);
                foreach (XElement SxsDependency in XmlManifest.Descendants(DependencyNodeName))
                {
                    Console.WriteLine("SxsDependency : \n{0}", SxsDependency);

                    foreach (XElement SxsAssembly in SxsDependency.Descendants(AssemblyIdentityNodeName))
                    {
                        Console.WriteLine("SxsAssembly : {0}", SxsAssembly);
                    }
                }
            }
        }


        static void Main(string[] args)
        {
            Phlib.InitializePhLib();
            

            Console.WriteLine("Printing system KnownDll");
            PrintKnownDlls();


            //String FileName = "F:\\Dev\\processhacker2\\TestBt\\ClangDll\\Release\\ClangDll.dll";
            //String FileName = "C:\\Windows\\System32\\kernelbase.dll";
            String FileName = "C:\\Program Files (x86)\\Google\\Chrome\\Application\\chrome.exe";
            PE Pe = new PE(FileName);
            List<PeExport> Exports = Pe.GetExports();
            List<PeImportDll> Imports = Pe.GetImports();

            Console.WriteLine("Printing selected Pe manifest");
            PrintApplicationManifest(Pe);


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
