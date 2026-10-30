#ifndef PublishDir
  #error PublishDir must point to the self-contained publish folder.
#endif
#ifndef OutputDir
  #error OutputDir must point to the installer output folder.
#endif
#define AppVersion GetFileVersion(PublishDir + "\WindowsAudit.exe")

[Setup]
AppId={{E45F0B73-903B-46B6-994A-3419AB425E91}
AppName=Windows Audit
AppVersion={#AppVersion}
AppPublisher=Richard Harris
AppPublisherURL=https://github.com/richardharris84
DefaultDirName={localappdata}\Programs\Windows Audit
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0
OutputDir={#OutputDir}
OutputBaseFilename=WindowsAudit-Setup
SetupIconFile=..\src\WindowsServiceAudit.App\Assets\WindowsAudit.ico
UninstallDisplayIcon={app}\WindowsAudit.exe
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
RestartApplications=no

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{userprograms}\Windows Audit"; Filename: "{app}\WindowsAudit.exe"; WorkingDir: "{app}"
Name: "{userdesktop}\Windows Audit"; Filename: "{app}\WindowsAudit.exe"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\WindowsAudit.exe"; Description: "Launch Windows Audit"; Flags: nowait postinstall skipifsilent
