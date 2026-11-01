# Windows 11 services: full catalog

Generated 1 Nov 2026 for Windows 11 23H2, 24H2 and 25H2. **290 services** (24 per-user templates, 33 optional or build/feature dependent). Machine-readable: `services-full.json`. Curated engine entries live in `catalog/services/*.yaml`; this file is the reference inventory.

> **Verification status: 0 of 290 defaults are confirmed on a clean VM. Every default below is UNVERIFIED** and compiled from Microsoft Learn, community references and prior knowledge. The M1 VM harness (`docs/testing-vms.md`) must harvest `sc qc` / `Get-Service` output per edition and build and replace these values. Kernel driver entries (type 1/2) are excluded.

## Risk scale (MASTER-PLAN section 5)

- **0**: Never touch (boot/security critical)
- **1**: Safe to set Manual
- **2**: Safe to Disable if feature unused
- **3**: Situational, breaks a visible feature
- **4**: Breaks updates, Store, login or security

## Totals

| Risk | Count |
|-|-|
| 0 | 60 |
| 1 | 65 |
| 2 | 47 |
| 3 | 93 |
| 4 | 25 |

| Default start type (Pro) | Count |
|-|-|
| Manual | 147 |
| ManualTrigger | 66 |
| Automatic | 53 |
| AutomaticDelayed | 12 |
| Disabled | 11 |
| AutomaticTrigger | 1 |

## Edition differences

Where the edition columns are not shown separately, Home, Pro and Enterprise share the default. Known differences:
- **Possibly absent or inert on Home (unverified):** CscService, AppMgmt, PeerDistSvc, HvHost and other Hyper-V host services, BitLocker management (Home has Device Encryption only). Netlogon, EFS, SessionEnv and TermService exist on Home, but domain join and the RDP host role are blocked by the edition.
- **Enterprise focused (present but Disabled or idle elsewhere):** AppVClient, UevAgentService, MsKeyboardFilter, Sense, AppIDSvc (AppLocker). hpatchmon (hotpatch) may be absent outside Enterprise (unverified).
- **Hardware dependent start types:** WbioSrvc becomes Automatic on devices with biometric hardware; SensorService and related services trigger only with sensors.
- **Feature or app dependent:** Hyper-V (vmms, vmcompute, HvHost), WSL (LxssManager, WSLService), IIS (W3SVC, WAS, AppHostSvc), MSMQ, OpenSSH Server, Edge updaters, OneDrive helper.

## Conflicts between sources

| Service | Conflict | Position taken |
|-|-|-|
| DoSvc | Older references list Automatic (Delayed); some 24H2 reports show Manual (Trigger). Microsoft says to control it by policy | Listed AutomaticDelayed, unverified; policy only |
| BITS | Win10 era references list Automatic (Delayed); Win11 shows Manual | Manual, unverified |
| SharedAccess | WinUtil sets Disabled; Microsoft notes Mobile Hotspot relies on ICS | Risk 3, keep Manual (Trigger) |
| SysMain | Community "disable on SSD" advice vs Microsoft guidance to leave enabled | Risk 3, benefit unproven |
| CDPSvc / CDPUserSvc | Optimizer lists mark safe to disable; community reports broken Night light, notifications, Phone Link | Risk 3 |
| StiSvc | Some references show Automatic | Manual, unverified |
| SgrmBroker | Community reports removal on some 24H2 builds | Listed, presence unverified |
| MapsBroker | Maps app retired; service may be missing on 25H2 | Listed, presence unverified |
| Microsoft Server 2016 guidance | Written for Server with Desktop Experience; client defaults differ (e.g. Xbox, per-user services) | Used for descriptions and intent, not for client defaults |

## Notable findings
- Roughly half of all services are already Manual or trigger-started and cost nothing at idle; "disable everything" yields little beyond a handful of Automatic services.
- Per-user templates (`*_xxxxx`) must be changed on the template key; editing the instance is lost at next sign-in.
- Protected services (WaaSMedicSvc, UsoSvc, DoSvc, WinDefend, mpssvc, SecurityHealthService, wscsvc, sppsvc, gpsvc, Dnscache, WinHttpAutoProxySvc, ClipSVC, CoreMessagingRegistrar, MDCoreSvc, Sense, WdNisSvc, SgrmBroker) reject changes; Windows Box must use policy alternatives.
- New in 24H2/25H2 (unverified names/defaults): ApxSvc, hpatchmon, LocalKdc, McmSvc, PrintDeviceConfigurationService, PrintScanBrokerService, refsdedupsvc, whesvc, ZTHELPER, CloudBackupRestoreSvc.
- Windows Mixed Reality was removed in 24H2, so MixedRealityOpenXRSvc, spectrum, SharedRealitySvc, perceptionsimulation and VacSvc may be absent.

## Sources

- https://learn.microsoft.com/windows-server/security/windows-services/security-guidelines-for-disabling-system-services-in-windows-server
- https://learn.microsoft.com/windows/iot/iot-enterprise/optimize/services
- https://learn.microsoft.com/windows/application-management/per-user-services-in-windows
- https://learn.microsoft.com/windows/privacy/manage-connections-from-windows-operating-system-components-to-microsoft-services
- https://learn.microsoft.com/windows/deployment/do/waas-delivery-optimization-reference
- https://www.elevenforum.com/tutorials/ (per-service default tutorials)
- https://web.archive.org/web/2020*/blackviper.com/service-configurations/ (Windows 10 era, methodology only)
- https://github.com/ChrisTitusTech/winutil (config/tweaks.json, WPFTweaksServices)
- https://github.com/farag2/Sophia-Script-for-Windows

## Catalog

Columns: start type is the Pro default (A = Automatic, AD = Automatic Delayed, AT = Automatic Trigger, M = Manual, MT = Manual Trigger, D = Disabled). Edition column lists differences only. All defaults unverified.

| Service | Display name | Start (Pro) | Editions | What it does | Depends on | Risk | Risk reason | Notes |
|-|-|-|-|-|-|-|-|-|
| `AarSvc_xxxxx (per-user)` | Agent Activation Runtime | M | same | Activates voice assistants (Cortana/Copilot voice) for the user | - | 2 | Only voice agents use it | per-user template |
| `AJRouter` | AllJoyn Router Service | MT | same | Routes AllJoyn IoT messages (deprecated protocol) | - | 1 | Deprecated, nothing modern uses it | - |
| `ALG` | Application Layer Gateway Service | M | same | Plug-in support for legacy Internet Connection Sharing | - | 1 | Only legacy ICS | - |
| `AppHostSvc` | Application Host Helper Service | A | same | IIS config history | - | 1 | Only with IIS installed | Optional feature |
| `AppIDSvc` | Application Identity | MT | same | Checks app identity for AppLocker policies | RpcSs, AppID, CryptSvc | 3 | AppLocker/WDAC rules stop working | Enterprise relevance higher |
| `Appinfo` | Application Information | MT | same | Runs apps with admin rights (UAC prompts) | RpcSs, ProfSvc | 0 | UAC elevation breaks completely | - |
| `AppMgmt` | Application Management | M | home: MayBeAbsent (unverified) | Group Policy software installation | - | 2 | Only domain software deployment | Pro/Enterprise only |
| `AppReadiness` | App Readiness | M | same | Prepares Store apps at first sign-in and after updates | - | 4 | First logon and feature updates hang | - |
| `AppVClient` | Microsoft App-V Client | D | same | Runs App-V virtualized applications | RpcSs, AppvVfs, AppvStrm | 1 | Disabled by default, enterprise only | Enterprise/Pro feature |
| `AppXSvc` | AppX Deployment Service (AppXSVC) | MT | same | Installs, updates and removes Store apps | RpcSs, staterepository | 0 | Store, Start and many inbox apps break | - |
| `ApxSvc` | Windows Virtual Audio Device Proxy Service | M | same | Proxy for virtual audio devices (24H2+) | - | 3 | Virtual audio devices may fail | 24H2+, unverified name |
| `AssignedAccessManagerSvc` | AssignedAccessManager Service | M | same | Kiosk (assigned access) mode | - | 1 | Only kiosk setups | - |
| `AudioEndpointBuilder` | Windows Audio Endpoint Builder | A | same | Manages audio devices for Windows Audio | - | 0 | No sound | - |
| `Audiosrv` | Windows Audio | A | same | Plays all system and app audio | RpcSs, AudioEndpointBuilder | 0 | No sound | - |
| `autotimesvc` | Cellular Time | M | same | Sets time from mobile network | - | 1 | Only cellular devices | - |
| `AxInstSV` | ActiveX Installer (AxInstSV) | M | same | Installs ActiveX controls from intranet sites per policy | RpcSs | 1 | Legacy IE-era | - |
| `BcastDVRUserService_xxxxx (per-user)` | GameDVR and Broadcast User Service | M | same | Game Bar screen recording and broadcasts | - | 2 | Only Game Bar capture | per-user template |
| `BDESVC` | BitLocker Drive Encryption Service | MT | home: MayBeAbsent (unverified) | BitLocker unlock prompts and management | - | 4 | Encrypted drives may not unlock/manage | Home has Device Encryption only |
| `BFE` | Base Filtering Engine | A | same | Core of firewall and IPsec filtering | RpcSs | 0 | Firewall, VPNs and IPsec stop | - |
| `BITS` | Background Intelligent Transfer Service | M | same | Background downloads for Windows Update, Store, Defender | RpcSs | 4 | Updates stop | Some sources list AD |
| `BluetoothUserService_xxxxx (per-user)` | Bluetooth User Support Service | MT | same | Per-user Bluetooth features | - | 3 | Bluetooth pairing UI | per-user template |
| `BrokerInfrastructure` | Background Tasks Infrastructure Service | A | same | Controls which background tasks may run | RpcEptMapper, DcomLaunch | 0 | Core system broker | - |
| `Browser` | Computer Browser | D | same | Legacy SMB1 network browse list | LanmanWorkstation, LanmanServer | 1 | Only present if SMB1 installed | Usually absent |
| `BTAGService` | Bluetooth Audio Gateway Service | MT | same | Bluetooth headset hands-free profile | RpcSs | 3 | BT headset mics break | - |
| `BthAvctpSvc` | AVCTP service | MT | same | Bluetooth audio remote control (play/pause) | RpcSs | 3 | BT media keys break | - |
| `bthserv` | Bluetooth Support Service | MT | same | Discovers and pairs Bluetooth devices | RpcSs | 3 | All Bluetooth stops | - |
| `camsvc` | Capability Access Manager Service | M | same | Grants apps access to camera, mic, location | RpcSs | 0 | App privacy permissions break | - |
| `CaptureService_xxxxx (per-user)` | CaptureService | M | same | Screen capture for apps (Windows.Graphics.Capture) | - | 3 | Snipping/screen share apps fail | per-user template |
| `cbdhsvc_xxxxx (per-user)` | Clipboard User Service | M | same | Clipboard history and cloud clipboard | - | 3 | Win+V history breaks | per-user template |
| `CDPSvc` | Connected Devices Platform Service | AD | same | Phone Link, nearby sharing, device discovery | ncbservice, RpcSs, Tcpip | 3 | Phone Link, nearby share, some settings sync break | Community reports Night light issues |
| `CDPUserSvc_xxxxx (per-user)` | Connected Devices Platform User Service | A | same | Per-user part of Connected Devices Platform | - | 3 | Notifications and Phone Link side effects | per-user template |
| `CertPropSvc` | Certificate Propagation | MT | same | Copies smart card certificates to the user store | RpcSs | 2 | Only smart card users | - |
| `ClipSVC` | Client License Service (ClipSVC) | MT | same | Licenses Store apps | RpcSs | 4 | Store apps refuse to launch | Protected |
| `CloudBackupRestoreSvc_xxxxx (per-user)` | Cloud Backup and Restore Service | M | same | Windows Backup app cloud restore (24H2+) | - | 3 | Windows Backup app | per-user template; 24H2+, unverified |
| `cloudidsvc` | Microsoft Cloud Identity Service | M | same | Cloud identity tenant restrictions | - | 2 | Enterprise tenant restrictions | - |
| `CmService` | Container Manager Service | M | same | Windows containers, Sandbox | - | 3 | Sandbox/containers | Feature-gated |
| `COMSysApp` | COM+ System Application | M | same | Configures COM+ components | SENS, EventSystem, RpcSs | 3 | Some installers and apps fail | - |
| `ConsentUxUserSvc_xxxxx (per-user)` | ConsentUX User Service | M | same | Consent dialogs for device pairing (Miracast etc.) | - | 3 | Pairing prompts fail | per-user template |
| `CoreMessagingRegistrar` | CoreMessaging | A | same | Inter-component messaging for UI | RpcSs | 0 | Shell UI breaks | Protected |
| `CredentialEnrollmentManagerUserSvc_xxxxx (per-user)` | CredentialEnrollmentManagerUserSvc | M | same | Windows Hello credential enrollment | - | 3 | Hello setup fails | per-user template |
| `CryptSvc` | Cryptographic Services | A | same | Certificates, signature checks, catalog database | RpcSs | 0 | Updates, installs, TLS break | - |
| `CscService` | Offline Files | M | home: MayBeAbsent (unverified) | Offline Files cache for network shares | RpcSs | 2 | Only domain/NAS offline files | Pro+; Home absent; WinUtil sets Disabled |
| `DcomLaunch` | DCOM Server Process Launcher | A | same | Starts COM servers; foundation of the system | - | 0 | System will not boot properly | - |
| `dcsvc` | Declared Configuration (DC) service | M | same | Declared-configuration MDM (newer builds) | - | 2 | MDM only | 23H2+, unverified |
| `defragsvc` | Optimize drives | M | same | Scheduled TRIM/defrag | RpcSs | 3 | SSD TRIM and HDD defrag stop | - |
| `DeviceAssociationBrokerSvc_xxxxx (per-user)` | DeviceAssociationBroker | M | same | Lets Store apps pair devices | - | 3 | App-driven pairing fails | per-user template |
| `DeviceAssociationService` | Device Association Service | MT | same | Pairs wired/wireless devices | RpcSs | 0 | Device pairing breaks | - |
| `DeviceInstall` | Device Install Service | MT | same | Installs drivers for new hardware | - | 0 | New hardware not installed | - |
| `DevicePickerUserSvc_xxxxx (per-user)` | DevicePicker | M | same | Device picker UI (Cast, Miracast) | - | 3 | Cast picker fails | per-user template |
| `DevicesFlowUserSvc_xxxxx (per-user)` | DevicesFlow | M | same | Connect/pair device flows | - | 3 | Pairing flows fail | per-user template |
| `DevQueryBroker` | DevQuery Background Discovery Broker | MT | same | Background device discovery for apps | RpcSs | 3 | Device discovery in apps | - |
| `Dhcp` | DHCP Client | A | same | Gets IP addresses from routers | Afd, Tcpip, NSI | 0 | No network | - |
| `diagnosticshub.standardcollector.service` | Microsoft (R) Diagnostics Hub Standard Collector Service | M | same | Collects ETW traces for diagnostics tools (Visual Studio) | - | 1 | Only diagnostics tools | - |
| `diagsvc` | Diagnostic Execution Service | MT | same | Runs troubleshooter actions | - | 1 | Troubleshooters only | - |
| `DiagTrack` | Connected User Experiences and Telemetry | A | same | Sends diagnostic and usage data to Microsoft | RpcSs | 2 | Only telemetry upload; policy is the supported control | - |
| `DialogBlockingService` | DialogBlockingService | D | same | Blocks dialogs in some setup states | - | 1 | Disabled by default | - |
| `DispBrokerDesktopSvc` | Display Policy Service | AD | same | Display settings and policy | - | 0 | Display configuration breaks | - |
| `DisplayEnhancementService` | Display Enhancement Service | MT | same | Adaptive brightness and display enhancement | - | 3 | Laptop auto brightness | - |
| `DmEnrollmentSvc` | Device Management Enrollment Service | M | same | MDM enrollment | - | 2 | Only MDM enrollment | 4 if enrolled |
| `dmwappushservice` | Device Management Wireless Application Protocol (WAP) Push message Routing Service | MT | same | Routes MDM push messages | RpcSs | 2 | MDM sync only | 4 if Intune |
| `Dnscache` | DNS Client | A | same | Caches DNS lookups | Tcpip, nsi | 0 | Name resolution slows/breaks; cannot be disabled via UI | Protected |
| `DoSvc` | Delivery Optimization | AD | same | Peer-to-peer and CDN downloads for updates and Store | RpcSs | 4 | Updates and Store downloads fail | Protected; MS docs: use policy. Some sources show M(T) on 24H2 |
| `dot3svc` | Wired AutoConfig | M | same | 802.1X authentication on wired networks | RpcSs, Ndisuio | 2 | Only 802.1X networks | - |
| `DPS` | Diagnostic Policy Service | A | same | Problem detection and troubleshooting, resource usage data | - | 2 | Network diagnostics and SRUM data stop | - |
| `DsmSvc` | Device Setup Manager | MT | same | Downloads device metadata and apps | RpcSs | 3 | Device-specific software not fetched | - |
| `DsSvc` | Data Sharing Service | MT | same | Data brokering between apps | RpcSs | 2 | App-to-app data sharing | - |
| `DusmSvc` | Data Usage | A | same | Network data usage, metered limits | - | 3 | Data usage and metered settings | - |
| `EapHost` | Extensible Authentication Protocol | M | same | EAP auth for Wi-Fi/VPN enterprise | RpcSs | 3 | Enterprise Wi-Fi/VPN fail | - |
| `edgeupdate` | Microsoft Edge Update Service (edgeupdate) | AD | same | Updates Edge | RpcSs | 3 | Edge stops updating (security) | Edge component |
| `edgeupdatem` | Microsoft Edge Update Service (edgeupdatem) | M | same | Updates Edge on demand | RpcSs | 3 | Edge stops updating | Edge component |
| `EFS` | Encrypting File System (EFS) | MT | same | File encryption with EFS | RpcSs | 0 | EFS-encrypted files unreadable | Pro+ |
| `embeddedmode` | Embedded Mode | MT | same | IoT background apps | BrokerInfrastructure | 1 | IoT only | - |
| `EntAppSvc` | Enterprise App Management Service | M | same | Enterprise app management | - | 2 | Enterprise only | - |
| `EventLog` | Windows Event Log | A | same | Records system and app events | - | 0 | Many services fail; no logs | - |
| `EventSystem` | COM+ Event System | A | same | Event notification for COM components | RpcSs | 0 | Many dependents fail | - |
| `Fax` | Fax | M | same | Sends and receives faxes | Spooler, TapiSrv | 2 | Only fax users | Optional on 24H2 (capability) |
| `fdPHost` | Function Discovery Provider Host | M | same | Hosts network discovery providers | RpcSs, http | 3 | Network discovery of PCs/printers | - |
| `FDResPub` | Function Discovery Resource Publication | M | same | Publishes this PC on the network | RpcSs, http, fdPHost | 3 | PC not visible on network | - |
| `fhsvc` | File History Service | M | same | File History backups | - | 3 | File History stops | - |
| `FileSyncHelper` | FileSyncHelper | M | same | OneDrive Files On-Demand helper | - | 3 | OneDrive placeholders | Installed by OneDrive; not inbox |
| `FontCache` | Windows Font Cache Service | A | same | Caches fonts for apps | - | 0 | Slow, broken font rendering | - |
| `FrameServer` | Windows Camera Frame Server | M | same | Lets several apps share the webcam | - | 4 | Webcams break in many apps | - |
| `FrameServerMonitor` | Windows Camera Frame Server Monitor | M | same | Monitors camera frame server | - | 3 | Camera presence detection | - |
| `GameInputRedistService` | GameInput Redist Service | M | same | GameInput redistributable updater | - | 3 | Game input | Installed by games; unverified |
| `GameInputSvc` | GameInput Service | M | same | Game controller input stack | - | 3 | Some game input | 24H2+ (installed with GameInput redist) |
| `gpsvc` | Group Policy Client | A | same | Applies Group Policy | RpcSs, Mup | 0 | Policy not applied; protected | Protected |
| `GraphicsPerfSvc` | GraphicsPerfSvc | MT | same | GPU performance counters | - | 1 | Only perf tooling | - |
| `hidserv` | Human Interface Device Service | MT | same | Hot keys on keyboards/remotes | - | 3 | Multimedia keys break | - |
| `hpatchmon` | Hotpatch Monitoring Service | M | home: MayBeAbsent (unverified), pro: MayBeAbsent (unverified) | Hotpatch update monitoring (24H2 Enterprise) | - | 4 | Hotpatch servicing | 24H2+, Enterprise, unverified |
| `HvHost` | HV Host Service | M | home: MayBeAbsent (unverified) | Hyper-V host performance counters | - | 4 | Hyper-V/WSL2 hosts | Pro+ with Hyper-V |
| `icssvc` | Windows Mobile Hotspot Service | MT | same | Mobile Hotspot | - | 2 | Only hotspot users | - |
| `IKEEXT` | IKE and AuthIP IPsec Keying Modules | MT | same | IPsec key exchange for VPNs | BFE | 3 | IPsec VPNs fail | - |
| `InstallService` | Microsoft Store Install Service | M | same | Installs Store apps | RpcSs | 4 | Store installs fail | - |
| `InventorySvc` | Inventory and Compatibility Appraisal service | MT | same | Compatibility appraisal for upgrades | - | 3 | Feature update readiness checks | 22H2+ |
| `iphlpsvc` | IP Helper | A | same | IPv6 transition tech (Teredo, 6to4, IP-HTTPS) | RpcSs, Tcpip, nsi, WinHttpAutoProxySvc | 3 | IPv6 tunnels, Xbox Teredo, DirectAccess | - |
| `IpxlatCfgSvc` | IP Translation Configuration Service | MT | same | 464XLAT for IPv6-only mobile networks | - | 2 | Cellular IPv6-only | - |
| `KeyIso` | CNG Key Isolation | MT | same | Protects private keys | RpcSs | 0 | Certificates and Wi-Fi auth break | - |
| `KtmRm` | KtmRm for Distributed Transaction Coordinator | M | same | Transactions between MSDTC and KTM | RpcSs, SamSS | 2 | Only distributed transactions | - |
| `LanmanServer` | Server | A | same | File and printer sharing (SMB server) | SamSS, Srv2 | 3 | Sharing from this PC stops | - |
| `LanmanWorkstation` | Workstation | A | same | SMB client to network shares | Bowser, MRxSmb20, NSI | 3 | Network drives stop | - |
| `lfsvc` | Geolocation Service | MT | same | Location for apps, auto time zone, Find my device | RpcSs | 3 | Location features break | - |
| `LicenseManager` | Windows License Manager Service | MT | same | Store license infrastructure | RpcSs | 4 | Store apps | - |
| `lltdsvc` | Link-Layer Topology Discovery Mapper | M | same | Network map | RpcSs, Tcpip | 1 | Network map only | - |
| `lmhosts` | TCP/IP NetBIOS Helper | MT | same | NetBIOS name resolution | Afd | 2 | Legacy name resolution | - |
| `LocalKdc` | Kerberos Local Key Distribution Center | M | same | Local KDC for Kerberos without DC (24H2+) | - | 2 | Newer local auth | 24H2+, unverified |
| `LSM` | Local Session Manager | A | same | Manages user sessions | RpcEptMapper, RpcSs | 0 | Logon breaks | - |
| `LxpSvc` | Language Experience Service | M | same | Installs language packs | - | 3 | Language packs | - |
| `LxssManager` | LxssManager | M | same | Legacy WSL (inbox WSL1) manager | - | 4 | WSL breaks for developers | Present only with WSL optional feature |
| `MapsBroker` | Downloaded Maps Manager | AD | same | Offline maps for the Maps app | RpcSs | 2 | Maps app offline maps only | Maps app retired; may be absent on 25H2 |
| `McmSvc` | Mobile Connectivity Management Service | M | same | Cellular connectivity (24H2+) | - | 2 | Cellular only | 24H2+, unverified |
| `McpManagementService` | McpManagementService | M | same | Universal Print/Mcp management | - | 1 | Rarely used | - |
| `MDCoreSvc` | Microsoft Defender Core Service | A | same | Defender core engine helper (2024+) | - | 0 | Security | Protected; added via platform update |
| `MessagingService_xxxxx (per-user)` | MessagingService | M | same | SMS and text messaging | - | 2 | Phone SMS relay | per-user template |
| `MicrosoftEdgeElevationService` | Microsoft Edge Elevation Service (MicrosoftEdgeElevationService) | M | same | Edge elevated operations | - | 3 | Edge updates/features | - |
| `MixedRealityOpenXRSvc` | Windows Mixed Reality OpenXR Service | M | same | Mixed Reality OpenXR runtime | - | 1 | WMR is deprecated (removed 24H2) | May be absent 24H2+ |
| `mpssvc` | Windows Defender Firewall | A | same | Firewall | mpsdrv, bfe | 0 | Security; store apps fail to install | Protected |
| `MSDTC` | Distributed Transaction Coordinator | M | same | Coordinates database/message transactions | RpcSs, SamSS | 2 | Only transaction apps | - |
| `MSiSCSI` | Microsoft iSCSI Initiator Service | M | same | Connects to iSCSI storage | - | 1 | iSCSI only | - |
| `msiserver` | Windows Installer | M | same | Installs MSI packages | RpcSs | 0 | Software can't be installed | - |
| `MsKeyboardFilter` | Microsoft Keyboard Filter | D | same | Blocks key combos (Enterprise lockdown) | - | 1 | Disabled by default | Enterprise |
| `MSMQ` | Message Queuing | A | same | Message queuing | RPCSS, LanmanServer | 1 | Only with MSMQ feature | Optional feature |
| `NaturalAuthentication` | Natural Authentication | M | same | Dynamic Lock signal aggregation | - | 2 | Dynamic Lock | - |
| `NcaSvc` | Network Connectivity Assistant | MT | same | DirectAccess status | BFE, dnscache, NSI, iphlpsvc | 3 | DirectAccess | - |
| `NcbService` | Network Connection Broker | MT | same | Network notifications for Store apps | RpcSs, tcpip | 3 | Apps miss network change events | - |
| `NcdAutoSetup` | Network Connected Devices Auto-Setup | MT | same | Auto-installs network devices | netprofm | 2 | Auto setup of network printers | - |
| `Netlogon` | Netlogon | M | same | Domain logon secure channel | LanmanWorkstation | 4 | Domain logon fails | Pro+ |
| `Netman` | Network Connections | M | same | Network Connections folder | RpcSs | 3 | Network adapter UI | - |
| `netprofm` | Network List Service | M | same | Identifies networks and their profiles | RpcSs, nlasvc | 0 | Network status breaks | - |
| `NetSetupSvc` | Network Setup Service | M | same | Installs network drivers and settings | RpcSs | 0 | Adapter install fails | - |
| `NetTcpPortSharing` | Net.Tcp Port Sharing Service | D | same | WCF port sharing | - | 1 | Disabled by default | - |
| `NgcCtnrSvc` | Microsoft Passport Container | M | same | Windows Hello key container | - | 4 | PIN/Hello sign-in fails | - |
| `NgcSvc` | Microsoft Passport | M | same | Windows Hello keys | - | 4 | PIN/Hello sign-in fails | - |
| `NlaSvc` | Network Location Awareness | A | same | Detects network and connectivity | NSI, RpcSs, TcpIp, Dhcp, Eventlog | 0 | Network profile/Internet status breaks | - |
| `NPSMSvc_xxxxx (per-user)` | Now Playing Session Manager Service | M | same | Media controls in quick settings | - | 3 | Media flyout breaks | per-user template |
| `nsi` | Network Store Interface Service | A | same | Network state notifications | rpcss, nsiproxy | 0 | No networking | - |
| `OneSyncSvc_xxxxx (per-user)` | Sync Host | AD | same | Syncs mail, contacts, calendar | - | 3 | Mail/Calendar sync | per-user template |
| `p2pimsvc` | Peer Networking Identity Manager | M | same | Peer-to-peer identity | - | 1 | Legacy P2P | - |
| `p2psvc` | Peer Networking Grouping | M | same | Peer-to-peer groups | p2pimsvc, PNRPsvc | 1 | Legacy P2P | - |
| `P9RdrService_xxxxx (per-user)` | P9RdrService | MT | same | Plan 9 file server for WSL file access | - | 3 | WSL \\wsl$ access | per-user template |
| `PcaSvc` | Program Compatibility Assistant Service | A | same | Detects compatibility issues in old apps | RpcSs | 2 | Compatibility prompts | - |
| `PeerDistSvc` | BranchCache | M | home: MayBeAbsent (unverified) | BranchCache content caching | http | 1 | Enterprise only | Pro+ |
| `PenService_xxxxx (per-user)` | PenService | M | same | Pen settings and features | - | 3 | Pen buttons | per-user template |
| `perceptionsimulation` | Windows Perception Simulation Service | M | same | Mixed Reality simulation | - | 1 | Dev/WMR only | - |
| `PerfHost` | Performance Counter DLL Host | M | same | Hosts remote perf counters | - | 1 | Remote perf monitoring | - |
| `PhoneSvc` | Phone Service | MT | same | Telephony state | - | 2 | Phone/VoIP integration | - |
| `PimIndexMaintenanceSvc_xxxxx (per-user)` | Contact Data | M | same | Indexes contacts | UnistoreSvc | 3 | People/contact search | per-user template |
| `pla` | Performance Logs & Alerts | M | same | Perf logging (perfmon data collector sets) | RpcSs | 1 | Perfmon logs only | - |
| `PlugPlay` | Plug and Play | M | same | Detects and configures hardware | - | 0 | Hardware stops working | - |
| `PNRPAutoReg` | PNRP Machine Name Publication Service | M | same | Peer name publication | pnrpsvc | 1 | Legacy | - |
| `PNRPsvc` | Peer Name Resolution Protocol | M | same | Peer name resolution | p2pimsvc | 1 | Legacy | - |
| `PolicyAgent` | IPsec Policy Agent | MT | same | IPsec policy | Tcpip, bfe | 3 | IPsec policies | - |
| `Power` | Power | A | same | Power policy and notifications | - | 0 | Power management breaks | - |
| `PrintDeviceConfigurationService` | Print Device Configuration Service | M | same | Configures print devices (24H2 protected print) | - | 3 | Printing setup | 24H2+, unverified |
| `PrintNotify` | Printer Extensions and Notifications | M | same | Printer UI and notifications | RpcSs | 2 | Printer pop-ups | - |
| `PrintScanBrokerService` | PrintScanBrokerService | M | same | Brokers print/scan for apps | - | 3 | Printing from apps | 24H2+, unverified |
| `PrintWorkflowUserSvc_xxxxx (per-user)` | PrintWorkflow | M | same | Print workflow apps | - | 3 | Printer apps | per-user template |
| `ProfSvc` | User Profile Service | A | same | Loads user profiles at sign-in | RpcSs | 0 | Cannot sign in | - |
| `PushToInstall` | Windows PushToInstall Service | M | same | Remote Store installs | - | 2 | Remote install from web Store | - |
| `QWAVE` | Quality Windows Audio Video Experience | M | same | Network QoS for AV streaming | rpcss, psched | 1 | Rarely used | - |
| `RasAuto` | Remote Access Auto Connection Manager | M | same | Auto-dial remote connections | RasMan | 1 | Legacy dial-up | - |
| `RasMan` | Remote Access Connection Manager | M | same | VPN and dial-up connections | SstpSvc | 4 | All VPNs fail | - |
| `refsdedupsvc` | ReFS Dedup Service | M | same | ReFS deduplication (24H2) | - | 1 | ReFS dedup only | 24H2+, unverified |
| `RemoteAccess` | Routing and Remote Access | D | same | LAN/WAN routing | RasMan, bfe, http, RpcSs | 1 | Disabled by default | - |
| `RemoteRegistry` | Remote Registry | D | same | Remote registry editing | RpcSs | 1 | Disabled by default; keep | - |
| `RetailDemo` | Retail Demo Service | M | same | Store demo mode | - | 1 | Unused outside shops | - |
| `RmSvc` | Radio Management Service | M | same | Airplane mode and radio switches | RpcSs | 3 | Airplane mode toggles break | - |
| `RpcEptMapper` | RPC Endpoint Mapper | A | same | Maps RPC endpoints | - | 0 | System fails | - |
| `RpcLocator` | Remote Procedure Call (RPC) Locator | M | same | Legacy RPC name service | - | 1 | Legacy | - |
| `RpcSs` | Remote Procedure Call (RPC) | A | same | Core RPC; almost everything depends on it | RpcEptMapper, DcomLaunch | 0 | System fails | - |
| `SamSs` | Security Accounts Manager | A | same | Local accounts database | RpcSs | 0 | Logon fails | - |
| `SCardSvr` | Smart Card | MT | same | Smart card readers | - | 2 | Smart cards only | 4 in enterprise |
| `ScDeviceEnum` | Smart Card Device Enumeration Service | MT | same | Enumerates smart card readers | - | 2 | Smart cards only | - |
| `Schedule` | Task Scheduler | A | same | Runs scheduled tasks | RPCSS, SystemEventsBroker | 0 | Maintenance and updates break; protected | - |
| `SCPolicySvc` | Smart Card Removal Policy | M | same | Locks PC on card removal | RpcSs | 2 | Smart cards only | - |
| `SDRSVC` | Windows Backup | M | same | Legacy Backup and Restore | RpcSs | 3 | Legacy backups | - |
| `seclogon` | Secondary Logon | M | same | Run as different user | - | 3 | Run as different user | - |
| `SecurityHealthService` | Windows Security Service | M | same | Windows Security app health | RpcSs | 0 | Security | Protected |
| `SEMgrSvc` | Payments and NFC/SE Manager | M | same | NFC payments and secure element | RpcSs | 2 | NFC payments | - |
| `SENS` | System Event Notification Service | A | same | Logon/network/power events to COM | EventSystem | 0 | Many dependents | - |
| `Sense` | Windows Defender Advanced Threat Protection Service | M | same | Microsoft Defender for Endpoint | - | 0 | EDR (enterprise) | Protected |
| `SensorDataService` | Sensor Data Service | M | same | Sensor data | - | 3 | Sensors on 2-in-1 | - |
| `SensorService` | Sensor Service | MT | same | Sensor management (rotation, light) | - | 3 | Rotation, auto brightness | - |
| `SensrSvc` | Sensor Monitoring Service | MT | same | Monitors sensors | - | 3 | Adaptive brightness | - |
| `SessionEnv` | Remote Desktop Configuration | M | same | RDP configuration | RpcSs | 3 | RDP host | Pro+ |
| `SgrmBroker` | System Guard Runtime Monitor Broker | AD | same | System Guard runtime integrity | - | 0 | Security | Protected; removed in some 24H2 builds per community, unverified |
| `SharedAccess` | Internet Connection Sharing (ICS) | MT | same | Internet sharing, used by Mobile Hotspot | Netman, WinMgmt | 3 | Hotspot breaks | WinUtil sets Disabled: conflict |
| `SharedRealitySvc` | Spatial Data Service | M | same | Mixed Reality spatial data | - | 1 | WMR only | - |
| `ShellHWDetection` | Shell Hardware Detection | A | same | AutoPlay events | RpcSs | 3 | AutoPlay stops | - |
| `shpamsvc` | Shared PC Account Manager | D | same | Shared PC mode | - | 1 | Disabled by default | - |
| `smphost` | Microsoft Storage Spaces SMP | M | same | Storage Spaces management provider | RpcSs | 3 | Storage Spaces management | - |
| `SmsRouter` | Microsoft Windows SMS Router Service | MT | same | Routes SMS | RpcSs | 2 | SMS only | - |
| `SNMPTrap` | SNMP Trap | M | same | Receives SNMP traps | - | 1 | SNMP only | - |
| `spectrum` | Windows Perception Service | M | same | Spatial perception for MR | - | 1 | WMR only | - |
| `Spooler` | Print Spooler | A | same | Printing, including PDF printer | RPCSS, http | 3 | All printing stops | - |
| `sppsvc` | Software Protection | AD | same | Windows activation and licensing | RpcSs | 4 | Activation fails | Protected |
| `SSDPSRV` | SSDP Discovery | M | same | Discovers UPnP devices | HTTP, NSI | 3 | DLNA/UPnP discovery | - |
| `ssh-agent` | OpenSSH Authentication Agent | D | same | Holds SSH keys | - | 1 | Disabled by default | - |
| `sshd` | OpenSSH SSH Server | M | same | SSH server | - | 1 | Only with OpenSSH Server capability | Optional capability |
| `SstpSvc` | Secure Socket Tunneling Protocol Service | M | same | SSTP VPN | - | 3 | SSTP VPN; RasMan depends | - |
| `StateRepository` | State Repository Service | A | same | App state database | RpcSs | 0 | Start and apps break | - |
| `StiSvc` | Windows Image Acquisition (WIA) | M | same | Scanners and cameras | RpcSs | 2 | Scanners | Some sources show A |
| `StorSvc` | Storage Service | MT | same | Storage Sense, app install location | RpcSs | 1 | Manual is safe | - |
| `svsvc` | Spot Verifier | M | same | Verifies file system corruption | RpcSs | 3 | Disk repair | - |
| `swprv` | Microsoft Software Shadow Copy Provider | M | same | Shadow copies | RpcSs | 0 | Restore points and backups fail | - |
| `SysMain` | SysMain | A | same | Prefetch and memory tuning | - | 3 | Slower launches on HDD/low RAM; benefit of disabling unproven | - |
| `SystemEventsBroker` | System Events Broker | A | same | Events for background tasks | RpcEptMapper | 0 | Core broker | - |
| `TabletInputService` | Touch Keyboard and Handwriting Panel Service | MT | same | Touch keyboard, handwriting, emoji panel | - | 3 | Emoji panel and IME input | - |
| `TapiSrv` | Telephony | M | same | Telephony API | RpcSs | 2 | Fax/legacy telephony | - |
| `TermService` | Remote Desktop Services | M | same | RDP host | RPCSS | 3 | RDP into this PC | Home: client only |
| `TextInputManagementService` | Text Input Management Service | A | same | Text input, IME | - | 0 | Typing breaks | 22H2+ |
| `Themes` | Themes | A | same | Visual themes | - | 0 | UI looks broken | - |
| `TieringEngineService` | Storage Tiers Management | M | same | Storage Spaces tiering | - | 1 | Storage Spaces only | - |
| `TimeBrokerSvc` | Time Broker | MT | same | Timed background tasks | RpcSs | 0 | Background tasks | - |
| `TokenBroker` | Web Account Manager | M | same | Microsoft account and work account sign-in for apps | - | 0 | App sign-ins break | - |
| `TrkWks` | Distributed Link Tracking Client | A | same | Repairs shortcuts to moved files on NTFS | RpcSs | 1 | Manual safe | - |
| `TroubleshootingSvc` | Recommended Troubleshooting Service | M | same | Recommended troubleshooters | - | 1 | Troubleshooters | - |
| `TrustedInstaller` | Windows Modules Installer | M | same | Installs Windows updates and components | - | 0 | Updates fail | - |
| `tzautoupdate` | Auto Time Zone Updater | D | same | Automatic time zone | - | 1 | Disabled by default | - |
| `UdkUserSvc_xxxxx (per-user)` | Udk User Service | M | same | Shell UI components (Start, search) | - | 0 | Shell breaks | per-user template |
| `UevAgentService` | User Experience Virtualization Service | D | same | UE-V settings roaming | - | 1 | Disabled by default | Enterprise |
| `uhssvc` | Microsoft Update Health Service | AD | same | Update health remediation | - | 4 | Update health fixes | Installed via KB |
| `UmRdpService` | Remote Desktop Services UserMode Port Redirector | M | same | RDP device redirection | TermService | 3 | RDP | - |
| `UnistoreSvc_xxxxx (per-user)` | User Data Storage | M | same | User data for contacts/mail | - | 3 | Mail/People | per-user template |
| `upnphost` | UPnP Device Host | M | same | Hosts UPnP devices | SSDPSRV, HTTP | 2 | Hosting UPnP | - |
| `UserDataSvc_xxxxx (per-user)` | User Data Access | M | same | App access to user data | - | 3 | Mail/People | per-user template |
| `UserManager` | User Manager | A | same | User runtime components | RpcSs, ProfSvc | 0 | Start and logon break | - |
| `UsoSvc` | Update Orchestrator Service | AD | same | Schedules Windows Updates | RpcSs | 4 | Updates stop | Protected |
| `VacSvc` | Volumetric Audio Compositor Service | M | same | Spatial audio for MR | - | 1 | WMR only | - |
| `VaultSvc` | Credential Manager | M | same | Stores saved credentials | RpcSs | 0 | Saved passwords lost | - |
| `vds` | Virtual Disk | M | same | Disk management | RpcSs | 3 | Disk Management | - |
| `vmcompute` | Hyper-V Host Compute Service | M | same | Hyper-V containers, WSL2, Sandbox | - | 4 | WSL2/Docker/Sandbox | Feature-gated |
| `vmicguestinterface` | Hyper-V Guest Service Interface | MT | same | Guest services when this PC is a VM | - | 1 | Only inside VMs | - |
| `vmicheartbeat` | Hyper-V Heartbeat Service | MT | same | VM heartbeat | - | 1 | Only inside VMs | - |
| `vmickvpexchange` | Hyper-V Data Exchange Service | MT | same | VM data exchange | - | 1 | Only inside VMs | - |
| `vmicrdv` | Hyper-V Remote Desktop Virtualization Service | MT | same | Enhanced session | - | 1 | Only inside VMs | - |
| `vmicshutdown` | Hyper-V Guest Shutdown Service | MT | same | Host-initiated shutdown | - | 1 | Only inside VMs | - |
| `vmictimesync` | Hyper-V Time Synchronization Service | MT | same | VM time sync | VmGid | 1 | Only inside VMs | - |
| `vmicvmsession` | Hyper-V PowerShell Direct Service | MT | same | PowerShell Direct | - | 1 | Only inside VMs | - |
| `vmicvss` | Hyper-V Volume Shadow Copy Requestor | MT | same | VM backups | - | 1 | Only inside VMs | - |
| `vmms` | Hyper-V Virtual Machine Management | A | same | Manages Hyper-V VMs | - | 4 | Hyper-V | Feature-gated |
| `VSS` | Volume Shadow Copy | M | same | Shadow copies for backups and restore points | RpcSs | 0 | Restore points fail (our safety net) | - |
| `W32Time` | Windows Time | MT | same | Clock synchronization | - | 3 | Clock drift breaks TLS/Kerberos | - |
| `W3SVC` | World Wide Web Publishing Service | A | same | IIS web server | WAS, HTTP | 1 | Only with IIS | Optional feature |
| `WaaSMedicSvc` | Windows Update Medic Service | M | same | Repairs Windows Update components | - | 4 | Protected; re-enables update services | Protected |
| `WalletService` | WalletService | M | same | Wallet clients | - | 1 | Rare | - |
| `WarpJITSvc` | Warp JIT Service | MT | same | JIT for WARP software rendering | - | 3 | Software GPU rendering | - |
| `WAS` | Windows Process Activation Service | M | same | IIS process activation | RpcSs | 1 | Only with IIS/WCF | Optional feature |
| `wbengine` | Block Level Backup Engine Service | M | same | Windows Backup image | - | 3 | System image backup | - |
| `WbioSrvc` | Windows Biometric Service | M | same | Fingerprint/face sign-in | RpcSs | 3 | Hello biometrics | A on devices with biometric hardware |
| `Wcmsvc` | Windows Connection Manager | AT | same | Chooses network connections | RpcSs, NSI | 0 | Wi-Fi/cellular switching breaks | - |
| `wcncsvc` | Windows Connect Now - Config Registrar | M | same | WPS setup | - | 1 | WPS only | - |
| `WdiServiceHost` | Diagnostic Service Host | M | same | Diagnostics for DPS | - | 1 | Diagnostics | - |
| `WdiSystemHost` | Diagnostic System Host | M | same | Diagnostics for DPS | - | 1 | Diagnostics | - |
| `WdNisSvc` | Microsoft Defender Antivirus Network Inspection Service | M | same | Network intrusion protection | - | 0 | Security | Protected |
| `WebClient` | WebClient | MT | same | WebDAV client | MRxDAV | 2 | WebDAV/SharePoint mapped drives | - |
| `webthreatdefsvc` | Web Threat Defense Service | M | same | SmartScreen phishing protection | RpcSs | 0 | Phishing protection | - |
| `webthreatdefusersvc_xxxxx (per-user)` | Web Threat Defense User Service | A | same | Per-user phishing protection | - | 0 | Phishing protection | per-user template |
| `Wecsvc` | Windows Event Collector | M | same | Collects events from remote sources | HTTP | 1 | Event forwarding | - |
| `WEPHOSTSVC` | Windows Encryption Provider Host Service | MT | same | Encryption providers | RpcSs | 3 | Encryption providers | - |
| `wercplsupport` | Problem Reports Control Panel Support | M | same | Problem reports UI | RpcSs | 1 | Reports UI | - |
| `WerSvc` | Windows Error Reporting Service | MT | same | Uploads crash reports | - | 2 | Crash reports | - |
| `WFDSConMgrSvc` | Wi-Fi Direct Services Connection Manager Service | M | same | Wi-Fi Direct | - | 3 | Miracast, Wi-Fi Direct printers | - |
| `whesvc` | Windows Health and Optimized Experiences | M | same | Health and optimization experiences (24H2) | - | 2 | unclear purpose | 24H2+, unverified |
| `WiaRpc` | Still Image Acquisition Events | M | same | Scanner/camera button events | RpcSs | 2 | Scanner buttons | - |
| `WinDefend` | Microsoft Defender Antivirus Service | A | same | Antivirus | RpcSs | 0 | Security | Protected |
| `WinHttpAutoProxySvc` | WinHTTP Web Proxy Auto-Discovery Service | M | same | Proxy auto-discovery | Dhcp | 0 | Network/proxy breaks; protected | Protected |
| `Winmgmt` | Windows Management Instrumentation | A | same | WMI management | RpcSs | 0 | Many tools and services fail | - |
| `WinRM` | Windows Remote Management (WS-Management) | M | same | PowerShell remoting | RPCSS, HTTP | 2 | Remoting | 4 enterprise |
| `wisvc` | Windows Insider Service | MT | same | Insider builds | - | 2 | Insider only | - |
| `WlanSvc` | WLAN AutoConfig | A | same | Wi-Fi | nativewifip, RpcSs, Ndisuio | 4 | No Wi-Fi | - |
| `wlidsvc` | Microsoft Account Sign-in Assistant | MT | same | Microsoft account sign-in | RpcSs | 4 | MSA sign-in and Store | - |
| `wlpasvc` | Local Profile Assistant Service | MT | same | eSIM profiles | RpcSs | 2 | eSIM only | - |
| `WManSvc` | Windows Management Service | M | same | Provisioning and management | - | 3 | Provisioning packages | - |
| `wmiApSrv` | WMI Performance Adapter | M | same | WMI perf counters | - | 1 | Perf tooling | - |
| `WMPNetworkSvc` | Windows Media Player Network Sharing Service | M | same | DLNA media sharing | http, WSearch | 2 | Media sharing | Absent with WMP removal |
| `workfolderssvc` | Work Folders | M | same | Work Folders sync | - | 2 | Enterprise only | - |
| `WpcMonSvc` | Parental Controls | M | same | Family Safety enforcement | - | 3 | Family Safety | - |
| `WPDBusEnum` | Portable Device Enumerator Service | MT | same | MTP devices, removable storage policy | RpcSs | 3 | Phones/cameras over MTP | - |
| `WpnService` | Windows Push Notifications System Service | A | same | Push notifications | rpcss | 3 | Notifications | - |
| `WpnUserService_xxxxx (per-user)` | Windows Push Notifications User Service | A | same | Per-user toasts | - | 3 | Notifications | per-user template |
| `wscsvc` | Security Center | AD | same | Security status | RpcSs | 0 | Security status | Protected |
| `WSearch` | Windows Search | AD | same | File/content indexing | RPCSS | 3 | Start/Outlook search slow or broken | - |
| `WSLService` | WSL Service | A | same | Store WSL service | - | 4 | WSL breaks for developers | Installed with Store WSL; start type unverified |
| `wuauserv` | Windows Update | MT | same | Downloads and installs updates | rpcss | 4 | Updates stop | - |
| `WwanSvc` | WWAN AutoConfig | M | same | Mobile broadband | RpcSs, NdisUio | 3 | LTE/5G modems | - |
| `XblAuthManager` | Xbox Live Auth Manager | M | same | Xbox sign-in | - | 3 | Xbox games/app | - |
| `XblGameSave` | Xbox Live Game Save | MT | same | Xbox cloud saves | UserManager, XblAuthManager | 3 | Cloud saves | - |
| `XboxGipSvc` | Xbox Accessory Management Service | MT | same | Xbox controllers | - | 3 | Xbox controller config | - |
| `XboxNetApiSvc` | Xbox Live Networking Service | M | same | Xbox networking/NAT | BFE, mpssvc, IKEEXT, KeyIso | 3 | Xbox multiplayer | - |
| `ZTHELPER` | ZTDNS Helper service | M | same | Zero Trust DNS (24H2+) | - | 3 | Enterprise ZTDNS | 24H2+, unverified |
