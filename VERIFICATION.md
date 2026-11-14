# VERIFICATION

Every feature → how it was verified and the result.
Methods: `auto-test` (xUnit), `snapshot` (PNG looked at), `live` (ran the real app and observed/measured),
`manual-needed` (could not be truly exercised here — steps for the owner), `unavailable` (with reason).

| Feature | Method | Result |
|---|---|---|
| Solution builds (Release, warnings as errors) | live | ok |
| Settings: defaults, roundtrip, corruption backup, migration, normalisation | auto-test | ok (5 tests) |
| Strings EN/UK key parity | auto-test | ok |
| ModuleHost: lazy start, enable/disable, failure isolation | auto-test | ok |
| EventBus | auto-test | ok |
| Single instance | live | ok — second process exits immediately |
| Pill visible top-centre, black, 120×32 @100% | live (screenshot at 125% DPI) | ok |
| Tray icon + menu | manual-needed | Right-click the Isle icon in the notification area: Show/Hide, Settings, Start with Windows, Quit should work |
| Idle budget (M0) | live | 8 MB WS / 31 MB private / 0% CPU |
| Hover expand (120 ms intent) / collapse on leave (250 ms) | live (cursor driven by script, screenshots) | ok |
| Spring expand/collapse (~400 ms perceived, slight overshoot, collapse faster) | auto-test + live | ok |
| Tab bar with sliding indicator, label on selected tab, "more" overflow list, gear | snapshot (dark/light × en/uk, 150% scale) | ok |
| Toast inside the panel (text + action) | snapshot | ok |
| Themes Dark / Light / System (follows Windows live via WM_SETTINGCHANGE) | snapshot (dark/light); System: manual-needed | Switch Windows Settings → Personalisation → Colours → app mode; the island recolours within a second |
| UI scale 80–200% | snapshot at 150% | ok (settings UI arrives in M15; live preview there) |
| Auto-hide to screen edge (sliver, returns on top-edge touch) | snapshot (edge-hidden) | manual-needed for the live slide: enable `AutoHideEdge` in settings.json, move cursor away 1.5 s → island slides up; touch the top edge → it returns |
| Click-through mode (WS_EX_TRANSPARENT) | manual-needed | Set `"ClickThrough": true` in settings.json → clicks on the pill reach the window below |
| Fullscreen auto-hide | manual-needed | Play a video fullscreen / press F11 in a browser → island disappears; exit → it returns |
| Excluded apps | auto-test (name matching) + manual-needed | Add `"notepad"` to `ExcludedApps` → island hides while Notepad is in front |
| Global hotkeys infrastructure (RegisterHotKey, failures non-fatal) | auto-test (gesture parsing) + manual-needed | Press Win+Alt+Space → panel opens/closes |
| Esc collapses | manual-needed | Expand, press Esc |
| Pill priority queue (selection, sharing, banners) | auto-test | ok |
| --snapshot tool | live | `Isle.exe --snapshot all <dir>` renders every state × theme × language |
| Start-up to visible pill | live | 457 ms to input-idle |
| Idle budget (M1) | live | 38.7 MB WS / 41.3 MB private / 0.009 % CPU over 60 s |
