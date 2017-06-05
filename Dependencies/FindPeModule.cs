using System;
using System.ClrPh;
using System.IO;
using System.Collections.Generic;

namespace Dependencies
{
    public class FindPe
    {
        static string FindPeFromPath(string ModuleName, List<string> CandidateFolders)
        {
            string PeFilePath = null;

            foreach (String CandidatePath in CandidateFolders)
            {
                PeFilePath = Path.Combine(CandidatePath, ModuleName);
                PE TestPe = new PE(PeFilePath);

                if (TestPe.LoadSuccessful)
                    return PeFilePath;
            }

            return null;

        }

        public static string FindPeFromDefault(string ModuleName, string RootPeFolder)
        {
            string FoundPePath = null;

            // Look in application folder
            FoundPePath = FindPeFromPath(ModuleName, new List<string>(new string[] { RootPeFolder }));
    
            if (FoundPePath != null)
                return FoundPePath;

            // Look in system folders
            List<String> SystemFolders = new List<string>(new string[] {
                Environment.GetFolderPath(Environment.SpecialFolder.System),
                Environment.GetFolderPath(Environment.SpecialFolder.SystemX86)
                }
            );
            FoundPePath = FindPeFromPath(ModuleName, SystemFolders);
    
            if (FoundPePath != null)
                return FoundPePath;

            // Look in well-known dll
            // HKEY_LOCAL_MACHINE\System\CurrentControlSet\Control\Session Manager\KnownDLLs
            // https://blogs.msdn.microsoft.com/larryosterman/2004/07/19/what-are-known-dlls-anyway/


            // Look in local app data (check for python for exemple)

            // Look in Sxs manifest (copious reversing needed)

            return null;
        }
    }
	
}
