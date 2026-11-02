# Architecture

```
WindowsBox.App (WinUI 3)     WindowsBox.Cli (wbox, Native AOT)
            \                       /
             \                     /
              v                   v
                 WindowsBox.Core
     Catalog · Preview · Journal · Undo · Drift · Modules
                       |
                       v  (per batch, UAC)
                 WindowsBox.Elevator  → exits
```

- **No** Windows service, tray icon, or always-on updater.
- Journals: `%ProgramData%\WindowsBox\journals`
- Offline restore: `%ProgramData%\WindowsBox\backups`
- Target: Windows 11 build ≥ 22621, **.NET 10 LTS**
