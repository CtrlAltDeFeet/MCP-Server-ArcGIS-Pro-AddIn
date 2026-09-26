# ArcGIS Pro MCP Extended — hardened fork

Version 3.0.1, built for Windows x64, ArcGIS Pro 3.7, and .NET 10.

This build adds the Lucas fork's 83 MCP tools to a hardened local transport, plus `get_capabilities` (84 tools total). It supports attribute/schema inspection, selection, map navigation, symbology, labels, layouts, PNG map capture, exports, editing, geoprocessing, ModelBuilder, and Python. Map capture returns an MCP image block for vision-capable clients, with a file-path fallback for large images.

The server is model-independent. Use it through Codex (including Astra), Claude Code, LM Studio, or another MCP client that supports local stdio. Each computer needs its own licensed ArcGIS Pro installation. Cloud-backed desktop clients work; cloud-only websites cannot directly access this local named-pipe bridge. No public HTTP endpoint is included.

Derived from [Lucas Coleman's fork](https://github.com/lucasmcoleman/MCP-Server-ArcGIS-Pro-AddIn) and [nicogis / Studio A&T s.r.l.](https://github.com/nicogis/MCP-Server-ArcGIS-Pro-AddIn). Their MIT notices and upstream changelog are retained. See [PROVENANCE.md](PROVENANCE.md), [CHANGELOG.md](CHANGELOG.md), and [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md). This is an independent modified build, not an Esri or upstream release.

## Install and connect

1. Extract the entire runtime ZIP to a permanent folder. Keep the complete `server` directory.
2. Close ArcGIS Pro normally, then run `Install.ps1` from the extracted folder. Alternatively, double-click `ArcGISProMcpExtended.esriAddInX` and follow ArcGIS's add-in installer.
3. Open a disposable, saved `.aprx` project. On the Add-In ribbon, click **MCP Extended Hardened > Start / stop extended bridge**.
4. Add `server/ArcGisMcpServer.exe` to the MCP client using [CLIENT-SETUP.md](CLIENT-SETUP.md). Use an absolute executable path and pin an absolute `.aprx` path.
5. Run `Test-LiveReadOnly.ps1 -ProjectPath 'C:\Projects\McpTest\McpTest.aprx'`, then perform the functional checks in [VALIDATION.md](VALIDATION.md) on copied data.

Run Pro and the client unelevated under the same Windows account. The AI client launches the server; double-clicking the MCP executable is not the setup procedure. If Windows reports a missing .NET framework, install the .NET 10 x64 runtime. The add-in itself runs within Pro's .NET 10 runtime.

The new add-in has a distinct GUID, module IDs, assembly name, pipe protocol, and registry directory. It does not replace the earlier five-tool hardened add-in. Use only one AI controller per Pro project at a time. Multiple Pro instances require unique full-path pins; ambiguity is rejected.

## Capabilities

| Capability | Default | Examples |
|---|---|---|
| Inspect | Enabled | Attributes, fields, counts, catalog, model descriptions |
| View | Enabled | Zoom, selections, opening map/layout views |
| Cartography | Enabled | Layers, renderers, labels, layouts, saving the current project |
| Export | Enabled | PNG captures, layout export, feature export under OutputRoot |
| Editing | Disabled | Insert, update, delete, save/discard edits |
| Automation | Disabled | Arbitrary GP, running/authoring models, opening/creating projects |
| Python | Disabled | Arbitrary Python inside Pro |

Cartography changes the open project's map/layout state. The default profile is not strictly read-only. For read-only inspection, set only `Inspect`.

Policy is `%USERPROFILE%\.arcgis-mcp-extended\policy.json`; bridge discovery uses the `bridges` subfolder. This shared location avoids Windows AppData virtualization differences between packaged and nonpackaged clients. Missing policy uses the defaults above. Invalid or inaccessible policy fails closed. `Set-Capabilities.ps1` writes an operator-selected policy; stop and restart the bridge afterward. `get_capabilities` reports the MCP process's current view of the policy and reminds you about the restart requirement. Disabled tools remain discoverable but return explicit permission errors.

When upgrading a local 3.0.0 preview, run Set-Capabilities.ps1 with the desired capabilities again. Old AppData policy/discovery files are not automatically imported, because different hosts can see conflicting virtualized copies.

For full functionality, explicitly enable all seven capabilities:

```powershell
.\Set-Capabilities.ps1 -Capabilities Inspect,View,Cartography,Export,Editing,Automation,Python
```

Stop/start the ribbon bridge afterward and check `get_project_info.bridgePolicy.enabled`. The installer preserves an existing policy. The full profile was used for advanced live acceptance; distributed defaults remain opt-in so installing an add-in alone does not authorize arbitrary code execution.

Automation and Python are full-trust opt-ins. Arbitrary GP/model tools can execute Python indirectly, so disabling the dedicated Python tool does not sandbox Automation. See [SECURITY.md](SECURITY.md). Use `-Capabilities Inspect` for inspection only, or omit `-Capabilities` to restore the four default families.

Outputs default to `%USERPROFILE%\Documents\ArcGISMcpOutputs` (Windows' actual Documents known folder is used). Explicit export paths must remain under the configured OutputRoot. Existing outputs, UNC paths, alternate streams, and reparse-point paths are rejected. No overwrite option is exposed for these exports.

## Validation status

The add-in and server have been installed and exercised in live Pro 3.7, including full-capability scratch editing, geoprocessing, Python, and model execution. The build passes 129 offline checks. LM Studio with Qwen 3.5 9B also completed an actual MCP chat test. See [VALIDATION.md](VALIDATION.md) for exact coverage and remaining limits. Successful cases are not exhaustive validation of every dataset, model graph, or client.

## Build from source

Install the .NET SDK listed in `global.json` and ArcGIS Pro 3.7 at its standard path. Run `Build.ps1`, then `Package.ps1`. Dependency lock files are included. No automatic Esri deployment target is imported; builds never install the add-in. `Package.ps1` produces the add-in archive and a portable runtime ZIP with checksums. See [CONTRIBUTING.md](CONTRIBUTING.md) and [GITHUB-PUBLISHING.md](GITHUB-PUBLISHING.md). A NuGet audit warning such as NU1900 means the feed check was unavailable; do not interpret it as a clean audit. The dated dependency report in `docs/validation` describes the separate check performed for this build.

## Remove

Close Pro and remove the add-in with GUID `{72dd2cd1-b63c-4c68-bab4-4ef6c41717fa}` from Pro's Add-In Manager, or remove that specific folder under Documents/ArcGIS/AddIns/ArcGISPro. Remove only the `arcgis_extended` entry from each MCP client. Preserve output files and other add-ins. No service, startup task, firewall rule, or network listener is installed.
