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
LicenseFile=.\license.rtf
WizardSmallImageFile=.\setup.bmp
WizardImageFile=.\side.bmp

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: ".\privacy.rtf"; Flags: dontcopy

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
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Parameters: "/cleanup ""{srcexe}"""; Flags: nowait postinstall skipifnotsilent

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
  if Exec(Cmd, Args, '', SW_HIDE, ewWaitUntilTerminated, ResultCode) and (ResultCode = 0) then begin
    if LoadStringFromFile(FileName, Output) then begin
      if Pos(DotNetName, Output) > 0 then begin
        Log('"' + DotNetName + '" found in output of "' + Command + '"');
        Result := True;
      end else begin
        Log('"' + DotNetName + '" not found in output of "' + Command + '"');
        Result := False;
      end;
    end else begin
      Log('Failed to read output of "' + Command + '"');
    end;
  end else begin
    Log('Failed to execute "' + Command + '"');
    Result := False;
  end;
  DeleteFile(FileName);
end;

function InitializeSetup(): Boolean;
var
  AppName: string;
  ErrCode: integer;
begin
  AppName := '{#SetupSetting("AppName")}';
  if not IsDotNetInstalled('Microsoft.WindowsDesktop.App 7.0.0') then begin
    MsgBox(AppName + ' requires Microsoft .NET 7.0 Desktop Runtime.'#13#13
      'Download it at https://aka.ms/dotnet-download and install it first!', mbError, MB_OK);
    ShellExec('open', 'https://dotnet.microsoft.com/download/dotnet/7.0#runtime-desktop-7.0.0', '', '', SW_SHOW, ewNoWait, ErrCode);
    result := false;
  end else
    result := true;
end;

var
  LicenseAcceptedRadioButtons: array of TRadioButton;

procedure CheckLicenseAccepted(Sender: TObject);
begin
  WizardForm.NextButton.Enabled := LicenseAcceptedRadioButtons[TComponent(Sender).Tag].Checked;
end;

procedure LicensePageActivate(Sender: TWizardPage);
begin
  CheckLicenseAccepted(LicenseAcceptedRadioButtons[Sender.Tag]);
end;

function CloneLicenseRadioButton(
  Page: TWizardPage; Source: TRadioButton): TRadioButton;
begin
  Result := TRadioButton.Create(WizardForm);
  Result.Parent := Page.Surface;
  Result.Caption := Source.Caption;
  Result.Left := Source.Left;
  Result.Top := Source.Top;
  Result.Width := Source.Width;
  Result.Height := Source.Height;
  Result.Anchors := Source.Anchors;
  Result.OnClick := @CheckLicenseAccepted;
  Result.Tag := Page.Tag;
end;

var
  LicenseAfterPage: Integer;

procedure AddLicensePage(LicenseFileName: string);
var
  Idx: Integer;
  Page: TOutputMsgMemoWizardPage;
  LicenseFilePath: string;
  Output: AnsiString;
  RadioButton: TRadioButton;
begin
  Idx := GetArrayLength(LicenseAcceptedRadioButtons);
  SetArrayLength(LicenseAcceptedRadioButtons, Idx + 1);

  Page :=
    CreateOutputMsgMemoPage(
      LicenseAfterPage, SetupMessage(msgWizardLicense),
      SetupMessage(msgLicenseLabel), SetupMessage(msgLicenseLabel3), '');
  Page.Tag := Idx;

  Page.RichEditViewer.Height := WizardForm.LicenseMemo.Height;
  Page.OnActivate := @LicensePageActivate;

  ExtractTemporaryFile(LicenseFileName);
  LicenseFilePath := ExpandConstant('{tmp}\' + LicenseFileName);
  
  if LoadStringFromFile(LicenseFilePath, Output) then begin
    Page.RichEditViewer.RTFText := Output;
  end;
  DeleteFile(LicenseFilePath);

  RadioButton := CloneLicenseRadioButton(Page, WizardForm.LicenseAcceptedRadio);
  LicenseAcceptedRadioButtons[Idx] := RadioButton;

  RadioButton := CloneLicenseRadioButton(Page, WizardForm.LicenseNotAcceptedRadio);
  RadioButton.Checked := True;

  LicenseAfterPage := Page.ID;
end;

procedure InitializeWizard();
begin
  if not WizardSilent() then begin
    LicenseAfterPage := wpLicense;
    AddLicensePage('privacy.rtf');
  end;
  
  WizardForm.MainPanel.Color := TColor($270d01);
  WizardForm.PageNameLabel.Font.Color := TColor($d5dcdf);
  WizardForm.PageDescriptionLabel.Font.Color := TColor($d5dcdf);
end;
