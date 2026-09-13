# Thunderstore release

Run:

```powershell
.\build.ps1 -Package
```

Upload the single ZIP in `dist/`. The matching unpacked directory is retained beside it for inspection.

Before release, update the version in:

- `DadsDecor.csproj`
- `src/AssemblyInfo.cs`
- `src/DadsDecorPlugin.cs`
- `package/manifest.json`
- `CHANGELOG.md`
