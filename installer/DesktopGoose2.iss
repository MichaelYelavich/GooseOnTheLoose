#ifndef ReleaseVersion
 #error Build through tools/package.ps1
#endif
#ifndef TestBuild
  #define ProductName "Goose on the Loose"
  #define LegacyName "Desktop Goose 2"
  #define DirectoryName "GooseOnTheLoose"
  #define ProductId "DesktopGoose2"
  #define OutputName PackagePrefix + "-Setup"
#else
  #define ProductName "Goose on the Loose Installer Test"
  #define LegacyName "Desktop Goose 2 Installer Test"
  #define DirectoryName "GooseOnTheLoose-IsolatedTest"
  #define ProductId "DesktopGoose2-IsolatedTest"
  #define OutputName "GooseOnTheLoose-Setup-Test"
#endif
[Setup]
AppId={#ProductId}
AppName={#ProductName}
AppVersion={#ReleaseVersion}
VersionInfoVersion={#FileVersion}
DefaultDirName={localappdata}\Programs\{#DirectoryName}
UsePreviousAppDir=yes
DefaultGroupName={#ProductName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
MinVersion=10.0.22000
OutputDir=..\.work\release
OutputBaseFilename={#OutputName}
SetupIconFile=..\dist\GooseOnTheLoose.ico
UninstallDisplayIcon={app}\GooseOnTheLoose.exe
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
RestartApplications=no
SetupLogging=yes
[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; Flags: unchecked
Name: "startup"; Description: "Start Goose on the Loose when I sign in to Windows"; Flags: unchecked
[Files]
Source: "..\dist\GooseOnTheLoose.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\dist\GooseOnTheLoose.ico"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\dist\README.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\dist\RELEASE_NOTES.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\dist\CONTRIBUTING.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\dist\RESEARCH.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\dist\VALIDATION.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\dist\global.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\dist\docs\*"; DestDir: "{app}\docs"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\dist\Assets\*"; DestDir: "{app}\Assets"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\dist\Packs\*"; DestDir: "{app}\Packs"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\dist\Goose.Core.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\dist\GooseOnTheLoose.exe.config"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\dist\GooseOnTheLoose.exe"; DestDir: "{app}"; DestName: "DesktopGoose2.exe"; Flags: ignoreversion; Check: LegacyExecutableExists
Source: "..\dist\GooseOnTheLoose.exe.config"; DestDir: "{app}"; DestName: "DesktopGoose2.exe.config"; Flags: ignoreversion; Check: LegacyExecutableExists
[Icons]
Name: "{userprograms}\{#ProductName}"; Filename: "{app}\GooseOnTheLoose.exe"
Name: "{userdesktop}\{#ProductName}"; Filename: "{app}\GooseOnTheLoose.exe"; Tasks: desktopicon
Name: "{userstartup}\{#ProductName}"; Filename: "{app}\GooseOnTheLoose.exe"; Tasks: startup
[InstallDelete]
Type: files; Name: "{userprograms}\{#LegacyName}.lnk"
Type: files; Name: "{userstartup}\{#LegacyName}.lnk"
Type: files; Name: "{userdesktop}\{#LegacyName}.lnk"
Type: files; Name: "{userstartup}\{#ProductName}.lnk"; Tasks: not startup
Type: files; Name: "{userdesktop}\{#ProductName}.lnk"; Tasks: not desktopicon
[Run]
Filename: "{app}\GooseOnTheLoose.exe"; Parameters: "--seed-content"; Flags: runhidden waituntilterminated
Filename: "{app}\GooseOnTheLoose.exe"; Description: "Let the goose loose"; Flags: nowait postinstall skipifsilent
[Code]
function LegacyExecutableExists(): Boolean;
begin
  Result := FileExists(ExpandConstant('{app}\DesktopGoose2.exe'));
end;
function StopGoose(): Boolean;
var ResultCode, I: Integer; GooseExe: String;
begin
  Result := True;
  GooseExe := ExpandConstant('{app}\GooseOnTheLoose.exe');
  if not FileExists(GooseExe) then GooseExe := ExpandConstant('{app}\DesktopGoose2.exe');
  if not FileExists(GooseExe) then begin
#ifdef TestBuild
    Result := True; exit;
#else
    if not CheckForMutexes('Local\DesktopGoose2.Instance') then exit;
    if IsUninstaller then begin Result := False; exit; end;
    ExtractTemporaryFile('GooseOnTheLoose.exe');
    ExtractTemporaryFile('Goose.Core.dll');
    ExtractTemporaryFile('GooseOnTheLoose.exe.config');
    GooseExe := ExpandConstant('{tmp}\GooseOnTheLoose.exe');
#endif
  end;
  begin
    Result := Exec(GooseExe, '--quit', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    if not Result then exit;
    Result := ResultCode = 0;
    if not Result then exit;
#ifndef TestBuild
    for I := 1 to 40 do begin
      if not CheckForMutexes('Local\DesktopGoose2.Instance') then exit;
      Sleep(100);
    end;
    Result := False;
#endif
  end;
end;
function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  Result := '';
  if not StopGoose() then Result := 'Please quit Goose on the Loose from its tray menu, then try again.';
end;
function InitializeUninstall(): Boolean;
begin
  Result := StopGoose();
end;
