$ErrorActionPreference = 'Stop'
if (Get-Process ArcGISPro -ErrorAction SilentlyContinue) { throw 'Close ArcGIS Pro normally before installing, then run this script again.' }
$bundle = Join-Path $PSScriptRoot 'ArcGISProMcpExtended.esriAddInX'
if (-not (Test-Path -LiteralPath $bundle)) { throw 'Run Install.ps1 from the extracted runtime release folder.' }
$destination = Join-Path ([Environment]::GetFolderPath('MyDocuments')) 'ArcGIS/AddIns/ArcGISPro/{72dd2cd1-b63c-4c68-bab4-4ef6c41717fa}'
$target = Join-Path $destination 'ArcGISProMcpExtended.esriAddInX'
New-Item -ItemType Directory -Force -Path $destination | Out-Null
if (Test-Path -LiteralPath $target) {
    $backupDirectory = Join-Path ([Environment]::GetFolderPath('UserProfile')) '.arcgis-mcp-extended/backups'
    New-Item -ItemType Directory -Force -Path $backupDirectory | Out-Null
    Copy-Item -LiteralPath $target -Destination (Join-Path $backupDirectory ((Get-Date -Format 'yyyyMMdd-HHmmss') + '-ArcGISProMcpExtended.esriAddInX'))
}
Copy-Item -LiteralPath $bundle -Destination $target -Force
Write-Host "Installed: $target"
Write-Host 'Open a disposable saved Pro project and click Add-In > MCP Extended Hardened > Start / stop extended bridge.'
Write-Host 'Configure your MCP client using CLIENT-SETUP.md. The previous small bridge has a separate identity.'
