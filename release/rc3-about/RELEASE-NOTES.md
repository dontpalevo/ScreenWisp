# ScreenWisp v1.0.0-rc.3

**Draw anywhere. Release Alt. Gone.**

ScreenWisp is a native Windows tray app for temporary screen annotations. Tap and release Alt, press the same Alt again within 400 ms and hold to draw. Release Alt to clear temporary ink across all monitors and return to normal mouse interaction.

## Included

- Multi-monitor overlays with one shared temporary undo order: press Z while holding Alt.
- Compact tray brush panel, immediate color/thickness selection, optional quiet per-user startup and single-instance handling.
- English, Russian and automatic Windows UI language selection, applied without restarting.
- Configurable alternative hold/undo shortcuts, Esc cleanup and Ctrl+Shift+F12 emergency exit.
- About and executable metadata identify dontpalevo, with explicit Desktop Ink attribution.

## Downloads

- **ScreenWisp-Setup.exe:** Windows x64 per-user installer, Start menu shortcut and uninstaller. No administrator rights or separate .NET installation required.
- **ScreenWisp-Portable.zip:** extract the entire archive into a permanent folder, then run ScreenWisp.exe. .NET is bundled. Disable startup before moving/deleting the folder, then launch at the new path and enable again.
- **ScreenWisp-Source.zip:** reviewed source snapshot without local diagnostics, builds or private files.
- **SHA256SUMS.txt:** verify the full SHA256 for the matching filename with PowerShell Get-FileHash -Algorithm SHA256.

These are the exact RC3 **About/authorship** packages, not the preceding localization-only RC3. Application ProductVersion is 1.0.0-rc.3; numeric Windows FileVersion is 1.0.0.0. The bundled README reflects packaging time; repository documentation is newer.

## Validation and limitations

Recorded local evidence for this candidate includes 399 Windows application/resource/WPF/Win32 assertions and four full-app lifecycle assertions. The actual final Portable EXE completed initialization, ran for 30 seconds, prevented a second instance, exited deliberately with code 0 and retained its hash. Alt/Z/overlay mechanics and prior working backups were not changed by release preparation.

Synthetic input and WPF rendering checks do **not** replace physical keyboard or actual mixed-DPI testing. Clean Windows 10/11 standard-user VMs, actual Windows sign-out/sign-in startup, and installation/upgrade/uninstallation of this exact About installer remain final acceptance checks. The installer compiled successfully and its product/company/version metadata and source payload were verified; this is not a claim that its complete installation lifecycle was retested. The previous RC2 installer had separate local lifecycle checks.

**Executables are unsigned.** Windows SmartScreen, browsers or antivirus may warn or block them. Do not disable protection or add exclusions to run ScreenWisp. Report a detection with the exact version and hash. RC1's confirmed self-copy startup trigger was removed in RC2; the current app registers its actual path without copying its executable. Local testing is not universal antivirus clearance.

Windows 10/11 x64 is the target; installer minimum is build 17763. UAC secure desktop, lock screen and exclusive fullscreen are unsupported. Start a separate stroke on each monitor. Temporary sessions have a 120-second safety limit and cleanup requires a responsive UI. NVIDIA Alt+Z may require changing temporary undo to Shift+Z. No demonstration video is included in this first release.

## License and credits

MIT. Developed by **dontpalevo**, based on [Desktop Ink by atman-33](https://github.com/atman-33/desktop-ink). The original LICENSE and THIRD-PARTY-NOTICES.md are preserved. LICENSE-SCREENWISP covers only original ScreenWisp additions; COPYRIGHT.md explains the distinction. Bundled runtime notices are included. Development used OpenAI Codex.
