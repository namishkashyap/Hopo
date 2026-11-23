param(
    [ValidateSet('Core','Desktop','Test','TestLarge','Build','Publish','Format','CheckFormat','Android')][string]$Mode = 'Build',
    [string]$DotnetPath = 'dotnet',
    [string]$BuildRoot = '',
    [string]$LargeSource = ''
)
$ErrorActionPreference = 'Stop'
$lantossSourceRoot = $PSScriptRoot
$lantossReleaseRoot = Split-Path -Parent $lantossSourceRoot
if (-not $BuildRoot) { $BuildRoot = Join-Path $lantossReleaseRoot 'work' }
$lantossBuildRoot = [System.IO.Path]::GetFullPath($BuildRoot)
New-Item -ItemType Directory -Force -Path $lantossBuildRoot | Out-Null
$lantossConfig = Join-Path $lantossSourceRoot 'NuGet.Config'
$lantossArtifacts = Join-Path $lantossBuildRoot 'artifacts'
$lantossOldEnvironment = @{}
$lantossBuildEnvironment = @{
    DOTNET_CLI_HOME = (Join-Path $lantossBuildRoot 'dotnet-home')
    DOTNET_CLI_TELEMETRY_OPTOUT = '1'
    DOTNET_ADD_GLOBAL_TOOLS_TO_PATH = '0'
    DOTNET_NOLOGO = '1'
    NUGET_PACKAGES = (Join-Path $lantossBuildRoot 'packages')
    NUGET_HTTP_CACHE_PATH = (Join-Path $lantossBuildRoot 'nuget-cache')
    APPDATA = (Join-Path $lantossBuildRoot 'build-appdata')
}
foreach ($lantossKey in $lantossBuildEnvironment.Keys) {
    $lantossOldEnvironment[$lantossKey] = [Environment]::GetEnvironmentVariable($lantossKey, 'Process')
    [Environment]::SetEnvironmentVariable($lantossKey, $lantossBuildEnvironment[$lantossKey], 'Process')
}
function Invoke-LantossDotnet {
    param([string[]]$LantossArguments)
    & $DotnetPath @LantossArguments
    if ($LASTEXITCODE -ne 0) { throw "dotnet failed with exit code $LASTEXITCODE." }
}
try {
    $lantossSharedArgs = @('--artifacts-path', $lantossArtifacts, "-p:RestoreConfigFile=$lantossConfig", '--nologo', '-c', 'Release')
    if ($Mode -in @('Format','CheckFormat')) {
        $lantossFormatArgs = @('format', 'whitespace', $lantossSourceRoot, '--folder')
        if ($Mode -eq 'CheckFormat') { $lantossFormatArgs += '--verify-no-changes' }
        Invoke-LantossDotnet $lantossFormatArgs
    }
    elseif ($Mode -eq 'Core') { Invoke-LantossDotnet (@('build', (Join-Path $lantossSourceRoot 'Core\Lantoss.Core.csproj')) + $lantossSharedArgs) }
    elseif ($Mode -eq 'Desktop') { Invoke-LantossDotnet (@('build', (Join-Path $lantossSourceRoot 'Desktop\Lantoss.Desktop.csproj')) + $lantossSharedArgs) }
    elseif ($Mode -in @('Test','TestLarge')) {
        Invoke-LantossDotnet (@('build', (Join-Path $lantossSourceRoot 'tests\Lantoss.Tests.csproj')) + $lantossSharedArgs)
        $lantossTestRoot = Join-Path $lantossBuildRoot ('verification-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
        $lantossTestArgs = @((Join-Path $lantossArtifacts 'bin\Lantoss.Tests\release\Lantoss.Tests.dll'), $lantossTestRoot)
        if ($Mode -eq 'TestLarge') { $lantossTestArgs += '--large' }
        if ($LargeSource) { $lantossTestArgs += @('--large-source', [System.IO.Path]::GetFullPath($LargeSource)) }
        Invoke-LantossDotnet $lantossTestArgs
    }
    elseif ($Mode -eq 'Publish') {
        Invoke-LantossDotnet (@('publish', (Join-Path $lantossSourceRoot 'UI\Lantoss.Windows.csproj'), '-r', 'win-x64', '--self-contained', 'true', '-p:PublishSingleFile=true', '-p:IncludeNativeLibrariesForSelfExtract=true', '-p:EnableCompressionInSingleFile=true', '-p:DebugType=None', '-o', (Join-Path $lantossReleaseRoot 'app')) + $lantossSharedArgs)
        # hopo Admin is the same app published as its own executable; separate artifacts keep the two builds apart.
        Invoke-LantossDotnet (@('publish', (Join-Path $lantossSourceRoot 'UI\Lantoss.Windows.csproj'), '-r', 'win-x64', '--self-contained', 'true', '-p:PublishSingleFile=true', '-p:IncludeNativeLibrariesForSelfExtract=true', '-p:EnableCompressionInSingleFile=true', '-p:DebugType=None', '-p:HopoAdmin=true', '-o', (Join-Path $lantossReleaseRoot 'app')) + ($lantossSharedArgs -replace [regex]::Escape($lantossArtifacts), ($lantossArtifacts + '-admin')))
    }
    elseif ($Mode -eq 'Android') {
        # The Android SDK and Java are downloaded once into work\ (see the Android change record); after that the APK builds offline.
        # The signing key is made once in work\android-keystore. Back it up: Android only installs updates signed with the same key.
        $lantossSdk = Join-Path $lantossBuildRoot 'android-sdk'; $lantossJdk = Join-Path $lantossBuildRoot 'jdk'; $lantossKeys = Join-Path $lantossBuildRoot 'android-keystore'
        $lantossKeyStore = Join-Path $lantossKeys 'hopo-release.keystore'; $lantossSecret = Join-Path $lantossKeys 'password.txt'
        if (-not (Test-Path $lantossKeyStore)) {
            New-Item -ItemType Directory -Force -Path $lantossKeys | Out-Null
            $lantossRandom = New-Object byte[] 24
            [System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($lantossRandom)
            Set-Content -Path $lantossSecret -Value ([Convert]::ToBase64String($lantossRandom) -replace '[+/=]', 'x') -NoNewline -Encoding ascii
        }
        $env:HOPO_ANDROID_KEY = (Get-Content -Raw $lantossSecret).Trim()
        try {
            if (-not (Test-Path $lantossKeyStore)) {
                # keytool reports progress on stderr, which must not stop the script.
                $ErrorActionPreference = 'Continue'
                & (Join-Path $lantossJdk 'bin\keytool.exe') -genkeypair -keystore $lantossKeyStore -alias hopo -keyalg RSA -keysize 4096 -validity 10000 -storepass:env HOPO_ANDROID_KEY -keypass:env HOPO_ANDROID_KEY -dname 'CN=hopo, O=hopo' 2>&1 | Out-Null
                $ErrorActionPreference = 'Stop'
                if ($LASTEXITCODE -ne 0) { throw 'keytool could not create the Android signing key.' }
            }
            Invoke-LantossDotnet (@('publish', (Join-Path $lantossSourceRoot 'Android\Lantoss.Android.csproj'), '-f', 'net10.0-android', "-p:AndroidSdkDirectory=$lantossSdk", "-p:JavaSdkDirectory=$lantossJdk", '-p:AndroidKeyStore=true', "-p:AndroidSigningKeyStore=$lantossKeyStore", '-p:AndroidSigningKeyAlias=hopo', '-p:AndroidSigningKeyPass=env:HOPO_ANDROID_KEY', '-p:AndroidSigningStorePass=env:HOPO_ANDROID_KEY', '-o', (Join-Path $lantossBuildRoot 'android-release')) + ($lantossSharedArgs -replace [regex]::Escape($lantossArtifacts), ($lantossArtifacts + '-android')))
        }
        finally { Remove-Item Env:HOPO_ANDROID_KEY -ErrorAction SilentlyContinue }
    }
    else { Invoke-LantossDotnet (@('build', (Join-Path $lantossSourceRoot 'UI\Lantoss.Windows.csproj')) + $lantossSharedArgs) }
}
finally {
    foreach ($lantossKey in $lantossOldEnvironment.Keys) { [Environment]::SetEnvironmentVariable($lantossKey, $lantossOldEnvironment[$lantossKey], 'Process') }
}
