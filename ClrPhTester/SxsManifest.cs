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

            // find "{name}.local" dir ?

            // find publisher manifest in %WINDIR%/WinSxs/Manifest
            if (SxsAssembly.Attribute("publicKeyToken") != null)
            {
                SxsEntries EntriesFromElement = new SxsEntries();
                EntriesFromElement.Add(new SxsEntry(SxsManifestName, String.Format("publisher {0:s} ???", SxsAssembly.Attribute("publicKeyToken")) ));
                return EntriesFromElement;
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
            
            XDocument XmlManifest = ParseSxsManifest(ManifestStream);
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
                foreach (var SxsTarget in ExtractDependenciesFromSxsElement(SxsAssembly, Folder))
                {
                    AdditionnalDependencies.Add(SxsTarget);
                }
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

            using (XmlTextReader xReader = new XmlTextReader(ManifestStream, XmlNodeType.Document, context))
            {
                XmlManifest = XDocument.Load(xReader);
            }

            return XmlManifest;
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
}