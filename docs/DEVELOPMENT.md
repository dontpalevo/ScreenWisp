# Development

ScreenWisp is a Windows x64 C# / .NET 10 WPF application. Install the .NET 10 SDK; distribution packages bundle the runtime. Inno Setup 6.7.3 was used for RC3 packaging. No administrator installation or GitHub publishing tool is part of the build.

From the source root, in PowerShell:

```powershell
.\build.ps1 -PublishDirectory artifacts/development-x64
```

This builds Release, runs the STA/WPF/Win32 test harness and lifecycle harness, and publishes a self-contained single-file Windows x64 app. It requires a real interactive Windows desktop: tests open temporary windows and register temporary hotkeys. Do not run desktop integration tests alongside an important drawing session. Synthetic sequences are not physical-keyboard validation.

To build new distribution packages intentionally:

```powershell
.\tools\BuildRelease.ps1 -IsccPath 'C:\Tools\InnoSetup\ISCC.exe'
```

That command rebuilds the app, runs tests and writes release/rc3 packages/checksums and preserves root RC2 packages. Do not run it just to edit documentation: verified binary hashes would need to be rechecked and the new packages accepted. Current RC3 packages are frozen, locally tested artifacts; their embedded README is the packaging-time edition. The public source README is updated independently.

`installer/ScreenWisp.iss` defaults to `artifacts/rc3-about-payload` and checks ProductVersion exactly `1.0.0-rc.3`. Pointing it at an older 0.4.1 payload is rejected. A future release must deliberately update matching product and installer versions.

## Public source layout

```text
src/ScreenWisp/       WPF application, Win32 integration and core state
tests/ScreenWisp.Tests/  Test harness and lifecycle checks
installer/           Per-user Inno Setup script
branding/            Approved S-mark, transparent ICO/PNG and social preview
docs/                Public guides and real UI captures
publication/         Manual launch materials; no publishing automation
tools/               Build, branding and source-archive preparation
release/             Prepared public notes; binaries are release attachments
```

The working folder also contains private diagnostic evidence and historical reports. They are intentionally excluded from the reviewed public source set. Never upload the whole working directory or use an unchecked `git add .` there.

For the frozen RC3 archive only, first place the accepted `ScreenWisp-Setup.exe` and `ScreenWisp-Portable.zip` in `release/rc3-about/` (from the verified local packages or, once public, the exact tagged release). These binaries are intentionally absent from the source ZIP. With Python 3, run `python tools/PreparePublicSource.py` to recreate the allowlisted source archive and `release/rc3-about/SHA256SUMS.txt`. It verifies the frozen RC3 Setup/Portable hashes, scans selected text for known secret/personal-path patterns, excludes diagnostics/builds and writes a local audit. It does not publish, run ScreenWisp, change startup registration or modify protection settings. Automated text scanning does not replace human review of images and any future Git staging/history.

See [architecture](PUBLIC-ARCHITECTURE.md), [validation](RELEASE-VALIDATION.md), [demo recording](DEMO-RECORDING.md) and the [manual publication checklist](../publication/PUBLISH-CHECKLIST.md).
