using System;
using System.ClrPh;
using System.IO;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Dependencies
{
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


        static List<Tuple<string, string>> ExtractDependenciesFromSxsManifest(System.IO.Stream ManifestStream, string Folder)
        {
            List<Tuple<string, string>> AdditionnalDependencies = new List<Tuple<string, string>>();

            // Use a memory stream to correctly handle BOM encoding for manifest resource
            using (var stream = ManifestStream ) // new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(PeManifest)))
            {
                XDocument XmlManifest = XDocument.Load(stream);
                XNamespace Namespace = XmlManifest.Root.GetDefaultNamespace();
                Console.WriteLine(XmlManifest);

                // Extracting assemblyIdentity and file names
                String DependencyNodeName = String.Format("{{{0}}}dependency", Namespace);
                String AssemblyIdentityNodeName = String.Format("{{{0}}}assemblyIdentity", Namespace);
                String FileNodeName = String.Format("{{{0}}}file", Namespace);

                foreach (XElement SxsDependency in XmlManifest.Descendants(DependencyNodeName))
                {
                    foreach (XElement SxsFileEntry in SxsDependency.Descendants(FileNodeName))
                    {
                        string SxsDllName = SxsFileEntry.Name.ToString();
                        string SxsDllPath = Path.Combine(Folder, SxsDllName);
                        AdditionnalDependencies.Add(new Tuple<string, string>(SxsDllName, SxsDllPath));
                    }

                    foreach (XElement SxsAssembly in SxsDependency.Descendants(AssemblyIdentityNodeName))
                    {
                        // find publisher manifest in current dir and %WINDIR%/WinSxs/Manifest
                    }
                }
            }

            return AdditionnalDependencies;
        }

        static List<Tuple<string, string>> GetAdditionnalDependencies(PE Pe)
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
        public static string FindPeFromDefault(PE RootPe, string ModuleName)
        {
            bool Wow64Dll = RootPe.IsWow64Dll();
            string RootPeFolder = Path.GetDirectoryName(RootPe.Filepath);
            string FoundPePath = null;
            
            Environment.SpecialFolder WindowsSystemFolder = (Wow64Dll) ?
                Environment.SpecialFolder.SystemX86 :
                Environment.SpecialFolder.System;
            String WindowsSystemFolderPath = Environment.GetFolderPath(WindowsSystemFolder);

            // Load additionnal dll from sxs entries in root pe.
            List<Tuple<string, string>> SxsEntries = GetAdditionnalDependencies(RootPe);

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
            Tuple<string, string> SxsEntry = SxsEntries.Find(t => t.Item1 == ModuleName);
            if (SxsEntry != null)
            {
                return SxsEntry.Item2;
            }

            return null;
        }
    }
	
}
