#ifndef PayloadDir
  #error Supply the self-contained ARM64 publish directory as /DPayloadDir=...
#endif
#ifndef AppDisplayName
  #define AppDisplayName "Amethyst Windows"
#endif
#ifndef InstallerAppId
  #define InstallerAppId "{{4DE6F7E3-B39D-4CB7-AA84-FE2B07E96755}"
#endif
#ifndef ConfigDest
  #define ConfigDest "{localappdata}\AmethystWindows"
#endif
#ifndef SetupCompression
  #define SetupCompression "lzma2/fast"
#endif
#ifndef SetupSolidCompression
  #define SetupSolidCompression "yes"
#endif

[Setup]
AppId={#InstallerAppId}
AppName={#AppDisplayName}
AppVersion=2026.11.01.1
AppPublisher=bayfitt
AppPublisherURL=https://github.com/bayfitt/amethystwindows
AppSupportURL=https://github.com/bayfitt/amethystwindows/issues
AppUpdatesURL=https://github.com/bayfitt/amethystwindows/releases/latest
DefaultDirName={localappdata}\Programs\AmethystWindows-ARM64
DefaultGroupName={#AppDisplayName}
DisableProgramGroupPage=yes
DisableWelcomePage=no
LicenseFile=..\LICENSE
SetupIconFile=..\AmethystWindows\Logo.ico
UninstallDisplayIcon={app}\AmethystWindows.exe
PrivilegesRequired=lowest
ArchitecturesAllowed=arm64
ArchitecturesInstallIn64BitMode=arm64
MinVersion=10.0.22000
OutputBaseFilename=AmethystWindows-Setup-win-arm64-2026.11.01.1
Compression={#SetupCompression}
SolidCompression={#SetupSolidCompression}
WizardStyle=modern
CloseApplications=no
RestartApplications=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"
Name: "startup"; Description: "Start Amethyst when I sign in"
Name: "recommended"; Description: "Use the recommended settings for a new installation"

[Files]
Source: "{#PayloadDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\LICENSE"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\examples\bayfitt-settings\MySettings-portable.json"; DestDir: "{#ConfigDest}"; DestName: "MySettings.json"; Tasks: recommended; Flags: onlyifdoesntexist uninsneveruninstall

[Icons]
Name: "{userprograms}\{#AppDisplayName}\{#AppDisplayName}"; Filename: "{app}\AmethystWindows.exe"; WorkingDir: "{app}"; IconFilename: "{app}\AmethystWindows.exe"
Name: "{userdesktop}\{#AppDisplayName}"; Filename: "{app}\AmethystWindows.exe"; WorkingDir: "{app}"; IconFilename: "{app}\AmethystWindows.exe"; Tasks: desktopicon
Name: "{userstartup}\{#AppDisplayName}"; Filename: "{app}\AmethystWindows.exe"; Parameters: "--background"; WorkingDir: "{app}"; IconFilename: "{app}\AmethystWindows.exe"; Tasks: startup

[Run]
Filename: "{app}\AmethystWindows.exe"; Description: "Launch Amethyst Windows (opens in the system tray)"; Flags: nowait postinstall skipifsilent

[Code]
function AmethystIsRunning: Boolean;
var
  Locator, Services, Processes: Variant;
begin
  { Avoid starting a second tiler, and avoid replacing a running installation. }
  try
    Locator := CreateOleObject('WbemScripting.SWbemLocator');
    Services := Locator.ConnectServer('', 'root\CIMV2');
    Processes := Services.ExecQuery('SELECT ProcessId FROM Win32_Process WHERE Name = ''AmethystWindows.exe''');
    Result := Processes.Count > 0;
  except
    { If the check fails, ask the user to close Amethyst rather than risk two copies. }
    Result := True;
  end;
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  Result := '';
  if AmethystIsRunning then
    Result := 'Amethyst Windows may already be running. Exit it from the system tray, then run Setup again.';
end;

function InitializeUninstall: Boolean;
begin
  Result := not AmethystIsRunning;
  if not Result then
    SuppressibleMsgBox('Exit Amethyst Windows from the system tray before uninstalling.', mbInformation, MB_OK, IDOK);
end;
