# Publishing this fork

This package is prepared for review; it has not been uploaded or published.

The companion local Git checkout retains the Lucas fork's history through
`4fef4836cc3f7fafc026d435890d96943821b23e`. Its working tree contains this build.
The source ZIP contains the same clean source files, without `.git`, local data,
credentials, caches, compiled binaries, or personal client configuration.

1. On GitHub, create your own fork of
   https://github.com/lucasmcoleman/MCP-Server-ArcGIS-Pro-AddIn.
   GitHub's Fork action establishes the fork relationship; uploading a ZIP into
   an unrelated new repository does not.
2. Use the prepared local Git checkout. Review `git status` and `git diff`,
   including deleted inherited workflows and development helpers. The hardened
   scripts replace that deployment flow; no upstream personal runner is used.
3. Add your own fork as a new remote named `origin`. The read-only source remote
   is named `lucas-upstream`; do not push there. Set your own Git author identity,
   review the staged changes, and commit them on `hardened-3.0.1`.
4. Push that branch to your fork, then choose its default branch or open a PR in
   your fork. No maintainer identity, destination account, commit, or remote push
   has been invented by this package.
5. After reviewing the release, create a `hardened-v3.0.1` tag/release and attach
   the runtime ZIP and its SHA-256 file. The runtime ZIP includes the add-in,
   complete stdio server, install/configuration scripts, documentation, licenses,
   and an internal file hash manifest.

Keep LICENSE, UPSTREAM-LICENSE, PROVENANCE.md, THIRD-PARTY-NOTICES.md,
ThirdPartyLicenses, and docs/upstream with the source. Preserve the original Git
history. Credit upstream authors without implying they endorse these changes.

No GitHub Actions workflow is enabled. Building the add-in requires installed
ArcGIS Pro assemblies. Review the security of any runner before introducing CI.
