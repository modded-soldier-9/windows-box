# WinUtil reference inventory

Source: [ChrisTitusTech/winutil](https://github.com/ChrisTitusTech/winutil) @ commit b521c90 (2026-10-30 20:48 UTC+2). © 2022 CT Tech Group LLC, **MIT License** — credited here; tables are generated from the repo's config JSON and summarised in our own words. Nothing was executed on any machine.

Risk column is Windows Box's own rating: WinUtil's category (Essential / Customize / Advanced-CAUTION) mapped to Low / Low / High, adjusted where noted in winutil-inspiration.md.

## Architecture at a glance
- Single PowerShell+WPF app compiled from `functions/`, `xaml/`, `config/*.json` (`Compile.ps1`); launched with `irm christitus.com/win | iex`.
- Tabs: **Install**, **Tweaks**, **Config** (features, fixes, legacy panels, PS profile, remote access), **Updates**. **MicroWin** (custom ISO builder) is **no longer in the repo at this commit** (no references found) — historically it debloated an ISO via DISM; treat it as retired.
- Data-driven: each tweak is a JSON object with `Content`, `Description`, `category`, `panel`, optional `registry[]` (Path/Name/Value/Type/OriginalValue), `service[]` (Name/StartupType/OriginalType), `ScheduledTask[]`, `appx[]`, `InvokeScript[]`, `UndoScript[]`, `link` to docs.
- Safety: optional 'Restore Point - Create' tweak (in Standard/Advanced presets), per-tweak undo ('Undo Selected Tweaks', `Invoke-WPFundoall`), import/export of selections (`Invoke-WPFImpex`, skips retired keys), logging via `Write-WinUtilLog`, step timing, environment report export.
- Docs site: Astro (`docs/`), per-item pages under code-reference, Tweak Explorer, Config Builder, FAQ, Known Issues.

## Tweaks (68)
|Key|Name|Tier|Mechanism|Undo|Risk|
|-|-|-|-|-|-|
|WPFTweaksActivity|Activity History - Disable|Essential Tweaks|registry×3|restore OriginalValue (or delete key)|Low|
|WPFTweaksHiber|Hibernation - Disable|Essential Tweaks|registry×2; PowerShell script|restore OriginalValue (or delete key); UndoScript|Low|
|WPFTweaksWidget|Widgets - Remove|Essential Tweaks|PowerShell script|**none** (one-way)|Low|
|WPFTweaksRevertStartMenu|Start Menu Previous Layout - Enable|Essential Tweaks|registry×1|restore OriginalValue (or delete key)|Low|
|WPFTweaksDisableStoreSearch|Microsoft Store Recommended Search Results - Disable|Essential Tweaks|PowerShell script|UndoScript|Low|
|WPFTweaksLocation|Location Tracking - Disable|Essential Tweaks|registry×3; services: lfsvc→Disabled|restore OriginalValue (or delete key); revert to OriginalType|Low|
|WPFTweaksServices|Services - Set to Manual|Essential Tweaks|services: CscService→Disabled, DiagTrack→Disabled, MapsBroker→Manual, StorSvc→Manual, SharedAccess→Disabled|revert to OriginalType|Low|
|WPFTweaksSvcHostSplitting|svchost.exe Splitting - Disable|Advanced Tweaks - CAUTION|PowerShell script|UndoScript|High|
|WPFTweaksBraveDebloat|Brave Browser - Debloat|Advanced Tweaks - CAUTION|registry×12|restore OriginalValue (or delete key)|High|
|WPFTweaksDisableWarningForUnsignedRdp|RDP Unsigned File Warnings - Disable|Advanced Tweaks - CAUTION|registry×2|restore OriginalValue (or delete key)|High|
|WPFTweaksEdgeDebloat|Microsoft Edge - Debloat|Advanced Tweaks - CAUTION|registry×17|restore OriginalValue (or delete key)|High|
|WPFTweaksConsumerFeatures|ConsumerFeatures - Disable|Essential Tweaks|registry×1|restore OriginalValue (or delete key)|Low|
|WPFTweaksTelemetry|Telemetry - Disable|Essential Tweaks|registry×13; services: diagtrack→Disabled, WerSvc→Disabled; PowerShell script|restore OriginalValue (or delete key); revert to OriginalType; UndoScript|Low|
|WPFTweaksDeliveryOptimization|Delivery Optimization - Disable|Essential Tweaks|registry×1|restore OriginalValue (or delete key)|Low|
|WPFTweaksRemoveEdge|Microsoft Edge - Remove|Advanced Tweaks - CAUTION|PowerShell script|UndoScript|High|
|WPFTweaksDisableBitLocker|BitLocker - Disable|Essential Tweaks|PowerShell script|UndoScript|Low|
|WPFTweaksUTC|Date & Time - Set Time to UTC|Advanced Tweaks - CAUTION|registry×1|restore OriginalValue (or delete key)|High|
|WPFTweaksRemoveOneDrive|Microsoft OneDrive - Remove|Advanced Tweaks - CAUTION|PowerShell script|UndoScript|High|
|WPFTweaksRemoveHomeAndGallery|File Explorer Home and Gallery - Disable|Advanced Tweaks - CAUTION|registry×3|restore OriginalValue (or delete key)|High|
|WPFTweaksDisplay|Visual Effects - Set to Best Performance|Advanced Tweaks - CAUTION|registry×10; PowerShell script|restore OriginalValue (or delete key); UndoScript|High|
|WPFTweaksReservedStorage|Disable Reserved Storage|Advanced Tweaks - CAUTION|PowerShell script|UndoScript|High|
|WPFTweaksRestorePoint|Restore Point - Create|Essential Tweaks|registry×1; PowerShell script|restore OriginalValue (or delete key)|Low|
|WPFTweaksEndTaskOnTaskbar|End Task With Right Click - Enable|Essential Tweaks|registry×1|restore OriginalValue (or delete key)|Low|
|WPFTweaksStorage|Storage Sense - Disable|Advanced Tweaks - CAUTION|registry×1|restore OriginalValue (or delete key)|High|
|WPFTweaksWindowsAI|Windows AI - Disable And Remove|Advanced Tweaks - CAUTION|registry×2; PowerShell script|restore OriginalValue (or delete key)|High|
|WPFTweaksWPBT|Windows Platform Binary Table (WPBT) - Disable|Essential Tweaks|registry×1|restore OriginalValue (or delete key)|Low|
|WPFTweaksPreventDeviceMetadataFromNetwork|Prevent Device Companion Apps|Essential Tweaks|registry×1|restore OriginalValue (or delete key)|Low|
|WPFTweaksRazerBlock|Razer Software Auto-Install - Disable|Advanced Tweaks - CAUTION|registry×2; PowerShell script|restore OriginalValue (or delete key); UndoScript|High|
|WPFTweaksLogiBlock|Logitech Download Assistant Auto-Install - Disable|Advanced Tweaks - CAUTION|PowerShell script|UndoScript|High|
|WPFTweaksDisableNotifications|System Tray Notifications & Calendar - Disable|Advanced Tweaks - CAUTION|registry×2|restore OriginalValue (or delete key)|High|
|WPFTweaksBlockAdobeNet|Adobe URL Block List - Enable|Advanced Tweaks - CAUTION|PowerShell script|UndoScript|High|
|WPFTweaksRightClickMenu|Right-Click Menu Previous Layout - Enable|Advanced Tweaks - CAUTION|PowerShell script|UndoScript|High|
|WPFTweaksDiskCleanup|Disk Cleanup - Run|Essential Tweaks|PowerShell script|**none** (one-way)|Low|
|WPFTweaksDeleteTempFiles|Temporary Files - Remove|Essential Tweaks|PowerShell script|**none** (one-way)|Low|
|WPFTweaksIPv46|IPv6 - Set IPv4 as Preferred|Advanced Tweaks - CAUTION|registry×1|restore OriginalValue (or delete key)|High|
|WPFTweaksTeredo|Teredo - Disable|Advanced Tweaks - CAUTION|registry×1; PowerShell script|restore OriginalValue (or delete key); UndoScript|High|
|WPFTweaksDisableIPv6|IPv6 - Disable|Advanced Tweaks - CAUTION|registry×1; PowerShell script|restore OriginalValue (or delete key); UndoScript|High|
|WPFTweaksDisableBGapps|Background Apps - Disable|Advanced Tweaks - CAUTION|registry×1|restore OriginalValue (or delete key)|High|
|WPFTweaksDisableExplorerAutoDiscovery|File Explorer Automatic Folder Discovery - Disable|Essential Tweaks|PowerShell script|UndoScript|Low|
|WPFToggleDetailedBSoD|BSoD Verbose Mode|Customize Preferences|registry×2|restore OriginalValue (or delete key)|Low (cosmetic/pref)|
|WPFToggleBatteryPercentage|System Tray Battery Percentage|Customize Preferences|registry×1|restore OriginalValue (or delete key)|Low (cosmetic/pref)|
|WPFToggleDarkMode|Dark Theme for Windows|Customize Preferences|registry×2; PowerShell script|restore OriginalValue (or delete key); UndoScript|Low (cosmetic/pref)|
|WPFToggleShowExt|File Explorer File Extensions|Customize Preferences|registry×1; PowerShell script|restore OriginalValue (or delete key); UndoScript|Low (cosmetic/pref)|
|WPFToggleHiddenFiles|File Explorer Hidden Files|Customize Preferences|registry×1; PowerShell script|restore OriginalValue (or delete key); UndoScript|Low (cosmetic/pref)|
|WPFToggleVerboseLogon|Logon Verbose Mode|Customize Preferences|registry×1|restore OriginalValue (or delete key)|Low (cosmetic/pref)|
|WPFToggleNewOutlook|Microsoft Outlook New Version|Customize Preferences|registry×4|restore OriginalValue (or delete key)|Low (cosmetic/pref)|
|WPFToggleScrollbars|Scrollbars Always Visible|Customize Preferences|registry×1|restore OriginalValue (or delete key)|Low (cosmetic/pref)|
|WPFMultiplaneOverlay|Multiplane Overlay|Customize Preferences|registry×2|restore OriginalValue (or delete key)|Low (cosmetic/pref)|
|WPFToggleMouseAcceleration|Mouse Acceleration|Customize Preferences|registry×3|restore OriginalValue (or delete key)|Low (cosmetic/pref)|
|WPFToggleNumLock|Num Lock on Startup|Customize Preferences|registry×2|restore OriginalValue (or delete key)|Low (cosmetic/pref)|
|WPFToggleWindowSnapping|Window Snapping|Customize Preferences|registry×1|restore OriginalValue (or delete key)|Low (cosmetic/pref)|
|WPFToggleStandbyFix|S0 Sleep Network Connectivity|Customize Preferences|registry×1|restore OriginalValue (or delete key)|Low (cosmetic/pref)|
|WPFToggleS3Sleep|S3 Sleep|Customize Preferences|registry×1|restore OriginalValue (or delete key)|Low (cosmetic/pref)|
|WPFToggleHideSettingsHome|Settings Home Page|Customize Preferences|registry×1|restore OriginalValue (or delete key)|Low (cosmetic/pref)|
|WPFToggleBingSearch|Start Menu Bing Search|Customize Preferences|registry×1|restore OriginalValue (or delete key)|Low (cosmetic/pref)|
|WPFToggleLoginBlur|Logon Screen Acrylic Blur|Customize Preferences|registry×1|restore OriginalValue (or delete key)|Low (cosmetic/pref)|
|WPFToggleDisableLockscreen|Lock Screen - Disable|Customize Preferences|registry×1|restore OriginalValue (or delete key)|Low (cosmetic/pref)|
|WPFToggleStartMenuRecommendations|Start Menu Recommendations|Customize Preferences|registry×3; PowerShell script|restore OriginalValue (or delete key); UndoScript|Low (cosmetic/pref)|
|WPFToggleStickyKeys|Sticky Keys|Customize Preferences|registry×1|restore OriginalValue (or delete key)|Low (cosmetic/pref)|
|WPFToggleTaskbarAlignment|Taskbar Centered Icons|Customize Preferences|registry×1; PowerShell script|restore OriginalValue (or delete key); UndoScript|Low (cosmetic/pref)|
|WPFToggleTaskbarSearch|Taskbar Search Icon|Customize Preferences|registry×1|restore OriginalValue (or delete key)|Low (cosmetic/pref)|
|WPFToggleTaskView|Taskbar Task View Icon|Customize Preferences|registry×1|restore OriginalValue (or delete key)|Low (cosmetic/pref)|
|WPFToggleGameMode|Game Mode|Customize Preferences|registry×2|restore OriginalValue (or delete key)|Low (cosmetic/pref)|
|WPFToggleLongPaths|Enable Long Paths|Customize Preferences|registry×1|restore OriginalValue (or delete key)|Low (cosmetic/pref)|
|WPFOOSUbutton|O&O ShutUp10++ - Run|Advanced Tweaks - CAUTION|button → dedicated function|n/a / manual|High|
|WPFchangedns|DNS - Set to:|Advanced Tweaks - CAUTION|button → dedicated function|n/a / manual|High|
|WPFAddUltPerf|Ultimate Performance Profile - Enable|Performance Plans - NOT FOR LAPTOPS|button → dedicated function|n/a / manual|Medium|
|WPFRemoveUltPerf|Ultimate Performance Profile - Disable|Performance Plans - NOT FOR LAPTOPS|button → dedicated function|n/a / manual|Medium|

### Presets
- **Standard** (12): WPFTweaksActivity, WPFTweaksConsumerFeatures, WPFTweaksDisableExplorerAutoDiscovery, WPFTweaksWPBT, WPFTweaksLocation, WPFTweaksServices, WPFTweaksTelemetry, WPFTweaksDeliveryOptimization, WPFTweaksDiskCleanup, WPFTweaksDeleteTempFiles, WPFTweaksEndTaskOnTaskbar, WPFTweaksRestorePoint
- **Minimal** (4): WPFTweaksConsumerFeatures, WPFTweaksWPBT, WPFTweaksServices, WPFTweaksTelemetry
- **Advanced** (17): WPFTweaksRestorePoint, WPFTweaksActivity, WPFTweaksConsumerFeatures, WPFTweaksDisableExplorerAutoDiscovery, WPFTweaksWPBT, WPFTweaksLocation, WPFTweaksServices, WPFTweaksTelemetry, WPFTweaksDeliveryOptimization, WPFTweaksDeleteTempFiles, WPFTweaksEndTaskOnTaskbar, WPFTweaksDisableStoreSearch, WPFTweaksRevertStartMenu, WPFTweaksWidget, WPFTweaksRemoveOneDrive, WPFTweaksWindowsAI, WPFTweaksRightClickMenu
- **AppxDefault** (18): WPFAppxMicrosoft_WindowsFeedbackHub, WPFAppxMicrosoft_GetHelp, WPFAppxMicrosoft_MicrosoftOfficeHub, WPFAppxMicrosoft_WindowsCalculator, WPFAppxClipchamp_Clipchamp, WPFAppxMicrosoft_WindowsAlarms, WPFAppxMicrosoftCorporationII_QuickAssist, WPFAppxMicrosoft_WindowsSoundRecorder, WPFAppxMicrosoft_MicrosoftStickyNotes, WPFAppxMicrosoft_Todos, WPFAppxMicrosoft_MicrosoftSolitaireCollection, WPFAppxMicrosoft_PowerAutomateDesktop, WPFAppxMicrosoft_WindowsDevHome, WPFAppxMicrosoft_BingWeather, WPFAppxMicrosoft_StartExperiencesApp, WPFAppxMicrosoft_BingNews, WPFAppxMicrosoft_Copilot, WPFAppxMicrosoft_BingSearch

Note: the services tweak is now only 5 entries (historically ~100+ set to Manual); WinUtil trimmed it after breakage reports — a key lesson for Windows Box's services-first focus.

## Config tab: features, fixes, panels (33)
|Key|Name|Group|Mechanism|Undo|Risk|
|-|-|-|-|-|-|
|WPFFeaturesdotnet|.NET Framework (Versions 2, 3, 4) - Enable|Features|optional features: NetFx4-AdvSrvs, NetFx3; PowerShell script|**none** (one-way)|Medium|
|WPFFixesNTPPool|NTP Server - Enable|Fixes|button → dedicated function|n/a / manual|Medium|
|WPFFeatureshyperv|Hyper-V - Enable|Features|optional features: Microsoft-Hyper-V-All|n/a / manual|Medium|
|WPFFeatureslegacymedia|Legacy Media Components (WMP, DirectPlay) - Enable|Features|optional features: WindowsMediaPlayer, MediaPlayback, DirectPlay, LegacyComponents; PowerShell script|**none** (one-way)|Medium|
|WPFFeaturewsl|Windows Subsystem for Linux (WSL) - Enable|Features|optional features: VirtualMachinePlatform, Microsoft-Windows-Subsystem-Linux; PowerShell script|**none** (one-way)|Medium|
|WPFFeaturenfs|Network File System (NFS) - Enable|Features|optional features: ServicesForNFS-ClientOnly, ClientForNFS-Infrastructure, NFS-Administration; PowerShell script|**none** (one-way)|Medium|
|WPFFeatureRegBackup|Registry Backup (Daily Task 12:30am) - Enable|Features|optional features: ; PowerShell script|**none** (one-way)|Medium|
|WPFFeatureEnableLegacyRecovery|Legacy F8 Boot Recovery - Enable|Features|optional features: ; PowerShell script|**none** (one-way)|Medium|
|WPFFeatureDisableLegacyRecovery|Legacy F8 Boot Recovery - Disable|Features|optional features: ; PowerShell script|**none** (one-way)|Medium|
|WPFFeaturesSandbox|Windows Sandbox - Enable|Features|optional features: Containers-DisposableClientVM|n/a / manual|Medium|
|WPFFeatureInstall|Install Features|Features|button → dedicated function|n/a / manual|Medium|
|WPFPanelAutologin|AutoLogon - Run|Fixes|button → dedicated function|n/a / manual|Medium|
|WPFFixesUpdate|Windows Update - Reset|Fixes|button → dedicated function|n/a / manual|Medium|
|WPFFixesNetwork|Network - Reset|Fixes|button → dedicated function|n/a / manual|Medium|
|WPFPanelDISM|System Corruption Scan - Run|Fixes|button → dedicated function|n/a / manual|Medium|
|WPFFixesWinget|WinGet - Reinstall|Fixes|button → dedicated function|n/a / manual|Medium|
|WPFPanelComputer|Computer Management|Legacy Windows Panels|PowerShell script|**none** (one-way)|Low (opens panel)|
|WPFPanelControl|Control Panel|Legacy Windows Panels|PowerShell script|**none** (one-way)|Low (opens panel)|
|WPFPanelMouse|Mouse Properties|Legacy Windows Panels|PowerShell script|**none** (one-way)|Low (opens panel)|
|WPFPanelNetwork|Network Connections|Legacy Windows Panels|PowerShell script|**none** (one-way)|Low (opens panel)|
|WPFPanelPower|Power Panel|Legacy Windows Panels|PowerShell script|**none** (one-way)|Low (opens panel)|
|WPFPanelPrinter|Printer Panel|Legacy Windows Panels|PowerShell script|**none** (one-way)|Low (opens panel)|
|WPFPanelPrograms|Programs and Features|Legacy Windows Panels|PowerShell script|**none** (one-way)|Low (opens panel)|
|WPFPanelRegion|Region|Legacy Windows Panels|PowerShell script|**none** (one-way)|Low (opens panel)|
|WPFPanelSecurity|Security and Maintenance|Legacy Windows Panels|PowerShell script|**none** (one-way)|Low (opens panel)|
|WPFPanelSound|Sound Settings|Legacy Windows Panels|PowerShell script|**none** (one-way)|Low (opens panel)|
|WPFPanelSystem|System Properties|Legacy Windows Panels|PowerShell script|**none** (one-way)|Low (opens panel)|
|WPFPanelTimedate|Time and Date|Legacy Windows Panels|PowerShell script|**none** (one-way)|Low (opens panel)|
|WPFPanelFirewall|Windows Defender Firewall|Legacy Windows Panels|PowerShell script|**none** (one-way)|Low (opens panel)|
|WPFPanelRestore|Windows Restore|Legacy Windows Panels|PowerShell script|**none** (one-way)|Low (opens panel)|
|WPFWinUtilInstallPSProfile|CTT PowerShell Profile - Install|Powershell Profile Powershell 7+ Only|button → dedicated function|n/a / manual|Medium|
|WPFWinUtilUninstallPSProfile|CTT PowerShell Profile - Remove|Powershell Profile Powershell 7+ Only|button → dedicated function|n/a / manual|Medium|
|WPFWinUtilSSHServer|OpenSSH Server - Enable|Remote Access|button → dedicated function|n/a / manual|Medium|

## Updates tab
- **Default**: removes deferral/policy keys, re-enables BITS/wuauserv (Manual) and UsoSvc (Auto), re-enables update tasks. Risk Low.
- **Security (recommended)**: defer feature updates 365 days, quality 4 days, excludes drivers; clears blocking policies. Risk Low-Medium.
- **Disable all**: NoAutoUpdate policy, DO off, BITS/wuauserv/UsoSvc Disabled, disables update tasks. Risk **High** (no security patches); undo = Default.
- Links: https://github.com/ChrisTitusTech/winutil/blob/main/functions/public/Invoke-WPFUpdatessecurity.ps1

## DNS presets (8)
Google, Cloudflare, Cloudflare_Malware, Cloudflare_Malware_Adult, Open_DNS, Quad9, AdGuard_Ads_Trackers, AdGuard_Ads_Trackers_Malware_Adult

## Appx debloat catalog (34)
|Key|Name|In AppxDefault preset|
|-|-|-|
|WPFAppxMicrosoft_WindowsFeedbackHub|Feedback Hub|yes|
|WPFAppxMicrosoft_GetHelp|Get Help|yes|
|WPFAppxMicrosoft_OutlookForWindows|Outlook for Windows||
|WPFAppxMSTeams|Microsoft Teams||
|WPFAppxClipchamp_Clipchamp|Clipchamp|yes|
|WPFAppxMicrosoft_MicrosoftOfficeHub|Microsoft 365|yes|
|WPFAppxMicrosoft_ZuneMusic|Media Player||
|WPFAppxMicrosoft_BingSearch|Bing Search|yes|
|WPFAppxMicrosoftCorporationII_QuickAssist|Quick Assist|yes|
|WPFAppxMicrosoft_WindowsDevHome|Dev Home|yes|
|WPFAppxMicrosoft_WindowsCrossDevice|Mobile Devices||
|WPFAppxMicrosoft_Todos|To Do|yes|
|WPFAppxMicrosoft_PowerAutomateDesktop|Power Automate|yes|
|WPFAppxMicrosoft_YourPhone|Phone Link||
|WPFAppxMicrosoft_MicrosoftStickyNotes|Sticky Notes|yes|
|WPFAppxMicrosoft_WindowsSoundRecorder|Sound Recorder|yes|
|WPFAppxMicrosoft_WindowsAlarms|Clock|yes|
|WPFAppxMicrosoft_Paint|Paint||
|WPFAppxMicrosoft_WindowsNotepad|Notepad||
|WPFAppxMicrosoft_ScreenSketch|Snipping Tool||
|WPFAppxMicrosoft_Copilot|Copilot|yes|
|WPFAppxMicrosoft_WindowsCalculator|Calculator|yes|
|WPFAppxMicrosoft_WindowsCamera|Camera||
|WPFAppxMicrosoft_WindowsPhotos|Photos||
|WPFAppxMicrosoft_BingNews|News|yes|
|WPFAppxMicrosoft_BingWeather|Weather|yes|
|WPFAppxMicrosoft_GamingApp|Xbox App||
|WPFAppxMicrosoft_XboxGamingOverlay|Xbox Game Bar||
|WPFAppxMicrosoft_XboxIdentityProvider|Xbox Identity Provider||
|WPFAppxMicrosoft_XboxSpeechToTextOverlay|Xbox Speech To Text Overlay||
|WPFAppxMicrosoft_Xbox_TCUI|Xbox TCUI||
|WPFAppxMicrosoft_StartExperiencesApp|Start Experiences App|yes|
|WPFAppxMicrosoft_MicrosoftSolitaireCollection|Solitaire Collection|yes|
|WPFAppxMicrosoft_ZuneVideo|Movies & TV||

## Install catalog (239 apps)
Backends: winget id and/or Chocolatey id per app; `foss` flag; upgrade-all (`winget upgrade --all`, `choco upgrade all`), uninstall, Get Installed, export/import JSON, WinGet reinstall fix, Chocolatey auto-bootstrap.

|Key|App|Category|winget|choco|FOSS|
|-|-|-|-|-|-|
|brave|Brave|Browsers|Brave.Brave|brave|✓|
|chrome|Chrome|Browsers|Google.Chrome|googlechrome||
|chromium|Chromium|Browsers|Hibbiki.Chromium|chromium|✓|
|edge|Edge|Browsers|Microsoft.Edge|microsoft-edge||
|firefox|Firefox|Browsers|Mozilla.Firefox|firefox|✓|
|firefoxesr|Firefox ESR|Browsers|Mozilla.Firefox.ESR|FirefoxESR|✓|
|floorp|Floorp|Browsers|Ablaze.Floorp|floorp|✓|
|helium|Helium|Browsers|ImputNet.Helium|helium|✓|
|librewolf|LibreWolf|Browsers|LibreWolf.LibreWolf|librewolf|✓|
|mullvadbrowser|Mullvad Browser|Browsers|MullvadVPN.MullvadBrowser|na|✓|
|tor|Tor Browser|Browsers|TorProject.TorBrowser|tor-browser|✓|
|ungoogled|Ungoogled Chromium|Browsers|eloston.ungoogled-chromium|ungoogled-chromium|✓|
|vivaldi|Vivaldi|Browsers|Vivaldi.Vivaldi|vivaldi||
|waterfox|Waterfox|Browsers|Waterfox.Waterfox|waterfox|✓|
|ZenBrowser|Zen Browser|Browsers|Zen-Team.Zen-Browser|na|✓|
|betterbird|Betterbird|Communications|Betterbird.Betterbird|betterbird|✓|
|chatterino|Chatterino|Communications|ChatterinoTeam.Chatterino|chatterino|✓|
|discord|Discord|Communications|Discord.Discord|discord||
|dorion|Dorion|Communications|SpikeHD.Dorion|dorion|✓|
|matrix|Element|Communications|Element.Element|element-desktop|✓|
|protonmail|Proton Mail|Communications|Proton.ProtonMail|protonmail|✓|
|qtox|QTox|Communications|Tox.qTox|qtox|✓|
|signal|Signal|Communications|OpenWhisperSystems.Signal|signal|✓|
|slack|Slack|Communications|SlackTechnologies.Slack|slack||
|teamspeak3|TeamSpeak 3|Communications|TeamSpeakSystems.TeamSpeakClient|teamspeak||
|teamspeak6|TeamSpeak 6|Communications|TeamSpeakSystems.TeamSpeakClient.Beta.6|na||
|teams|Teams|Communications|Microsoft.Teams|microsoft-teams||
|telegram|Telegram|Communications|Telegram.TelegramDesktop|telegram|✓|
|thunderbird|Thunderbird|Communications|Mozilla.Thunderbird|thunderbird|✓|
|vesktop|Vesktop|Communications|Vencord.Vesktop|na|✓|
|viber|Viber|Communications|Rakuten.Viber|viber||
|whatsapp|WhatsApp Desktop|Communications|msstore:9NKSQGP7F2NH|na||
|zoom|Zoom|Communications|Zoom.Zoom|zoom||
|java21|Amazon Corretto 21 (LTS)|Development|Amazon.Corretto.21.JDK|corretto21jdk|✓|
|java25|Amazon Corretto 25 (LTS)|Development|Amazon.Corretto.25.JDK|corretto25jdk|✓|
|java8|Amazon Corretto 8 (LTS)|Development|Amazon.Corretto.8.JDK|corretto8jdk|✓|
|bruno|Bruno|Development|Bruno.Bruno|bruno|✓|
|cmake|CMake|Development|Kitware.CMake|cmake|✓|
|chatgpt|ChatGPT Desktop|Development|msstore:9NT1R1C2HH7J|na||
|claude-code|Claude Code|Development|Anthropic.ClaudeCode|claude-code||
|claude|Claude Desktop|Development|Anthropic.Claude|claude||
|codex|Codex|Development|OpenAI.Codex|codex|✓|
|cursor|Cursor|Development|Anysphere.Cursor|cursoride||
|dockerdesktop|Docker Desktop|Development|Docker.DockerDesktop|docker-desktop||
|fnm|Fast Node Manager|Development|Schniz.fnm|fnm|✓|
|git|Git|Development|Git.Git|git|✓|
|gitextensions|Git Extensions|Development|GitExtensionsTeam.GitExtensions|gitextensions|✓|
|githubcli|GitHub CLI|Development|GitHub.cli|gh|✓|
|githubdesktop|GitHub Desktop|Development|GitHub.GitHubDesktop|git;github-desktop|✓|
|golang|Go|Development|GoLang.Go|golang|✓|
|hugo|Hugo|Development|Hugo.Hugo.Extended|hugo-extended|✓|
|jetbrains|Jetbrains Toolbox|Development|JetBrains.Toolbox|jetbrainstoolbox||
|lazygit|Lazygit|Development|JesseDuffield.lazygit|lazygit|✓|
|Lua|Lua|Development|rjpcomputing.luaforwindows|lua|✓|
|neovim|Neovim|Development|Neovim.Neovim|neovim|✓|
|nodejs|NodeJS|Development|OpenJS.NodeJS|nodejs|✓|
|nodejslts|NodeJS LTS|Development|OpenJS.NodeJS.LTS|nodejs-lts|✓|
|notepadplus|Notepad++|Development|Notepad++.Notepad++|notepadplusplus|✓|
|posh|Oh My Posh (Prompt)|Development|JanDeDobbeleer.OhMyPosh|oh-my-posh|✓|
|opencode|OpenCode|Development|SST.opencode|opencode|✓|
|postman|Postman|Development|Postman.Postman|postman||
|python3|Python3|Development|Python.Python.3.14|python|✓|
|Ruby|Ruby|Development|RubyInstallerTeam.Ruby.4.0|ruby|✓|
|rustlang|Rust|Development|Rustlang.Rust.MSVC|rust|✓|
|starship|Starship (Shell Prompt)|Development|Starship.Starship|starship|✓|
|sublimetext|Sublime Text|Development|SublimeHQ.SublimeText.4|sublimetext4||
|unity|Unity Game Engine|Development|Unity.UnityHub|unity-hub||
|vscode|VS Code|Development|Microsoft.VisualStudioCode|vscode|✓|
|vscodium|VS Codium|Development|VSCodium.VSCodium|vscodium|✓|
|vagrant|Vagrant|Development|Hashicorp.Vagrant|vagrant||
|visualstudio2022|Visual Studio 2022|Development|Microsoft.VisualStudio.2022.Community|visualstudio2022community||
|visualstudio2026|Visual Studio 2026|Development|Microsoft.VisualStudio.Community|visualstudio2026community||
|yarn|Yarn|Development|Yarn.Yarn|yarn|✓|
|Zed|Zed|Development|ZedIndustries.Zed|zed|✓|
|pnpm|pnpm|Development|pnpm.pnpm||✓|
|uv|uv|Development|astral-sh.uv|uv|✓|
|adobe|Adobe Acrobat Reader|Document|Adobe.Acrobat.Reader.64-bit|adobereader||
|foxpdfreader|Foxit PDF Reader|Document|Foxit.FoxitReader|foxitreader||
|joplin|Joplin|Document|Joplin.Joplin|joplin|✓|
|libreoffice|LibreOffice|Document|TheDocumentFoundation.LibreOffice|libreoffice-fresh|✓|
|naps2|NAPS2 (Scanner)|Document|Cyanfish.NAPS2|naps2|✓|
|onlyoffice|ONLYOFFICE Desktop|Document|ONLYOFFICE.DesktopEditors|onlyoffice|✓|
|obsidian|Obsidian|Document|Obsidian.Obsidian|obsidian||
|okular|Okular|Document|KDE.Okular|okular|✓|
|pdf-xchange|PDF-XChange Editor|Document|TrackerSoftware.PDF-XChangeEditor|pdfxchangeeditor||
|pdf24creator|PDF24 Creator|Document|geeksoftwareGmbH.PDF24Creator|pdf24||
|pdfgear|PDFgear|Document|PDFgear.PDFgear|pdfgear||
|pdfsam|PDFsam Basic|Document|PDFsam.PDFsam|pdfsam|✓|
|qownnotes|QOwnNotes|Document|pbek.QOwnNotes|qownnotes|✓|
|simplenote|Simplenote|Document|Automattic.Simplenote|simplenote|✓|
|sumatra|Sumatra PDF|Document|SumatraPDF.SumatraPDF|sumatrapdf|✓|
|xournal|Xournal++|Document|Xournal++.Xournal++|xournalplusplus|✓|
|zotero|Zotero|Document|DigitalScholar.Zotero|zotero|✓|
|battlenet|Battle.net|Games|Blizzard.BattleNet|na||
|cemu|Cemu|Games|Cemu.Cemu|cemu|✓|
|Overwolf|CurseForge|Games|Overwolf.CurseForge|||
|eaapp|EA App|Games|ElectronicArts.EADesktop|ea-app||
|es-de|EmulationStation Desktop Edition|Games|ES-DE.EmulationStation-DE||✓|
|epicgames|Epic Games Launcher|Games|EpicGames.EpicGamesLauncher|epicgameslauncher||
|gog|GOG Galaxy|Games|GOG.Galaxy|goggalaxy||
|geforcenow|GeForce NOW|Games|Nvidia.GeForceNow|nvidia-geforce-now||
|heroiclauncher|Heroic Games Launcher|Games|HeroicGamesLauncher.HeroicGamesLauncher|heroic-games-launcher|✓|
|itch|Itch.io|Games|ItchIo.Itch|itch|✓|
|modrinth|Modrinth App|Games|Modrinth.ModrinthApp|modrinth-app|✓|
|playnite|Playnite|Games|Playnite.Playnite|playnite|✓|
|prismlauncher|Prism Launcher|Games|PrismLauncher.PrismLauncher|prismlauncher|✓|
|roblox|Roblox|Games|Roblox.Roblox|na||
|steam|Steam|Games|Valve.Steam|steam-client||
|ubisoft|Ubisoft Connect|Games|Ubisoft.Connect|ubisoft-connect||
|vrdesktopstreamer|Virtual Desktop Streamer|Games|VirtualDesktop.Streamer|na||
|dotnet10|.NET Desktop Runtime 10|Microsoft Tools|Microsoft.DotNet.DesktopRuntime.10|dotnet-10.0-runtime|✓|
|dotnet6|.NET Desktop Runtime 6|Microsoft Tools|Microsoft.DotNet.DesktopRuntime.6|dotnet-6.0-runtime|✓|
|dotnet8|.NET Desktop Runtime 8|Microsoft Tools|Microsoft.DotNet.DesktopRuntime.8|dotnet-8.0-runtime|✓|
|dotnet9|.NET Desktop Runtime 9|Microsoft Tools|Microsoft.DotNet.DesktopRuntime.9|dotnet-9.0-runtime|✓|
|autoruns|Autoruns|Microsoft Tools|Microsoft.Sysinternals.Autoruns|autoruns||
|dismtools|DISMTools|Microsoft Tools|CodingWondersSoftware.DISMTools.Stable|dismtools|✓|
|ntlite|NTLite|Microsoft Tools|Nlitesoft.NTLite|ntlite-free||
|nuget|NuGet|Microsoft Tools|Microsoft.NuGet|nuget.commandline|✓|
|onedrive|OneDrive|Microsoft Tools|Microsoft.OneDrive|onedrive||
|powershell|PowerShell|Microsoft Tools|Microsoft.PowerShell|powershell-core|✓|
|powertoys|PowerToys|Microsoft Tools|Microsoft.PowerToys|powertoys|✓|
|processexplorer|Process Explorer|Microsoft Tools|Microsoft.Sysinternals.ProcessExplorer|procexp||
|processmonitor|Process Monitor|Microsoft Tools|Microsoft.Sysinternals.ProcessMonitor|procmon||
|pstools|PsTools|Microsoft Tools|Microsoft.Sysinternals.PsTools|pstools||
|rdcman|RDCMan|Microsoft Tools|Microsoft.Sysinternals.RDCMan|rdcman||
|tcpview|TCPView|Microsoft Tools|Microsoft.Sysinternals.TCPView|tcpview||
|vc2015_32|Visual C++ 2015-2022 32-bit|Microsoft Tools|Microsoft.VCRedist.2015+.x86|vcredist2015||
|vc2015_64|Visual C++ 2015-2022 64-bit|Microsoft Tools|Microsoft.VCRedist.2015+.x64|vcredist2015||
|terminal|Windows Terminal|Microsoft Tools|Microsoft.WindowsTerminal|microsoft-windows-terminal|✓|
|aimp|AIMP (Music Player)|Multimedia Tools|AIMP.AIMP|aimp||
|audacity|Audacity|Multimedia Tools|Audacity.Audacity|audacity|✓|
|blender|Blender (3D Graphics)|Multimedia Tools|BlenderFoundation.Blender|blender|✓|
|calibre|Calibre|Multimedia Tools|calibre.calibre|calibre|✓|
|eartrumpet|EarTrumpet (Audio)|Multimedia Tools|File-New-Project.EarTrumpet|eartrumpet|✓|
|fileconverter|File Converter|Multimedia Tools|AdrienAllard.FileConverter|file-converter|✓|
|gimp|GIMP (Image Editor)|Multimedia Tools|GIMP.GIMP.3|gimp|✓|
|handbrake|HandBrake|Multimedia Tools|HandBrake.HandBrake|handbrake|✓|
|imageglass|ImageGlass (Image Viewer)|Multimedia Tools|DuongDieuPhap.ImageGlass|imageglass|✓|
|irfanview|IrfanView|Multimedia Tools|IrfanSkiljan.IrfanView|irfanview||
|klite|K-Lite Codec Standard|Multimedia Tools|CodecGuide.K-LiteCodecPack.Standard|k-litecodecpack-standard||
|mpchc|Media Player Classic - Home Cinema|Multimedia Tools|clsid2.mpc-hc|mpc-hc-clsid2|✓|
|obs|OBS Studio|Multimedia Tools|OBSProject.OBSStudio|obs-studio|✓|
|Paintdotnet|Paint.NET|Multimedia Tools|dotPDN.PaintDotNet|paint.net||
|sharex|ShareX (Screenshots)|Multimedia Tools|ShareX.ShareX|sharex|✓|
|vlc|VLC (Video Player)|Multimedia Tools|VideoLAN.VLC|vlc|✓|
|foobar|foobar2000 (Music Player)|Multimedia Tools|PeterPawlowski.foobar2000|foobar2000||
|itunes|iTunes|Multimedia Tools|Apple.iTunes|itunes||
|mpc-qt|mpc-qt|Multimedia Tools|mpc-qt.mpc-qt|mpc-qt|✓|
|mpv|mpv|Multimedia Tools|shinchiro.mpv||✓|
|nomacs|nomacs|Multimedia Tools|nomacs.nomacs|nomacs|✓|
|advancedip|Advanced IP Scanner|Pro Tools|Famatech.AdvancedIPScanner|advanced-ip-scanner||
|angryipscanner|Angry IP Scanner|Pro Tools|angryziber.AngryIPScanner|angryip|✓|
|cpuz|CPU-Z|Pro Tools|CPUID.CPU-Z|cpu-z||
|cinebenchr23|Cinebench R23|Pro Tools|Maxon.CinebenchR23|na||
|ddu|Display Driver Uninstaller|Pro Tools|Wagnardsoft.DisplayDriverUninstaller|ddu|✓|
|gpuz|GPU-Z|Pro Tools|TechPowerUp.GPU-Z|gpu-z||
|hwmonitor|HWMonitor|Pro Tools|CPUID.HWMonitor|hwmonitor||
|hwinfo|HWiNFO|Pro Tools|REALiX.HWiNFO|hwinfo||
|imhex|ImHex|Pro Tools|WerWolv.ImHex|imhex|✓|
|msiafterburner|MSI Afterburner|Pro Tools|Guru3D.Afterburner|msiafterburner||
|mullvadvpn|Mullvad VPN|Pro Tools|MullvadVPN.MullvadVPN|mullvad-app|✓|
|nmap|Nmap|Pro Tools|Insecure.Nmap|nmap|✓|
|OpenVPN|OpenVPN Connect|Pro Tools|OpenVPNTechnologies.OpenVPNConnect|openvpn-connect||
|protonvpn|Proton VPN|Pro Tools|Proton.ProtonVPN|protonvpn|✓|
|putty|PuTTY|Pro Tools|PuTTY.PuTTY|putty|✓|
|rufus|Rufus Imager|Pro Tools|Rufus.Rufus|rufus|✓|
|simplewall|Simplewall|Pro Tools|Henry++.simplewall|simplewall|✓|
|systeminformer|System Informer|Pro Tools|WinsiderSS.SystemInformer|systeminformer|✓|
|tailscale|Tailscale|Pro Tools|Tailscale.Tailscale|tailscale||
|ventoy|Ventoy|Pro Tools|Ventoy.Ventoy|ventoy|✓|
|winscp|WinSCP|Pro Tools|WinSCP.WinSCP|winscp|✓|
|wireguard|WireGuard|Pro Tools|WireGuard.WireGuard|wireguard|✓|
|wireshark|Wireshark|Pro Tools|WiresharkFoundation.Wireshark|wireshark|✓|
|gsudo|gsudo|Pro Tools|gerardog.gsudo|gsudo|✓|
|jellyfinmediaplayer|Jellyfin Media Player|Selfhosted Tools|Jellyfin.JellyfinMediaPlayer|jellyfin-media-player|✓|
|jellyfinserver|Jellyfin Server|Selfhosted Tools|Jellyfin.Server|jellyfin|✓|
|kodi|Kodi Media Center|Selfhosted Tools|XBMCFoundation.Kodi|kodi|✓|
|localsend|LocalSend|Selfhosted Tools|LocalSend.LocalSend|localsend.install|✓|
|moonlight|Moonlight/GameStream Client|Selfhosted Tools|MoonlightGameStreamingProject.Moonlight|moonlight-qt|✓|
|netbird|NetBird|Selfhosted Tools|Netbird.Netbird|netbird|✓|
|nextclouddesktop|Nextcloud Desktop|Selfhosted Tools|Nextcloud.NextcloudDesktop|nextcloud-client|✓|
|plexdesktop|Plex Desktop|Selfhosted Tools|Plex.Plex|plex||
|plex|Plex Media Server|Selfhosted Tools|Plex.PlexMediaServer|plexmediaserver||
|sunshine|Sunshine/GameStream Server|Selfhosted Tools|LizardByte.Sunshine|sunshine|✓|
|synctrayzor|SyncTrayzor|Selfhosted Tools|GermanCoding.SyncTrayzor|synctrayzor||
|syncthing|Syncthing (CLI / Web UI)|Selfhosted Tools|Syncthing.Syncthing|syncthing|✓|
|1password|1Password|Utilities|AgileBits.1Password|1password||
|7zip|7-Zip|Utilities|7zip.7zip|7zip|✓|
|abdownloadmanager|AB Download Manager|Utilities|amir1376.ABDownloadManager|ab-download-manager|✓|
|anydesk|AnyDesk|Utilities|AnyDesk.AnyDesk|anydesk||
|autohotkey|AutoHotkey|Utilities|AutoHotkey.AutoHotkey|autohotkey|✓|
|bitwarden|Bitwarden|Utilities|Bitwarden.Bitwarden|bitwarden|✓|
|blurautoclicker|BlurAutoClicker|Utilities|Blur009.BlurAutoClicker|na|✓|
|bulkcrapuninstaller|Bulk Crap Uninstaller|Utilities|Klocman.BulkCrapUninstaller|bulk-crap-uninstaller|✓|
|CloudflareWARP|Cloudflare WARP|Utilities|Cloudflare.Warp|warp||
|crystaldiskinfo|Crystal Disk Info|Utilities|CrystalDewWorld.CrystalDiskInfo|crystaldiskinfo|✓|
|crystaldiskmark|Crystal Disk Mark|Utilities|CrystalDewWorld.CrystalDiskMark|crystaldiskmark|✓|
|deskflow|Deskflow|Utilities|Deskflow.Deskflow|deskflow|✓|
|dropbox|Dropbox|Utilities|Dropbox.Dropbox|dropbox||
|enteauth|Ente Auth|Utilities|ente-io.auth-desktop|ente-auth|✓|
|everything|Everything|Utilities|voidtools.Everything|everything||
|flux|F.lux|Utilities|flux.flux|f.lux||
|files|Files|Utilities|FilesCommunity.Files|files|✓|
|glazewm|GlazeWM|Utilities|glzr-io.glazewm|glazewm|✓|
|googledrive|Google Drive|Utilities|Google.GoogleDrive|googledrive||
|xeheditor|HxD Hex Editor|Utilities|MHNexus.HxD|HxD||
|internetdownloadmanager|Internet Download Manager|Utilities|Tonec.InternetDownloadManager|internet-download-manager||
|jpegview|JPEG View|Utilities|sylikc.JPEGView|jpegview|✓|
|keepassxc|KeePassXC|Utilities|KeePassXCTeam.KeePassXC|keepassxc|✓|
|msedgeredirect|MSEdgeRedirect|Utilities|rcmaehl.MSEdgeRedirect|msedgeredirect|✓|
|minitoolpartitionwizard|MiniTool Partition Wizard|Utilities|MiniTool.PartitionWizard.Free|partitionwizard||
|nvclean|NVCleanstall|Utilities|TechPowerUp.NVCleanstall|na||
|nanazip|NanaZip|Utilities|M2Team.NanaZip|nanazip|✓|
|nilesoftShell|Nilesoft Shell|Utilities|Nilesoft.Shell|nilesoft-shell||
|OFGB|OFGB (Oh Frick Go Back)|Utilities|xM4ddy.OFGB|ofgb|✓|
|OPAutoClicker|OPAutoClicker|Utilities|OPAutoClicker.OPAutoClicker|autoclicker||
|openrgb|OpenRGB|Utilities|OpenRGB.OpenRGB|openrgb|✓|
|OVirtualBox|Oracle VirtualBox|Utilities|Oracle.VirtualBox|virtualbox|✓|
|parsec|Parsec|Utilities|Parsec.Parsec|parsec||
|peazip|PeaZip|Utilities|Giorgiotani.Peazip|peazip|✓|
|policyplus|Policy Plus|Utilities|Fleex255.PolicyPlus|na|✓|
|processlasso|Process Lasso|Utilities|BitSum.ProcessLasso|plasso||
|protonauth|Proton Authenticator|Utilities|Proton.ProtonAuthenticator|protonauth|✓|
|protondrive|Proton Drive|Utilities|Proton.ProtonDrive|protondrive|✓|
|protonpass|Proton Pass|Utilities|Proton.ProtonPass|protonpass|✓|
|revo|Revo Uninstaller|Utilities|RevoUninstaller.RevoUninstaller|revo-uninstaller||
|signalrgb|SignalRGB|Utilities|WhirlwindFX.SignalRgb|na||
|sdio|Snappy Driver Installer Origin|Utilities|GlennDelahoy.SnappyDriverInstallerOrigin|sdio|✓|
|startallback|StartAllBack|Utilities|StartIsBack.StartAllBack|StartAllBack||
|teamviewer|TeamViewer|Utilities|TeamViewer.TeamViewer|teamviewer||
|tightvnc|TightVNC|Utilities|GlavSoft.TightVNC|TightVNC|✓|
|totalcommander|Total Commander|Utilities|Ghisler.TotalCommander|TotalCommander||
|ttaskbar|TranslucentTB|Utilities|CharlesMilette.TranslucentTB|translucenttb|✓|
|treesize|TreeSize Free|Utilities|JAMSoftware.TreeSize.Free|treesizefree||
|wingetui|UniGetUI|Utilities|Devolutions.UniGetUI|wingetui|✓|
|winrar|WinRAR|Utilities|RARLab.WinRAR|winrar||
|WiseProgramUninstaller|Wise Program Uninstaller (WiseCleaner)|Utilities|WiseCleaner.WiseProgramUninstaller|na||
|wiztree|WizTree|Utilities|AntibodySoftware.WizTree|wiztree||
|qbittorrent|qBittorrent|Utilities|qBittorrent.qBittorrent|qbittorrent|✓|
