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
        public static void DumpKnownDlls()
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



        public static void DumpManifest(PE Application)
        {
            String PeManifest = Application.GetManifest();
            Console.WriteLine("Manifest for file : {0}", Application.Filepath);

            if (PeManifest.Length == 0)
            {
                return;
            }

            try
            {
                // Use a memory stream to correctly handle BOM encoding for manifest resource
                using (var stream = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(PeManifest)))
                {
                    XDocument XmlManifest = XDocument.Load(stream);
                    XNamespace Namespace = XmlManifest.Root.GetDefaultNamespace();
                    Console.WriteLine(XmlManifest);
                }
            }
            catch (System.Xml.XmlException)
            {
                Console.WriteLine(" \"Malformed\" pe manifest : {0}", PeManifest);
            }
        }

        public static void DumpSxsEntries(PE Application)
        {
            SxsEntries SxsDependencies = SxsManifest.GetSxsEntries(Application);

            Console.WriteLine("sxs dependencies for executable : {0}", Application.Filepath);
            foreach (var entry in SxsDependencies)
            {
                if (entry.Item2.Contains("???"))
                {
                    Console.WriteLine("  [x] {0:s} : {1:s}", entry.Item1, entry.Item2);
                }
                else
                {
                    Console.WriteLine("  [+] {0:s} : {1:s}", entry.Item1, entry.Item2);
                }
            }
        }


        public static void DumpExports(PE Pe)
        {
            List<PeExport> Exports = Pe.GetExports();
            Console.WriteLine("Export listing for file : {0}", Pe.Filepath);

            foreach (PeExport Export in Exports)
            {
                Console.WriteLine("Export {0:d} :", Export.Ordinal);
                Console.WriteLine("\t Name : {0:s}", Export.Name);
                Console.WriteLine("\t VA : 0x{0:X}", (int)Export.VirtualAddress);
                if (Export.ForwardedName.Length > 0)
                    Console.WriteLine("\t ForwardedName : {0:s}", Export.ForwardedName);
            }

            Console.WriteLine("Export listing done");
        }

        public static void DumpImports(PE Pe)
        {
            List<PeImportDll> Imports = Pe.GetImports();
            Console.WriteLine("Import listing for file : {0}", Pe.Filepath);

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


        static void Main(string[] args)
        {

            Phlib.InitializePhLib();
            var ProgramArgs = ParseArgs(args);

            //String FileName = "F:\\Dev\\processhacker2\\TestBt\\ClangDll\\Release\\ClangDll.dll";
            //String FileName = "C:\\Windows\\System32\\kernelbase.dll";
            //String FileName = "C:\\Program Files (x86)\\Google\\Chrome\\Application\\chrome.exe";
            String FileName = ProgramArgs["file"];
            PE Pe = new PE(FileName);

            if (ProgramArgs.ContainsKey("-knowndll"))
                DumpKnownDlls();
            if (ProgramArgs.ContainsKey("-manifest"))
                DumpManifest(Pe);
            if (ProgramArgs.ContainsKey("-sxsentries"))
                DumpSxsEntries(Pe);
            if (ProgramArgs.ContainsKey("-imports"))
                DumpImports(Pe);
            if (ProgramArgs.ContainsKey("-exports"))
                DumpExports(Pe);


        }

        private static Dictionary<string, string> ParseArgs(string[] args)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (string s in args)
            {
                if (s.StartsWith("-", StringComparison.OrdinalIgnoreCase))
                {
                    if (!dict.ContainsKey(s))
                        dict.Add(s, string.Empty);

                }
                else
                {
                    dict.Add("file", s);
                }
            }

            return dict;
        }
    }
}
