# EmpireAtWarLauncher

Velopack main exe for the Windows player. Packaging, release flow and pinned versions: `Tools/Jenkins/README.md` → *Velopack installer and launcher*.

- Build: `dotnet publish Tools/Launcher -c Release -o Tools/Launcher/publish` (self-contained single file).
- Run outside an install (dev/ZIP): skips the update check and starts `EmpireAtWar.exe` beside it.
- `Velopack` NuGet version must equal the `vpk` CLI version used by Jenkins.
