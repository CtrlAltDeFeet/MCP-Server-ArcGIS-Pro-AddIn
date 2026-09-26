# Scratch-data live acceptance

These scripts mutate synthetic fixtures. They are not an unattended production
test and are deliberately not run by Build.ps1. Use one AI/controller at a time.
Python 3 with the standard library is sufficient for the MCP harness;
create_scratch.py needs ArcGIS Pro's licensed Python environment.

1. Install the runtime, open a new saved test project, and enable all seven
   capabilities using Set-Capabilities.ps1. Start the extended ribbon bridge.
2. Set these environment variables to your own absolute paths:

   ```powershell
   $env:ARCGIS_MCP_SERVER = '<runtime-folder>\server\ArcGisMcpServer.exe'
   $env:ARCGIS_PROJECT = '<test-project>.aprx'
   $env:ARCGIS_TEST_ROOT = '<new-empty-scratch-folder>'
   ```

3. Run create_scratch.py with Pro's Python. It refuses to replace validation.gdb.
   Add MCP_Points, MCP_Lines, and MCP_Polygons from that geodatabase to the active
   map. Save the project and ensure no unrelated edits are pending.
4. Wait at least three minutes after Pro starts for the inherited Python warm-up
   gate. Run `python test_advanced.py`, then `python test_models.py`. Their
   assertions check actual data counts and outputs. Inspect any failure before
   retrying: these are stateful tests and must not blindly replay edits.
5. Evidence is written only under ARCGIS_TEST_ROOT. Keep the fixtures for review
   or remove the dedicated scratch workspace after closing Pro. Never commit
   raw logs or test data; they contain local paths and project details.

The model test expects the scratch map to include an empty map named
MCP_Secondary. Create it before that test. Successful advanced tests leave the
original three points, one line, one polygon, Buffer outputs, and a test toolbox.
See VALIDATION.md for the separate cartography/export, policy, and routing checks.
