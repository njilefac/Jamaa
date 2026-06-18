param (
    [string]$Version = "1.0.0"
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$solutionRoot = "$PSScriptRoot/.."
$projectPath = Join-Path $solutionRoot 'Jamaa.Desktop/Jamaa.Desktop.csproj'
$publishDir = Join-Path $solutionRoot 'publish/windows'
$outputFile = Join-Path $PSScriptRoot "Jamaa-Installer-$Version.exe"
$stagingDir = Join-Path $solutionRoot 'bundle/windows-staging'
$installerScript = Join-Path $PSScriptRoot 'package-windows.nsi'
$iconPath = Join-Path $solutionRoot 'Jamaa.Desktop/Assets/Icons/jamaa.ico'

Write-Host "Building Windows package..." -ForegroundColor Yellow

foreach ($path in @($publishDir, $stagingDir, $outputFile)) {
    if (Test-Path $path) {
        Remove-Item -Recurse -Force $path
    }
}

Write-Host "🚀 Publishing .NET project..." -ForegroundColor Cyan
dotnet publish $projectPath -c Release -r win-x64 --self-contained true -p:PublishReadyToRun=false -o $publishDir -p:Version=$Version

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed"
}

Write-Host "📦 Creating staging directory and copying binaries..." -ForegroundColor Cyan
New-Item -ItemType Directory -Path $stagingDir | Out-Null
Copy-Item -Path "$publishDir/*" -Destination $stagingDir -Recurse

Write-Host "🏗️ Starting NSIS packaging..." -ForegroundColor Yellow
# Check if makensis is available
$makensis = Get-Command makensis -ErrorAction SilentlyContinue
if (-not $makensis) {
    Write-Host "makensis (NSIS) not found in PATH. Attempting to install via Chocolatey..." -ForegroundColor Yellow
    $choco = Get-Command choco -ErrorAction SilentlyContinue
    if ($choco) {
        & choco install nsis -y
        if ($LASTEXITCODE -eq 0) {
            # Refresh PATH for the current process
            $env:Path = [System.Environment]::GetEnvironmentVariable("Path","Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path","User")
            $makensis = Get-Command makensis -ErrorAction SilentlyContinue
            if (-not $makensis) {
                # Fallback to common NSIS installation path
                $nsisPath = "C:\Program Files (x86)\NSIS\makensis.exe"
                if (Test-Path $nsisPath) {
                    $makensis = Get-Command $nsisPath
                }
            }
        }
    }
}

if (-not $makensis) {
    throw "makensis (NSIS) not found in PATH and could not be installed. Installer creation failed."
}

& $makensis.Source /V4 `
    "/DAPP_NAME=Jamaa" `
    "/DAPP_VERSION=$Version" `
    "/DINPUT_DIR=$stagingDir" `
    "/DOUTPUT_FILE=$outputFile" `
    "/DICON_FILE=$iconPath" `
    $installerScript

if ($LASTEXITCODE -ne 0) {
    throw "NSIS packaging failed"
}

Write-Host "✅ Success! Installer created at: $outputFile" -ForegroundColor Green
Remove-Item -Recurse -Force $stagingDir
