using System;
using System.Linq;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using System.IO;
using System.Text.RegularExpressions;

using Dependencies.ClrPh;

namespace Dependencies;

// C# typedefs
#region Sxs Classes
public class SxsEntry
{
    public SxsEntry(string _Name, string _Path, string _Version = "", string _Type = "", string _PublicKeyToken = "")
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
    public bool HasError = false;
    public static SxsEntries FromSxsAssembly(XElement SxsAssembly, XNamespace Namespace, string Folder)
    {
        SxsEntries Entries = new SxsEntries();

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

    public static SxsEntries ExtractDependenciesFromSxsElement(XElement SxsAssembly, string Folder, string ExecutableName = "", string ProcessorArch = "")
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
            string PublicKeyToken = SxsAssembly.Attribute("publicKeyToken").Value;
            string Name = SxsAssembly.Attribute("name").Value.ToLower();
            string ProcessArch = SxsAssembly.Attribute("processorArchitecture") != null ? SxsAssembly.Attribute("processorArchitecture").Value : "*";
            string Version = SxsAssembly.Attribute("version").Value;
            string Langage = SxsAssembly.Attribute("langage") != null ? SxsAssembly.Attribute("langage").Value : "neutral"; // TODO : support localized sxs redirection

            switch (ProcessArch.ToLower())
            {
                case "$(build.arch)":
                case "*":
                    ProcessArch = ProcessorArch;
                    break;
                case "amd64":
                case "x86":
                case "wow64":
                case "msil":
                case "arm":
                case "arm64":
                    break; // nothing to do
                default:
                    ProcessArch = ".*";
                    break;
            }

            if (System.Version.TryParse(Version, out var sysVer))
            {
                var foundPathes = SxsSearcher.GetMatchPathes(true, true, ProcessArch, Name, PublicKeyToken, sysVer.Major, sysVer.Minor, Langage);

                if (foundPathes.Count > 0)
                {
                    var fullManifestFile = foundPathes[0];
                    var sxsName = Path.GetFileNameWithoutExtension(fullManifestFile);

                    string sxsContentDir = Path.Combine(SxsSearcher.WinSxsDir, sxsName);

                    // "{name}.local" local sxs directory hijack ( really used for UAC bypasses )
                    if (ExecutableName != "")
                    {
                        string LocalSxsDir = Path.Combine(Folder, String.Format("{0:s}.local", ExecutableName));
                        string MatchingLocalSxsDir = Path.Combine(LocalSxsDir, sxsName);

                        if (Directory.Exists(LocalSxsDir) && Directory.Exists(MatchingLocalSxsDir))
                        {
                            sxsContentDir = MatchingLocalSxsDir;
                        }
                    }

                    return ExtractDependenciesFromSxsManifestFile(fullManifestFile, sxsContentDir, ExecutableName, ProcessorArch);
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
            return ExtractDependenciesFromSxsManifestFile(TargetSxsManifestPath, Folder, ExecutableName, ProcessorArch);
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
            return ExtractDependenciesFromSxsManifestFile(TargetSxsManifestPath, SxsManifestDir, ExecutableName, ProcessorArch);
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

    public static SxsEntries ExtractDependenciesFromSxsManifestFile(string ManifestFile, string Folder, string ExecutableName = "", string ProcessorArch = "")
    {
        // Console.WriteLine("Extracting deps from file {0:s}", ManifestFile);

        using (FileStream fs = new FileStream(ManifestFile, FileMode.Open, FileAccess.Read))
        {
            return ExtractDependenciesFromSxsManifest(fs, Folder, ExecutableName, ProcessorArch);
        }
    }

    public static SxsEntries ExtractDependenciesFromSxsManifest(System.IO.Stream ManifestStream, string Folder, string ExecutableName = "", string ProcessorArch = "")
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
            AdditionnalDependencies.AddRange(ExtractDependenciesFromSxsElement(SxsAssembly, Folder, ExecutableName, ProcessorArch));
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

        Stream textStream;

        byte[] tryDCM = new byte[WcpExConstants.DCMHeader.Count];
        ManifestStream.Read(tryDCM, 0, tryDCM.Length);
        ManifestStream.Seek(0, SeekOrigin.Begin);
        if (WcpExConstants.DCMHeader.SequenceEqual(tryDCM))
        {
            var orgStream = new MemoryStream();
            ManifestStream.CopyTo(orgStream);
            var decompressed = WcpEx.DecompressManifest(orgStream.ToArray());
            textStream = new MemoryStream(decompressed);
        }
        else
        {
            textStream = ManifestStream;
        }

        using (StreamReader xStream = new StreamReader(textStream))
        {
            // Trim double quotes in manifest attributes
            // Example :
            //      * Before : <assemblyIdentity name=""Microsoft.Windows.Shell.DevicePairingFolder"" processorArchitecture=""amd64"" version=""5.1.0.0"" type="win32" />
            //      * After  : <assemblyIdentity name="Microsoft.Windows.Shell.DevicePairingFolder" processorArchitecture="amd64" version="5.1.0.0" type="win32" />

            string PeManifest = xStream.ReadToEnd();
            PeManifest = new Regex("\\\"\\\"([\\w\\d\\.]*)\\\"\\\"").Replace(PeManifest, "\"$1\""); // Regex magic here

            // some manifests have "macros" that break xml parsing
            PeManifest = new Regex("SXS_PROCESSOR_ARCHITECTURE").Replace(PeManifest, "\"amd64\"");
            PeManifest = new Regex("SXS_ASSEMBLY_VERSION").Replace(PeManifest, "\"\"");
            PeManifest = new Regex("SXS_ASSEMBLY_NAME").Replace(PeManifest, "\"\"");

            // Remove blank lines
            PeManifest = Regex.Replace(PeManifest, @"^\s+$[\r\n]*", string.Empty, RegexOptions.Multiline);

            // Remove leading whitespaces
            PeManifest = PeManifest.TrimStart();

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
            try
            {
                return ExtractDependenciesFromSxsManifestFile(
                    OverridingManifest,
                    RootPeFolder,
                    RootPeFilename,
                    Pe.GetProcessor()
                );
            }
            catch (Exception)
            {
                Entries.HasError = true;
                return Entries;
            }
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
            Pe.GetProcessor()
            );
        return Entries;
    }
}
#endregion SxsManifest
