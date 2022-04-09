
function Test-Executable {
    param (
        [String] $TestName,
        [String] $TestExecutable,
        [String] $BinFolder,
        [Parameter(Mandatory=$False)][String] $StringToFound = $null,
        [Parameter(Mandatory=$False)][String] $StringNotToFound = $null,
        [String] $Command
    )


    Write-Host -ForegroundColor Magenta "[-] Regress test : $TestName -> $StringToFound"
    $SxsDepsResult  = &"$($BinFolder)/Dependencies.exe" $Command $TestExecutable 2>&1 | Out-String 
    Write-Debug "[-] result : "
    Write-Debug  $($SxsDepsResult -join "`r`n" )

    $DllFound = $False;
    if (-not($StringToFound))
    {
        $DllFound = $True;
    }

    foreach ($line in $SxsDepsResult)
    {
    
        if ($StringToFound)
        {
            Write-Debug "$line contains $StringToFound : $($line.ToLower().Contains($StringToFound.ToLower()))"
            if ($line.ToLower().Contains($StringToFound.ToLower()))
            {
                $DllFound = $True;
                break;
            }
        }
        

        if ($StringNotToFound)
        {
            Write-Debug "$line not contains $StringNotToFound : $($line.ToLower().Contains($StringNotToFound.ToLower()))"
            if ($line.ToLower().Contains($StringNotToFound.ToLower()))
            {
                Write-Error "[x] $TestName test failed";
                Write-Output ""
                return
            }
        }
    }

    if (-not $DllFound)
    {
        Write-Error "[x] $TestName test failed";
    }
    else 
    {
        Write-Host -ForegroundColor Green "[+] $TestName test passed";
    }

    Write-Output ""
}

$DependenciesDir = $args[0]
$RegressDir = [System.IO.Path]::GetDirectoryName($myInvocation.MyCommand.Definition)

# Malformed manifest due to asmv3 url redefining
$ChangePkPath = Join-Path $RegressDir "changepk.exe";
Test-Executable -TestName "Changepk" -TestExecutable $ChangePkPath -Command "-manifest" -StringNotToFound "System.Xml.XmlException" -StringToFound '<requestedExecutionLevel level="requireAdministrator" />' -BinFolder $DependenciesDir

# Embededd Manifest resource index > 1
$SystemSettingsPath = Join-Path $RegressDir "SystemSettings.exe";
Test-Executable -TestName "SystemSettings" -TestExecutable $SystemSettingsPath -Command "-manifest" -StringToFound "<assembly xmlns="urn:schemas-microsoft-com:asm.v1" xmlns:asmv3="urn:schemas-microsoft-com:asm.v3" manifestVersion="1.0">" -BinFolder $DependenciesDir

# Double quotes in assemblyIdentity name attribute !
$DevicePairingFolderPath = Join-Path $RegressDir "DevicePairingFolder.dll";
Test-Executable -TestName "DevicePairingFolder" -TestExecutable $DevicePairingFolderPath -Command "-manifest" -StringToFound '<assemblyIdentity name="Microsoft.Windows.Shell.DevicePairingFolder" processorArchitecture="amd64"' -BinFolder $DependenciesDir

# Redirect dll loading to a directory, so search for this directory name in the sxs dependencies
$UseDll32Path = Join-Path $RegressDir "use_dll32.exe";
Test-Executable -TestName "DllRedirection32" -TestExecutable $UseDll32Path -Command "-sxsentries" -StringToFound '\test_folder\depdll32.dll' -BinFolder $DependenciesDir

# Redirect the dll to a directory, but also change the dll name!
$UseDll64Path = Join-Path $RegressDir "use_dll64.exe";
Test-Executable -TestName "DllRedirection64CustomName" -TestExecutable $UseDll64Path -Command "-sxsentries" -StringToFound '\test_folder\custom_name64.DLL' -BinFolder $DependenciesDir

# Re-run the previous test, but ensure dependencies also knows the original dll name!
$UseDll64Path = Join-Path $RegressDir "use_dll64.exe";
Test-Executable -TestName "DllRedirection64RealName" -TestExecutable $UseDll64Path -Command "-sxsentries" -StringToFound 'depdll64.dll' -BinFolder $DependenciesDir
