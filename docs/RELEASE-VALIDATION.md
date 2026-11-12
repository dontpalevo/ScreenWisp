# RC3 localization addendum

RC3 is built separately in release/rc3. It adds English/Russian/Automatic UI localization. 365 test assertions and four app lifecycle assertions passed in the final build; see [localization validation](LOCALIZATION.md). A first desktop-test run failed to retain mouse capture after screenshot work; a repeat passed without drawing code changes. This transient test limitation is recorded rather than hidden. Actual Windows scale changes, physical input, clean VM, interactive installer and sign-in remain manual. The following is retained RC2 evidence, not a claim that RC3 repeated every installer check.

# RC2 validation and remaining acceptance gates

This is a public summary of recorded local tests, not a promise of universal compatibility. Raw local event logs, usernames, machine paths and settings backups are intentionally not part of the public source set.

## Evidence already recorded

- **162 application assertions:** 158 in the application test harness and four lifecycle assertions. WPF/Win32 tests ran on Windows. Keyboard sequences in tests are synthetic; they do not certify a physical keyboard.
- **40 local installer checks passed:** per-user non-admin setup, exact installed RC2 EXE hash, Start shortcut, Windows uninstall registration, full app readiness, two 40-second runs, duplicate-instance guard, setup/uninstall running-app guards, direct HKCU Run command, disable/re-enable startup, RC2-over-RC2 reinstall preserving preferences, and uninstall cleanup with user settings retained/restored.
- **Corrected Portable:** the EXE extracted from the final ZIP completed startup, ran for 60 seconds with startup enabled, prevented a second instance and exited deliberately with code 0. File/hash remained present during the later follow-up.
- **Provenance:** Setup installed the same corrected RC2 app bytes as Portable. Its compiler default points at the RC2 release payload, with an exact version guard; a negative compile against preserved 0.4.1 was rejected.
- **No self-copy:** startup code registers the current path. During recorded installer setup/run/reconfigure/update/uninstall monitoring, zero executable-copy events were observed under the app's LocalAppData folder.
- **Kaspersky:** RC1's self-copy behavior caused a confirmed behavior detection and file deletion. RC2 removed that branch. Read-only reports inspected after corrected local runs showed no corresponding new detections. Protection was not disabled, no exclusions added and no quarantined EXE restored. This is bounded local evidence, not antivirus certification.
- **Owner testing:** the owner reports the current installed RC2 is checked on their real Windows PC. This is separate from automated test results.
- **Icons:** existing transparent multi-resolution S icons are retained. The launch-materials pass checks PNG alpha and ICO frame sizes, not every Windows theme rendering.

App ProductVersion: `1.0.0-rc.2` (Windows numeric version `1.0.0.0`). The corrected application EXE SHA256 is:

```text
635738b47e0c62348d619b1bd8e287222702f7cca9cfe6e3b0a13f13f99b9b41
```

Current download hashes are in the prepared `release/SHA256SUMS.txt` attachment ([tagged download after publication](https://github.com/dontpalevo/ScreenWisp/releases/download/v1.0.0-rc.2/SHA256SUMS.txt)); the checksum file is not embedded in its own source archive; Setup and Portable are frozen verified files. The launch-materials task does not rebuild or re-sign them. Their embedded README remains the packaging-time edition; the public source README is newer.

## Still required before a public launch

- Clean standard-user Windows 10 and Windows 11 x64 VMs: Setup, Portable, no preinstalled .NET, first run, update and uninstall. The existing local setup test is not a clean-VM test.
- Actual Windows sign-out/sign-in with startup on; confirm one quiet tray instance, correct installed/portable path, and that disabling startup survives a later sign-in. Direct `--startup` execution alone does not prove login behavior.
- Interactive Setup wizard and Windows Apps uninstall UI; prior install/uninstall automation used silent mode.
- Final physical Alt в†’ release в†’ same Alt hold в†’ draw on two monitors в†’ Z undo в†’ release в†’ full cleanup/click-through. Include both Alt sides, AltGr, Alt+Tab, NVIDIA conflict/fallback, mixed DPI, display disconnection, Esc and emergency exit. Owner testing is valuable; a reproducible real demo is still missing.
- Check icons on light/dark taskbars, tray overflow and different scaling levels. Check the social preview at small display sizes.
- Independently review unsigned-binary warnings and security reports. No digital signature or broad reputation/antivirus guarantee exists. Do not instruct users to bypass protection.
- Record and review the real 15-second demo for privacy. Verify staged source and any history before the first push; the existing working folder has not been initialized or published as a repository by this task.

These are explicit acceptance gates, not tests claimed as passed. See the [publication checklist](../publication/PUBLISH-CHECKLIST.md).
