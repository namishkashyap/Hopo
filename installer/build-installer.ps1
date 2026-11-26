param(
    [string]$DotnetPath = 'dotnet',
    [string]$BuildRoot = '',
    [switch]$SkipPublish,
    # The Windows 11 menu tools and private signing key (see SETUP.md); by default under work\.
    [string]$CompilerPath = '',
    [string]$SdkToolsPath = '',
    [string]$SigningCertificatePath = ''
)
$ErrorActionPreference = 'Stop'
$lantossRelease = Split-Path -Parent $PSScriptRoot
if (-not $BuildRoot) { $BuildRoot = Join-Path $lantossRelease 'work' }
$lantossBuild = [IO.Path]::GetFullPath($BuildRoot)
New-Item -ItemType Directory -Force -Path $lantossBuild | Out-Null
# Build the Windows 11 menu first: it writes the public certificate that the app embeds.
if (-not $CompilerPath) { $CompilerPath = Join-Path $lantossBuild 'explorer-tools\w64devkit\bin\g++.exe' }
if (-not $SdkToolsPath) { $SdkToolsPath = Join-Path $lantossBuild 'explorer-tools\sdk' }
if (-not $SigningCertificatePath) { $SigningCertificatePath = Join-Path $lantossBuild 'explorer-signing\hopo-explorer.pfx' }
$lantossKeyPassword = Join-Path (Split-Path -Parent $SigningCertificatePath) 'password.txt'
if (-not $env:LANTOSS_PFX_PASSWORD -and (Test-Path -LiteralPath $lantossKeyPassword)) { $env:LANTOSS_PFX_PASSWORD = [IO.File]::ReadAllText($lantossKeyPassword).Trim() }
& (Join-Path $lantossRelease 'source\Explorer\build-explorer.ps1') -CompilerPath $CompilerPath -SdkToolsPath $SdkToolsPath -SigningCertificatePath $SigningCertificatePath -NoTimestamp -RunTests -BuildRoot (Join-Path $lantossBuild 'explorer')
if (-not $SkipPublish) {
    & (Join-Path $lantossRelease 'source\build.ps1') -Mode Publish -DotnetPath $DotnetPath -BuildRoot $lantossBuild
    if ($LASTEXITCODE) { throw 'App publication failed.' }
}
foreach ($lantossPayload in @('LantossExplorer.dll','hopoUsers.Explorer.msix','hopoAdmin.Explorer.msix','RegisterPackage.ps1','Assets\Logo.png','Assets\SmallLogo.png')) {
    if (-not (Test-Path -LiteralPath (Join-Path $lantossRelease ('app\'+$lantossPayload)))) { throw ('The Windows 11 menu build is missing '+$lantossPayload) }
}
Copy-Item -LiteralPath (Join-Path $lantossRelease 'source\Explorer\RegisterPackage.ps1') -Destination (Join-Path $lantossRelease 'app\RegisterPackage.ps1') -Force
foreach ($lantossExe in @('hopoUsers.exe','hopoAdmin.exe')) {
    if ((Get-Item (Join-Path $lantossRelease ('app\'+$lantossExe))).VersionInfo.FileVersion -ne '2.6.9.0') { throw ('Publish '+$lantossExe+' 2.6.9 first.') }
}

# Build-only tools are pinned and hash checked. No compiler or installer is installed on the host.
$lantossTools = Join-Path $lantossBuild 'installer-tools'
New-Item -ItemType Directory -Force -Path $lantossTools | Out-Null
$lantossPackages = @(
    @{Name='wix';SHA256='A94DD42AE1FB56B32DA180E2173CEDA4F0D10B4C8871C5EE59ECB502131A1EB6'},
    @{Name='wixtoolset.ui.wixext';SHA256='E92C4DDAC5D17F5360291AB856C676E19FD92AF14DAFFB5D5D4B8E1E9C716B47'}
)
foreach ($lantossPackage in $lantossPackages) {
    $lantossArchive = Join-Path $lantossTools ($lantossPackage.Name+'.zip')
    if (-not (Test-Path -LiteralPath $lantossArchive)) {
        Invoke-WebRequest -Uri ('https://api.nuget.org/v3-flatcontainer/'+$lantossPackage.Name+'/4.0.6/'+$lantossPackage.Name+'.4.0.6.nupkg') -OutFile $lantossArchive
    }
    if ((Get-FileHash -LiteralPath $lantossArchive -Algorithm SHA256).Hash -ne $lantossPackage.SHA256) { throw ('Installer tool hash mismatch: '+$lantossPackage.Name) }
    $lantossExpanded = Join-Path $lantossTools $lantossPackage.Name
    # Always extract afresh from the verified archive so a modified earlier extraction is never reused.
    if (Test-Path -LiteralPath $lantossExpanded) { Remove-Item -LiteralPath $lantossExpanded -Recurse -Force }
    Expand-Archive -LiteralPath $lantossArchive -DestinationPath $lantossExpanded
}
$lantossWix = Join-Path $lantossTools 'wix\tools\net6.0\any\wix.dll'
$lantossUI = Join-Path $lantossTools 'wixtoolset.ui.wixext\wixext4\WixToolset.UI.wixext.dll'
# One setup per app, so each person installs only hopo Users or hopo Admin.
foreach ($lantossApp in @('Users','Admin')) {
    $lantossMsi = Join-Path $lantossRelease ('hopo-'+$lantossApp+'-Setup-2.6.9-x64.msi')
    & $DotnetPath $lantossWix build (Join-Path $PSScriptRoot 'Lantoss.wxs') -arch x64 -ext $lantossUI -d ('ReleaseDir='+$lantossRelease) -d ('App='+$lantossApp) -out $lantossMsi -intermediatefolder (Join-Path $lantossBuild ('installer-objects-'+$lantossApp)) -pdbtype none -wx
    if ($LASTEXITCODE) { throw 'MSI compilation or validation failed.' }
    Get-FileHash -LiteralPath $lantossMsi -Algorithm SHA256
}
