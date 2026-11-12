# Manual launch checklist — dontpalevo

Nothing is published automatically. Current release name is **1.0.0 RC3**, not stable 1.0.0.

## Before any public upload

- [ ] Run the remaining clean standard-user Windows 10/11 VM acceptance checks, including no preinstalled .NET, Setup, Portable, uninstall and upgrade preserving preferences.
- [ ] Test actual Windows sign-out/sign-in startup on/off; direct startup command execution is not sufficient.
- [ ] Confirm physical double Alt, Z undo across two monitors, release cleanup/click-through, Alt+Tab, NVIDIA fallback and emergency exit. Keep the working drawing implementation unchanged.
- [ ] Make and privately review the real 15-second recording using docs/DEMO-RECORDING.md. Remove personal data by using a clean scene; do not fake screens or claim a recording exists before it does.
- [ ] Review unsigned executable/SmartScreen/security handling and known limitations. Do not bypass protection. Code signing remains unresolved; if signing changes bytes, regenerate checksums and repeat package verification.
- [ ] Re-run `python tools/PreparePublicSource.py`. Confirm frozen Setup/Portable hashes, original MIT, source audit and updated three-file SHA256SUMS. Human-review screenshots and cover.

## GitHub — owner action

- [ ] Under **dontpalevo**, prepare `ScreenWisp` from the reviewed source ZIP. Do not upload the private diagnostic working folder. Inspect every staged file and any Git history for logs, paths, secrets and binaries before the first push.
- [ ] Confirm README attribution, original LICENSE, notices, documentation links and actual UI captures. Add the real approved GIF only when available; update recording status honestly.
- [ ] Apply About description, topics and the 1280×640 social preview from GITHUB-SETUP.md.
- [ ] Prepare tag `v1.0.0-rc.3`, draft **pre-release**, release notes, Setup, Portable, source ZIP and SHA256SUMS. Verify the exact app/package versions and hashes; no RC1 file.
- [ ] Review the draft and explicitly choose to publish. Re-open every download from a signed-out browser and compare hashes. README uses this exact tag, not the prerelease-incompatible latest route.
- [ ] Once actually published, replace README's preparation-status paragraph with the true public status in a follow-up documentation commit. Do not claim it is published beforehand.

## Reddit — after working downloads and real demo

- [ ] Recheck subreddit rules and flair at submission time. r/SideProject draft is in REDDIT-SIDEPROJECT.md; edit facts/voice and remove editorial notes before any manual post.
- [ ] **r/opensource: HOLD.** Current rules ban AI-generated content. Do not paste its draft, hide Codex use or assume a rewrite bypasses the rule. Skip this community unless its rules change or moderators explicitly permit the material.
- [ ] Use the real MP4 where supported; no simulated release-to-clear footage. Disclose Codex development and MIT Desktop Ink origin. Do not invent users, metrics or endorsements.
- [ ] Post manually with working GitHub links. Engage with feedback and actual issues; no mass-posting, paid/upvote requests or fabricated discussion.

Primary rule sources: [r/SideProject](https://www.reddit.com/r/SideProject/about/rules.json), [r/opensource](https://www.reddit.com/r/opensource/about/rules.json).
