using System;
using System.ClrPh;
using System.IO;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Dependencies
{
    // C# typedefs
    using SxsEntry = Tuple<string, string>;
    public class SxsEntries : List<SxsEntry> { }

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


        static SxsEntries ExtractDependenciesFromSxsManifest(System.IO.Stream ManifestStream, string Folder)
        {
            SxsEntries AdditionnalDependencies = new SxsEntries();

            // Use a memory stream to correctly handle BOM encoding for manifest resource
            using (var stream = ManifestStream ) // new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(PeManifest)))
            {
                XDocument XmlManifest = XDocument.Load(stream);
                XNamespace Namespace = XmlManifest.Root.GetDefaultNamespace();

                // Extracting assemblyIdentity and file names
                String DependencyNodeName = String.Format("{{{0}}}dependency", Namespace);
                String AssemblyIdentityNodeName = String.Format("{{{0}}}assemblyIdentity", Namespace);
                String AssemblyNodeName = String.Format("{{{0}}}assembly", Namespace);
                String FileNodeName = String.Format("{{{0}}}file", Namespace);


                // Find any declared dll
                //< assembly xmlns = 'urn:schemas-microsoft-com:asm.v1' manifestVersion = '1.0' >
                //    < assemblyIdentity name = 'additional_dll' version = 'XXX.YY.ZZ' type = 'win32' />
                //    < file name = 'additional_dll.dll' />
                //</ assembly >
                foreach (XElement SxsAssembly in XmlManifest.Descendants(AssemblyNodeName))
                {
                    foreach (XElement SxsFileEntry in SxsAssembly.Descendants(FileNodeName))
                    {
                        string SxsDllName = SxsFileEntry.Attribute("name").Value.ToString();
                        string SxsDllPath = Path.Combine(Folder, SxsDllName);
                        AdditionnalDependencies.Add(new SxsEntry(SxsDllName, SxsDllPath));
                    }
                }

                // Find any dependencies
                foreach (XElement SxsDependency in XmlManifest.Descendants(DependencyNodeName))
                {
                    foreach (XElement SxsAssembly in SxsDependency.Descendants(AssemblyIdentityNodeName))
                    {
                        if (SxsAssembly.Attribute("publicKeyToken") != null)
                        {
                            // find publisher manifest in %WINDIR%/WinSxs/Manifest
                        }
                        else
                        {
                            // find manifest in current dir and %WINDIR%/WinSxs/Manifest
                            string SxsManifestName = SxsAssembly.Attribute("name").Value.ToString();
                            string SxsManifestDir = Path.Combine(Folder, SxsManifestName);
                            string TargetSxsManifestpath = Path.Combine(SxsManifestDir, String.Format("{0:s}.manifest", SxsManifestName));
                            if (Directory.Exists(SxsManifestDir) && File.Exists(TargetSxsManifestpath))
                            {
                                using (FileStream fs = new FileStream(TargetSxsManifestpath, FileMode.Open, FileAccess.Read))
                                {
                                    foreach (SxsEntry Entry in ExtractDependenciesFromSxsManifest(fs, SxsManifestDir))
                                    {
                                        AdditionnalDependencies.Add(Entry);
                                    }
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
            string RootPeFolder = Path.GetDirectoryName(Pe.Filepath);
            string PeManifest = Pe.GetManifest();
            if (PeManifest.Length == 0)
            {
                return null;
            }

            return ExtractDependenciesFromSxsManifest(new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(PeManifest)), RootPeFolder);
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
	
}
