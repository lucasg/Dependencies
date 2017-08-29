using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using System.IO;
using System.ClrPh;


namespace ClrPhTester
{
    // C# typedefs
    using SxsEntry = Tuple<string, string>;
    public class SxsEntries : List<SxsEntry> { }

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
                SxsEntries EntriesFromElement =  new SxsEntries();
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
                            string SxsManifestName = SxsAssembly.Attribute("name").Value.ToString();
                            AdditionnalDependencies.Add(new SxsEntry(SxsManifestName, "publisher ???"));
                        }
                        else
                        {
                            // find target PE
                            foreach(var SxsTarget in ExtractDependenciesFromSxsElement(SxsAssembly, Folder))
                            {
                                AdditionnalDependencies.Add(SxsTarget);
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
}