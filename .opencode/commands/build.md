---
description: Build the plugin and report only errors
---

Build the plugin: run `dotnet build -c Release -nologo -v q`.

Report only:

- errors and warnings, as `file:line: message`
- on success, the DLL path and size under `bin\Release\net472\`

Do not paste the full MSBuild output. If it fails with `VH001`, explain that `local.props` is
missing or `ValheimDir` points at the wrong folder.
