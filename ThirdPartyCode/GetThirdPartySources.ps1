param([Parameter(Mandatory = $true)][string] $Destination)

$ErrorActionPreference = 'Stop'

if (Get-ChildItem -LiteralPath $Destination -File -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1) {
    return
}

New-Item -ItemType Directory -Path $Destination -Force | Out-Null

Write-Host "Cloning third-party sources into $Destination"

git clone -c advice.detachedHead=false --revision=83299a7b82ab70b0ff3029a2ba9eaac445e1a3be --depth 1 --no-tags https://github.com/processhacker2/processhacker2.git "$Destination/processhacker"
if ($LASTEXITCODE -ne 0) { throw "Cloning Process Hacker failed with exit code $LASTEXITCODE" }
git -C "$Destination/processhacker" apply --ignore-whitespace --whitespace=nowarn "$PSScriptRoot/ProcessHacker.patch"
if ($LASTEXITCODE -ne 0) { throw "Applying ProcessHacker.patch failed with exit code $LASTEXITCODE" }

git clone -c advice.detachedHead=false --revision=9628c34f00dc25755954ee47383a5a55fed8389d --depth 1 --no-tags https://github.com/nico/demumble.git "$Destination/demumble"
if ($LASTEXITCODE -ne 0) { throw "Cloning demumble failed with exit code $LASTEXITCODE" }

git clone -c advice.detachedHead=false --revision=77c5e43bd2babdfeaf60abc7dd37b3587a147c42 --depth 1 --no-tags https://github.com/llvm-mirror/llvm.git "$Destination/llvm-demangle"
if ($LASTEXITCODE -ne 0) { throw "Cloning LLVM failed with exit code $LASTEXITCODE" }
