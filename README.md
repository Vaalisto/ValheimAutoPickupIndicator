# Auto Pickup HUD Indicator

Shows whether auto pickup is enabled, styled like the vanilla HUD status effects
(Fire, Wisplight, ...) instead of a separate panel: the state is green when on and
red when off.

## Requirements

- Valheim (Steam)
- [BepInEx 5](https://github.com/BepInEx/BepInEx/releases)

## Install

1. Install BepInEx 5 into your Valheim folder.
2. Copy `AutoPickupHUD.dll` to `BepInEx/plugins/`.
3. Launch the game.


## Build from source

The project references the game and engine assemblies from your own Valheim
install. No game or Unity assemblies are stored in this repository, so you need a
legitimate copy of the game to compile it.

Either:
- Copy `local.props.example` to `local.props` and point `ValheimDir` at your
   install:

   ```xml
   <ValheimDir>E:\SteamLibrary\steamapps\common\Valheim</ValheimDir>
   ```

   `local.props` is gitignored, so your path stays out of the repository.

or:
- pass it for a single build: `dotnet build -p:ValheimDir="D:\Games\Valheim"`.

If the path is wrong, or BepInEx is not installed in that folder, the build stops
with `VH001` instead of a list of missing types.

## Example image

![example](autopickupexample.jpg)

## Notes

- Originally vibecoded with Gemini and rework with Deepseek 4.1 so vibe is strong with this one.

## License

MIT, see [LICENSE](LICENSE).
