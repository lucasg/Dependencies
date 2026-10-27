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

  & $launchVsDevShellCommand -SkipAutomaticLocation -Arch $platform

  Info "Run msbuild $configuration $platform"
  msbuild /nologo `
          /verbosity:minimal `
          /restore `
          /property:Configuration=$configuration `
          /property:Platform=$platform `
          /property:Version=$version `
          $root/Dependencies.slnx
  CheckReturnCodeOfPreviousCommand "build failed"

  Info "Run 'dotnet test' $configuration $platform"
  dotnet test --nologo `
              --no-build `
              --configuration Release `
              -verbosity:minimal `
              --logger:"console;verbosity=normal" `
              $root/Test/Test.csproj
  CheckReturnCodeOfPreviousCommand "tests failed"
}

Function GetLaunchVsDevShellCommand() {
  $vswhereCommand = Get-Command -Name "${Env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
  $installationPath = & $vswhereCommand -prerelease -latest -property installationPath
  return "$installationPath\Common7\Tools\Launch-VsDevShell.ps1"
}

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

$root = Resolve-Path "$PSScriptRoot"
$buildDir = "$root/build"
$publishDir = "$buildDir/Publish"
$version = GetVersion
$launchVsDevShellCommand = GetLaunchVsDevShellCommand

Info "Version: '$version'`nlaunchVsDevShellCommand: '$launchVsDevShellCommand'"

BuildAndTest Release x64
BuildAndTest Debug x64

BuildAndTest Release x86
BuildAndTest Debug x86

foreach ($platform in @("x64", "x86")) {
  $archiveName = "$buildDir/dependencies_v${version}_$platform.zip"
  Info "Create zip archive '$archiveName'"
  $files = Get-ChildItem -Path "$buildDir/$platform/Release/DependenciesGui/*" -File -Exclude "*.pdb"
  Compress-Archive -LiteralPath $files.FullName `
                   -DestinationPath $archiveName `
                   -Force
}
