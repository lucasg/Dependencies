using System;
using System.ClrPh;
using System.IO;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using System.Text.RegularExpressions;

namespace Dependencies
{
    public enum ModuleSearchStrategy
    {
        ROOT,

        SxS,
        WellKnownDlls,
        ApplicationDirectory,
        WorkingDirectory,
        System32Folder,
        WindowsFolder,
        AppInit,
        Environment,
        ApiSetSchema,

        NOT_FOUND
    };

    // C# typedefs
    #region Sxs Classes
    public class SxsEntry 
    {
        public SxsEntry(string _Name, string _Path, string _Version="", string _Type="", string _PublicKeyToken = "")
        { 

            Name = _Name;
            Path = _Path;
            Version = _Version;
            Type = _Type;
            PublicKeyToken = _PublicKeyToken;
        }

        public SxsEntry(SxsEntry OtherSxsEntry)
        {
            Name = OtherSxsEntry.Name;
            Path = OtherSxsEntry.Path;
            Version = OtherSxsEntry.Version;
            Type = OtherSxsEntry.Type;
            PublicKeyToken = OtherSxsEntry.PublicKeyToken;
        }

        public SxsEntry(XElement SxsAssemblyIdentity, XElement SxsFile, string Folder)
        {
            var RelPath = SxsFile.Attribute("name").Value.ToString();

            Name = System.IO.Path.GetFileName(RelPath);
            Path = System.IO.Path.Combine(Folder, RelPath);
            Version = "";
            Type = "";
            PublicKeyToken = "";

            if (SxsAssemblyIdentity != null)
            {
                if (SxsAssemblyIdentity.Attribute("version") != null)
                    Version = SxsAssemblyIdentity.Attribute("version").Value.ToString();

                if (SxsAssemblyIdentity.Attribute("type") != null)
                    Type = SxsAssemblyIdentity.Attribute("type").Value.ToString();

                if (SxsAssemblyIdentity.Attribute("publicKeyToken") != null)
                    PublicKeyToken = SxsAssemblyIdentity.Attribute("publicKeyToken").Value.ToString();
            }


            // TODO : DLL search order ?
            //if (!File.Exists(Path))
            //{
            //    Path = "???";
            //}

        }

        public string Name;
        public string Path;
        public string Version;
        public string Type;
        public string PublicKeyToken;
    }

    public class SxsEntries : List<SxsEntry> 
    {
        public static SxsEntries FromSxsAssembly(XElement SxsAssembly, XNamespace Namespace, string Folder)
        {
            SxsEntries Entries =  new SxsEntries();

            XElement SxsAssemblyIdentity = SxsAssembly.Element(Namespace + "assemblyIdentity");
            foreach (XElement SxsFile in SxsAssembly.Elements(Namespace + "file"))
            {
                Entries.Add(new SxsEntry(SxsAssemblyIdentity, SxsFile, Folder));
            }

            return Entries;
        }
    }
    #endregion Sxs Classes

    #region SxsManifest
    public class SxsManifest
    {
        // find dll with same name as sxs assembly in target directory
        public static SxsEntries SxsFindTargetDll(string AssemblyName, string Folder)
        {
            SxsEntries EntriesFromElement = new SxsEntries();

            string TargetFilePath = Path.Combine(Folder, AssemblyName);
            if (File.Exists(TargetFilePath))
            {
                var Name = System.IO.Path.GetFileName(TargetFilePath);
                var Path = TargetFilePath;

                EntriesFromElement.Add(new SxsEntry(Name, Path));
                return EntriesFromElement;
            }

            string TargetDllPath = Path.Combine(Folder, String.Format("{0:s}.dll", AssemblyName));
            if (File.Exists(TargetDllPath))
            {
                var Name = System.IO.Path.GetFileName(TargetDllPath);
                var Path = TargetDllPath;

                EntriesFromElement.Add(new SxsEntry(Name, Path));
                return EntriesFromElement;
            }

            return EntriesFromElement;
        }

        public static SxsEntries ExtractDependenciesFromSxsElement(XElement SxsAssembly, string Folder, string ExecutableName = "", bool Wow64Pe = false)
        {
            // Private assembly search sequence : https://msdn.microsoft.com/en-us/library/windows/desktop/aa374224(v=vs.85).aspx
            // 
            // * In the application's folder. Typically, this is the folder containing the application's executable file.
            // * In a subfolder in the application's folder. The subfolder must have the same name as the assembly.
            // * In a language-specific subfolder in the application's folder. 
            //      -> The name of the subfolder is a string of DHTML language codes indicating a language-culture or language.
            // * In a subfolder of a language-specific subfolder in the application's folder.
            //      -> The name of the higher subfolder is a string of DHTML language codes indicating a language-culture or language. The deeper subfolder has the same name as the assembly.
            //
            // 
            // 0.   Side-by-side searches the WinSxS folder.
            // 1.   \\<appdir>\<assemblyname>.DLL
            // 2.   \\<appdir>\<assemblyname>.manifest
            // 3.   \\<appdir>\<assemblyname>\<assemblyname>.DLL
            // 4.   \\<appdir>\<assemblyname>\<assemblyname>.manifest

            string TargetSxsManifestPath;
            string SxsManifestName = SxsAssembly.Attribute("name").Value.ToString();
            string SxsManifestDir = Path.Combine(Folder, SxsManifestName);


            // 0. find publisher manifest in %WINDIR%/WinSxs/Manifest
            if (SxsAssembly.Attribute("publicKeyToken") != null)
            {

                string WinSxsDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                    "WinSxs"
                );

                string WinSxsManifestDir = Path.Combine(WinSxsDir, "Manifests");
                var RegisteredManifests = Directory.EnumerateFiles(
                    WinSxsManifestDir,
                    "*.manifest"
                );

                string PublicKeyToken = SxsAssembly.Attribute("publicKeyToken").Value;
                string Name = SxsAssembly.Attribute("name").Value.ToLower();
                string ProcessArch = SxsAssembly.Attribute("processorArchitecture") != null ? SxsAssembly.Attribute("processorArchitecture").Value : "*";
                string Version = SxsAssembly.Attribute("version").Value;
                string Langage = SxsAssembly.Attribute("langage") != null ? SxsAssembly.Attribute("langage").Value : "none"; // TODO : support localized sxs redirection


                switch (ProcessArch.ToLower())
                {
                    case "$(build.arch)":
                    case "*":
                        ProcessArch = (Wow64Pe) ? "x86" : "amd64";
                        break;
                    case "amd64":
                    case "x86":
                    case "wow64":
                    case "msil":
                        break; // nothing to do
                    default:
                        ProcessArch = ".*";
                        break;
                }

                Regex MajorVersionRegex = new Regex(@"([0-9]+)\.(.*)", RegexOptions.IgnoreCase);
                Match MajorVersionMatch = MajorVersionRegex.Match(Version);
                string MajorVersion = (MajorVersionMatch.Success) ? MajorVersionMatch.Groups[1].Value.ToString() : ".*";

                // Manifest filename : {ProcArch}_{Name}_{PublicKeyToken}_{FuzzyVersion}_{Langage}_{some_hash}.manifest
                Regex ManifestFileNameRegex = new Regex(
                    String.Format(@"({0:s}_{1:s}_{2:s}_({3:s}\.[\.0-9]*)_none_([a-fA-F0-9]+))\.manifest",
                        ProcessArch,
                        Name,
                        PublicKeyToken,
                        MajorVersion
                    //Langage,
                    // some hash
                    ),
                    RegexOptions.IgnoreCase
                );

                foreach (var FileName in RegisteredManifests)
                {
                    Match MatchingSxsFile = ManifestFileNameRegex.Match(FileName);
                    if (MatchingSxsFile.Success)
                    {

                        TargetSxsManifestPath = Path.Combine(WinSxsManifestDir, FileName);
                        SxsManifestDir = Path.Combine(WinSxsDir, MatchingSxsFile.Groups[1].Value);

                        // "{name}.local" local sxs directory hijack ( really used for UAC bypasses )
                        if (ExecutableName != "")
                        {
                            string LocalSxsDir = Path.Combine(Folder, String.Format("{0:s}.local", ExecutableName));
                            string MatchingLocalSxsDir = Path.Combine(LocalSxsDir, MatchingSxsFile.Groups[1].Value);

                            if (Directory.Exists(LocalSxsDir) && Directory.Exists(MatchingLocalSxsDir))
                            {
                                SxsManifestDir = MatchingLocalSxsDir;
                            }
                        }

                        return ExtractDependenciesFromSxsManifestFile(TargetSxsManifestPath, SxsManifestDir, ExecutableName, Wow64Pe);
                    }
                }
            }

            // 1. \\<appdir>\<assemblyname>.DLL
            // find dll with same assembly name in same directory
            SxsEntries EntriesFromMatchingDll = SxsFindTargetDll(SxsManifestName, Folder);
            if (EntriesFromMatchingDll.Count > 0)
            {
                return EntriesFromMatchingDll;
            }


            // 2. \\<appdir>\<assemblyname>.manifest
            // find manifest with same assembly name in same directory
            TargetSxsManifestPath = Path.Combine(Folder, String.Format("{0:s}.manifest", SxsManifestName));
            if (File.Exists(TargetSxsManifestPath))
            {
                return ExtractDependenciesFromSxsManifestFile(TargetSxsManifestPath, Folder, ExecutableName, Wow64Pe);
            }


            // 3. \\<appdir>\<assemblyname>\<assemblyname>.DLL
            // find matching dll in sub directory
            SxsEntries EntriesFromMatchingDllSub = SxsFindTargetDll(SxsManifestName, SxsManifestDir);
            if (EntriesFromMatchingDllSub.Count > 0)
            {
                return EntriesFromMatchingDllSub;
            }

            // 4. \<appdir>\<assemblyname>\<assemblyname>.manifest
            // find manifest in sub directory
            TargetSxsManifestPath = Path.Combine(SxsManifestDir, String.Format("{0:s}.manifest", SxsManifestName));
            if (Directory.Exists(SxsManifestDir) && File.Exists(TargetSxsManifestPath))
            {
                return ExtractDependenciesFromSxsManifestFile(TargetSxsManifestPath, SxsManifestDir, ExecutableName, Wow64Pe);
            }

            // TODO : do the same thing for localization
            // 
            // 0. Side-by-side searches the WinSxS folder.
            // 1. \\<appdir>\<language-culture>\<assemblyname>.DLL
            // 2. \\<appdir>\<language-culture>\<assemblyname>.manifest
            // 3. \\<appdir>\<language-culture>\<assemblyname>\<assemblyname>.DLL
            // 4. \\<appdir>\<language-culture>\<assemblyname>\<assemblyname>.manifest

            // TODO : also take into account Multilanguage User Interface (MUI) when
            // scanning manifests and WinSxs dll. God this is horrendously complicated.

            // Could not find the dependency
            {
                SxsEntries EntriesFromElement = new SxsEntries();
                EntriesFromElement.Add(new SxsEntry(SxsManifestName, "file ???"));
                return EntriesFromElement;
            }
        }

        public static SxsEntries ExtractDependenciesFromSxsManifestFile(string ManifestFile, string Folder, string ExecutableName = "", bool Wow64Pe = false)
        {
            // Console.WriteLine("Extracting deps from file {0:s}", ManifestFile);

            using (FileStream fs = new FileStream(ManifestFile, FileMode.Open, FileAccess.Read))
            {
                return ExtractDependenciesFromSxsManifest(fs, Folder, ExecutableName, Wow64Pe);
            }
        }


        public static SxsEntries ExtractDependenciesFromSxsManifest(System.IO.Stream ManifestStream, string Folder, string ExecutableName = "", bool Wow64Pe = false)
        {
            SxsEntries AdditionnalDependencies = new SxsEntries();

            XDocument XmlManifest = ParseSxsManifest(ManifestStream);
            XNamespace Namespace = XmlManifest.Root.GetDefaultNamespace();

            // Find any declared dll
            //< assembly xmlns = 'urn:schemas-microsoft-com:asm.v1' manifestVersion = '1.0' >
            //    < assemblyIdentity name = 'additional_dll' version = 'XXX.YY.ZZ' type = 'win32' />
            //    < file name = 'additional_dll.dll' />
            //</ assembly >
            foreach (XElement SxsAssembly in XmlManifest.Descendants(Namespace + "assembly"))
            {
                AdditionnalDependencies.AddRange(SxsEntries.FromSxsAssembly(SxsAssembly, Namespace, Folder));
            }



            // Find any dependencies :
            // <dependency>
            //     <dependentAssembly>
            //         <assemblyIdentity
            //             type="win32"
            //             name="Microsoft.Windows.Common-Controls"
            //             version="6.0.0.0"
            //             processorArchitecture="amd64" 
            //             publicKeyToken="6595b64144ccf1df"
            //             language="*"
            //         />
            //     </dependentAssembly>
            // </dependency>
            foreach (XElement SxsAssembly in XmlManifest.Descendants(Namespace + "dependency")
                                                        .Elements(Namespace + "dependentAssembly")
                                                        .Elements(Namespace + "assemblyIdentity")
            )
            {
                // find target PE
                AdditionnalDependencies.AddRange(ExtractDependenciesFromSxsElement(SxsAssembly, Folder, ExecutableName, Wow64Pe));
            }

            return AdditionnalDependencies;
        }

        public static XDocument ParseSxsManifest(System.IO.Stream ManifestStream)
        {
            XDocument XmlManifest = null;
            // Hardcode namespaces for manifests since they are no always specified in the embedded manifests.
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(new NameTable());
            nsmgr.AddNamespace(String.Empty, "urn:schemas-microsoft-com:asm.v1"); //default namespace : manifest V1
            nsmgr.AddNamespace("asmv3", "urn:schemas-microsoft-com:asm.v3");      // sometimes missing from manifests : V3
            nsmgr.AddNamespace("asmv3", "http://schemas.microsoft.com/SMI/2005/WindowsSettings");      // sometimes missing from manifests : V3
            XmlParserContext context = new XmlParserContext(null, nsmgr, null, XmlSpace.Preserve);




            using (StreamReader xStream = new StreamReader(ManifestStream))
            {
                // Trim double quotes in manifest attributes
                // Example :
                //      * Before : <assemblyIdentity name=""Microsoft.Windows.Shell.DevicePairingFolder"" processorArchitecture=""amd64"" version=""5.1.0.0"" type="win32" />
                //      * After  : <assemblyIdentity name="Microsoft.Windows.Shell.DevicePairingFolder" processorArchitecture="amd64" version="5.1.0.0" type="win32" />

                string PeManifest = xStream.ReadToEnd();
                PeManifest = new Regex("\\\"\\\"([\\w\\d\\.]*)\\\"\\\"").Replace(PeManifest, "\"$1\""); // Regex magic here

                using (XmlTextReader xReader = new XmlTextReader(PeManifest, XmlNodeType.Document, context))
                {
                    XmlManifest = XDocument.Load(xReader);
                }
            }

            return XmlManifest;
        }


        public static SxsEntries GetSxsEntries(PE Pe)
        {
            SxsEntries Entries = new SxsEntries();

            string RootPeFolder = Path.GetDirectoryName(Pe.Filepath);
            string RootPeFilename = Path.GetFileName(Pe.Filepath);

            // Look for overriding manifest file (named "{$name}.manifest)
            string OverridingManifest = String.Format("{0:s}.manifest", Pe.Filepath);
            if (File.Exists(OverridingManifest))
            {
                return ExtractDependenciesFromSxsManifestFile(
                    OverridingManifest,
                    RootPeFolder,
                    RootPeFilename,
                    Pe.IsWow64Dll()
                );
            }

            // Retrieve embedded manifest
            string PeManifest = Pe.GetManifest();
            if (PeManifest.Length == 0)
                return Entries;


            byte[] RawManifest = System.Text.Encoding.UTF8.GetBytes(PeManifest);
            System.IO.Stream ManifestStream = new System.IO.MemoryStream(RawManifest);

            Entries = ExtractDependenciesFromSxsManifest(
                ManifestStream,
                RootPeFolder,
                RootPeFilename,
                Pe.IsWow64Dll()
            );
            return Entries;
        }
    }
    #endregion SxsManifest

    #region FindPe
    public class FindPe
    {
        static string FindPeFromPath(string ModuleName, List<string> CandidateFolders, bool Wow64Dll = false)
        {
            string PeFilePath = null;

            foreach (String CandidatePath in CandidateFolders)
            {
                PeFilePath = Path.Combine(CandidatePath, ModuleName);
                PE TestPe = BinaryCache.LoadPe(PeFilePath);

                if ((TestPe != null) && (TestPe.LoadSuccessful) && (TestPe.IsWow64Dll() == Wow64Dll))
                    return PeFilePath;
            }

            return null;
        }

        // default search order : 
        // https://msdn.microsoft.com/en-us/library/windows/desktop/ms682586(v=vs.85).aspx
        // 
        // if (SafeDllSearchMode) {
        //      -1. Sxs manifests
        //      0. KnownDlls list
        //      1. Loaded PE folder
        //      2. C:\Windows\(System32 | SysWow64 )
        //      3. 16-bit system directory   <-- ignored
        //      4. C:\Windows
        //      5. %pwd%
        //      6. AppDatas
        //      }
        public static Tuple<ModuleSearchStrategy, string> FindPeFromDefault(PE RootPe, string ModuleName, SxsEntries SxsCache)
        {
            bool Wow64Dll = RootPe.IsWow64Dll();
            string RootPeFolder = Path.GetDirectoryName(RootPe.Filepath);
            string FoundPePath = null;
            
            Environment.SpecialFolder WindowsSystemFolder = (Wow64Dll) ?
                Environment.SpecialFolder.SystemX86 :
                Environment.SpecialFolder.System;
            String WindowsSystemFolderPath = Environment.GetFolderPath(WindowsSystemFolder);


            // -1. Look in Sxs manifest (copious reversing needed)
            // TODO : find dll search order
            if (SxsCache.Count != 0)
            {
                SxsEntry Entry = SxsCache.Find( SxsItem =>
                    string.Equals(SxsItem.Name, ModuleName, StringComparison.OrdinalIgnoreCase)
                );

                if (Entry != null)
                {
                    return new Tuple<ModuleSearchStrategy, string>(ModuleSearchStrategy.SxS, Entry.Path);
                }
            }


            // 0. Look in well-known dlls list
            // HKEY_LOCAL_MACHINE\System\CurrentControlSet\Control\Session Manager\KnownDLLs
            // https://blogs.msdn.microsoft.com/larryosterman/2004/07/19/what-are-known-dlls-anyway/
            String KnownDll = Phlib.GetKnownDlls(Wow64Dll).Find(x => string.Equals(x, ModuleName, StringComparison.OrdinalIgnoreCase));
            if (KnownDll != null)
            {
                return new Tuple<ModuleSearchStrategy, string>(
                    ModuleSearchStrategy.WellKnownDlls, 
                    Path.Combine(WindowsSystemFolderPath, KnownDll)
                );
            }


            // 1. Look in application folder
            FoundPePath = FindPeFromPath(ModuleName, new List<string>(new string[] { RootPeFolder }), Wow64Dll);
            if (FoundPePath != null)
            {
                return new Tuple<ModuleSearchStrategy, string>(
                    ModuleSearchStrategy.ApplicationDirectory,
                   FoundPePath
                );
            }

            // {2-3-4}. Look in system folders
            List<String> SystemFolders = new List<string>(new string[] {
                WindowsSystemFolderPath,
                Environment.GetFolderPath(Environment.SpecialFolder.Windows)
                }
            );

            FoundPePath = FindPeFromPath(ModuleName, SystemFolders, Wow64Dll);
            if (FoundPePath != null)
            {
                return new Tuple<ModuleSearchStrategy, string>(
                    ModuleSearchStrategy.WindowsFolder,
                   FoundPePath
                );
            }

            // 5. Look in current directory
            // Ignored for the time being since we can't know from
            // where the exe is run
            // TODO : Add a user supplied path emulating %cwd%


            // 6. Look in local app data (check for python for exemple)



            // 7. Find in PATH
            string PATH = Environment.GetEnvironmentVariable("PATH");
            List<String> PATHFolders = new List<string>(PATH.Split(';'));
            FoundPePath = FindPeFromPath(ModuleName, PATHFolders, Wow64Dll);
            if (FoundPePath != null)
            {
                return new Tuple<ModuleSearchStrategy, string>(
                    ModuleSearchStrategy.Environment,
                   FoundPePath
                );
            }


            return new Tuple<ModuleSearchStrategy, string>(
                ModuleSearchStrategy.NOT_FOUND,
                null
            );
        }
    }
    #endregion FindPe
}
