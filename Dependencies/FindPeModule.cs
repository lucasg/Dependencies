using System;
using System.ClrPh;
using System.IO;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;

namespace Dependencies
{
    // C# typedefs
    using SxsEntry = Tuple<string, string>;
    public class SxsEntries : List<SxsEntry> { }

    #region SxsManifest 
    public class SxsManifest
    {
        public static SxsEntries ExtractDependenciesFromSxsElement(XElement SxsAssembly, string Folder)
        {
            // TODO : find search order 
            string SxsManifestName = SxsAssembly.Attribute("name").Value.ToString();

            // find dll with same name in same directory
            string TargetDllPath = Path.Combine(Folder, String.Format("{0:s}.dll", SxsManifestName));
            if (File.Exists(TargetDllPath))
            {
                SxsEntries EntriesFromElement = new SxsEntries();
                EntriesFromElement.Add(new SxsEntry(SxsManifestName, TargetDllPath));
                return EntriesFromElement;
            }

            // find manifest with same name in same directory
            string TargetSxsManifestPath = Path.Combine(Folder, String.Format("{0:s}.manifest", SxsManifestName));
            if (File.Exists(TargetSxsManifestPath))
            {
                return ExtractDependenciesFromSxsManifestFile(TargetSxsManifestPath, Folder);
            }

            // find manifest in sub directory
            string SxsManifestDir = Path.Combine(Folder, SxsManifestName);
            TargetSxsManifestPath = Path.Combine(SxsManifestDir, String.Format("{0:s}.manifest", SxsManifestName));
            if (Directory.Exists(SxsManifestDir) && File.Exists(TargetSxsManifestPath))
            {
                return ExtractDependenciesFromSxsManifestFile(TargetSxsManifestPath, SxsManifestDir);
            }

            // find "{name}.local" dir ?

            // Could not find the dependency
            {
                SxsEntries EntriesFromElement = new SxsEntries();
                EntriesFromElement.Add(new SxsEntry(SxsManifestName, "file ???"));
                return EntriesFromElement;
            }
        }

        public static SxsEntries ExtractDependenciesFromSxsManifestFile(string ManifestFile, string Folder)
        {
            using (FileStream fs = new FileStream(ManifestFile, FileMode.Open, FileAccess.Read))
            {
                return ExtractDependenciesFromSxsManifest(fs, Folder);
            }
        }


        public static SxsEntries ExtractDependenciesFromSxsManifest(System.IO.Stream ManifestStream, string Folder)
        {
            SxsEntries AdditionnalDependencies = new SxsEntries();


            // Hardcode namespaces for manifests since they are no always specified in the embedded manifests.
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(new NameTable());
            nsmgr.AddNamespace(String.Empty, "urn:schemas-microsoft-com:asm.v1"); //default namespace : manifest V1
            nsmgr.AddNamespace("asmv3", "urn:schemas-microsoft-com:asm.v3");      // sometimes missing from manifests : V3
            XmlParserContext context = new XmlParserContext(null, nsmgr, null, XmlSpace.Preserve);


            using (XmlTextReader xReader = new XmlTextReader(ManifestStream, XmlNodeType.Document, context))
            {
                XDocument XmlManifest = XDocument.Load(xReader);
                XNamespace Namespace = XmlManifest.Root.GetDefaultNamespace();

                // Find any declared dll
                //< assembly xmlns = 'urn:schemas-microsoft-com:asm.v1' manifestVersion = '1.0' >
                //    < assemblyIdentity name = 'additional_dll' version = 'XXX.YY.ZZ' type = 'win32' />
                //    < file name = 'additional_dll.dll' />
                //</ assembly >
                foreach (XElement SxsAssembly in XmlManifest.Descendants(Namespace + "assembly"))
                {
                    foreach (XElement SxsFileEntry in SxsAssembly.Elements(Namespace + "file"))
                    {
                        string SxsDllName = SxsFileEntry.Attribute("name").Value.ToString();
                        string SxsDllPath = Path.Combine(Folder, SxsDllName);
                        AdditionnalDependencies.Add(new SxsEntry(SxsDllName, SxsDllPath));
                    }
                }

                // Find any dependencies
                foreach (XElement SxsDependency in XmlManifest.Descendants(Namespace + "dependency"))
                {
                    foreach (XElement SxsDependentAssembly in SxsDependency.Elements(Namespace + "dependentAssembly"))
                    {
                        foreach (XElement SxsAssembly in SxsDependentAssembly.Elements(Namespace + "assemblyIdentity"))
                        {
                            if (SxsAssembly.Attribute("publicKeyToken") != null)
                            {
                                // find publisher manifest in %WINDIR%/WinSxs/Manifest
                                string SxsManifestName = SxsAssembly.Attribute("name").Value.ToString();
                                AdditionnalDependencies.Add(new SxsEntry(SxsManifestName, "publisher ???"));
                            }
                            else
                            {
                                // find target PE
                                foreach (var SxsTarget in ExtractDependenciesFromSxsElement(SxsAssembly, Folder))
                                {
                                    AdditionnalDependencies.Add(SxsTarget);
                                }
                            }
                        }
                    }
                }
            }

            return AdditionnalDependencies;
        }

        public static SxsEntries GetSxsEntries(PE Pe)
        {
            SxsEntries Entries = new SxsEntries();

            string RootPeFolder = Path.GetDirectoryName(Pe.Filepath);

            // Look for overriding manifest file (named "{$name}.manifest)
            string OverridingManifest = String.Format("{0:s}.manifest", Pe.Filepath);
            if (File.Exists(OverridingManifest))
            {
                return ExtractDependenciesFromSxsManifestFile(
                    OverridingManifest,
                    RootPeFolder
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
                RootPeFolder
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
                PE TestPe = new PE(PeFilePath);

                if ((TestPe.LoadSuccessful) && (TestPe.IsWow64Dll() == Wow64Dll))
                    return PeFilePath;
            }

            return null;
        }

        // default search order : 
        // https://msdn.microsoft.com/en-us/library/windows/desktop/ms682586(v=vs.85).aspx
        // 
        // if (SafeDllSearchMode) {
        //      0. KnownDlls list
        //      1. Loaded PE folder
        //      2. C:\Windows\(System32 | SysWow64 )
        //      3. 16-bit system directory   <-- ignored
        //      4. C:\Windows
        //      5. %pwd%
        //      6. AppDatas
        //      7. Sxs manifests
        public static string FindPeFromDefault(PE RootPe, string ModuleName, SxsEntries SxsCache )
        {
            bool Wow64Dll = RootPe.IsWow64Dll();
            string RootPeFolder = Path.GetDirectoryName(RootPe.Filepath);
            string FoundPePath = null;
            
            Environment.SpecialFolder WindowsSystemFolder = (Wow64Dll) ?
                Environment.SpecialFolder.SystemX86 :
                Environment.SpecialFolder.System;
            String WindowsSystemFolderPath = Environment.GetFolderPath(WindowsSystemFolder);


            // 0. Look in well-known dlls list
            // HKEY_LOCAL_MACHINE\System\CurrentControlSet\Control\Session Manager\KnownDLLs
            // https://blogs.msdn.microsoft.com/larryosterman/2004/07/19/what-are-known-dlls-anyway/
            String KnownDll = Phlib.GetKnownDlls(Wow64Dll).Find(x => x == ModuleName);
            if (KnownDll != null)
            {
                return Path.Combine(WindowsSystemFolderPath, KnownDll);
            }

            // 1. Look in application folder
            FoundPePath = FindPeFromPath(ModuleName, new List<string>(new string[] { RootPeFolder }), Wow64Dll);
            if (FoundPePath != null)
            {
                return FoundPePath;
            }

            // {2-3-4}. Look in system folders
            List <String> SystemFolders = new List<string>(new string[] {
                WindowsSystemFolderPath,
                Environment.GetFolderPath(Environment.SpecialFolder.Windows)
                }
            );

            FoundPePath = FindPeFromPath(ModuleName, SystemFolders, Wow64Dll);
            if (FoundPePath != null)
            {
                return FoundPePath;
            }

            // 5. Look in %pwd%

            // 6. Look in local app data (check for python for exemple)

            // 7. Look in Sxs manifest (copious reversing needed)
            if (SxsCache.Count != 0)
            {
                SxsEntry Entry = SxsCache.Find(t => t.Item1 == ModuleName);
                if (Entry != null)
                {
                    return Entry.Item2;
                }
            }
            

            // 8. Find in PATH

            return null;
        }
    }
    #endregion FindPe
}
