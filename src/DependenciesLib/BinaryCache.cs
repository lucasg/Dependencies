using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using Dependencies.ClrPh;
using System.ComponentModel;
using System.Linq;

namespace Dependencies;

/// <summary>
/// Application wide PE cache on disk. This is used to solve the issue of phlib mapping
/// analyzed binaries in memory and thus locking those in the filesystem (https://github.com/lucasg/Dependencies/issues/9).
/// The BinaryCache copy every PE the application wants to open in a special folder in LocalAppData
/// and open this one instead, prevent the original file from being locked.
/// </summary>
public abstract class BinaryCache
{
    #region Singleton implementation
    private static BinaryCache SingletonInstance;

    /// <summary>
    /// Singleton implemenation for the BinaryCache. This class must be
    /// visible and unique throughout the whole application in order to be efficient.
    /// </summary>
    public static BinaryCache Instance
    {
        get
        {
            return SingletonInstance;
        }
        set
        {
            SingletonInstance = value;
        }
    }

    public static void InitializeBinaryCache(bool UseCache)
    {
        if (UseCache)
        {
            string ApplicationLocalAppDataPath = Path.Combine(
               Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
               "Dependencies"
           );
            Instance = new BinaryCacheImpl(ApplicationLocalAppDataPath, 200);
        }
        else
        {
            Instance = new BinaryNoCacheImpl();
        }

        Instance.Load();
    }
    #endregion Singleton implementation

    #region PublicAPI

    /// <summary>
    /// Ask the BinaryCache to load a PE from the filesystem. The
    /// whole cache magic is hidden underneath
    ///
    /// </summary>
    /// <param name="PePath"> Path to desired PE file.</param>
    /// <returns>
    ///     return null if the file is not found
    ///     return PE.LoadSuccessful == false if the file exists but it's not a valid PE file
    /// </returns>
    public static PE LoadPe(string PePath)
    {
        return Instance.GetBinary(PePath);
    }

    public static Tuple<ModuleSearchStrategy, PE> ResolveModule(string ModuleName)
    {
        PE RootPe = LoadPe(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "ntdll.dll"));
        string WorkingDirectory = Path.GetDirectoryName(RootPe.Filepath);
        List<string> CustomSearchFolders = new List<string>();
        SxsEntries SxsCache = SxsManifest.GetSxsEntries(RootPe);

        return ResolveModule(RootPe, ModuleName, SxsCache, CustomSearchFolders, WorkingDirectory);
    }

    public static Tuple<ModuleSearchStrategy, PE> ResolveModule(PE RootPe, string ModuleName)
    {
        string WorkingDirectory = Path.GetDirectoryName(RootPe.Filepath);
        List<string> CustomSearchFolders = new List<string>();
        SxsEntries SxsCache = SxsManifest.GetSxsEntries(RootPe);

        return ResolveModule(RootPe, ModuleName, SxsCache, CustomSearchFolders, WorkingDirectory);
    }


    public static Tuple<ModuleSearchStrategy, PE> ResolveModule(PE RootPe, string ModuleName, SxsEntries SxsCache, List<string> CustomSearchFolders, string WorkingDirectory)
    {
        Tuple<ModuleSearchStrategy, string> ResolvedFilepath;

        // if no extension is used, assume a .dll
        if (Path.GetExtension(ModuleName) == String.Empty)
        {
            ModuleName = String.Format("{0:s}.dll", ModuleName);
        }

        string ApiSetName = LookupApiSetLibrary(ModuleName);
        if (!string.IsNullOrEmpty(ApiSetName))
        {
            ModuleName = ApiSetName;
        }

        ResolvedFilepath = FindPe.FindPeFromDefault(RootPe, ModuleName, SxsCache, CustomSearchFolders, WorkingDirectory);

        // ApiSet override the underneath search location if found or not
        ModuleSearchStrategy ModuleLocation = ResolvedFilepath.Item1;
        if (!string.IsNullOrEmpty(ApiSetName) /*&& (ResolvedFilepath.Item2 != null)*/)
            ModuleLocation = ModuleSearchStrategy.ApiSetSchema;

        //
        PE ResolvedModule = null;
        if (ResolvedFilepath.Item2 != null)
            ResolvedModule = LoadPe(ResolvedFilepath.Item2);


        return new Tuple<ModuleSearchStrategy, PE>(ModuleLocation, ResolvedModule);
    }

    public static Tuple<ModuleSearchStrategy, PE> ResolveModule(PE RootPe, string ModulePath, string WorkingDirectory)
    {
        Tuple<ModuleSearchStrategy, string> ResolvedFilepath;

        string absPath;
        if (string.IsNullOrWhiteSpace(ModulePath))
            throw new ArgumentException("File path cannot be null or empty.", nameof(ModulePath));

        if (Path.IsPathRooted(ModulePath))
        {
            // If already an absolute path, get the normalized full path
            absPath = Path.GetFullPath(ModulePath);
        }
        else
        {
            // If relative, combine with the working directory
            if (string.IsNullOrWhiteSpace(WorkingDirectory))
                throw new ArgumentException("Base folder must be provided for relative paths.", nameof(WorkingDirectory));

            // Combine base folder and relative file path, then get full path
            absPath = Path.Combine(WorkingDirectory, ModulePath);
            absPath = Path.GetFullPath(absPath);
        }

        if (File.Exists(absPath))
        {
            // if the file exists, we can use it directly
            ResolvedFilepath = new Tuple<ModuleSearchStrategy, string>(ModuleSearchStrategy.Fullpath, absPath);
        }
        else
        {
            // Can not find the path.
            ResolvedFilepath = new Tuple<ModuleSearchStrategy, string>(ModuleSearchStrategy.NOT_FOUND, null);
        }

        PE ResolvedModule = null;
        if (ResolvedFilepath.Item2 != null)
            ResolvedModule = LoadPe(ResolvedFilepath.Item2);


        return new Tuple<ModuleSearchStrategy, PE>(ResolvedFilepath.Item1, ResolvedModule);
    }

    private static ApiSetSchema ApiSetmapCache = Phlib.GetApiSetSchema();

    /// <summary>
    /// Attempt to query the HostDll pointed by the Apiset contract.
    /// </summary>
    /// <param name="ImportDllName"> DLL name as in the parent import entry. May or may not be an apiset contract </param>
    /// <returns> Return the first host dll pointed by the apiset contract if found, otherwise it return an empty string.</returns>
    public static string LookupApiSetLibrary(string ImportDllName)
    {
        // Look for api set target
        if (!ImportDllName.StartsWith("api-", StringComparison.CurrentCultureIgnoreCase) && !ImportDllName.StartsWith("ext-", StringComparison.CurrentCultureIgnoreCase))
            return "";


        // Strip the .dll extension and search for matching targets
        var ImportDllWIthoutExtension = Path.GetFileNameWithoutExtension(ImportDllName);
        var Targets = ApiSetmapCache.Lookup(ImportDllWIthoutExtension);
        if ((Targets != null) && (Targets.Count > 0))
            return Targets[0];

        return "";
    }

    public static bool LookupImport(string ModuleFilePath, string ImportName, int ImportOrdinal, bool ImportByOrdinal)
    {
        if (ModuleFilePath == null)
            return false;

        string ApiSetName = LookupApiSetLibrary(ModuleFilePath);
        if (!string.IsNullOrEmpty(ApiSetName))
        {
            ModuleFilePath = ApiSetName;
        }

        PE Module = LoadPe(ModuleFilePath);
        if (Module == null)
            return false;

        foreach (var export in Module.GetExports())
        {
            if (ImportByOrdinal)
            {
                if ((export.Ordinal == ImportOrdinal) && export.ExportByOrdinal)
                    return true;
            }
            else
            {
                if (export.ForwardedName == ImportName)
                    return true;

                if (export.Name == ImportName)
                    return true;

            }

        }

        return false;
    }

    public static List<Tuple<PeImport, bool>> LookupImports(PeImportDll ParentImports, List<PeExport> ModuleExports)
    {
        List<Tuple<PeImport, bool>> Result = new List<Tuple<PeImport, bool>>();

        foreach (PeImport Import in ParentImports.ImportList)
        {
            bool bFoundImport = false;

            foreach (var export in ModuleExports)
            {
                if (Import.ImportByOrdinal)
                {
                    // Even if the export has a Name (therefore not a pure export by ordinal)
                    // we can still possibly import it by its ordinal, although it's not recommended.
                    if ((export.Ordinal == Import.Ordinal) /*&& export.ExportByOrdinal*/)
                    {
                        bFoundImport = true;
                        break;
                    }

                }
                else
                {
                    if (export.ForwardedName == Import.Name)
                    {
                        bFoundImport = true;
                        break;
                    }


                    if (export.Name == Import.Name)
                    {
                        bFoundImport = true;
                        break;
                    }

                }
            }

            Result.Add(new Tuple<PeImport, bool>(Import, bFoundImport));
        }

        return Result;
    }

    public static List<Tuple<PeImport, bool>> LookupImports(PeImportDll ModuleImport, string ModuleFilePath)
    {
        PE Module = null;
        List<Tuple<PeImport, bool>> Result = new List<Tuple<PeImport, bool>>();

        // if there is a module name, try to resolve apiset for attempting to load it
        if (ModuleFilePath != null)
        {
            string ApiSetName = LookupApiSetLibrary(ModuleFilePath);
            if (!string.IsNullOrEmpty(ApiSetName))
            {
                Module = ResolveModule(ApiSetName).Item2;
            }
            else
            {
                Module = LoadPe(ModuleFilePath);
            }
        }

        // If the module has not been found, mark all imports as not found
        if (Module == null)
        {
            foreach (PeImport Import in ModuleImport.ImportList)
            {
                Result.Add(new Tuple<PeImport, bool>(Import, false));
            }

            return Result;
        }

        return LookupImports(ModuleImport, Module.GetExports());

    }

    #endregion PublicAPI



    #region constructors
    #endregion constructors

    #region Contract

    // Attempt to load a file as a PE
    public abstract PE GetBinary(string PePath);

    // static initialization => warmup
    public abstract void Load();

    // Graceful cleanup
    public abstract void Unload();

    #endregion Contract


    #region Members
    #endregion Members
}

public class BinaryCacheImpl : BinaryCache
{
    public BinaryCacheImpl(string ApplicationAppDataPath, int _MaxBinaryCount)
    {
        BinaryDatabase = new Dictionary<string, PE>();
        MaxBinaryCount = _MaxBinaryCount;
        string platform = (IntPtr.Size == 8) ? "x64" : "x86";

        BinaryCacheFolderPath = Path.Combine(ApplicationAppDataPath, "BinaryCache", platform);
        Directory.CreateDirectory(BinaryCacheFolderPath);
    }

    public override void Load()
    {
        string System32Folder = Environment.GetFolderPath(Environment.SpecialFolder.System);
        string SysWow64Folder = Environment.GetFolderPath(Environment.SpecialFolder.SystemX86);

        // wow64.dll, wow64cpu.dll and wow64win.dll are listed as wow64 known dlls,
        // but they are actually x64 binaries.
        List<String> Wow64Dlls = new List<string>(new string[] {
                "wow64.dll",
                "wow64cpu.dll",
                "wow64win.dll"
            });

        // preload all well known dlls
        foreach (String KnownDll in Phlib.GetKnownDlls(false))
        {
            GetBinaryAsync(Path.Combine(System32Folder, KnownDll));
        }

        foreach (String KnownDll in Phlib.GetKnownDlls(true))
        {
            if (Wow64Dlls.Contains(KnownDll))
            {
                GetBinaryAsync(Path.Combine(System32Folder, KnownDll));
            }
            else
            {
                GetBinaryAsync(Path.Combine(SysWow64Folder, KnownDll));
            }
        }
    }

    public override void Unload()
    {
        foreach (var kv in BinaryDatabase)
        {
            kv.Value.Unload();
        }
        BinaryDatabase.Clear();

        foreach (var file in new DirectoryInfo(BinaryCacheFolderPath).EnumerateFiles()
            .OrderByDescending(fi => fi.LastAccessTime).Skip(MaxBinaryCount))
        {
            try
            {
                file.Delete();
            }
            catch (System.UnauthorizedAccessException uae)
            {
                // The BinaryCache is shared among serveral Dependencies.exe instance
                // so only the last one alive can clear the cache.
                Debug.WriteLine("[BinaryCache] Could not unload file {0:s} : {1:s} ", file.FullName, uae);
            }
        }
    }

    private PE LoadCachedBinary(string peHash)
    {
        var cachedBinaryFile = Path.Combine(BinaryCacheFolderPath, peHash);
        if (!NativeFile.Exists(cachedBinaryFile))
        {
            return null;
        }

        PE cachedPE = new PE(cachedBinaryFile);
        try
        {
            // update LastAccessTime to save LRU to disk
            // note: Windows from Vista disable updating LastAccessTime by default,
            // so we have to update it manually.
            new FileInfo(cachedBinaryFile).LastAccessTime = DateTime.Now;
        }
        catch { }

        if (!cachedPE.Load())
        {
            return null;
        }

        return cachedPE;
    }

    public void GetBinaryAsync(string PePath, RunWorkerCompletedEventHandler Callback = null)
    {
        BackgroundWorker bw = new BackgroundWorker();
        bw.DoWork += (sender, e) =>
        {
            GetBinary(PePath);
        };

        if (Callback != null)
        {
            bw.RunWorkerCompleted += Callback;
        }

        bw.RunWorkerAsync();
    }

    public override PE GetBinary(string PePath)
    {
        //Debug.WriteLine(String.Format("Attempt to load : {0:s}", PePath), "BinaryCache");

        if (!NativeFile.Exists(PePath))
        {
            Debug.WriteLine(String.Format("File not present on the filesystem : {0:s} ", PePath), "BinaryCache");
            return null;
        }

        string PeHash = GetBinaryHash(PePath);
        //Debug.WriteLine(String.Format("File {0:s} hash : {1:s} ", PePath, PeHash), "BinaryCache");

        // A sync lock is mandatory here in order not to load twice the
        // same binary from two differents workers
        PE cachedPE;
        lock (BinaryDatabase)
        {
            // Memory Cache "miss"
            if (!BinaryDatabase.TryGetValue(PeHash, out cachedPE))
            {
                cachedPE = LoadCachedBinary(PeHash);
                if (cachedPE == null)
                {
                    // Disk Cache miss
                    string DestFilePath = Path.Combine(BinaryCacheFolderPath, PeHash);
                    if (!File.Exists(DestFilePath) && (DestFilePath != PePath))
                    {
                        // Debug.WriteLine(String.Format("FileCopy from {0:s} to {1:s}", PePath, DestFilePath), "BinaryCache");
                        NativeFile.Copy(PePath, DestFilePath);
                    }
                    cachedPE = LoadCachedBinary(PeHash);
                    if (cachedPE == null)
                    {
                        BinaryDatabase.Remove(PeHash);
                        return null;
                    }
                }
                BinaryDatabase.Add(PeHash, cachedPE);
            }
        }

        PE ShadowBinary = cachedPE;
        ShadowBinary.Filepath = Path.GetFullPath(PePath); // convert any paths to an absolute one.

        Debug.WriteLine(String.Format("File {0:s} loaded from {1:s}", PePath, Path.Combine(BinaryCacheFolderPath, PeHash)), "BinaryCache");
        return ShadowBinary;
    }

    protected string GetBinaryHash(string PePath)
    {
        return NativeFile.GetPartialHashFile(PePath, 1024);
    }

    #region Members
    private readonly Dictionary<string, PE> BinaryDatabase;
    private int MaxBinaryCount;

    private string BinaryCacheFolderPath;

    #endregion Members
}


public class BinaryNoCacheImpl : BinaryCache
{
    public override PE GetBinary(string PePath)
    {
        PE pefile = new PE(PePath);
        pefile.Load();

        return pefile;
    }

    // static initialization => warmup
    public override void Load()
    {

    }

    // Graceful cleanup
    public override void Unload()
    {

    }
}
