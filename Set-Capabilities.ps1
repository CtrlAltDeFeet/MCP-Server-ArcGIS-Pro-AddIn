param(
    [ValidateSet('Inspect','View','Cartography','Export','Editing','Automation','Python')]
    [string[]]$Capabilities = @('Inspect','View','Cartography','Export'),
    [string]$OutputRoot = (Join-Path ([Environment]::GetFolderPath('MyDocuments')) 'ArcGISMcpOutputs')
)
$ErrorActionPreference = 'Stop'
if (-not [IO.Path]::IsPathRooted($OutputRoot) -or $OutputRoot.StartsWith('\\')) { throw 'Choose an absolute local output directory.' }
$policyDirectory = Join-Path ([Environment]::GetFolderPath('UserProfile')) '.arcgis-mcp-extended'
New-Item -ItemType Directory -Force -Path $policyDirectory | Out-Null
$policyPath = Join-Path $policyDirectory 'policy.json'
if (Test-Path -LiteralPath $policyPath) { Copy-Item -LiteralPath $policyPath -Destination ($policyPath + '.backup') -Force }
$policyText = @{ Capabilities=@($Capabilities | Select-Object -Unique); OutputRoot=[IO.Path]::GetFullPath($OutputRoot) } | ConvertTo-Json -Depth 3
[IO.File]::WriteAllText($policyPath, $policyText)
Write-Host "Saved policy: $policyPath"
Write-Host 'Stop and restart the extended bridge in Pro to apply it.'
if ($Capabilities -contains 'Automation' -or $Capabilities -contains 'Python') {
    Write-Host 'Automation and Python permit unrestricted code execution with your Windows account permissions. This is not sandboxed geoprocessing.'
}
