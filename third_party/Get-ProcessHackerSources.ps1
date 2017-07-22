<#
.SYNOPSIS
Retrieve ProcessHacker source code and apply a patch for CLR compilation

.DESCRIPTION
Dependencies relies on a slightly modified version of phlib which fixes bugs when compiling with the /clr option (for a C++/CLI DLL target). It also modify the project's output folder for binaries and compilation objects.

One day I'll be able to upstream thoses changes and therefore only use  ProcessHacker's sdk, but until them I use this patching routine to keep the source relatively up-to-date.

#>

Push-Location

Write-Host -ForegroundColor Blue "Checkout out ProcessHackerSources";
&git clone https://github.com/processhacker2/processhacker2.git "tmp"
&cd tmp;
&git checkout -b "Dependencies" dc6a8a94f7e4b381090b46eb8e1b9fd7de052dbe
Write-Host -ForegroundColor Green "Checkout out ProcessHackerSources OK";

Write-Host -ForegroundColor Blue "Pathching ProcessHackerSources";
&git apply "../ProcessHacker-Fix-__acrt_fp_format-bug-and-specify-CLR-compilation.patch"
Write-Host -ForegroundColor Green "Pathching ProcessHackerSources OK";

Write-Host -ForegroundColor Blue "Exporting src folders";
Copy-Item -Force -Recurse  "phlib" "../"
Copy-Item -Force -Recurse  "phnt" "../"
Copy-Item -Force -Recurse  "tools/peview" "../"
Write-Host -ForegroundColor Green "Exporting src folders OK";


Write-Host -ForegroundColor Blue "Cleanup";
&cd "../";
Remove-Item -Recurse -Force "tmp";
Write-Host -ForegroundColor Green "Cleanup OK";

Pop-Location