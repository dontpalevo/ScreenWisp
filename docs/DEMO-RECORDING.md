# A real 15-second ScreenWisp demo

Status: recording plan, **not a completed demo**. Use the unchanged, owner-tested RC2 on a real Windows desktop with two physical monitors and a physical keyboard/mouse. No generated UI, fabricated input or composited ink.

## Prepare a safe scene

1. Close personal apps on both displays. Use neutral local documents or simple blank canvases with generic labels such as “Display A” and “Display B”; no real names, work, messages, browser sessions, file paths or account details.
2. Review both taskbars and notification areas. Use Windows notification controls for a quiet recording. Do not change antivirus/security settings. Record no password entry or login screen.
3. Put ScreenWisp's icon in an accessible tray position. Set a high-contrast purple/red brush and a visible thickness. Close Settings. Confirm left-click opens the real mini-panel.
4. Check the physical double-Alt gesture, Z undo, release cleanup, click-through and emergency exit before recording. Use the same Alt key twice, within 400 ms after the first release.
5. Record both monitors at their actual aspect ratios, with the cursor visible. A clean combined capture or synchronized side-by-side crops is fine. Make a short test: the recorder must capture the transparent WPF overlays on both monitors. Check the footage; do not assume the capture API includes them.
6. If Alt+Z opens NVIDIA Overlay, select the supported alternative undo chord in ScreenWisp. Label the demo with the chord actually used, e.g. Alt+Shift+Z. Do not caption a remapped input as Alt+Z.

## One continuous take

| Time | Physical action | What the viewer sees |
|---|---|---|
| 0–1 s | Start on the two neutral displays. | Both displays visible, no ink. |
| 1–2 s | Tap and release Alt, press the same Alt again within 400 ms and hold it. | Small caption: “Tap Alt → hold Alt”. |
| 2–5 s | Draw a clear circle/arrow on display A. | Actual pointer and real ink. |
| 5–8 s | Release the mouse button, move to display B and draw two separate strokes. Keep Alt held. | A's ink stays; B gets its own strokes. |
| 8–9 s | Press and release Z once, still holding Alt. | Only the latest completed stroke disappears. Caption: “Alt+Z: undo”. |
| 9–10.5 s | Release Alt. | All remaining ink on both displays disappears together. Keep this transition uncut and at real speed. |
| 10.5–11.5 s | Click/drag in the neutral underlying app on each display. | Normal mouse interaction is restored. |
| 11.5–15 s | Left-click ScreenWisp's tray icon, select a color and adjust thickness. | Real mini-panel and brush preview. |

If the whole sequence cannot fit naturally in 15 seconds, simplify the strokes and repeat the take. Do not speed up the release transition, remove a failed release, or splice separate monitor cleanups to imply simultaneous behavior. Keep a longer unedited proof take locally. A caption describes the action; it is not evidence that a key was physically pressed.

## Record and export

Use a recorder you already trust that can capture both displays and the overlay. Keep the raw recording in the ignored `recordings/` folder, outside the public source archive. Disable microphone/system audio capture unless intentionally needed. Record at 30 or 60 fps; export an H.264 MP4 at 30 fps. If a 15-second take would hide interaction details, also retain a readable longer clip for technical review.

The following FFmpeg commands are templates, not commands already run or a claim that video exists. Adjust `-ss` to the real take start. They use actual captured frames only, drop audio, preserve aspect ratio and retain normal playback speed. Install FFmpeg separately if needed.

```powershell
ffmpeg -ss 00:00:02 -i screenwisp-source.mp4 -t 15 -an -vf "scale=1920:-2:flags=lanczos,fps=30" -c:v libx264 -crf 20 -preset medium -pix_fmt yuv420p -movflags +faststart screenwisp-demo.mp4
ffmpeg -i screenwisp-demo.mp4 -vf "fps=15,scale=1600:-1:flags=lanczos,palettegen=stats_mode=diff" screenwisp-palette.png
ffmpeg -i screenwisp-demo.mp4 -i screenwisp-palette.png -lavfi "fps=15,scale=1600:-1:flags=lanczos[x];[x][1:v]paletteuse=dither=sierra2_4a" -loop 0 screenwisp-demo.gif
```

Two 1080p displays often make a very wide frame. Inspect the result on a phone and at README width. Crop empty borders or use synchronized side-by-side crops from the same recording if needed; retain both displays, actual timing and the tray popup. Do not stretch the displays. MP4 should be the primary Reddit upload; GIF is optional for README and may be large. Do not prescribe a claimed file size before encoding.

## Review before publishing

- Watch every exported frame at full resolution: no names, paths, notifications, account avatars, license keys or private data. If one appears, prefer a clean retake. A real security alert is a reason to investigate, not hide it and claim a clean run.
- Check that all temporary ink vanishes on both monitors immediately on release and that the next clicks reach the underlying windows.
- Check actual undo input, colors, panel legibility, no cut in the cleanup, no accidental keyboard logger/typed text, and no audio.
- Strip avoidable metadata on export and inspect the output's metadata before upload.
- Only after approval, copy the real GIF to `docs/media/screenwisp-demo.gif` and add its image to README. Keep the MP4 as a release/demo attachment if desired. Do not commit the raw desktop recording.
- Update README's “recording still to be made” status only once a genuine approved clip exists.
