#ifndef TestBuild
  #define ProductName "Desktop Goose 2"
  #define ProductId "DesktopGoose2"
  #define OutputName "DesktopGoose2-Setup"
#else
  #define ProductName "Desktop Goose 2 Installer Test"
  #define ProductId "DesktopGoose2-IsolatedTest"
  #define OutputName "DesktopGoose2-Setup-Test"
#endif
[Setup]
AppId={#ProductId}
AppName={#ProductName}
AppVersion=0.3.1
DefaultDirName={localappdata}\Programs\{#ProductId}
DefaultGroupName={#ProductName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
MinVersion=10.0
OutputDir=..\artifacts
OutputBaseFilename={#OutputName}
SetupIconFile=..\dist\DesktopGoose2.ico
UninstallDisplayIcon={app}\DesktopGoose2.exe
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
RestartApplications=no
SetupLogging=yes
[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; Flags: unchecked
Name: "startup"; Description: "Start Desktop Goose 2 when I sign in to Windows"; Flags: unchecked
[Files]
Source: "..\dist\DesktopGoose2.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\dist\DesktopGoose2.ico"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\dist\README.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\dist\RELEASE_NOTES.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\dist\Assets\*"; DestDir: "{app}\Assets"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\dist\Packs\*"; DestDir: "{app}\Packs"; Flags: ignoreversion recursesubdirs createallsubdirs
[Icons]
Name: "{userprograms}\{#ProductName}"; Filename: "{app}\DesktopGoose2.exe"
Name: "{userdesktop}\{#ProductName}"; Filename: "{app}\DesktopGoose2.exe"; Tasks: desktopicon
Name: "{userstartup}\{#ProductName}"; Filename: "{app}\DesktopGoose2.exe"; Tasks: startup
[InstallDelete]
Type: files; Name: "{userstartup}\{#ProductName}.lnk"; Tasks: not startup
Type: files; Name: "{userdesktop}\{#ProductName}.lnk"; Tasks: not desktopicon
[Run]
Filename: "{app}\DesktopGoose2.exe"; Parameters: "--seed-content"; Flags: runhidden waituntilterminated
Filename: "{app}\DesktopGoose2.exe"; Description: "Let the goose loose"; Flags: nowait postinstall skipifsilent
[Code]
function StopGoose(): Boolean;
var ResultCode, I: Integer; GooseExe: String;
begin
  Result := True;
  GooseExe := ExpandConstant('{app}\DesktopGoose2.exe');
  if not FileExists(GooseExe) then begin
    if not CheckForMutexes('Local\DesktopGoose2.Instance') then exit;
    if IsUninstaller then begin Result := False; exit; end;
    ExtractTemporaryFile('DesktopGoose2.exe');
    GooseExe := ExpandConstant('{tmp}\DesktopGoose2.exe');
  end;
  begin
    Result := Exec(GooseExe, '--quit', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    if not Result then exit;
    Result := ResultCode = 0;
    if not Result then exit;
    for I := 1 to 40 do begin
      if not CheckForMutexes('Local\DesktopGoose2.Instance') then exit;
      Sleep(100);
    end;
    Result := False;
  end;
end;
function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  Result := '';
  if not StopGoose() then Result := 'Please quit Desktop Goose 2 from its tray menu, then try again.';
end;
function InitializeUninstall(): Boolean;
begin
  Result := StopGoose();
end;
