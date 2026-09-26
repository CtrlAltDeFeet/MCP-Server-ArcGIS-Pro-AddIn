using ArcGisBridge;
using ArcGisMcpServer.Ipc;
using System.Buffers.Binary;
using System.Diagnostics;
using System.IO.Pipes;
using System.Text.Json;

try
{
int passed = 0;
void Check(bool condition, string name)
{ if (!condition) throw new Exception("FAIL: " + name); Console.WriteLine("PASS: " + name); passed++; }
void Denied(Action action, string name)
{ bool denied = false; try { action(); } catch { denied = true; } Check(denied, name); }
var temp = Path.Combine(Path.GetTempPath(), "arcgis-extended-tests-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(temp);
var policy = new BridgePolicy { OutputRoot = temp };
policy.Authorize("pro.listFields", null); Check(true, "schema read allowed by default");
policy.Authorize("pro.setLayerRenderer", new()); Check(true, "cartography allowed by default");
foreach (var op in new[] { "pro.deleteFeatures", "pro.runGPTool", "pro.runModel", "pro.executePython", "pro.createProject" })
    Denied(() => policy.Authorize(op, new()), "default denies " + op);
Denied(() => policy.Authorize("pro.notRegistered", new()), "unknown operations fail closed");
Denied(() => BridgePolicy.Parse("{bad json"), "malformed policy fails closed");
Denied(() => BridgePolicy.Parse("{\"Capabilities\":[\"All\"]}"), "unknown capability fails closed");
Denied(() => BridgePolicy.Parse("{\"EnablePython\":true}"), "unknown policy properties fail closed");
Denied(() => BridgePolicy.Parse("{\"Capabilities\":null}"), "null capabilities fail closed");
var opted = new BridgePolicy { Capabilities = ["Editing"] }; opted.Authorize("pro.deleteFeatures", new());
Check(true, "explicit Editing opt-in permits edits");
Denied(() => opted.Authorize("pro.executePython", new()), "Editing does not enable Python");
Check(policy.ValidateOutput(Path.Combine(temp, "new.png")).StartsWith(temp), "new output in root accepted");
Denied(() => policy.ValidateOutput(Path.Combine(temp, "..", "outside.png")), "output traversal rejected");
Denied(() => policy.ValidateOutput(temp + "-sibling/file.png"), "root-prefix sibling rejected");
Denied(() => policy.ValidateOutput("relative.png"), "relative output rejected");
Denied(() => policy.ValidateOutput("\\\\server\\share\\file.png"), "UNC output rejected");
Denied(() => policy.ValidateOutput(Path.Combine(temp, "file.png:stream")), "alternate stream rejected");
Denied(() => policy.ValidateOutput(Path.Combine(temp, "NUL.png")), "DOS device filename rejected");
Denied(() => policy.ValidateOutput(Path.Combine(temp, "folder. ", "file.png")), "trailing-dot/space alias rejected");
File.WriteAllText(Path.Combine(temp, "exists.png"), "keep");
Denied(() => policy.ValidateOutput(Path.Combine(temp, "exists.png")), "output overwrite rejected");
Check(File.ReadAllText(Path.Combine(temp, "exists.png")) == "keep", "existing output preserved");
var junction = Path.Combine(temp, "junction");
var target = Path.Combine(temp, "target"); Directory.CreateDirectory(target);
var junctionStart = new ProcessStartInfo("powershell.exe") { UseShellExecute=false, CreateNoWindow=true, RedirectStandardOutput=true, RedirectStandardError=true };
foreach (var argument in new[] { "-NoProfile", "-NonInteractive", "-Command", $"New-Item -ItemType Junction -Path '{junction.Replace("'", "''")}' -Target '{target.Replace("'", "''")}' | Out-Null" }) junctionStart.ArgumentList.Add(argument);
using (var createJunction = Process.Start(junctionStart)!)
{
    await createJunction.WaitForExitAsync();
    Check(createJunction.ExitCode == 0, "junction fixture created without elevation");
}
Denied(() => policy.ValidateOutput(Path.Combine(junction, "escape.png")), "junction/reparse output rejected");
var captures = new Dictionary<string,string>(); policy.Authorize("pro.captureMapView", captures);
Check(captures["output"].StartsWith(temp), "default capture placed in output root");

var projects = new[] { new Project(@"C:\A\Map.aprx", "Map"), new Project(@"C:\B\Map.aprx", "Map") };
Check(ProjectRouting.Select(projects, p => p.Path, p => p.Name, @"C:\A\Map.aprx") == projects[0], "full-path pin distinguishes identical filenames");
Denied(() => ProjectRouting.Select(projects, p => p.Path, p => p.Name, "Map"), "ambiguous bare pin rejected");
Denied(() => ProjectRouting.Select(projects, p => p.Path, p => p.Name, null), "unconfigured multi-project routing rejected");
Check(ProjectRouting.Select(projects, p => p.Path, p => p.Name, @"C:\Missing\Map.aprx") == null, "missing full-path pin never falls back");
Denied(() => ProjectRouting.RequireSameProject(projects[0].Path, projects[1].Path), "project switch rejected at dispatch");
Denied(() => ProjectRouting.RequireUniqueName(["Roads", "roads"], "Roads", "layer"), "case-insensitive duplicate layers rejected");

async Task<NamedPipeClientStream> Connect(string name)
{
    var pipe = new NamedPipeClientStream(".", name, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
    await pipe.ConnectAsync(3000); return pipe;
}
async Task<Reply> Call(string pipeName, string op = "read")
{
    using var pipe = await Connect(pipeName); using var deadline = new CancellationTokenSource(5000);
    await BridgeProtocol.WriteAsync(pipe, new Request(op), BridgeProtocol.MaxRequestBytes, deadline.Token);
    return await BridgeProtocol.ReadAsync<Reply>(pipe, BridgeProtocol.MaxResponseBytes, deadline.Token);
}
string name = "ArcGIS-Extended-Test-" + Guid.NewGuid().ToString("N");
int dispatched = 0;
using (var server = new BridgeServer<Request,Reply>(name, async (r, ct) =>
{
    Interlocked.Increment(ref dispatched);
    if (r.Op == "throw") throw new InvalidOperationException("handler failed");
    if (r.Op == "slow") await Task.Delay(350, ct);
    return new(true, r.Op == "big" ? new string('a', BridgeProtocol.MaxResponseBytes + 1) : "done");
}, error => new(false, error), TimeSpan.FromMilliseconds(150)))
{
    server.Start(); Check((await Call(name)).Ok, "bounded transport roundtrip");
    Check((await Call(name, "slow")).Ok, "operation may exceed idle deadline after request is read");
    Check(!(await Call(name, "throw")).Ok && (await Call(name)).Ok, "handler exceptions do not kill listener");
    Check(!(await Call(name, "big")).Ok && (await Call(name)).Ok, "oversized response rejected and listener recovers");
    using (var idle = await Connect(name))
    { await Task.Delay(300); Check((await Call(name)).Ok, "idle client disconnected; subsequent request succeeds"); }
    foreach (var length in new[] { int.MaxValue, -1, 0 })
    {
        using (var pipe = await Connect(name))
        { var header = new byte[4]; BinaryPrimitives.WriteInt32LittleEndian(header, length); await pipe.WriteAsync(header); }
        Check((await Call(name)).Ok, "invalid frame length recovered: " + length);
    }
    using (var pipe = await Connect(name)) await pipe.WriteAsync(new byte[] { 1, 0, 0, 0, (byte)'{' });
    Check((await Call(name)).Ok, "malformed JSON recovered");
    using (var pipe = await Connect(name)) await pipe.WriteAsync(new byte[] { 4, 0 });
    Check((await Call(name)).Ok, "partial frame disconnect recovered");
    Denied(() => { using var duplicate = new BridgeServer<Request,Reply>(name, (r,c) => Task.FromResult(new Reply(true,"bad")), e => new(false,e)); }, "first pipe instance reservation prevents impostor listener");
    using var identityProbe = await Connect(name);
    Denied(() => BridgeClient.VerifyServer(identityProbe, Environment.ProcessId), "client rejects same-user pipe owned by non-ArcGIS process");
}
using (var restarted = new BridgeServer<Request,Reply>(name, (r,c) => Task.FromResult(new Reply(true,"restarted")), e=>new(false,e)))
{ restarted.Start(); Check((await Call(name)).Ok, "shutdown releases idle listener for restart"); }
using (var lostResponse = new NamedPipeServerStream(name, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly))
{
    int mutations = 0;
    var lostTask = Task.Run(async () =>
    {
        await lostResponse.WaitForConnectionAsync();
        await BridgeProtocol.ReadAsync<Request>(lostResponse, BridgeProtocol.MaxRequestBytes, CancellationToken.None);
        Interlocked.Increment(ref mutations); lostResponse.Disconnect();
    });
    using var caller = await Connect(name); bool failed = false;
    try { await BridgeProtocol.ExchangeOnceAsync<Request,Reply>(caller,new("insert"),CancellationToken.None); }
    catch (IOException) { failed = true; }
    await lostTask;
    Check(failed && mutations == 1, "lost response after mutation is not replayed");
}

// Tool protocol checks run against the packaged stdio executable, never live Pro.
string executable = Path.GetFullPath(args[0]);
foreach (string version in new[] { "2024-11-05", "2025-06-18" })
{
    var start = new ProcessStartInfo(executable) { RedirectStandardInput=true, RedirectStandardOutput=true, RedirectStandardError=true, UseShellExecute=false, CreateNoWindow=true };
    start.Environment.Remove("MCP_TRANSPORT");
    start.Environment["ARCGIS_PROJECT"] = Path.Combine(temp, "never-opened.aprx");
    using var process = Process.Start(start)!; var errors = process.StandardError.ReadToEndAsync(); int id = 0;
    async Task<JsonElement> Rpc(string method, object? parameters = null)
    {
        int requestId = ++id;
        await process.StandardInput.WriteLineAsync(JsonSerializer.Serialize(new {jsonrpc="2.0",id=requestId,method,@params=parameters}));
        await process.StandardInput.FlushAsync(); using var timeout = new CancellationTokenSource(15000);
        while (true)
        {
            var line = await process.StandardOutput.ReadLineAsync(timeout.Token) ?? throw new Exception("MCP exited: " + await errors);
            var message = JsonDocument.Parse(line).RootElement.Clone();
            if (message.TryGetProperty("id",out var responseId) && responseId.GetInt32()==requestId) return message;
        }
    }
    try
    {
        var init = await Rpc("initialize",new {protocolVersion=version,capabilities=new {},clientInfo=new {name="hardened-test",version="1"}});
        Check(init.TryGetProperty("result",out _), "MCP handshake " + version);
        await process.StandardInput.WriteLineAsync("{\"jsonrpc\":\"2.0\",\"method\":\"notifications/initialized\"}");
        var tools = (await Rpc("tools/list")).GetProperty("result").GetProperty("tools").EnumerateArray().ToArray();
        Check(tools.Length == 84, "84 tools discoverable with schemas");
        var names = tools.Select(t=>t.GetProperty("name").GetString()).ToArray();
        Check(names.Contains("execute_python") && names.Contains("get_capabilities") && names.Contains("capture_map_view"), "extended tools and capability inspector present");
        var gateway = tools.Single(t=>t.GetProperty("name").GetString()=="bridge_op");
        Check(!gateway.GetProperty("annotations").GetProperty("readOnlyHint").GetBoolean(), "raw operation gateway is not mislabeled read-only");
        // Respect the operator's policy without rewriting it. The impossible project
        // pin prevents all live dispatch even when full capabilities are enabled.
        var installedPolicy = BridgePolicy.Load();
        string RejectionFor(string capability) => installedPolicy.Capabilities.Contains(capability)
            ? "No matching hardened bridge" : "disabled";
        var rawDenied = (await Rpc("tools/call",new {name="bridge_op",arguments=new {op="pro.executePython",argsJson="{\"code\":\"must_not_run()\"}"}})).GetProperty("result");
        Check(rawDenied.GetProperty("isError").GetBoolean() && rawDenied.GetRawText().Contains(RejectionFor("Python")), "raw gateway respects policy and missing-project isolation");
        var ping = await Rpc("tools/call",new {name="ping",arguments=new {}});
        Check(!ping.GetProperty("result").TryGetProperty("isError",out var err) || !err.GetBoolean(), "ping works without ArcGIS");
        foreach (var tool in new[] { "execute_python", "delete_features" })
        {
            object arguments = tool == "execute_python" ? new {code="raise Exception('must not run')"} : new {layer="none",where="1=1"};
            var result=(await Rpc("tools/call",new {name=tool,arguments})).GetProperty("result");
            Check(result.GetProperty("isError").GetBoolean() && result.GetRawText().Contains(RejectionFor(tool == "execute_python" ? "Python" : "Editing")), "MCP respects policy and missing-project isolation: " + tool);
        }
        var missing=(await Rpc("tools/call",new {name="get_active_map_name",arguments=new {}})).GetProperty("result");
        Check(missing.GetProperty("isError").GetBoolean() && missing.GetRawText().Contains("No matching hardened bridge"), "missing project produces actionable MCP error");
        process.StandardInput.Close(); await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5));
        Check(process.ExitCode==0, "clean MCP EOF shutdown");
        Check((await errors).Contains("Application started"), "logging restricted to stderr");
    }
    finally { if(!process.HasExited) process.Kill(true); }
}
Console.WriteLine($"All {passed} checks passed.");
}
catch (Exception ex) { Console.Error.WriteLine(ex); Environment.ExitCode = 1; }
record Request(string Op);
record Reply(bool Ok,string Data);
record Project(string Path,string Name);
