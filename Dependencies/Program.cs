using System;
using System.Collections.Generic;
using System.Xml.Linq;
using System.ClrPh;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using System.Diagnostics;

namespace Dependencies
{
    interface IPrettyPrintable
    {
        void PrettyPrint();
    }

    /// <summary>
    /// Printable KnownDlls object
    /// </summary>
    class NtKnownDlls : IPrettyPrintable
    {
        public NtKnownDlls()
        {
            x64 = Phlib.GetKnownDlls(false);
            x86 = Phlib.GetKnownDlls(true);
        }

        public void PrettyPrint()
        {
            Console.WriteLine("[-] 64-bit KnownDlls : ");

            foreach (String KnownDll in this.x64)
            {
                string System32Folder = Environment.GetFolderPath(Environment.SpecialFolder.System);
                Console.WriteLine("  {0:s}\\{1:s}", System32Folder, KnownDll);
            }

            Console.WriteLine("");

            Console.WriteLine("[-] 32-bit KnownDlls : ");

            foreach (String KnownDll in this.x86)
            {
                string SysWow64Folder = Environment.GetFolderPath(Environment.SpecialFolder.SystemX86);
                Console.WriteLine("  {0:s}\\{1:s}", SysWow64Folder, KnownDll);
            }


            Console.WriteLine("");
        }

        public List<String> x64;
        public List<String> x86;
    }

    /// <summary>
    /// Printable ApiSet schema object
    /// </summary>
    class NtApiSet : IPrettyPrintable
    {
        public NtApiSet()
        {
            Schema = Phlib.GetApiSetSchema();
        }

        public void PrettyPrint()
        {
            Console.WriteLine("[-] Api Sets Map : ");

            foreach (var ApiSetEntry in this.Schema)
            {
                ApiSetTarget ApiSetImpl = ApiSetEntry.Value;
                string ApiSetName = ApiSetEntry.Key;
                string ApiSetImplStr = (ApiSetImpl.Count > 0) ? String.Join(",", ApiSetImpl.ToArray()) : "";

                Console.WriteLine("{0:s} -> [ {1:s} ]", ApiSetName, ApiSetImplStr);
            }

            Console.WriteLine("");
        }

        public ApiSetSchema Schema;
    }


    class PEManifest : IPrettyPrintable
    {

        public PEManifest(PE _Application)
        {
            Application = _Application;
            Manifest = Application.GetManifest();
            XmlManifest = null;
            Exception = "";

            if (Manifest.Length != 0)
            {
                try
                {
                    // Use a memory stream to correctly handle BOM encoding for manifest resource
                    using (var stream = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(Manifest)))
                    {
                        XmlManifest = SxsManifest.ParseSxsManifest(stream);
                    }
                    

                }
                catch (System.Xml.XmlException e)
                {
                    //Console.Error.WriteLine("[x] \"Malformed\" pe manifest for file {0:s} : {1:s}", Application.Filepath, PeManifest);
                    //Console.Error.WriteLine("[x] Exception : {0:s}", e.ToString());
                    XmlManifest = null;
                    Exception = e.ToString();
                }
            }
        }


        public void PrettyPrint()
        {
            Console.WriteLine("[-] Manifest for file : {0}", Application.Filepath);

            if (Manifest.Length == 0)
            {
                Console.WriteLine("[x] No embedded pe manifest for file {0:s}", Application.Filepath);
                return;
            }

            if (Exception.Length != 0)
            {
                Console.Error.WriteLine("[x] \"Malformed\" pe manifest for file {0:s} : {1:s}", Application.Filepath, Manifest);
                Console.Error.WriteLine("[x] Exception : {0:s}", Exception);
                return;
            }

            Console.WriteLine(XmlManifest);
        }

        public string Manifest;
        public XDocument XmlManifest;

        // stays private in order not end up in the json output
        private PE Application;
        private string Exception;
    }

    class PEImports : IPrettyPrintable
    {
        public PEImports(PE _Application)
        {
            Application = _Application;
            Imports = Application.GetImports();
        } 

        public void PrettyPrint()
        {
            Console.WriteLine("[-] Import listing for file : {0}", Application.Filepath);

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

            Console.WriteLine("[-] Import listing done");
        }

        public List<PeImportDll> Imports;
        private PE Application;
    }

    class PEExports : IPrettyPrintable
    {
        public PEExports(PE _Application)
        {
            Application = _Application;
            Exports =  Application.GetExports();
        } 

        public void PrettyPrint()
        {
            Console.WriteLine("[-] Export listing for file : {0}", Application.Filepath);

            foreach (PeExport Export in Exports)
            {
                Console.WriteLine("Export {0:d} :", Export.Ordinal);
                Console.WriteLine("\t Name : {0:s}", Export.Name);
                Console.WriteLine("\t VA : 0x{0:X}", (int)Export.VirtualAddress);
                if (Export.ForwardedName.Length > 0)
                    Console.WriteLine("\t ForwardedName : {0:s}", Export.ForwardedName);
            }

            Console.WriteLine("[-] Export listing done");
        }

        public List<PeExport> Exports;
        private PE Application;
    }


    class SxsDependencies : IPrettyPrintable
    {
        public SxsDependencies(PE _Application)
        {
            Application = _Application;
            SxS = SxsManifest.GetSxsEntries(Application);
        } 

        public void PrettyPrint()
        {
            Console.WriteLine("[-] sxs dependencies for executable : {0}", Application.Filepath);
            foreach (var entry in SxS)
            {
                if (entry.Path.Contains("???"))
                {
                    Console.WriteLine("  [x] {0:s} : {1:s}", entry.Name, entry.Path);
                }
                else
                {
                    Console.WriteLine("  [+] {0:s} : {1:s}", entry.Name, Path.GetFullPath(entry.Path));
                }
            }
        }

        public SxsEntries SxS;
        private PE Application;

    }



    class PeDependencyItem : IPrettyPrintable
    {

        public PeDependencyItem(PeDependencies _Root, string _ModuleName,  string ModuleFilepath, ModuleSearchStrategy Strategy, int Level)
        {
            Root = _Root;
            ModuleName = _ModuleName;

            if (ModuleFilepath != null)
            {
                PE Module = BinaryCache.LoadPe(ModuleFilepath);
                Imports = Module.GetImports();
            }
            else
            {
                //Module = null;
                Imports = new List<PeImportDll>();
            }
            
            Filepath = ModuleFilepath;
            SearchStrategy = Strategy;
            RecursionLevel = Level;

            DependenciesResolved = false;
        }

        public void ResolveDependencies()
        {
            if (DependenciesResolved)
            {
                return;
            }

            Dependencies = new List<PeDependencyItem>();

            foreach (PeImportDll DllImport in Imports)
            {
                string ModuleFilepath = null;
                ModuleSearchStrategy Strategy;
                

                // Find Dll in "paths"
                Tuple<ModuleSearchStrategy, PE> ResolvedModule =  Root.ResolveModule(DllImport.Name);
                Strategy = ResolvedModule.Item1;

                if (Strategy != ModuleSearchStrategy.NOT_FOUND)
                {
                    ModuleFilepath = ResolvedModule.Item2.Filepath;
                }


                Debug.WriteLine("[{0:d}] [{1:s}] Adding dep {2:s}", RecursionLevel,  ModuleName, ModuleFilepath);
                PeDependencyItem DependencyItem = Root.GetModuleItem(DllImport.Name, ModuleFilepath, Strategy, RecursionLevel + 1);
                Dependencies.Add(DependencyItem);
            }

            DependenciesResolved = true;


            // Recursively resolve dependencies
            foreach (var Dep in Dependencies)
            {
                Dep.ResolveDependencies();
            }
        }

        public void PrettyPrint()
        {
            string Tabs = string.Concat(Enumerable.Repeat("|  ", RecursionLevel));
            Console.WriteLine("{0:s}├ {1:s} ({2:s}) : {3:s} ", Tabs, ModuleName, SearchStrategy.ToString(), Filepath);

            foreach (var Dep in Dependencies)
            {
                if (Root.VisitModule(Dep.ModuleName, Dep.Filepath))
                {
                    Dep.PrettyPrint();
                }
            }
        }

        public string ModuleName;
        public string Filepath;
        public ModuleSearchStrategy SearchStrategy;
        public List<PeDependencyItem> Dependencies;

        protected List<PeImportDll> Imports;
        protected PeDependencies Root;
        protected int RecursionLevel;

        private bool DependenciesResolved;
    }


    class ModuleCacheKey : Tuple<string, string>
    {
        public ModuleCacheKey(string Name, string Filepath)
        : base(Name, Filepath)
        {
        }
    }

    class ModuleEntries : Dictionary<ModuleCacheKey, PeDependencyItem>, IPrettyPrintable
    {
        public void PrettyPrint()
        {
            foreach (var item in this.Values.OrderBy(module => module.SearchStrategy))
            {
                Console.WriteLine("[{0:s}] {1:s} : {2:s} ", item.SearchStrategy.ToString(), item.ModuleName, item.Filepath);
            }
            
        }
    }

    class PeDependencies : IPrettyPrintable
    {
        public PeDependencies(PE Application)
        {
            string RootFilename = Path.GetFileName(Application.Filepath);

            RootPe = Application;
            SxsEntriesCache = SxsManifest.GetSxsEntries(RootPe);
            ModulesCache = new ModuleEntries();

            Root = GetModuleItem(RootFilename, Application.Filepath, ModuleSearchStrategy.ROOT, 0);
            Root.ResolveDependencies();
        }

        public Tuple<ModuleSearchStrategy, PE> ResolveModule(string ModuleName)
        {
            return BinaryCache.ResolveModule(RootPe, ModuleName /*DllImport.Name*/, SxsEntriesCache);
        }

        public PeDependencyItem GetModuleItem(string ModuleName, string ModuleFilepath, ModuleSearchStrategy SearchStrategy, int RecursionLevel)
        {
            // Do not process twice the same item
            ModuleCacheKey ModuleKey = new ModuleCacheKey(ModuleName, ModuleFilepath);
            if (!ModulesCache.ContainsKey(ModuleKey))
            {
                ModulesCache[ModuleKey] = new PeDependencyItem(this, ModuleName, ModuleFilepath, SearchStrategy, RecursionLevel);
            }

            return ModulesCache[ModuleKey];
        }

        public void PrettyPrint()
        {
            ModulesVisited = new Dictionary<ModuleCacheKey, bool>();
            Root.PrettyPrint();
        }

        public bool VisitModule(string ModuleName, string ModuleFilepath)
        {
            ModuleCacheKey ModuleKey = new ModuleCacheKey(ModuleName, ModuleFilepath);

            // do not visit recursively the same node (in order to prevent stack overflow)
            if (ModulesVisited.ContainsKey(ModuleKey))
            {
                return false;
            }

            ModulesVisited[ModuleKey] = true;
            return true;
        }

        public ModuleEntries GetModules 
        {
            get {return ModulesCache;}
        }

        public PeDependencyItem Root;

        private PE RootPe;
        private SxsEntries SxsEntriesCache;
        private ModuleEntries ModulesCache;
        private Dictionary<ModuleCacheKey, bool> ModulesVisited;
    }

    class Program
    {
        public static void PrettyPrinter(IPrettyPrintable obj)
        {
            obj.PrettyPrint();
        }

        public static void JsonPrinter(IPrettyPrintable obj)
        {
            JsonSerializerSettings Settings = new JsonSerializerSettings
            {
                // ReferenceLoopHandling = ReferenceLoopHandling.Serialize
            };

            Console.WriteLine(JsonConvert.SerializeObject(obj, Formatting.Indented, Settings));
        }

        public static void DumpKnownDlls(Action<IPrettyPrintable> Printer)
        { 
            NtKnownDlls KnownDlls = new NtKnownDlls();
            Printer(KnownDlls);
        }

        public static void DumpApiSets(Action<IPrettyPrintable> Printer)
        {
            NtApiSet ApiSet = new NtApiSet();
            Printer(ApiSet);
        }

        public static void DumpManifest(PE Application, Action<IPrettyPrintable> Printer)
        {
            PEManifest Manifest = new PEManifest(Application);
            Printer(Manifest);
        }

        public static void DumpSxsEntries(PE Application, Action<IPrettyPrintable> Printer)
        {
            SxsDependencies SxsDeps = new SxsDependencies(Application);
            Printer(SxsDeps);
        }


        public static void DumpExports(PE Pe, Action<IPrettyPrintable> Printer)
        {
            PEExports Exports = new PEExports(Pe);
            Printer(Exports);
        }

        public static void DumpImports(PE Pe, Action<IPrettyPrintable> Printer)
        {
            PEImports Imports = new PEImports(Pe);
            Printer(Imports);
        }

        public static void DumpDependencies(PE Pe, Action<IPrettyPrintable> Printer)
        {
            if (Printer == JsonPrinter)
            {
                Console.Error.WriteLine("Json output is not currently supported when dumping the dependency chain.");
                return;
            }

            PeDependencies Deps = new PeDependencies(Pe);
            Printer(Deps);
        }

        public static void DumpModules(PE Pe, Action<IPrettyPrintable> Printer)
        {
            if (Printer == JsonPrinter)
            {
                Console.Error.WriteLine("Json output is not currently supported when dumping the dependency chain.");
                return;
            }

            PeDependencies Deps = new PeDependencies(Pe);
            Printer(Deps.GetModules);
        }

        public static void DumpUsage()
        {
            string Usage = String.Join(Environment.NewLine,
                "Dependencies.exe : command line tool for dumping dependencies and various utilities.",
                "",
                "Usage : Dependencies.exe [OPTIONS] FILE",
                "",
                "Options :",
                "  -h -help : display this help",
                "  -json : activate json output.",
                "  -apisets : dump the system's ApiSet schema (api set dll -> host dll)",
                "  -knowndll : dump all the system's known dlls (x86 and x64)",
                "  -manifest : dump FILE embedded manifest, if it exists.",
                "  -sxsentries : dump all of FILE's sxs dependencies.",
                "  -imports : dump FILE imports",
                "  -exports : dump  FILE exports",
                "  -dependencies : dump FILE whole dependency chain",
                "  -modules : dump FILE resolved modules"
            );

            Console.WriteLine(Usage);
        }


        static void Main(string[] args)
        {
            String FileName = null;
            var ProgramArgs = ParseArgs(args);
            Action<IPrettyPrintable> ObjectPrinter = PrettyPrinter;

            // always the first call to make
            Phlib.InitializePhLib();
            
            if (ProgramArgs.ContainsKey("file"))
            { 
                FileName = ProgramArgs["file"];
            }

            if (ProgramArgs.ContainsKey("-json"))
            {
                ObjectPrinter = JsonPrinter;
            }
                

            // no need to load PE for those commands
            if ((args.Length == 0) || ProgramArgs.ContainsKey("-h") || ProgramArgs.ContainsKey("-help"))
            {
                DumpUsage();
                return;
            }

            if (ProgramArgs.ContainsKey("-knowndll"))
            {
                DumpKnownDlls(ObjectPrinter);
                return;
            }

            if (ProgramArgs.ContainsKey("-apisets"))
            {
                DumpApiSets(ObjectPrinter);
                return;
            }

            //Console.WriteLine("[-] Loading file {0:s} ", FileName);
            PE Pe = new PE(FileName);
            if (!Pe.Load())
            {
                Console.Error.WriteLine("[x] Could not load file {0:s} as a PE", FileName);
                return;
            }

            
            if (ProgramArgs.ContainsKey("-manifest"))
                DumpManifest(Pe, ObjectPrinter);
            else if (ProgramArgs.ContainsKey("-sxsentries"))
                DumpSxsEntries(Pe, ObjectPrinter);
            else if (ProgramArgs.ContainsKey("-imports"))
                DumpImports(Pe, ObjectPrinter);
            else if (ProgramArgs.ContainsKey("-exports"))
                DumpExports(Pe, ObjectPrinter);
            else if (ProgramArgs.ContainsKey("-dependencies"))
                DumpDependencies(Pe, ObjectPrinter);
            else if (ProgramArgs.ContainsKey("-modules"))
                DumpModules(Pe, ObjectPrinter);


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
