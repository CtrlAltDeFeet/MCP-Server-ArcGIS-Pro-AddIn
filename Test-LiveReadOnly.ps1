param([Parameter(Mandatory=$true)][string]$ProjectPath, [string]$ServerPath)
$ErrorActionPreference = 'Stop'
if (-not [IO.Path]::IsPathRooted($ProjectPath)) { throw 'Use an absolute project path.' }
if (-not $ServerPath) {
    $ServerPath = Join-Path $PSScriptRoot 'server/ArcGisMcpServer.exe'
    if (-not (Test-Path -LiteralPath $ServerPath)) { $ServerPath = Join-Path $PSScriptRoot 'dist/server/ArcGisMcpServer.exe' }
}
$start = New-Object System.Diagnostics.ProcessStartInfo
$start.FileName = $ServerPath
$start.UseShellExecute = $false
$start.CreateNoWindow = $true
$start.RedirectStandardInput = $true
$start.RedirectStandardOutput = $true
$start.RedirectStandardError = $true
$start.EnvironmentVariables['ARCGIS_PROJECT'] = [IO.Path]::GetFullPath($ProjectPath)
$start.EnvironmentVariables['MCP_TRANSPORT'] = 'stdio'
$process = [Diagnostics.Process]::Start($start)
$stderrTask = $process.StandardError.ReadToEndAsync()
$script:rpcId = 0
$results = [Collections.Generic.List[object]]::new()
function Invoke-McpRpc([string]$Method, $Parameters) {
    $script:rpcId++
    $requestId = $script:rpcId
    $process.StandardInput.WriteLine((@{jsonrpc='2.0';id=$requestId;method=$Method;params=$Parameters} | ConvertTo-Json -Depth 12 -Compress))
    $process.StandardInput.Flush()
    do {
        $readTask = $process.StandardOutput.ReadLineAsync()
        if (-not $readTask.Wait(30000)) { throw "Timed out reading MCP response for $Method" }
        if ($null -eq $readTask.Result) { throw 'MCP server exited unexpectedly.' }
        $response = $readTask.Result | ConvertFrom-Json
    } while ($response.id -ne $requestId)
    if ($response.error) { throw ($response.error | ConvertTo-Json -Compress) }
    return $response.result
}
try {
    $null = Invoke-McpRpc 'initialize' @{protocolVersion='2025-06-18';capabilities=@{};clientInfo=@{name='extended-readonly-smoke';version='1.0'}}
    $process.StandardInput.WriteLine('{"jsonrpc":"2.0","method":"notifications/initialized"}')
    foreach ($name in @('get_capabilities','list_bridges','get_project_info','get_active_map_name','list_layers')) {
        $result = Invoke-McpRpc 'tools/call' @{name=$name;arguments=@{}}
        if ($result.isError) { throw ($result.content | ConvertTo-Json -Depth 8) }
        $results.Add(@{tool=$name;result=$result})
        Write-Host "PASS $name"
    }
    $resultPath = Join-Path $PSScriptRoot 'live-readonly-results.json'
    $results | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $resultPath
    Write-Host "Saved $resultPath. These checks did not request GIS edits or exports."
} finally {
    $process.StandardInput.Close()
    if (-not $process.WaitForExit(5000)) { $process.Kill() }
    $process.Dispose()
}
