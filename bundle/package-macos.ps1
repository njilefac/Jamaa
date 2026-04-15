# --- Static Configuration ---
$solutionRoot = Get-Location
$projectPath  = "$solutionRoot/Jamaa.Desktop/Jamaa.Desktop.csproj"
$projectName  = "Jamaa.Desktop"  # The binary name produced by the project
$bundleName   = "Jamaa"
$identifier   = "com.nubiasystems.jamaa"
$runtime      = "osx-x64"       # Intel-based Mac
$iconPath     = "$solutionRoot/Jamaa.Desktop/Assets/Icons/jamaa.icns"

# Output Paths
$publishDir   = "$solutionRoot/publish"
$appPath      = "$solutionRoot/$bundleName.app"
$dmgName      = "$bundleName-Installer.dmg"

Write-Host "🏗️  Starting build for $bundleName ($runtime)..." -ForegroundColor Yellow

# 1. Clean up old builds
if (Test-Path $publishDir) { Remove-Item -Recurse -Force $publishDir }
if (Test-Path $appPath) { Remove-Item -Recurse -Force $appPath }
if (Test-Path $dmgName) { Remove-Item -Force $dmgName }

# 2. Build Project
Write-Host "🚀 Publishing .NET project..." -ForegroundColor Cyan
dotnet publish $projectPath -c Release -r $runtime --self-contained true -p:UseAppHost=true -p:PublishReadyToRun=false -o $publishDir

if ($LASTEXITCODE -ne 0) {
    Write-Error "Dotnet publish failed. Verify the project path: $projectPath"
    exit
}

# 3. Create Bundle Structure
Write-Host "📂 Creating .app bundle structure..." -ForegroundColor Cyan
$macosFolder = New-Item -ItemType Directory -Path "$appPath/Contents/MacOS" -Force
$resFolder = New-Item -ItemType Directory -Path "$appPath/Contents/Resources" -Force

# 4. Move Files
Write-Host "📦 Moving binaries to bundle..." -ForegroundColor Cyan
Copy-Item -Path "$publishDir/*" -Destination "$macosFolder" -Recurse

# 5. Handle Icon
$iconEntry = ""
if (Test-Path $iconPath) {
    Write-Host "🎨 Adding application icon..." -ForegroundColor Cyan
    Copy-Item -Path $iconPath -Destination "$resFolder/icon.icns"
    $iconEntry = "<key>CFBundleIconFile</key>`n    <string>icon.icns</string>"
} else {
    Write-Warning "Icon not found at $iconPath - skipping icon step."
}

# 6. Create Info.plist
Write-Host "📝 Generating Info.plist..." -ForegroundColor Cyan
$plistContent = @"
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleExecutable</key>
    <string>Jamaa</string>
    <key>CFBundleIdentifier</key>
    <string>$identifier</string>
    <key>CFBundleName</key>
    <string>$bundleName</string>
    <key>CFBundlePackageType</key>        
    <string>APPL</string>
    <key>CFBundleShortVersionString</key>
    <string>1.0</string>
    <key>LSMinimumSystemVersion</key>
    <string>10.12</string>
    <key>NSHighResolutionCapable</key>
    <true/>
    $iconEntry
</dict>
</plist>
"@
[System.IO.File]::WriteAllText("$appPath/Contents/Info.plist", $plistContent)

# 7. macOS Post-Processing
if ($IsMacOS) {
    Write-Host "🛡️  Fixing permissions and clearing quarantine..." -ForegroundColor Cyan
    
    $binaryPath = "$appPath/Contents/MacOS/Jamaa"
    if (Test-Path $binaryPath) {
        chmod +x "$binaryPath"
    }

    # Clear quarantine flags and ad-hoc sign
    xattr -rd com.apple.quarantine "$appPath" 2>$null
    codesign --force --deep --sign - "$appPath"

    # 8. Create DMG
    Write-Host "💿 Creating DMG installer..." -ForegroundColor Green
    $dmgStage = "$solutionRoot/dmg_stage"
    if (Test-Path $dmgStage) { Remove-Item -Recurse -Force $dmgStage }
    New-Item -ItemType Directory -Path $dmgStage | Out-Null
    Copy-Item -Path $appPath -Destination "$dmgStage/" -Recurse
    
    # Create the Applications symlink
    ln -s /Applications "$dmgStage/Applications"
    
    # Generate DMG
    hdiutil create -volname "$bundleName" -srcfolder $dmgStage -ov -format UDZO $dmgName
    
    Remove-Item -Recurse -Force $dmgStage
    Write-Host "`n✅ Success! DMG created: $dmgName" -ForegroundColor Green
} else {
    Write-Warning "`n⚠️  Run on macOS to complete Signing and DMG creation."
    Write-Host "✅ .app bundle prepared at $appPath" -ForegroundColor Green
}