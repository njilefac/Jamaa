$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$solutionRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$projectPath = Join-Path $solutionRoot 'Jamaa.Desktop/Jamaa.Desktop.csproj'
$publishDir = Join-Path $solutionRoot 'publish/windows'
$outputFile = Join-Path $PSScriptRoot 'Jamaa-Installer.exe'
$stagingDir = Join-Path $solutionRoot 'bundle/windows-staging'
$installerScript = Join-Path $PSScriptRoot 'package-windows.nsi'
$iconPath = Join-Path $solutionRoot 'Jamaa.Desktop/Assets/Icons/jamaa.ico'

Write-Host "Building Windows package..." -ForegroundColor Yellow

foreach ($path in @($publishDir, $stagingDir, $outputFile)) {
    if (Test-Path $path) {
        Remove-Item -Recurse -Force $path
    }
}

dotnet publish $projectPath -c Release -r win-x64 --self-contained true -p:PublishReadyToRun=false -o $publishDir

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed"
}

New-Item -ItemType Directory -Path $stagingDir | Out-Null
Copy-Item -Path "$publishDir/*" -Destination $stagingDir -Recurse

& makensis `
    "/DAPP_NAME=Jamaa" `
    "/DAPP_VERSION=1.0.0" `
    "/DINPUT_DIR=$stagingDir" `
    "/DOUTPUT_FILE=$outputFile" `
    "/DICON_FILE=$iconPath" `
    $installerScript

if ($LASTEXITCODE -ne 0) {
    throw "NSIS packaging failed"
}

Remove-Item -Recurse -Force $stagingDir
