# Contributing

Preserve LICENSE, UPSTREAM-LICENSE, PROVENANCE.md, ThirdPartyLicenses, and the
upstream history. Describe modifications in CHANGELOG.md; do not attribute new
behavior or validation to upstream authors.

Build on Windows with the SDK in global.json and ArcGIS Pro 3.7 installed at its
standard path. Run `./Build.ps1` and record the results. `-SkipRestore` is for a
previously restored checkout. Builds do not install the add-in. `./Package.ps1`
creates a new runtime release folder beside the checkout.

Test GIS changes on a saved disposable project with synthetic data. Confirm the
exact project path and active capabilities before edits. See VALIDATION.md and
tests/live/README.md. Never use production datasets as regression fixtures or
include project data, user configuration, credentials, or raw machine logs in a
pull request. Report the limits of testing, including required Esri extensions.

Do not automatically replay mutations after a timeout or broken connection.
Inspect actual state first. Keep error messages actionable and preserve the
client and bridge policy checks when adding a new operation.

No hosted CI is enabled in this package: ArcGIS Pro SDK references and live
acceptance require an appropriately licensed Windows installation. Do not expose
a personal self-hosted runner to untrusted pull-request code.
