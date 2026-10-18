$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
Set-Location $root
$compiler=Join-Path $root 'artifacts\build-tools\Inno Setup\ISCC.exe'
& $compiler /DTestBuild installer\DesktopGoose2.iss | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'Test installer compilation failed.' }
$stage=Join-Path $root ('artifacts\test-installer-'+[guid]::NewGuid().ToString('N'))
$install=Join-Path $stage 'Programs With Spaces\DesktopGoose2'
$data=Join-Path $stage 'Redirected Documents\DesktopGoose2'
New-Item -ItemType Directory -Force $stage | Out-Null
$oldData=$env:DESKTOPGOOSE2_DATA_DIR
$env:DESKTOPGOOSE2_DATA_DIR=$data
$setup=Join-Path $root 'artifacts\DesktopGoose2-Setup-Test.exe'
$startup=Join-Path ([Environment]::GetFolderPath('Startup')) 'Desktop Goose 2 Installer Test.lnk'
$desktop=Join-Path ([Environment]::GetFolderPath('DesktopDirectory')) 'Desktop Goose 2 Installer Test.lnk'
$start=Join-Path ([Environment]::GetFolderPath('Programs')) 'Desktop Goose 2 Installer Test.lnk'
$report=[Collections.Generic.List[string]]::new()
function Check($condition,$label) { if (!$condition) { throw $label }; $report.Add('PASS  '+$label) }
function Install($tasks,$log) {
  $args=@('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART',('/DIR="'+$install+'"'),('/TASKS="'+$tasks+'"'),('/LOG="'+(Join-Path $stage $log)+'"'))
  $p=Start-Process -FilePath $setup -ArgumentList $args -WindowStyle Hidden -Wait -PassThru
  Check ($p.ExitCode -eq 0) ('Installer completed: '+$log)
}
try {
  $previous=Join-Path $root 'artifacts\DesktopGoose2-0.3.0-source.zip'
  if (Test-Path -LiteralPath $previous) {
    $oldSource=Join-Path $stage 'Previous Source With Spaces'
    Expand-Archive -LiteralPath $previous -DestinationPath $oldSource
    & (Join-Path $oldSource 'build.ps1') | Out-Null
    Set-Location $root
    & $compiler /Q /DTestBuild (Join-Path $oldSource 'installer\DesktopGoose2.iss') | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Previous-release installer failed to compile.' }
    $newSetup=$setup
    $setup=Join-Path $oldSource 'artifacts\DesktopGoose2-Setup-Test.exe'
  }
  Install '' 'install.log'
  if ($newSetup) { Check ((Get-Item -LiteralPath (Join-Path $install 'DesktopGoose2.exe')).VersionInfo.FileVersion -eq '0.3.0.0') 'Upgrade starts from the actual 0.3.0 application'; $setup=$newSetup }
  Check ((Test-Path -LiteralPath (Join-Path $install 'DesktopGoose2.exe')) -and (Test-Path -LiteralPath $start)) 'Per-user application and Start menu shortcut installed in paths containing spaces'
  Check (!(Test-Path -LiteralPath $startup) -and !(Test-Path -LiteralPath $desktop)) 'Startup and desktop shortcuts default to off'
  Check ((Test-Path -LiteralPath (Join-Path $data 'settings.json')) -and (Test-Path -LiteralPath (Join-Path $data 'Behaviors\default.json'))) 'First install seeds the separate content directory'
  $note=Join-Path $data 'Notes\Custom.txt'
  [IO.File]::WriteAllText($note,'Keep my custom honk')
  $settings=Join-Path $data 'settings.json'
  [IO.File]::WriteAllText($settings,'{"Version":2,"Sound":false,"Mischief":false,"Companion":true,"Hat":"Scarf","Scale":1.5}')
  $stock=Join-Path $data 'Pictures\renamed-stock.png'
  $custom=Join-Path $data 'Pictures\honk.png'
  Copy-Item -LiteralPath (Join-Path $root 'tools\fixtures\stock-honk-0.3.png') -Destination $stock
  Add-Type -AssemblyName System.Drawing
  $image=[Drawing.Bitmap]::new(24,24)
  $drawing=[Drawing.Graphics]::FromImage($image)
  $drawing.Clear([Drawing.Color]::DarkOrange)
  $image.Save($custom,[Drawing.Imaging.ImageFormat]::Png)
  $drawing.Dispose()
  $image.Dispose()
  $customHash=(Get-FileHash -LiteralPath $custom).Hash
  $app=Start-Process -FilePath (Join-Path $install 'DesktopGoose2.exe') -WindowStyle Hidden -PassThru
  $deadline=[DateTime]::UtcNow.AddSeconds(5)
  while (!(Test-Path -LiteralPath (Join-Path $data 'runtime.json')) -and [DateTime]::UtcNow -lt $deadline) { Start-Sleep -Milliseconds 50 }
  Install 'desktopicon,startup' 'upgrade.log'
  Check ($app.HasExited) 'Upgrade gracefully shuts down the running desktop goose'
  Check ((Test-Path -LiteralPath $startup) -and (Test-Path -LiteralPath $desktop)) 'Explicit startup and desktop options create shortcuts'
  Check (([IO.File]::ReadAllText($note) -eq 'Keep my custom honk') -and ((Get-Content -Raw $settings | ConvertFrom-Json).Hat -eq 'Scarf')) 'Upgrade preserves custom content and preferences'
  Check ((Get-Item -LiteralPath (Join-Path $install 'DesktopGoose2.exe')).VersionInfo.FileVersion -eq '0.3.1.0') 'Upgrade installs version 0.3.1'
  Check (((Get-FileHash -LiteralPath $stock).Hash -eq (Get-FileHash -LiteralPath (Join-Path $root 'dist\Packs\Images\honk.png')).Hash) -and ((Get-FileHash -LiteralPath $custom).Hash -eq $customHash)) 'Upgrade migrates recognized stock art and preserves a customized honk.png'
  $app=Start-Process -FilePath (Join-Path $install 'DesktopGoose2.exe') -WindowStyle Hidden -PassThru
  Start-Sleep -Milliseconds 300
  $uninstaller=Join-Path $install 'unins000.exe'
  $p=Start-Process -FilePath $uninstaller -ArgumentList @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART',('/LOG="'+(Join-Path $stage 'uninstall.log')+'"')) -WindowStyle Hidden -Wait -PassThru
  Check ($p.ExitCode -eq 0 -and $app.HasExited) 'Uninstall gracefully shuts down the running application'
  Check (!(Test-Path -LiteralPath (Join-Path $install 'DesktopGoose2.exe')) -and !(Test-Path -LiteralPath $startup) -and !(Test-Path -LiteralPath $desktop) -and !(Test-Path -LiteralPath $start)) 'Uninstall removes installed binaries and all test shortcuts'
  Check ((Test-Path -LiteralPath $settings) -and ([IO.File]::ReadAllText($note) -eq 'Keep my custom honk')) 'Uninstall retains Documents settings and custom notes'
  $freshData=Join-Path $stage 'Fresh Documents\DesktopGoose2'
  $env:DESKTOPGOOSE2_DATA_DIR=$freshData
  Install '' 'fresh-0.3.1.log'
  Check (((Get-Item -LiteralPath (Join-Path $install 'DesktopGoose2.exe')).VersionInfo.FileVersion -eq '0.3.1.0') -and ((Get-FileHash -LiteralPath (Join-Path $freshData 'Pictures\honk.png')).Hash -eq (Get-FileHash -LiteralPath (Join-Path $root 'dist\Packs\Images\honk.png')).Hash) -and !(Test-Path -LiteralPath $startup) -and !(Test-Path -LiteralPath $desktop)) 'Fresh 0.3.1 install seeds text-only art and keeps optional shortcuts off'
  $p=Start-Process -FilePath $uninstaller -ArgumentList '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART' -WindowStyle Hidden -Wait -PassThru
  Check ($p.ExitCode -eq 0 -and !(Test-Path -LiteralPath (Join-Path $install 'DesktopGoose2.exe')) -and (Test-Path -LiteralPath (Join-Path $freshData 'settings.json'))) 'Fresh 0.3.1 uninstalls cleanly while retaining content'
  $report.Add('NOTE  Isolated application/data directories and separate installer AppId on this Windows host; not a disposable Windows VM. Real known-folder redirection and reboot/logon were not changed.')
} catch { $report.Add('FAIL  '+$_); throw }
finally {
  $uninstaller=Join-Path $install 'unins000.exe'
  if (Test-Path -LiteralPath $uninstaller) { Start-Process -FilePath $uninstaller -ArgumentList '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART' -WindowStyle Hidden -Wait }
  $env:DESKTOPGOOSE2_DATA_DIR=$oldData
  $report | Set-Content -LiteralPath (Join-Path $root 'artifacts\installer-verification.txt')
  $report
}
