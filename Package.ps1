param([string]$ReleaseName = ('ArcGISProMCP-Extended-Hardened-Pro37-' + (Get-Date -Format 'yyyyMMdd-HHmmss')), [switch]$StageOnly)
$ErrorActionPreference = 'Stop'
Push-Location -LiteralPath $PSScriptRoot
try {
if ($ReleaseName -notmatch '^[A-Za-z0-9-]+$') { throw 'ReleaseName must contain only letters, digits, and hyphens.' }
$releaseParent = Join-Path (Split-Path -Parent $PSScriptRoot) 'Releases'
$release = Join-Path $releaseParent $ReleaseName
if (Test-Path -LiteralPath $release) { throw 'Release directory already exists; choose a new name.' }
$assembly = 'AddIn/APBridgeAddIn/bin/Release/net10.0-windows/ArcGISProMcpExtendedAddIn.dll'
if (-not (Test-Path -LiteralPath $assembly)) { throw 'Run Build.ps1 first.' }
New-Item -ItemType Directory -Force -Path $release | Out-Null
Copy-Item -LiteralPath 'dist/server' -Destination (Join-Path $release 'server') -Recurse
Get-ChildItem -LiteralPath (Join-Path $release 'server') -Filter '*.pdb' -File | Remove-Item
$stage = Join-Path $PSScriptRoot ('dist/addin-stage-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path (Join-Path $stage 'Install') | Out-Null
Copy-Item -LiteralPath $assembly -Destination (Join-Path $stage 'Install')
Copy-Item -LiteralPath 'AddIn/APBridgeAddIn/Config.daml' -Destination $stage
foreach ($folder in @('Images','DarkImages')) { Copy-Item -LiteralPath (Join-Path 'AddIn/APBridgeAddIn' $folder) -Destination $stage -Recurse }
[IO.Compression.ZipFile]::CreateFromDirectory($stage, (Join-Path $release 'ArcGISProMcpExtended.esriAddInX'))
foreach ($file in @('README.md','CHANGELOG.md','CLIENT-SETUP.md','CONTRIBUTING.md','GITHUB-PUBLISHING.md','VALIDATION.md','SECURITY.md','Install.ps1','Set-Capabilities.ps1','Test-LiveReadOnly.ps1','capabilities.json','LICENSE','UPSTREAM-LICENSE','THIRD-PARTY-NOTICES.md','PROVENANCE.md')) {
    Copy-Item -LiteralPath $file -Destination $release
}
Copy-Item -LiteralPath 'ThirdPartyLicenses' -Destination (Join-Path $release 'ThirdPartyLicenses') -Recurse
Copy-Item -LiteralPath 'docs' -Destination (Join-Path $release 'docs') -Recurse
$manifest = Get-ChildItem -LiteralPath $release -Recurse -File | ForEach-Object {
    [ordered]@{ Path=[IO.Path]::GetRelativePath($release,$_.FullName); SHA256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash }
}
$manifest | ConvertTo-Json -Depth 3 | Set-Content -LiteralPath (Join-Path $release 'SHA256SUMS.json')
if (-not $StageOnly) {
    [IO.Compression.ZipFile]::CreateFromDirectory($release, ($release + '.zip'))
    Get-FileHash -LiteralPath ($release + '.zip') -Algorithm SHA256 | Format-List
}
Write-Host "Runtime release: $release"
} finally { Pop-Location }
