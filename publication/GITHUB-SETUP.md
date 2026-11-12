# Manual GitHub setup

Target owner/repository: **dontpalevo/ScreenWisp**. This is a plan, not a claim that a repository or release is already public.

## About description

```text
Temporary screen annotations for Windows. Double-tap and hold Alt to draw across monitors; release to clear. Native WPF, tray controls, MIT.
```

## Topics

```text
windows wpf csharp dotnet screen-annotation drawing-overlay desktop-annotation multi-monitor system-tray productivity mit-license
```

## Social preview

Upload `branding/ScreenWisp-social-preview.png` in repository Settings → Social preview. It is a 1280×640 PNG using the approved S-mark, with SVG source beside it. It is a brand cover, not a screenshot or an input demonstration. GitHub recommends 1280×640 for best display and an image under 1 MB: [official documentation](https://docs.github.com/en/repositories/managing-your-repositorys-settings-and-features/customizing-your-repository/customizing-your-repositorys-social-media-preview).

## Clean source import

Use the reviewed `release/rc3/ScreenWisp-Source.zip` / `publication/public-source-files.txt`, not the entire working directory. Keep original MIT LICENSE and notices. The source set excludes diagnostic logs, settings backups, private paths, original reference uploads, raw recordings and temporary builds. README contains real neutral UI captures; the approved demo must be recorded separately.

Do not initialize Git or push from a folder containing unchecked diagnostic history. Before the first commit, inspect the staged file list/diff and run secret scanning appropriate to the chosen repository workflow. A clean source ZIP does not certify future Git history.

## Manual release draft

- Tag: `v1.0.0-rc.3`
- Title: `ScreenWisp 1.0.0 RC3`
- Mark **pre-release**; do not call it stable 1.0 or route README through `releases/latest` (GitHub latest normally excludes prereleases).
- Body: `release/rc3/RELEASE-NOTES.md`.
- Attach `ScreenWisp-Setup.exe`, `ScreenWisp-Portable.zip`, `ScreenWisp-Source.zip`, `SHA256SUMS.txt`.
- Keep Setup/Portable bytes unchanged. Hash verification uses the exact frozen artifacts, not a new build with the same label.
- After owner publication, open each pinned README link while signed out, download and hash every asset, and check the prerelease label. Only then advertise the links on Reddit.

Nothing here performs a repository creation, push, tag or release publication.
