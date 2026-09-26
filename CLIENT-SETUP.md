# MCP client setup

Use the same runtime package with any local stdio MCP client. Tool-calling quality depends on the selected model; image blocks require a vision-capable model/client. These configuration examples are provided, not automatically applied to your applications.

Local acceptance on 2026-09-26: LM Studio 0.4.24 was configured and Qwen 3.5 9B successfully inspected the running test project and counted its synthetic features through the chat UI. A 32768-token context was used because all 84 tool schemas consume substantial context (about half of that window in this session). Individual tool approvals remain enabled. This does not establish interoperability with every model or client.

Replace `C:/Tools/ArcGIS-MCP` with the extracted runtime folder and the project path with your saved test project. Keep the executable and its sibling files together.

## Codex / Astra

Add to the appropriate Codex `config.toml`:

```toml
[mcp_servers.arcgis_extended]
command = 'C:\Tools\ArcGIS-MCP\server\ArcGisMcpServer.exe'
tool_timeout_sec = 660

[mcp_servers.arcgis_extended.env]
ARCGIS_PROJECT = 'C:\Projects\McpTest\McpTest.aprx'
```

Restart the MCP connection. Selecting Astra in Codex does not require a different server build. [Official Codex MCP documentation](https://learn.chatgpt.com/docs/extend/mcp).

## Claude Code

Merge this entry into a trusted project's `.mcp.json`:

```json
{
  "mcpServers": {
    "arcgis_extended": {
      "type": "stdio",
      "command": "C:/Tools/ArcGIS-MCP/server/ArcGisMcpServer.exe",
      "args": [],
      "env": { "ARCGIS_PROJECT": "C:/Projects/McpTest/McpTest.aprx" }
    }
  }
}
```

Restart/reconnect and approve the project MCP server in Claude's own interface. [Official Claude Code MCP documentation](https://code.claude.com/docs/en/mcp).

## LM Studio / local models

In LM Studio, use **Program > Install > Edit mcp.json**. Merge the same `mcpServers` entry above; preserve existing server entries. Choose a model that supports tool use. Your installed Qwen 3.5 9B and GPT-OSS 20B are available for evaluation; this build does not depend on a specific local model. No Ollama API connection is required when LM Studio is the MCP host. [LM Studio MCP documentation](https://lmstudio.ai/blog/lmstudio-v0.3.17).

An Ollama model needs an MCP-capable host or adapter; Ollama's chat endpoint alone does not launch this server or execute its MCP tools.

## First checks

Ask the model to run `get_capabilities`, `list_bridges`, `get_project_info`, and `list_layers`. `ping` tests only the MCP process. Check the returned project path before making changes. Use `capture_map_view` to verify an active map view visually.

The bridge never retries an uncertain operation automatically. If an error says “Outcome uncertain,” inspect Pro before issuing the same edit again. Long models should use the asynchronous model tool and `get_run_status`; only one model job is allowed at a time.

## Your friend's computer

Send the runtime ZIP. Your friend installs it beside their own ArcGIS Pro 3.7 and configures their MCP client locally. Claude or another cloud-backed client may send tool results and images to its AI provider; the bridge itself does not contact an LLM provider. This package does not provide remote access to your computer.
