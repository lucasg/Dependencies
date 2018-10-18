# Download external dependencies like peview
$PEVIEW_BIN="";

Push-Location;
New-Item -ItemType Directory -Force -Path "tmp";
cd tmp;
&wget "https://github.com/processhacker2/processhacker2/releases/download/v2.39/processhacker-2.39-bin.zip" -OutFile processhacker-2.39-bin.zip;
$PhArchiveHash = (Get-FileHash -Algorithm SHA256 -Path ./processhacker-2.39-bin.zip).Hash;
if ($PhArchiveHash -eq "2afb5303e191dde688c5626c3ee545e32e52f09da3b35b20f5e0d29a418432f5") {
  &7z.exe x ./processhacker-2.39-bin.zip $($env:platform)/peview.exe
  $PEVIEW_BIN = (Resolve-Path ./$($env:platform)/peview.exe).Path;
}
Pop-Location;

# Bundling dbghelp.dll along for undecorating names
$DbgHelpDll="";
if (Test-Path "$env:SystemRoot\System32\dbghelp.dll") {
  $DbgHelpDll=(Resolve-Path $env:SystemRoot\System32\dbghelp.dll).Path;
}

# Creating output directory
New-Item -ItemType Directory -Force -Path "output";
cd output;

$BINPATH="C:/projects/dependencies/bin/$($env:CONFIGURATION)$($env:platform)";

Write-Host "Test if the binary (and the underlying lib) actually works"
&"$BINPATH/Dependencies.exe" -knowndll
&"$BINPATH/Dependencies.exe" -apisets
&"$BINPATH/Dependencies.exe" -manifest "$($env:windir)/System32/shell32.dll" 
&"$BINPATH/Dependencies.exe" -sxsentries "$($env:windir)/System32/ctfmon.exe" 

# &"$BINPATH/demangler-test.exe"

Write-Host "Zipping everything"
&7z.exe a Dependencies_$($env:platform)_$($env:CONFIGURATION).zip $BINPATH/*.dll $BINPATH/*.exe $BINPATH/*.config $BINPATH/*.pdb $PEVIEW_BIN $DbgHelpDll;
&7z.exe a "Dependencies_$($env:platform)_$($env:CONFIGURATION)_(without peview.exe).zip" $BINPATH/*.dll $BINPATH/*.exe $BINPATH/*.config $BINPATH/*.pdb $DbgHelpDll;

# APPX packaging
if (( $($env:CONFIGURATION) -eq "Release") -and ($env:APPVEYOR_REPO_TAG)) {
  $makeappx = "${env:ProgramFiles(x86)}\Windows Kits\10\App Certification Kit\makeappx.exe";
  $signtool = "${env:ProgramFiles(x86)}\Windows Kits\10\App Certification Kit\signtool.exe";

  # Copy assets to build folder
  Copy-Item "C:/projects/dependencies/DependenciesAppx/Assets" -Destination "C:/projects/dependencies/bin/Appx_$($env:CONFIGURATION)$($env:platform)" -Force -Recurse

  # Create appx package
  & $makeappx pack /d "C:/projects/dependencies/bin/Appx_$($env:CONFIGURATION)$($env:platform)" /l /p "C:/projects/dependencies/bin/Appx_$($env:CONFIGURATION)$($env:platform).appx"

  # Sign appx package
  & $signtool sign /fd SHA256 /a /f "C:/projects/dependencies/DependenciesAppx/DependenciesAppx_TemporaryKey.pfx" "C:/projects/dependencies/bin/Appx_$($env:CONFIGURATION)$($env:platform).appx"
}