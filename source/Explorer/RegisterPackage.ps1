param(
    [ValidateSet('Install','Remove')][string]$Mode,
    [Parameter(Mandatory)][string]$AppDirectory,
    [ValidateSet('Lantoss.ExplorerIntegration','Lantoss.ExplorerIntegration.Admin')][string]$Name = 'Lantoss.ExplorerIntegration',
    [ValidateSet('hopoUsers.Explorer.msix','hopoAdmin.Explorer.msix')][string]$Package = 'hopoUsers.Explorer.msix'
)
$ErrorActionPreference = 'Stop'
try {
    # Paths arrive as arguments, never interpolated into executable PowerShell text.
    $root = [IO.Path]::GetFullPath($AppDirectory)
    $registered = @(Get-AppxPackage -Name $Name | Where-Object { $_.Publisher -eq 'CN=Lantoss Development' })
    if ($Mode -eq 'Remove') {
        foreach ($package in $registered) { Remove-AppxPackage -Package $package.PackageFullName -ErrorAction Stop }
    } elseif ($registered.Count -eq 1 -and $registered[0].Version -eq '2.6.3.0') {
        # The same version: register in place without tearing down a working package on error.
        Add-AppxPackage -Register (Join-Path $registered[0].InstallLocation 'AppxManifest.xml') -DisableDevelopmentMode -ExternalLocation $root -ErrorAction Stop
    } else {
        # Older packages (hopo 2.3 and earlier start an app that no longer exists) are replaced.
        foreach ($package in $registered) { Remove-AppxPackage -Package $package.PackageFullName -ErrorAction Stop }
        Add-AppxPackage -Path (Join-Path $root $Package) -ExternalLocation $root -ErrorAction Stop
    }
    exit 0
} catch {
    Write-Error $_ -ErrorAction Continue
    exit 1
}
