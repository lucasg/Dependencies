function Get-PeviewBinary {
  param(
    [String] $Url,
    [String] $Hash
  )

  Push-Location;
  $PeviewBinaryFile = "";

  # use temporary folder for download
  New-Item -ItemType Directory -Force -Path "$($env:TEMP)/tmp";
  Set-Location "$($env:TEMP)/tmp";

  &wget $Url -OutFile "processhacker-2.39-bin.zip";
  $PhArchiveHash = (Get-FileHash -Algorithm SHA256 -Path "./processhacker-2.39-bin.zip").Hash;

  if ($PhArchiveHash -eq "2afb5303e191dde688c5626c3ee545e32e52f09da3b35b20f5e0d29a418432f5") {
    &7z.exe x "./processhacker-2.39-bin.zip" "$($env:platform)/peview.exe";
    $PeviewBinaryFile = (Resolve-Path "./$($env:platform)/peview.exe").Path;
  }

  # Because of wacky Powershell "return" behavior, it's better to store the result in a env variable.
  $env:PEVIEW_PATH = $PeviewBinaryFile;
  Pop-Location;
}

function Copy-SystemDll {
  param(
    [String] $DllName,
    [String] $OutputFolder
  )

  $SystemFolder = [System.Environment]::GetFolderPath('SystemX86');
  if ($env:os -eq "x64")
  {
    # Check if it's a 32-bit powershell application, in order to force accessing System32
    if (Test-path "$([System.Environment]::GetFolderPath('Windows'))\sysnative")
    {
      $SystemFolder = "$([System.Environment]::GetFolderPath('Windows'))\sysnative";
    }
    else
    {
      $SystemFolder = [System.Environment]::GetFolderPath('System');
    }
  }

  $DllPath="$($SystemFolder)\$($DllName)";
  if (Test-Path $DllPath) {
    Write-Host "Copy system dll unresolved $DllPath";
    Write-Host "Copy system dll $((Resolve-Path $DllPath).Path)";
    Copy-Item (Resolve-Path $DllPath).Path -Destination $OutputFolder;
  }
}

function Get-DependenciesDeps {
  param(
    [String] $Binpath,
    [String] $OutputFolder
  )

  # Download external dependencies like peview
  $ProcessHackerReleaseUrl = "https://github.com/processhacker2/processhacker2/releases/download/v2.39/processhacker-2.39-bin.zip";
  $PeviewReleaseHash = "2afb5303e191dde688c5626c3ee545e32e52f09da3b35b20f5e0d29a418432f5";
  Get-PeviewBinary -Url $ProcessHackerReleaseUrl -Hash $PeviewReleaseHash;
  if (-not $env:PEVIEW_PATH)
  {
    Write-Error "[x] Peview binary has not correctly been downloaded."
  }


  # Bundling dbghelp.dll along for undecorating names
  Copy-SystemDll -DllName "dbghelp.dll" -OutputFolder $OutputFolder;

  # Bundling every msvc redistribuables
  $ClrPhLibPath = "$($Binpath)/ClrPhLib.dll";
  $ClrPhLibImports = &"$($Binpath)/Dependencies.exe" -json -imports $ClrPhLibPath | ConvertFrom-Json;
  foreach($DllImport in $ClrPhLibImports.Imports) {
    
    # vcruntime
    if ($DllImport.Name.ToLower().StartsWith("vcruntime"))
    {
      Copy-SystemDll -DllName $DllImport.Name -OutputFolder $OutputFolder;
    }

    # msvc
    if ($DllImport.Name.ToLower().StartsWith("msvc"))
    {
      Copy-SystemDll -DllName $DllImport.Name -OutputFolder $OutputFolder;
    }

    # ucrtbase
    if ($DllImport.Name.ToLower().StartsWith("ucrtbase"))
    {
      Copy-SystemDll -DllName $DllImport.Name -OutputFolder $OutputFolder;
    }

     # concrt
    if ($DllImport.Name.ToLower().StartsWith("concrt"))
    {
      Copy-SystemDll -DllName $DllImport.Name -OutputFolder $OutputFolder;
    }

  }

  return [string]$PeviewBinaryFile;
}

function Run-RegressTests {
  param(
    [String] $Binpath
  )

  Write-Host "Test if the binary (and the underlying lib) actually works"
  
  Write-Host "Test basic functionnality"
  &"$($Binpath)/Dependencies.exe" -knowndll
  &"$($Binpath)/Dependencies.exe" -apisets
  &"$($Binpath)/Dependencies.exe" -sxsentries "$($env:windir)/System32/ctfmon.exe" 


  Write-Host "Test manifest parsing"
  ./test/manifest-regress/Test-ManifestRegress.ps1 $($Binpath)

  # deactivated since it's too long
  # &"$BINPATH/demangler-test.exe"

  Write-Host "Tests done."
}

$BINPATH="C:/projects/dependencies/bin/$($env:CONFIGURATION)$($env:platform)";
$DepsFolder="C:/projects/dependencies/deps/$($env:CONFIGURATION)$($env:platform)";
$OutputFolder="C:/projects/dependencies/output";

# Creating output directory
New-Item -ItemType Directory -Force -Path $DepsFolder;
New-Item -ItemType Directory -Force -Path $OutputFolder;


# Retrieve all dependencies that need to be packaged
Get-DependenciesDeps -Binpath $BINPATH -OutputFolder $DepsFolder;

# Running regress tests
Run-RegressTests -Binpath $BINPATH;


Write-Host "Zipping everything"
if ($($env:CONFIGURATION) -eq "Debug") {
	&7z.exe a "$($OutputFolder)/Dependencies_$($env:platform)_$($env:CONFIGURATION).zip" $BINPATH/tests $BINPATH/*.dll $BINPATH/*.exe $BINPATH/*.config $BINPATH/*.pdb $DepsFolder/* $env:PEVIEW_PATH;
}
else {
	&7z.exe a "$($OutputFolder)/Dependencies_$($env:platform)_$($env:CONFIGURATION).zip" $BINPATH/*.dll $BINPATH/*.exe $BINPATH/*.config $BINPATH/*.pdb $DepsFolder/* $env:PEVIEW_PATH;
	&7z.exe a "$($OutputFolder)/Dependencies_$($env:platform)_$($env:CONFIGURATION)_(without peview.exe).zip" $BINPATH/*.dll $BINPATH/*.exe $BINPATH/*.config $BINPATH/*.pdb $DepsFolder/*;
}

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