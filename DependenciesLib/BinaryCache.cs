using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using Dependencies.ClrPh;
using System.ComponentModel;

namespace Dependencies
{
    /// <summary>
    /// Application wide PE cache on disk. This is used to solve the issue of phlib mapping
    /// analyzed binaries in memory and thus locking those in the filesystem (https://github.com/lucasg/Dependencies/issues/9).
    /// The BinaryCache copy every PE the application wants to open in a special folder in LocalAppData
    /// and open this one instead, prevent the original file from being locked.
    /// </summary>
    public class BinaryCache
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
                if (SingletonInstance == null)
                {
                    string ApplicationLocalAppDataPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "Dependencies"
                    );

                    SingletonInstance = new BinaryCache(ApplicationLocalAppDataPath, 200);
                }

                return SingletonInstance;
            }
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


        private static ApiSetSchema ApiSetmapCache = Phlib.GetApiSetSchema();

        /// <summary>
        /// Attempt to query the HostDll pointed by the Apiset contract.
        /// </summary>
        /// <param name="ImportDllName"> DLL name as in the parent import entry. May or may not be an apiset contract </param>
        /// <returns> Return the first host dll pointed by the apiset contract if found, otherwise it return an empty string.</returns>
        public static string LookupApiSetLibrary(string ImportDllName)
        {
            
            // Look for api set target 
            if (!ImportDllName.StartsWith("api-") && !ImportDllName.StartsWith("ext-"))
                return "";
           
            // Strip the .dll extension and the last number (which is probably a build counter)
            string ImportDllNameWithoutExtension = Path.GetFileNameWithoutExtension(ImportDllName);
            string ImportDllHashKey = ImportDllNameWithoutExtension.Substring(0, ImportDllNameWithoutExtension.LastIndexOf("-"));

            if (ApiSetmapCache.ContainsKey(ImportDllHashKey))
            {
                ApiSetTarget Targets = ApiSetmapCache[ImportDllHashKey];
                if (Targets.Count > 0)
                {
                    return Targets[0];
                }
            }
            
            return "";
        }

        public static bool LookupImport(string ModuleFilePath, string ImportName, int ImportOrdinal, bool ImportByOrdinal)
        {
            if (ModuleFilePath == null)
                return false;

            string ApiSetName = LookupApiSetLibrary(ModuleFilePath);
            if (ApiSetName != null)
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
						if ((export.Ordinal == Import.Ordinal) && export.ExportByOrdinal)
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
			List<Tuple<PeImport, bool>> Result = new List<Tuple<PeImport, bool>>();

			if (ModuleFilePath == null)
                return Result;

            string ApiSetName = LookupApiSetLibrary(ModuleFilePath);
            if (ApiSetName != null)
            {
                ModuleFilePath = ApiSetName;
            }

            PE Module = LoadPe(ModuleFilePath);
            if (Module == null)
                return Result;

			return LookupImports(ModuleImport, Module.GetExports());

		}

        #endregion PublicAPI



        #region constructors


        public BinaryCache(string ApplicationAppDataPath, int _MaxBinaryCount)
        {
            
            BinaryDatabase = new Dictionary<string, PE>();
            FilepathDatabase = new Dictionary<string, PE>();
            BinaryDatabaseLock = new Object();
            LruCache = new List<string>();

            MaxBinaryCount = _MaxBinaryCount;
	        string platform = (IntPtr.Size == 8) ? "x64" : "x86";

            BinaryCacheFolderPath = Path.Combine(ApplicationAppDataPath, "BinaryCache", platform);
            Directory.CreateDirectory(BinaryCacheFolderPath);
        }

        #endregion constructors

        
        public void Load()
        {
            // "warm up" the cache
            foreach (var CachedBinary in Directory.EnumerateFiles(BinaryCacheFolderPath))
            {
                GetBinaryAsync(CachedBinary);
            }

            string System32Folder = Environment.GetFolderPath(Environment.SpecialFolder.System);
            string SysWow64Folder = Environment.GetFolderPath(Environment.SpecialFolder.SystemX86);

            // wow64.dll, wow64cpu.dll and wow64win.dll are listed as wow64 known dlls,
            // but they are actually x64 binaries.
            List<String> Wow64Dlls = new List<string>(new string[] {
                "wow64.dll",
                "wow64cpu.dll",
                "wow64win.dll"
            });

            // preload all well konwn dlls
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

        public void Unload()
        { 
            // cut off the LRU cache
            LruCache = LruCache.GetRange(0, Math.Min(LruCache.Count, MaxBinaryCount));

            foreach (var CachedBinary in Directory.EnumerateFiles(BinaryCacheFolderPath))
            {
                string PeHash = GetBinaryHash(CachedBinary);

                if (LruCache.Find(Hash => (Hash == PeHash)) == null)
                {
                    // Force map unloading before deleting file
                    if (BinaryDatabase.ContainsKey(PeHash))
                    {
                        BinaryDatabase[PeHash].Unload();
                    }

                    try
                    {
                        File.Delete(CachedBinary);
                    }
                    catch (System.UnauthorizedAccessException uae)
                    {
                        // The BinaryCache is shared among serveral Dependencies.exe instance
                        // so only the last one alive can clear the cache.
                        Debug.WriteLine("[BinaryCache] Could not unload file {0:s} : {1:s} ", CachedBinary, uae);
                    }
                    
                }
            }


            // flush the cache
            BinaryDatabase.Clear();
            FilepathDatabase.Clear();
        }

        public void GetBinaryAsync(string PePath, RunWorkerCompletedEventHandler Callback = null)
        {
            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += (sender, e) => {

                GetBinary(PePath);
            };

            if (Callback != null)
            {
                bw.RunWorkerCompleted += Callback;
            }
            

            bw.RunWorkerAsync();
        }

        public PE GetBinary(string PePath)
        {
            Debug.WriteLine(String.Format("Attempt to load : {0:s}", PePath), "BinaryCache");

            if (!NativeFile.Exists(PePath))
            {
                Debug.WriteLine(String.Format("File not present on the filesystem : {0:s} ", PePath), "BinaryCache");
                return null;
            }

            string Fullpath = Path.GetFullPath(PePath);
            if (FilepathDatabase.ContainsKey(Fullpath))
            {
                // TODO : update LRU cache
                PE sShadowBinary = FilepathDatabase[Fullpath];
                sShadowBinary.Filepath = Fullpath;
                return sShadowBinary;
            }

            string PeHash = GetBinaryHash(PePath);
            Debug.WriteLine(String.Format("File {0:s} hash : {1:s} ", PePath, PeHash), "BinaryCache");

            // A sync lock is mandatory here in order not to load twice the
            // same binary from two differents workers
            lock (BinaryDatabaseLock)
            {
                bool hit = BinaryDatabase.ContainsKey(PeHash);
                
                // Cache "miss"
                if (!hit)
                {
                
                    string DestFilePath = Path.Combine(BinaryCacheFolderPath, PeHash);
                    if (!File.Exists(DestFilePath) && (DestFilePath != PePath))
                    {
                        Debug.WriteLine(String.Format("FileCopy from {0:s} to {1:s}", PePath, DestFilePath), "BinaryCache");
                        NativeFile.Copy(PePath, DestFilePath);
                    }
                
                    PE NewShadowBinary = new PE(DestFilePath);
                    NewShadowBinary.Load();

                    LruCache.Add(PeHash);
                    BinaryDatabase.Add(PeHash, NewShadowBinary);
                    FilepathDatabase.Add(Fullpath, NewShadowBinary);
                }
            }

            // Cache "Hit"
            UpdateLru(PeHash);
            PE ShadowBinary = BinaryDatabase[PeHash];
            ShadowBinary.Filepath = Path.GetFullPath(PePath); // convert any paths to an absolute one.

            Debug.WriteLine(String.Format("File {0:s} loaded from {1:s}", PePath, Path.Combine(BinaryCacheFolderPath, PeHash)), "BinaryCache");
            return ShadowBinary;
        }

        protected string GetBinaryHash(string PePath)
        {
            return NativeFile.GetPartialHashFile(PePath, 1024);
        }

        protected void UpdateLru(string PeHash)
        {
            string MatchingHash = LruCache.Find(Hash => (Hash == PeHash));
            if (null == MatchingHash)
                return;

            lock (BinaryDatabaseLock)
            {
                // prepend the matching item at the beginning of the list
                LruCache.Remove(MatchingHash);
                LruCache.Insert(0, MatchingHash);
            }
        }

        #region Members

        private List<string> LruCache;
        private Dictionary<string, PE> BinaryDatabase;
        private Dictionary<string, PE> FilepathDatabase;
        private Object BinaryDatabaseLock; 

        private string BinaryCacheFolderPath;
        private int MaxBinaryCount;

        #endregion Members
    }
}
