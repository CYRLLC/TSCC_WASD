# Releasing

## First public repository

1. Create a repository under the intended GitHub account and push the source.
   This working folder does not embed an assumed account or remote URL.
2. Enable Actions and private vulnerability reporting in repository settings.
3. Protect `main` with the Build and test workflow and review requirements.
4. Review MIT attribution, third-party notices, README scope and validation status.

## Preview release

1. Update `Directory.Build.props`, CHANGELOG and README version together.
2. Run `pwsh -File scripts/package.ps1` on Windows x64.
3. Extract the ZIP into a new folder; verify the checksum with `Get-FileHash`.
4. Check launch, profile editing, missing-driver messages, start/stop and closing.
   Record actual hardware results in VALIDATION; never mark untested rows as passing.
5. Commit the source and push a matching `v0.1.0`-style tag. The tag workflow
   builds/tests and creates a **draft prerelease**, never a public stable release.
6. Review assets, setup instructions and limitations in the draft before publishing.

The ZIP is framework-dependent: it requires .NET 10 Windows Desktop Runtime x64.
It contains no driver installer, runtime, private profile or signing certificate.
The EXE is unsigned until code signing is set up; see [SIGNING.md](SIGNING.md).
A source checkout can be archived with `git archive` after reviewing tracked files.

## Stable-release gate

Complete physical-device and game checks below, including hot unplug while holding
inputs, repeated start/stop and shutdown during mapping. Resolve failures and
re-run affected tests before labeling a release stable. Track the migration away
from end-of-life ViGEmBus as a separate compatibility project.
