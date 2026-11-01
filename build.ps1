Function Info($msg) {
  Write-Host -ForegroundColor DarkGreen "`nINFO: $msg`n"
}

Function Error($msg) {
  Write-Host -ForegroundColor Red "`n`nERROR: $msg"
  exit 1
}

Function CheckReturnCodeOfPreviousCommand($msg) {
  if(-Not $?) {
    Error "${msg}. Error code: $LastExitCode"
  }
}

Function GetVersion() {
  try { $tag = git describe --exact-match --tags HEAD 2> $null } catch { }
  if(-Not $?) {
    Info "The commit is not tagged. Use 'v0.0-dev' as a version instead"
    $tag = "v0.0-dev"
  }

  return $tag.Substring(1)
}

Function BuildAndTest($configuration, $platform) {
  Info "Build and test $configuration $platform"

  Info "Run msbuild $configuration $platform"
  msbuild /nologo `
          /verbosity:minimal `
          /restore `
          /property:Configuration=$configuration `
          /property:Platform=$platform `
          /property:Version=$version `
          $root/Dependencies.slnx
  CheckReturnCodeOfPreviousCommand "build failed"

  if ($platform -eq 'ARM64' -and -not $isArm64Host) {
    Info "ARM64 cross-build complete. Running these tests requires an ARM64 Windows host."
    return
  }

  Info "Run 'dotnet test' $configuration $platform"
  dotnet test --nologo `
              --no-build `
              --configuration $configuration `
              /property:Platform=$platform `
              -verbosity:minimal `
              --logger:"console;verbosity=normal" `
              $root/src/Test/Test.csproj `
              -- RunConfiguration.TreatNoTestsAsError=true
  CheckReturnCodeOfPreviousCommand "tests failed"
}

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

$root = Resolve-Path "$PSScriptRoot"
$buildDir = "$root/build"
$version = GetVersion
$isArm64Host = [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture -eq 'Arm64'

Info "Version: '$version'"

Info "Open Visual Studio Developer PowerShell"

$vswhereCommand = Get-Command -Name "${Env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
$installationPath = & $vswhereCommand -prerelease -latest -property installationPath
& "$installationPath\Common7\Tools\Launch-VsDevShell.ps1" -SkipAutomaticLocation

BuildAndTest Release x64
BuildAndTest Debug x64

BuildAndTest Release x86
BuildAndTest Debug x86

BuildAndTest Release ARM64
BuildAndTest Debug ARM64

Info "Download Process Hacker binaries"
$processHackerDir = "$buildDir/processhacker-bin"
Invoke-WebRequest -Uri "https://github.com/winsiderss/systeminformer/releases/download/v4.0.26241.138/systeminformer-4.0.26241.138-bin.zip" `
                  -OutFile "$processHackerDir.zip"
Expand-Archive -LiteralPath "$processHackerDir.zip" -DestinationPath $processHackerDir -Force

foreach ($pair in @(@("x64", "amd64"), @("x86", "i386"), @("ARM64", "arm64"))) {
  $platform, $processHackerPlatform = $pair

  $archiveName = "$buildDir/dependencies_v${version}_$platform.zip"
  Info "Create zip archive '$archiveName'"

  $files = @(Get-ChildItem -Path "$buildDir/$platform/Release/DependenciesGui/*" -File -Exclude "*.pdb"
             Get-Item -LiteralPath "$processHackerDir/$processHackerPlatform/peview.exe")

  Compress-Archive -LiteralPath $files.FullName `
                   -DestinationPath $archiveName `
                   -Force
}
