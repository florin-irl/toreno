# Publishes a self-contained Release build and compiles the Inno Setup installer.
# Requires Inno Setup 6 (https://jrsoftware.org/isinfo.php, or `winget install JRSoftware.InnoSetup`).

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repoRoot
try {
    dotnet publish src/Toreno/Toreno.csproj -c Release -r win-x64 --self-contained true `
        -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
        -o publish/win-x64

    $iscc = Get-Command ISCC.exe -ErrorAction SilentlyContinue
    if (-not $iscc) {
        $candidate = "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
        if (Test-Path $candidate) {
            $iscc = $candidate
        } else {
            throw "ISCC.exe not found. Install Inno Setup 6 first."
        }
    }

    & $iscc "installer\Toreno.iss"
}
finally {
    Pop-Location
}
