#ifndef PayloadDir
 #define PayloadDir "..\artifacts\rc3-about-payload"
#endif
#ifndef ReleaseDir
 #define ReleaseDir "..\release\rc3-about"
#endif
#define ExpectedPayloadVersion "1.0.0-rc.3"
#define ActualPayloadVersion GetStringFileInfo(PayloadDir + "\ScreenWisp.exe", "ProductVersion")
#if ActualPayloadVersion != ExpectedPayloadVersion
 #error Installer payload must be the corrected RC3 (1.0.0-rc.3). Rebuild with tools/BuildRelease.ps1.
#endif
[Setup]
AppId={{A3D96A88-1026-4AD9-9025-E4F7E403C5A6}
AppName=ScreenWisp
AppVersion=1.0.0-rc.3
AppVerName=ScreenWisp 1.0.0 RC 3
AppPublisher=dontpalevo
AppPublisherURL=https://github.com/dontpalevo
VersionInfoCompany=dontpalevo
VersionInfoProductName=ScreenWisp
VersionInfoProductTextVersion=1.0.0-rc.3
VersionInfoCopyright=ScreenWisp additions (c) 2026 dontpalevo; Desktop Ink original code (c) 2026 atman-33
VersionInfoVersion=1.0.0.0
VersionInfoDescription=ScreenWisp Setup (1.0.0 RC 3)
DefaultDirName={localappdata}\Programs\ScreenWisp
DefaultGroupName=ScreenWisp
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.17763
OutputDir={#ReleaseDir}
OutputBaseFilename=ScreenWisp-Setup
SetupIconFile=..\src\ScreenWisp\Assets\screenwisp.ico
UninstallDisplayIcon={app}\ScreenWisp.exe
LicenseFile=..\LICENSE
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
DisableProgramGroupPage=yes
DisableDirPage=auto
UsePreviousAppDir=yes
AppMutex=Local\ScreenWisp.SingleInstance
SetupMutex=Local\ScreenWisp.Setup
CloseApplications=no
RestartApplications=no
UninstallDisplayName=ScreenWisp
[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"
[CustomMessages]
english.LaunchScreenWisp=Launch ScreenWisp
russian.LaunchScreenWisp=Запустить ScreenWisp
[Files]
Source: "{#PayloadDir}\ScreenWisp.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#PayloadDir}\LICENSE"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#PayloadDir}\THIRD-PARTY-NOTICES.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#PayloadDir}\README.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#PayloadDir}\README.ru.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#PayloadDir}\docs\*.md"; DestDir: "{app}\docs"; Flags: ignoreversion
Source: "screenwisp.installed"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#PayloadDir}\runtime-licenses\*"; DestDir: "{app}\runtime-licenses"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#PayloadDir}\docs\screenshots\*.png"; DestDir: "{app}\docs\screenshots"; Flags: ignoreversion
Source: "{#PayloadDir}\branding\png\ScreenWisp-128.png"; DestDir: "{app}\branding\png"; Flags: ignoreversion
Source: "{#PayloadDir}\LICENSE-SCREENWISP"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#PayloadDir}\COPYRIGHT.md"; DestDir: "{app}"; Flags: ignoreversion
[Icons]
Name: "{userprograms}\ScreenWisp"; Filename: "{app}\ScreenWisp.exe"; WorkingDir: "{app}"
[Run]
Filename: "{app}\ScreenWisp.exe"; Parameters: "--configure-startup"; Flags: runhidden waituntilterminated
Filename: "{app}\ScreenWisp.exe"; Description: "{cm:LaunchScreenWisp}"; Flags: nowait postinstall skipifsilent
[UninstallRun]
Filename: "{app}\ScreenWisp.exe"; Parameters: "--remove-startup"; Flags: runhidden waituntilterminated; RunOnceId: "CleanupScreenWispStartup"
; No AppData files are in the uninstall log. User preferences are always preserved.

[UninstallDelete]
Type: filesandordirs; Name: "{localappdata}\ScreenWisp\Autostart"
