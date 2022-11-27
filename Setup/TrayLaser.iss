#define MyAppName "TrayLaser"
#define MyAppVersion GetFileVersion("..\LaserPointer\bin\Release\net7.0-windows\LaserPointer.exe")
#define MyAppPublisher "Jonas Kohl"
#define MyAppURL "https://jonaskohl.de/goto.php?t=tl"
#define MyAppExeName "LaserPointer.exe"

[Setup]
AppId={{457EEC5F-6142-4CE9-8CE4-9F37BB937420}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
OutputBaseFilename=TrayLaserInstaller-{#MyAppVersion}
Compression=lzma
SolidCompression=yes
DisableWelcomePage=False
MinVersion=0,10.0
WizardStyle=modern
WizardSizePercent=100
WizardResizable=no
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog commandline
SetupIconFile=.\jksetup.ico

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "..\LaserPointer\bin\Release\net7.0-windows\LaserPointer.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\LaserPointer\bin\Release\net7.0-windows\LaserPointer.deps.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\LaserPointer\bin\Release\net7.0-windows\LaserPointer.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\LaserPointer\bin\Release\net7.0-windows\LaserPointer.pdb"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\LaserPointer\bin\Release\net7.0-windows\LaserPointer.runtimeconfig.json"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{commondesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]

function IsDotNetInstalled(DotNetName: string): Boolean;
var
  Cmd, Args: string;
  FileName: string;
  Output: AnsiString;
  Command: string;
  ResultCode: Integer;
begin
  FileName := ExpandConstant('{tmp}\dotnet.txt');
  Cmd := ExpandConstant('{cmd}');
  Command := 'dotnet --list-runtimes';
  Args := '/C ' + Command + ' > "' + FileName + '" 2>&1';
  if Exec(Cmd, Args, '', SW_HIDE, ewWaitUntilTerminated, ResultCode) and
     (ResultCode = 0) then
  begin
    if LoadStringFromFile(FileName, Output) then
    begin
      if Pos(DotNetName, Output) > 0 then
      begin
        Log('"' + DotNetName + '" found in output of "' + Command + '"');
        Result := True;
      end
        else
      begin
        Log('"' + DotNetName + '" not found in output of "' + Command + '"');
        Result := False;
      end;
    end
      else
    begin
      Log('Failed to read output of "' + Command + '"');
    end;
  end
    else
  begin
    Log('Failed to execute "' + Command + '"');
    Result := False;
  end;
  DeleteFile(FileName);
end;

function InitializeSetup(): Boolean;
var
  AppName: string;
begin
    AppName := '{#SetupSetting("AppName")}';
    if not IsDotNetInstalled('Microsoft.WindowsDesktop.App 7.0.0') then begin
        
        MsgBox(AppName + ' requires Microsoft .NET 7.0 Desktop Runtime.'#13#13
            'Download it at https://aka.ms/dotnet-download and install it first!', mbError, MB_OK);
        result := false;
    end else
        result := true;
end;
