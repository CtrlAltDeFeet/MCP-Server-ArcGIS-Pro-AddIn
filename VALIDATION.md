# Validation — 2026-09-26

Version 3.0.1 was built, installed, and exercised in ArcGIS Pro 3.7 on Windows.
All seven capabilities were enabled for advanced acceptance. The installer
preserves operator configuration; distribution defaults remain Inspect, View,
Cartography, and Export.

## Results

- **129 offline checks passed:** 69 policy, transport, routing, and MCP protocol checks, plus 60 ModelBuilder file-format checks. Sanitized output is in docs/validation. Protocol tests pin an impossible project so they cannot dispatch into an open project even with full capabilities.
- **84 distinct MCP tools returned successful results across acceptance.** This is representative case coverage, not exhaustive validation of every argument, dataset, or model graph. See docs/validation/tool-coverage.json.
- Inspection verified fields, properties, attributes, count 3, statistics min 10/max 30/mean 20, attribute/spatial selection, and clearing.
- Map/cartography checks covered three renderer types, labels, visibility, transparency, order/name, definition queries, maps, bookmarks, navigation, layouts, text, legends, scale/north elements, and saving.
- Map capture returned an MCP image block. PNG/PDF layouts and shapefile exports succeeded. Images decoded at requested dimensions, the PDF header was valid, and layout output was visually inspected. Existing-output, outside-root, traversal, sibling-folder, and relative-path exports were refused.
- Editing inserted points, lines, and polygons, updated attributes, saved edits, deleted a scratch point, and restored it with discard. Final baseline points retained their count and values. Missing/conflicting targets and an OID list exceeding 10,000 entries were refused.
- Geoprocessing Buffer produced three polygons; GetCount returned 3.
- Python returned structured data and captured stdout, accessed CURRENT, reported a deliberate exception, and recovered on the next call. Startup warm-up refusal was observed and the call succeeded after its stated delay.
- ModelBuilder created a toolbox/model, described and updated it, changed input defaults and step parameters, and executed synchronously and as a background job. Both runs produced three polygons at the specified paths.
- Project creation, reopening the original project, and explicit bridge selection passed. A regression exposed Pro's fallback to its default directory when the requested location did not exist. The final build creates that directory first; the regression confirmed the requested location. Invalid names and replacing the currently open project were refused.
- Adding a public Esri tiled map service by HTTPS URL succeeded; the test layer was subsequently removed. Encoded file URIs were rejected by Pro's URL layer factory; use add_layer_from_file for local paths.
- LM Studio 0.4.24 with local Qwen 3.5 9B completed actual get_project_info and count_features calls in its chat UI. A 32768-token context accommodated the tool catalog. Individual confirmations were retained. The final local configuration points to the release executable.

Version 3.0.0 regression covered project location/routing, edit rollback and
limits, Python CURRENT, GP, model execution, and a public service layer. Earlier
cartography and model-authoring cases were performed on rc2; those handlers are
unchanged. Binaries omit embedded build-directory debug information.

Version 3.0.1 corrected a Windows AppData virtualization mismatch between a
packaged desktop host and LM Studio. Policy and discovery now share a per-user
profile directory. The installed 3.0.1 build passed all 129 offline checks and
live capability/routing, count, edit rollback, Python CURRENT, and GP GetCount
checks. LM Studio's actual get_capabilities payload confirmed 3.0.1 and all seven
capabilities after the correction. The local Qwen model then completed
get_project_info and a read-only execute_python call through LM Studio, returning
the current project and answer 4. Individual tool confirmations remained enabled.

## Fixes and evidence handling

Acceptance corrected forward-slash file paths passed to LayerFactory, added
the running policy snapshot, and clarified export errors. An early export
rejection did not recur after reinstall/restart; its original cause was not
established. This is not presented as a proven root-cause fix.

Local models helped draft checklists and review ideas. Suggestions were checked
against actual schemas and results. Incorrect claims, including an assertion
that stdio makes cloud-backed use “air-gapped,” were rejected.

Raw evidence remains in the operator's private workspace. Distribution includes
sanitized summaries, portable harnesses, and offline results. Personal paths,
account details, client configuration, screenshots, datasets, debug profiles,
and symbols are excluded. Required public author/license credits are retained.
See docs/validation/privacy-scan.json for the scan scope.

The 2026-09-25 NuGet feed check found no matching advisory among 32 resolved
packages; it is a dated check, not a current guarantee. Signed package checks
were also performed. Native NuGet audit emitted NU1900 because its feed access
failed; a separate official-feed check is recorded in
docs/validation/dependency-audit-2026-09-25.json.

## Remaining limits

- Multi-instance ambiguity and duplicate-name behavior have offline coverage; two simultaneous live Pro instances were not part of this acceptance.
- Claude, Codex/Astra, and other cloud-backed clients have setup examples; their end-to-end sessions were not validated here.
- Model iterators are unsupported. Complex nested/script/Python-toolbox graphs, enterprise geodatabases, authenticated services, and licensed extensions need separate workload-specific acceptance.
- Automation and Python execute with the Windows user's permissions. Export containment does not sandbox arbitrary code or GP/model outputs.
- Native operations may outlive a cooperative timeout. Inspect state after an uncertain outcome; do not automatically replay mutations.

Use tests/live/README.md for synthetic editing/model fixtures, and
Test-LiveReadOnly.ps1 for installation checks with an exact saved-project pin.

