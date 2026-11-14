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
