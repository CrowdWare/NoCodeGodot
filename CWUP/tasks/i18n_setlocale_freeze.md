# Task: Fix `i18n.setLocale()` runtime freeze in SMS

## Background

`i18n.tr(...)` works as expected, but calling `i18n.setLocale(...)` can freeze the app at runtime.
As a safety measure, `setLocale` is currently disabled in `SmsUiRuntime` and only logs a warning.

## Goal

Re-introduce `i18n.setLocale(...)` with a robust implementation that never blocks the UI thread and does not trigger re-entrant runtime loops.

## Observed behavior

- Repro snippet:

```sms
var caption = ui.getObject("caption")
if (caption != null) {
    i18n.setLocale("pt")
    caption.text = i18n.tr("caption.demo", "Nichts gefunden")
}
```

- Result: app freezes (hard hang) in current runtime state before temporary deactivation.

## Scope

1. Analyze root cause (deadlock, re-entrancy, event feedback loop, sync-over-async issue).
2. Design non-blocking locale switch path (no `.GetResult()`/blocking waits on UI thread).
3. Ensure safe UI relocalization for controls with `textKey` / `titleKey` metadata.
4. Add guardrails for repeated/rapid `setLocale` calls.
5. Add/extend tests where feasible and verify with runtime scenario.
6. Update docs when re-enabled.

## Acceptance criteria

- `i18n.setLocale("pt")` does not freeze app.
- Subsequent `i18n.tr(...)` returns translations for the new locale.
- Existing localized controls update correctly (or behavior is documented if deferred).
- No regressions in SMS runtime startup and event dispatch.
- Build/tests pass.

## Notes

- Temporary fallback currently active: `setLocale` logs warning and keeps current locale.
- Keep API signature stable (`setLocale(locale: String)`) for forward compatibility.
