# Auto Pickup HUD Indicator — agent notes

Valheim BepInEx 5 plugin. Shows the auto-pickup state in the HUD, styled as a vanilla
status effect entry (same font and outline as Fire / Wisplight): green `ON`, red `OFF`.

## Environment

- Game folder: `ValheimDir`, resolved from `local.props` (gitignored), then `VALHEIM_DIR`,
  then `-p:ValheimDir=`. Currently `E:\SteamLibrary\steamapps\common\Valheim`
  (Unity 6000.0.75f1, BepInEx 5.4.23.5).
- Plugin GUID `com.user.autopickuphud`.
- Config: `<ValheimDir>\BepInEx\config\com.user.autopickuphud.cfg`.
- Log: `<ValheimDir>\BepInEx\LogOutput.log`.
- Target `net472`, one source file (`Plugin.cs`), no NuGet packages.

## Build

```
dotnet build -c Release -nologo -v q
```

Output: `bin\Release\net472\AutoPickupHUD.dll`. Keep the build at 0 warnings.

There is no deploy target: copying the DLL into `<ValheimDir>\BepInEx\plugins\` is manual.

## Rules

- Never commit game or engine assemblies. All references resolve through `$(ManagedDir)` and
  `$(BepInExDir)`, and every `<Reference>` must keep `<Private>false</Private>` so no game DLL
  ends up in `bin\`.
- Never modify anything under `ValheimDir` except `BepInEx\plugins\` and `BepInEx\config\`.
- Verify game APIs against the real assemblies before using them; do not guess field or method
  names. The members this mod relies on are listed below.
- Keep the per-frame path cheap: never assign `TMP_Text.text` or call `SetActive` without
  comparing against a cached value first.
- Never write to a shared TMP material. Use `fontMaterial` (per-object instance), not
  `fontSharedMaterial`.
- Changing the GUID breaks existing config files; only do it deliberately.

## How the HUD integration works

- A `Hud.Update` postfix drives everything; a `Hud.Awake` postfix clears the cached statics.
- The indicator is a clone of `Hud.m_statusEffectTemplate` parented to
  `Hud.m_statusEffectListRoot`, with the `Icon`, `Cooldown` and `TimeText` children hidden.
  Vanilla finds the label via `GetComponentInChildren<TMP_Text>()`; the mod does the same.
- Placement is the row below the vanilla effects: right-aligned at `x = -4`, `y = -rows *
  m_statusEffectSpacing`, with rows derived from active children and `m_effectsPerRow`.
- State comes from `Player.m_enableAutoPickup` (private **static** bool) through
  `AccessTools.StaticFieldRefAccess`. Resolution is lazy and fails closed with one log line.

## Verification

The agent cannot launch or play the game. After a change, ask the user to test in game and
provide a screenshot or the log tail. A successful build does not prove the patch fires.
