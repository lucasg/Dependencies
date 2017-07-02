using System;
using System.ClrPh;
using System.IO;
using System.Collections.Generic;

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
        public static string FindPeFromDefault(string ModuleName, string RootPeFolder, bool Wow64Dll = false)
        {
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

            return null;
        }
    }
	
}
