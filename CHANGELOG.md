# Changelog

This log covers the local hardening fork. The inherited history is preserved in
[the upstream changelog](docs/upstream/CHANGELOG.md) and Git history. Versions here
identify this fork, not releases from Esri, nicogis, or Lucas Coleman.

## 3.0.1 — 2026-09-26

- Store policy and bridge discovery under the shared per-user profile folder,
  avoiding Windows AppData virtualization. Live client testing found that a
  packaged desktop host and LM Studio could see different files at the same
  apparent AppData path, leaving advanced capabilities disabled in one client.
- Fail closed on unreadable policy files instead of treating all File.Exists
  failures as a missing configuration.
- Rebuilt and passed 129 offline checks plus live routing, edit rollback, Python,
  and GP regression. LM Studio with local Qwen confirmed all capabilities and
  executed Python against the running bridge. Supersedes the local 3.0.0 candidate.

## 3.0.0 — 2026-09-26

### Added

- ArcGIS Pro 3.7 / .NET 10 build with 84 discoverable MCP tools: the Lucas fork's
  83 tools plus `get_capabilities`.
- Local stdio transport, same-user named pipes with server process verification,
  bounded messages and deadlines, strict project routing, and no automatic replay
  of requests whose outcome is uncertain.
- Capability policy enforced by both endpoints. Editing, Automation, and Python
  are available through explicit operator opt-in; the documented full profile
  enables all tool families.
- Output containment, overwrite refusal, Windows path checks, and MCP image
  content for map captures.
- Separate add-in identity, locked dependencies, build and packaging scripts,
  installation backups, checksums, client setup, provenance, and validation notes.

### Fixed during live acceptance

- Canonicalized file paths before passing them to Pro's LayerFactory, supporting
  forward-slash paths from MCP clients.
- Exposed the running bridge's policy snapshot in `get_project_info` and made
  output denials identify the configured root.
- Create the requested project location before calling Pro, which otherwise
  silently selected its default directory for a nonexistent location. Reject
  invalid project folder names and replacement of the currently open project.
- Abort project switching when saving the current project raises an error.
- Remove embedded build-directory debug information, resolve installation paths
  at build/runtime, and exclude personal configuration and raw evidence from
  distribution. Preserve required public license and author credits.
- Offline protocol tests now respect the operator's capability configuration
  while pinning an impossible project; builds never rewrite the user's policy.

### Validation and limits

- Live scratch-data acceptance covers edits/save/discard, geoprocessing, Python
  CURRENT and error recovery, model authoring, and synchronous/background models,
  in addition to inspection, mapping, cartography, layouts, and exports.
- LM Studio with local Qwen 3.5 9B successfully called tools from its chat UI.
- See [VALIDATION.md](VALIDATION.md) for the final counts and specific limits.
  Model iterators remain unsupported. Automation/Python execute with the Windows
  user's permissions and are not confined by the Export output directory.

## Local release candidates — 2026-09-25 to 2026-09-26

- Initial transport/policy integration and offline validation.
- rc2 installation and 58-tool default-capability acceptance, followed by the
  full-capability acceptance and project-location fix included in 3.0.0.
