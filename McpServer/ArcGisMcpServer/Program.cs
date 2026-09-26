using ArcGisMcpServer.Ipc;
using ArcGisMcpServer.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
if (args.Length > 0 || Environment.GetEnvironmentVariable("MCP_TRANSPORT") is { Length: > 0 } transport && transport != "stdio")
    throw new InvalidOperationException("This hardened build supports local stdio only. No HTTP listener is included.");
var builder = Host.CreateApplicationBuilder();
builder.Logging.ClearProviders();
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);
ProTools.Configure(new BridgeClient());
builder.Services.AddMcpServer().WithStdioServerTransport().WithTools<ProTools>();
await builder.Build().RunAsync();
