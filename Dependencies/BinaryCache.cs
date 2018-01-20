using System;
using System.Collections.Generic;
using System.IO;
using System.ClrPh;
using System.Security.Cryptography;

namespace Dependencies
{
    /// <summary>
    /// Application wide PE cache on disk. This is used to solve the issue of phlib mapping
    /// analyzed binaries in memory and thus locking those in the filesystem (https://github.com/lucasg/Dependencies/issues/9).
    /// The BinaryCache copy every PE the application wants to open in a special folder in LocalAppData
    /// and open this one instead, prevent the original file from being locked.
    /// </summary>
    class BinaryCache
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

        #endregion Singleton implementation



        #region constructors


        public BinaryCache(string ApplicationAppDataPath, int _MaxBinaryCount)
        {
            MaxBinaryCount = _MaxBinaryCount;
            BinaryDatabase = new Dictionary<string, PE>();
            LruCache = new List<string>();

            BinaryCacheFolderPath = Path.Combine(ApplicationAppDataPath, "BinaryCache");
            Directory.CreateDirectory(BinaryCacheFolderPath);
        }

        #endregion constructors

        #region PublicAPI
        public void Load()
        {
            // "warm up" the cache
            foreach (var CachedBinary in Directory.EnumerateFiles(BinaryCacheFolderPath))
            {
                GetBinary(CachedBinary);
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

                    File.Delete(CachedBinary);
                }
            }


            // flush the cache
            BinaryDatabase.Clear();
        }

        public PE GetBinary(string PePath)
        {
            if (!File.Exists(PePath))
            {
                return null;
            }

            string PeHash = GetBinaryHash(PePath);

            // Cache "miss"
            bool hit = BinaryDatabase.ContainsKey(PeHash);
            if (!hit)
            {
                string DestFilePath = Path.Combine(BinaryCacheFolderPath, PeHash);
                if (DestFilePath != PePath)
                {
                    File.Copy(PePath, DestFilePath, true);
                }
                
                PE NewShadowBinary = new PE(DestFilePath);
                NewShadowBinary.Load();

                LruCache.Add(PeHash);
                BinaryDatabase.Add(PeHash, NewShadowBinary);
            }

            // Cache "Hit"
            UpdateLru(PeHash);
            PE ShadowBinary = BinaryDatabase[PeHash];
            ShadowBinary.Filepath = PePath;
            return ShadowBinary;
        }

        #endregion PublicAPI

        protected string GetBinaryHash(string PePath)
        {
            // Compute checksum only on first 1 KB of file data
            // in order not to spend too much CPU cycles here.
            // Hopefully there is enough entropy in PE headers 
            // not to trigger too many collisions.
            using (FileStream stream = File.OpenRead(PePath))
            {
                var sha = new SHA256Managed();
                byte[] buffer = new byte[1024];

                stream.Read(buffer, 0, buffer.Length);
                byte[] checksum = sha.ComputeHash(buffer, 0, buffer.Length);
                return BitConverter.ToString(checksum).Replace("-", String.Empty);
            }
        }

        protected void UpdateLru(string PeHash)
        {
            string MatchingHash = LruCache.Find(Hash => (Hash == PeHash));
            if (null != MatchingHash)
            { 
                // prepend the matching item at the beginning of the list
                LruCache.Remove(MatchingHash);
                LruCache.Insert(0, MatchingHash);
            }
        }

        #region Members

        private List<string> LruCache;
        private Dictionary<string, PE> BinaryDatabase;
        private string BinaryCacheFolderPath;
        private int MaxBinaryCount;

        #endregion Members
    }
}
