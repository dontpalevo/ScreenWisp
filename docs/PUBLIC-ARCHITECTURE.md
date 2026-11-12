# Architecture

ScreenWisp reuses MIT-licensed Desktop Ink drawing/Win32 foundations and replaces the palette-centric lifecycle with a tray application. See the original license and THIRD-PARTY-NOTICES.md for attribution and reused files.

- **Core:** the Alt gesture state machine distinguishes left/right Alt, the 400 ms re-press window, unrelated chords and temporary undo. Temporary and persistent histories remain separate. A shared undo order spans all monitor overlays.
- **Infrastructure:** a Windows low-level keyboard hook drives temporary drawing; global hotkeys provide configurable alternatives and emergency exit. Startup registers the quoted current executable path in the current user's Run key. It does not create a managed EXE copy. Settings and limited gesture diagnostics use the user's profile.
- **OverlayManager and OverlayWindow:** each connected monitor has a WPF transparent overlay. Drawing sessions control mouse interception and click-through. Physical display coordinates, per-monitor DPI and topology changes are handled independently of tray location.
- **Tray and brush panel:** left-click opens the existing non-activating mini-panel; right-click exposes Settings and Exit. Brush selections are immediate and persisted. The application uses a named mutex for a single interactive-session instance and explicit shutdown/readiness signals for local validation.
- **Packaging:** a self-contained .NET 10 Windows x64 publication is used for both Portable and the per-user Inno installer. An installed marker differentiates setup lifecycle actions; it is not in Portable. License and runtime notices accompany both packages.

RC3 adds centralized .NET resource localization with live UI updates. It does not alter the working Alt/Z, click-through, overlay or mini-panel implementation. Watchdog cleanup requires a responsive UI dispatcher; this is not a claim that arbitrary UI hangs can never affect input.
