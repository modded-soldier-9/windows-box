# Windows 11 services catalog (Windows Box)

Defaults reflect Windows 11 23H2/24H2 **Pro** clean installs (Home differences noted). "M(T)" = Manual (Trigger Start); "A(D)" = Automatic (Delayed). **Every default must be re-verified by our VM harness (`sc qc` / `Get-Service`) per build before shipping** — Microsoft changes start types between builds.

Risk: **0** never touch (boot/security) · **1** safe to set Manual · **2** safe to Disable if feature unused · **3** situational, breaks a visible feature · **4** don't (breaks updates/Store/login).
Profiles benefiting: G gamer · L laptop/battery · O office/work · P privacy · D developer · M minimal/low-RAM.

## Telemetry, diagnostics, privacy
| Name | Display name | Default | What it does | Depends on / dependents | Risk | Rec. | Profiles |
|-|-|-|-|-|-|-|-|
| DiagTrack | Connected User Experiences and Telemetry | Auto | Sends diagnostic/usage data to Microsoft | RPCSS | 2 | Disabled | P G M |
| dmwappushservice | Device Management WAP Push message Routing | M(T) | WAP push for MDM/telemetry | RPCSS | 2 (3 if Intune) | Disabled unless MDM | P |
| diagsvc | Diagnostic Execution Service | M(T) | Runs troubleshooter diagnostics | – | 1 | Manual | – |
| DPS | Diagnostic Policy Service | Auto | Problem detection/troubleshooting; feeds SRUM | – | 2 | Manual | P M |
| WdiServiceHost / WdiSystemHost | Diagnostic Service/System Host | Manual | Hosts DPS diagnostics | DPS | 1 | Manual | – |
| WerSvc | Windows Error Reporting Service | M(T) | Uploads crash reports | – | 2 | Disabled (keeps local dumps off) | P |
| PcaSvc | Program Compatibility Assistant | Auto | Detects legacy-app issues | – | 2 | Manual | G M |
| lfsvc | Geolocation Service | M(T) | Location for apps, Find my device, auto time zone | – | 3 laptops | Disabled on desktops | P |
| MapsBroker | Downloaded Maps Manager | A(D) | Offline maps | – | 2 | Disabled/Manual | P M |
| RetailDemo | Retail Demo Service | Manual | Store demo mode | – | 2 | Disabled | all |
| wisvc | Windows Insider Service | M(T) | Insider builds | – | 2 | Disabled unless Insider | P |
| WMPNetworkSvc (if present) | WMP Network Sharing | Manual | DLNA sharing | – | 2 | Disabled | P |
| CDPSvc / CDPUserSvc_* | Connected Devices Platform | A(D)/Auto | Phone Link, nearby sharing, timeline, **also Night light/settings sync glue** | – | 3 | Leave (known breakage of Night light, notifications) | – |
| OneSyncSvc_* | Sync Host | A(D) | Mail/Calendar/People sync | – | 3 | Leave unless no MS account | P |
| PimIndexMaintenanceSvc_*, UnistoreSvc_*, UserDataSvc_* | Contact/User data | Manual | Contacts/People data | – | 3 | Leave (per-user templates; disabling breaks apps) | – |
| MessagingService_* | MessagingService | Manual | SMS relay | – | 2 | Disabled | P |

## Gaming / Xbox
| Name | Display name | Default | What it does | Depends on / dependents | Risk | Rec. | Profiles |
|-|-|-|-|-|-|-|-|
| XblAuthManager | Xbox Live Auth Manager | Manual | Xbox sign-in | – | 3 G | Leave for gamers, Disabled for non-gamers | M O |
| XblGameSave | Xbox Live Game Save | Manual | Cloud saves | XblAuthManager | 3 G | same | M O |
| XboxNetApiSvc | Xbox Live Networking Service | Manual | Xbox networking / NAT | – | 3 G | same | M O |
| XboxGipSvc | Xbox Accessory Management | M(T) | Xbox controller firmware/config | – | 3 | Leave if Xbox pad | M |
| GameInputSvc (24H2) | GameInput Service | Manual | Game input stack | – | 3 G | Leave | – |
| BcastDVRUserService_* | GameDVR and Broadcast | Manual | Game bar capture | – | 2 | Manual (+ disable GameDVR policy) | G |

## Hardware-gated (detect hardware first)
| Name | Display name | Default | What it does | Depends on / dependents | Risk | Rec. | Profiles |
|-|-|-|-|-|-|-|-|
| Spooler | Print Spooler | Auto | Printing, PDF printer | RPCSS, http | 3 | Disabled only if no printers (also PrintNightmare hardening) | M P |
| PrintNotify | Printer Extensions and Notifications | Manual | Printer UI | Spooler | 2 | Manual | – |
| Fax | Fax | Manual | Fax | Spooler, TapiSrv | 2 | Disabled | all |
| bthserv / BTAGService / BthAvctpSvc | Bluetooth Support / Audio Gateway / AVCTP | M(T) | Bluetooth stack, headsets | – | 3 | Disabled only if no BT radio | – |
| WbioSrvc | Windows Biometric Service | Manual (Auto on biometric HW) | Windows Hello fingerprint/face | – | 3 | Leave if Hello used | – |
| SCardSvr / ScDeviceEnum / SCPolicySvc | Smart Card* | Manual/M(T) | Smart cards | – | 2 home, 4 enterprise | Disabled at home | M |
| TabletInputService | Touch Keyboard and Handwriting Panel | M(T) | Touch keyboard, **also emoji panel/IME input in some builds** | – | 3 | Leave | – |
| SensorService / SensrSvc / SensorDataService | Sensor services | M(T) | Rotation, ambient light | – | 3 L | Leave on laptops/2-in-1 | G (desktop) |
| WiaRpc / stisvc | Still Image / WIA | Manual | Scanners/cameras | – | 2 | Manual | – |
| FrameServer / FrameServerMonitor | Windows Camera Frame Server | M(T) | Webcam sharing | – | 4 for webcam users | Leave | – |
| PhoneSvc | Phone Service | M(T) | Telephony state | – | 2 | Manual | – |
| SEMgrSvc | Payments and NFC/SE Manager | Manual | NFC payments | – | 2 | Disabled | P |
| icssvc | Windows Mobile Hotspot | M(T) | Hotspot | – | 2 | Manual | – |
| WwanSvc | WWAN AutoConfig | Manual | Cellular modems | – | 3 L | Leave if LTE | – |
| WlanSvc | WLAN AutoConfig | Auto | Wi-Fi | – | 4 if Wi-Fi | Never on laptops | – |

## Performance-adjacent (frequently tweaked, often wrongly)
| Name | Display name | Default | What it does | Depends on / dependents | Risk | Rec. | Profiles |
|-|-|-|-|-|-|-|-|
| SysMain | SysMain (Superfetch) | Auto | Prefetch/memory compression tuning | – | 3 | Leave on HDD/low RAM; Manual optional on NVMe+32GB (benefit unproven) | G (debatable) |
| WSearch | Windows Search | A(D) | Indexing for Start/Explorer/Outlook search | RPCSS | 3 | Leave for office; Disabled only on request (Outlook search breaks) | M G |
| DoSvc | Delivery Optimization | A(D)/Manual | P2P update delivery | – | 4 (protected) | Use policy (DODownloadMode=0/99), not service | P |
| BITS | Background Intelligent Transfer | Manual | Update/Store downloads | RPCSS, EventSystem | 4 | Never | – |
| wuauserv / UsoSvc / WaaSMedicSvc | Windows Update / Update Orchestrator / Medic | Manual / A(D) / Manual (protected) | Updates; Medic re-enables tampering | – | 4 | Never disable; control via policy (deferral) | – |
| TrkWks | Distributed Link Tracking Client | Auto | NTFS link tracking across network | – | 1 | Manual | M |
| WpnService | Windows Push Notifications System | Auto | Toasts, Store push | – | 3 | Leave | – |
| StorSvc | Storage Service | M(T) | Storage Sense, app install locations | – | 1 | Manual | – |
| CscService | Offline Files | Manual (Pro) | Offline file cache | – | 2 home, 4 domain | Disabled at home | M |
| SSDPSRV / upnphost / FDResPub / fdPHost | SSDP / UPnP / Function Discovery | Manual | Network discovery, DLNA, network printers | – | 3 | Manual | P |
| lmhosts | TCP/IP NetBIOS Helper | M(T) | NetBIOS name resolution | – | 2 | Manual | P |
| RemoteRegistry | Remote Registry | Disabled | Remote reg access | – | 1 | keep Disabled | P |
| RemoteAccess | Routing and Remote Access | Disabled | Routing | – | 1 | keep Disabled | – |
| SharedAccess | Internet Connection Sharing | M(T) | ICS, **also used by Mobile Hotspot / some VM NAT** | – | 3 | Manual (WinUtil sets Disabled — breaks hotspot) | – |
| TermService / UmRdpService / SessionEnv | Remote Desktop | Manual | RDP host (Pro) | – | 3 | Manual | P |
| WinRM | Windows Remote Management | Manual | PS remoting | – | 2 home, 4 enterprise | Manual | – |
| AJRouter | AllJoyn Router | M(T) | IoT AllJoyn (deprecated) | – | 1 | Disabled | all |
| NcbService / NcaSvc | Network Connection Broker / Connectivity Assistant | M(T) | Network status for apps / DirectAccess | – | 3 | Leave | – |
| iphlpsvc | IP Helper | Auto | IPv6 transition (Teredo, 6to4, IP-HTTPS) | – | 3 G (Xbox Teredo) | Manual if no IPv6 transition needed | M |
| SgrmBroker / SecurityHealthService / WinDefend / mpssvc / BFE | Defender, Firewall, Base Filtering | Auto | Security | – | 0 | **Never** | – |

## Virtualization & dev (role-gated)
| Name | Display name | Default | What it does | Depends on / dependents | Risk | Rec. | Profiles |
|-|-|-|-|-|-|-|-|
| vmcompute / vmms / HvHost / vmic* | Hyper-V services | Manual (if feature on) | Hyper-V, WSL2, Sandbox, Docker | – | 4 for D | Never on dev profile | D |
| LxssManager / WSLService | WSL | Manual | WSL | – | 4 for D | Leave | D |
| CmService | Container Manager | Manual | Containers/Sandbox | – | 3 D | Leave | D |
| ssh-agent | OpenSSH Auth Agent | Disabled | SSH keys | – | 1 | Auto for D on request | D |

## Core / never touch (risk 0)
RpcSs, RpcEptMapper, DcomLaunch, LSM, BrokerInfrastructure, SamSs, Schedule, EventLog, Power, PlugPlay, ProfSvc, CryptSvc, Dhcp, Dnscache, nsi, NlaSvc, netprofm, Winmgmt, gpsvc, AudioSrv, AudioEndpointBuilder, Themes, UserManager, StateRepository, AppXSvc, ClipSVC, InstallService, TokenBroker, TimeBrokerSvc, SystemEventsBroker, CoreMessagingRegistrar, KeyIso, VaultSvc, Appinfo (UAC!), wscsvc, EFS, DeviceAssociationService, DispBrokerDesktopSvc, camsvc, W32Time (Manual-trigger; leave), seclogon, msiserver, TrustedInstaller, VSS/swprv (needed for restore points — our own backups depend on it).

## Per-user service templates
Services with `_xxxxx` suffix (CDPUserSvc, OneSyncSvc, BcastDVRUserService, PimIndex…, cbdhsvc clipboard, DevicesFlowUserSvc, PrintWorkflowUserSvc, NPSMSvc media controls) are instantiated from a template at logon. Changing the instance is lost; change the template (`HKLM\SYSTEM\CurrentControlSet\Services\<name>` Start value) instead, and warn: cbdhsvc → clipboard history breaks, NPSMSvc → media flyout breaks.

## Protected services
WaaSMedicSvc, DoSvc, SecurityHealthService, WinDefend, Sense, wscsvc and others reject `sc config` (Access denied, TrustedInstaller/PPL). Windows Box must **not** take ownership to force changes; offer the supported policy alternative and label "Managed by Windows".

## Sources
- Microsoft, *Guidance on disabling system services on Windows Server 2016 with Desktop Experience* (per-service descriptions, "OK to disable" judgements; still the best official reference): https://learn.microsoft.com/windows-server/security/windows-services/security-guidelines-for-disabling-system-services-in-windows-server
- Microsoft, *Per-user services in Windows*: https://learn.microsoft.com/windows/application-management/per-user-services-in-windows
- Microsoft, *Manage connections from Windows OS components to Microsoft services* (DiagTrack, DoSvc, lfsvc policies): https://learn.microsoft.com/windows/privacy/manage-connections-from-windows-operating-system-components-to-microsoft-services
- Microsoft, Delivery Optimization reference: https://learn.microsoft.com/windows/deployment/do/waas-delivery-optimization-reference
- Microsoft, PrintNightmare/Spooler guidance (CVE-2021-34527): https://msrc.microsoft.com/update-guide/vulnerability/CVE-2021-34527
- BlackViper service configurations (archived; Win10 era, methodology reference only): https://web.archive.org/web/2020*/blackviper.com/service-configurations/
- WinUtil `config/tweaks.json` WPFTweaksServices history (shrunk list after breakage): https://github.com/ChrisTitusTech/winutil
- Sophia Script service functions (MIT): https://github.com/farag2/Sophia-Script-for-Windows
- Community: r/WindowsHelp, r/sysadmin, ElevenForum service-default tutorials (e.g. https://www.elevenforum.com/tutorials/) for per-build defaults and breakage reports.
