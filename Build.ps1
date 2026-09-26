param([switch]$SkipRestore)
$ErrorActionPreference = 'Stop'
Push-Location -LiteralPath $PSScriptRoot
try {
$env:DOTNET_CLI_HOME = $PSScriptRoot
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
$projects = @('McpServer/ArcGisMcpServer/ArcGisMcpServer.csproj','AddIn/APBridgeAddIn/APBridgeAddIn.csproj','tests/BridgeTests/BridgeTests.csproj','tests/AtbxTests/AtbxTests.csproj')
foreach ($project in $projects) {
    if (-not $SkipRestore) {
        dotnet restore $project --locked-mode --configfile NuGet.Config --ignore-failed-sources -v minimal
        if ($LASTEXITCODE -ne 0) { throw "Restore failed: $project" }
    }
    dotnet build $project --no-restore -c Release -v minimal
    if ($LASTEXITCODE -ne 0) { throw "Build failed: $project" }
}
dotnet publish $projects[0] --no-build --no-restore -c Release -o dist/server
if ($LASTEXITCODE -ne 0) { throw 'Publish failed' }
New-Item -ItemType Directory -Force -Path review | Out-Null
dotnet run --project $projects[2] --no-build --no-restore -c Release -- (Join-Path $PSScriptRoot 'dist/server/ArcGisMcpServer.exe') | Tee-Object -FilePath review/hardening-tests.txt
if ($LASTEXITCODE -ne 0) { throw 'Hardening tests failed' }
dotnet run --project $projects[3] --no-build --no-restore -c Release | Tee-Object -FilePath review/modelbuilder-tests.txt
if ($LASTEXITCODE -ne 0) { throw 'ModelBuilder tests failed' }
Write-Host 'Build and offline tests passed. Package.ps1 creates the portable artifacts; it does not install.'
} finally { Pop-Location }
