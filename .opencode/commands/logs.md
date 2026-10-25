---
description: Inspect the BepInEx log for this plugin
---

Resolve `ValheimDir` from `local.props` (fall back to `VALHEIM_DIR`), then inspect
`<ValheimDir>\BepInEx\LogOutput.log`.

Report only:

- lines mentioning `com.user.autopickuphud` or `AutoPickupHUD`
- lines containing `Exception`, `Error` or `Warning`
- if nothing matched, the last 20 lines

Quote a stack trace once, summarize what the plugin did, and propose a concrete fix.

Never dump the whole log.
