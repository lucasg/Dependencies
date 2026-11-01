using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Dependencies;

/// <summary>
/// Reference https://learn.microsoft.com/en/archive/blogs/jonwis/whats-that-awful-directory-name-under-windowswinsxs
/// But there are changes since Vista
/// </summary>
public class SxsSearcher
{
    public static readonly string WindowsDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
    public static readonly string WinSxsDir = Path.Combine(WindowsDir, "WinSxS");
    public static readonly string WinSxsManifestDir = Path.Combine(WinSxsDir, "Manifests");

    public static string NormalizeName(string name, int len = 40)
    {
        name = name.ToLowerInvariant();
        if (len == 0)
        {
            return name;
        }
        if (name.Length > len)
        {
            int lenpre = (len - 2) / 2;
            int lenpost = len - 2 - lenpre;
            return name.Substring(0, lenpre) + ".." + name.Substring(name.Length - lenpost);
        }
        return name;
    }

    // Contents dir : {WinSxsDir}/{ProcArch}_{NormalizeName}_{PublicKeyToken}_{FuzzyVersion}_{Lang}_{some_hash}/
    // Manifest filename : {WinSxsManifestDir}/{ProcArch}_{NormalizeName}_{PublicKeyToken}_{FuzzyVersion}_{Lang}_{some_hash}.manifest
    // matchLang: null, "", "neutral", -> none, x-ww
    public static IList<string> GetMatchPathes(bool isManifest, bool highest, string processArch, string name, string publicKeyToken, int major, int minor, string matchLang = null)
    {
        foreach (var nameNormal in new string[] { NormalizeName(name, 40), NormalizeName(name, 64), NormalizeName(name, 0) })
        {
            IEnumerable<string> pathes;
            string prefix = $"{processArch}_{nameNormal}_{publicKeyToken}_{major}.{minor}";
            var regex = new Regex($"({Regex.Escape(prefix)}\\.(\\d+)\\.(\\d+)_([a-zA-Z0-9\\-]+)_([a-fA-F0-9]+))", RegexOptions.IgnoreCase);
            if (isManifest)
            {
                pathes = Directory.EnumerateFiles(WinSxsManifestDir, $"{prefix}.*.manifest");
            }
            else
            {
                pathes = Directory.EnumerateDirectories(WinSxsDir, $"{prefix}.*");
            }
            pathes = pathes.ToArray();
            if (pathes.Any())
            {
                if (highest)
                {
                    string foundPath = null;
                    int highestBuild = 0;
                    int highestPatch = 0;
                    foreach (var path in pathes)
                    {
                        Match matching = regex.Match(path);
                        if (matching.Success)
                        {
                            string lang = matching.Groups[4].Value.ToLowerInvariant();
                            if (matchLang == "*")
                            {
                                // accept all
                            }
                            else if (string.IsNullOrEmpty(matchLang) || matchLang == "neutral")
                            {
                                if (lang != "none" && lang != "x-ww")
                                {
                                    continue;
                                }
                            }
                            else
                            {
                                if (lang != matchLang)
                                {
                                    continue;
                                }
                            }

                            int matchingBuild = int.Parse(matching.Groups[2].Value);
                            int matchingPatch = int.Parse(matching.Groups[3].Value);

                            if ((matchingBuild > highestBuild) || ((matchingBuild == highestBuild) && (matchingPatch > highestPatch)))
                            {
                                foundPath = path;
                                highestBuild = matchingBuild;
                                highestPatch = matchingPatch;
                            }
                        }
                    }
                    if (foundPath != null)
                    {
                        return new string[] { foundPath };
                    }
                    return new string[0];
                }
                return pathes as IList<string>;
            }
        }
        return new string[0];
    }

    // WindowsXP Policy: C:\WINDOWS\WinSxS\Policies\amd64_policy.9.0.Microsoft.VC90.CRT_1fc8b3b9a1e18e3b_x-ww_16f3e195\9.0.30729.6161.policy

    // New Policy: HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\SideBySide\Winners\{ProcArch}_{Name}_{PublicKeyToken}_{Langage}_{some_hash}
}
