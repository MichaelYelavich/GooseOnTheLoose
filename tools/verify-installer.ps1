param([string]$Compiler, [string]$BaselineRef = '0d87ae6')
. (Join-Path $PSScriptRoot 'common.ps1')
Push-Location $ProjectRoot
$Compiler = Find-Inno $Compiler
$stage=Join-Path $ProjectRoot ('.work/installer-'+[guid]::NewGuid().ToString('N'))
$install=Join-Path $stage 'Programs With Spaces/DesktopGoose2'
$data=Join-Path $stage 'Redirected Documents/DesktopGoose2'
$previousData=$env:DESKTOPGOOSE2_DATA_DIR
$env:DESKTOPGOOSE2_DATA_DIR=$data
$report=[Collections.Generic.List[string]]::new()
$setup=Join-Path $ProjectRoot '.work/release/GooseOnTheLoose-Setup-Test.exe'
$oldSetup=$null
$startup=Join-Path ([Environment]::GetFolderPath('Startup')) 'Goose on the Loose Installer Test.lnk'
$desktop=Join-Path ([Environment]::GetFolderPath('DesktopDirectory')) 'Goose on the Loose Installer Test.lnk'
$start=Join-Path ([Environment]::GetFolderPath('Programs')) 'Goose on the Loose Installer Test.lnk'
function Check($condition,$label) { if (!$condition) { throw $label }; $report.Add('PASS  '+$label) }
function Install([string]$File,[string]$Tasks,[string]$Log,[switch]$UsePrevious) {
    $arguments=@('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART',('/TASKS="'+$Tasks+'"'),('/LOG="'+(Join-Path $stage $Log)+'"'))
    if (!$UsePrevious) { $arguments+=('/DIR="'+$install+'"') }
    $process=Start-Process -FilePath $File -ArgumentList $arguments -WindowStyle Hidden -Wait -PassThru
    Check ($process.ExitCode -eq 0) ('Installer completed: '+$Log)
}
try {
    New-Item -ItemType Directory -Force $stage,(Join-Path $ProjectRoot '.work/release') | Out-Null
    & $Compiler /Q /DTestBuild ("/DReleaseVersion="+$ReleaseVersion) ("/DFileVersion="+$FileVersion) ("/DPackagePrefix="+$PackagePrefix) installer/DesktopGoose2.iss
    if ($LASTEXITCODE -ne 0) { throw 'Test installer compilation failed.' }
    $baselineSource=Join-Path $stage 'Previous Source With Spaces'
    $baselineZip=Join-Path $stage 'baseline.zip'
    & git archive --format=zip ('--output='+$baselineZip) $BaselineRef
    if ($LASTEXITCODE -ne 0) { throw 'The actual 0.3.1 Git baseline is required for upgrade verification.' }
    Expand-Archive -LiteralPath $baselineZip -DestinationPath $baselineSource
    & (Join-Path $baselineSource 'build.ps1') | Out-Null
    Set-Location $ProjectRoot
    & $Compiler /Q /DTestBuild (Join-Path $baselineSource 'installer/DesktopGoose2.iss')
    if ($LASTEXITCODE -ne 0) { throw 'Baseline installer compilation failed.' }
    $oldSetup=Join-Path $baselineSource 'artifacts/DesktopGoose2-Setup-Test.exe'
    Install $oldSetup '' 'install-0.3.1.log'
    Check ((Get-Item -LiteralPath (Join-Path $install 'DesktopGoose2.exe')).VersionInfo.FileVersion -eq '0.3.1.0') 'Upgrade starts from the actual 0.3.1 binary'
    Check (!(Test-Path -LiteralPath $startup) -and !(Test-Path -LiteralPath $desktop)) 'Optional shortcuts start disabled'
    $note=Join-Path $data 'Notes/Custom.txt'; [IO.File]::WriteAllText($note,'Keep my custom honk')
    $settings=Join-Path $data 'settings.json'; [IO.File]::WriteAllText($settings,'{"Version":2,"Sound":false,"Mischief":false,"Companion":true,"Hat":"Scarf","Scale":1.5}')
    $custom=Join-Path $data 'Pictures/honk.png'; Copy-Item -LiteralPath tools/fixtures/stock-honk-0.3.png -Destination $custom -Force
    # Use a distinct valid custom image, so stock migration cannot replace it.
    Add-Type -AssemblyName System.Drawing
    $image=[Drawing.Bitmap]::new(24,24); $drawing=[Drawing.Graphics]::FromImage($image)
    try { $drawing.Clear([Drawing.Color]::DarkOrange); $image.Save($custom,[Drawing.Imaging.ImageFormat]::Png) } finally { $drawing.Dispose(); $image.Dispose() }
    $customHash=(Get-FileHash -LiteralPath $custom).Hash
    $app=Start-Process -FilePath (Join-Path $install 'DesktopGoose2.exe') -WindowStyle Hidden -PassThru
    Start-Sleep -Milliseconds 400
    Install $setup 'desktopicon,startup' 'upgrade.log' -UsePrevious
    Check ($app.WaitForExit(5000)) 'Upgrade gracefully shuts down the running 0.3.1 instance'
    $exe=Join-Path $install 'GooseOnTheLoose.exe'
    Check ((Test-Path -LiteralPath $exe) -and ((Get-Item -LiteralPath $exe).VersionInfo.FileVersion -eq $FileVersion)) 'Upgrade reuses the previous installation directory and installs the beta'
    foreach ($document in @('docs/PLAYING.md','docs/assets/desktop-geese.png','CONTRIBUTING.md','RESEARCH.md','VALIDATION.md','global.json')) {
        Check ((Test-Path -LiteralPath (Join-Path $install $document)) -and ((Get-FileHash -LiteralPath (Join-Path $install $document)).Hash -eq (Get-FileHash -LiteralPath (Join-Path $ProjectRoot $document)).Hash)) ('Installer includes current documentation: '+$document)
    }
    Check (((Get-FileHash -LiteralPath (Join-Path $install 'DesktopGoose2.exe')).Hash) -eq ((Get-FileHash -LiteralPath $exe).Hash)) 'Old executable shortcuts launch the current beta compatibility binary'
    Check ((Test-Path -LiteralPath $startup) -and (Test-Path -LiteralPath $desktop) -and (Test-Path -LiteralPath $start)) 'Selected startup, desktop and Start menu shortcuts use the new name'
    Check (!(Test-Path -LiteralPath (Join-Path ([Environment]::GetFolderPath('Programs')) 'Desktop Goose 2 Installer Test.lnk'))) 'Upgrade removes the obsolete Start menu shortcut'
    Check (([IO.File]::ReadAllText($note) -eq 'Keep my custom honk') -and ((Get-Content -Raw $settings | ConvertFrom-Json).Hat -eq 'Scarf') -and ((Get-FileHash -LiteralPath $custom).Hash -eq $customHash)) 'Upgrade preserves custom notes, pictures and preferences'
    $app=Start-Process -FilePath $exe -WindowStyle Hidden -PassThru
    Start-Sleep -Milliseconds 300
    $legacyCommand=Start-Process -FilePath (Join-Path $install 'DesktopGoose2.exe') -ArgumentList '--honk' -WindowStyle Hidden -Wait -PassThru
    Check ($legacyCommand.ExitCode -eq 0 -and !$app.HasExited) 'Legacy and renamed binaries share one running instance'
    $uninstaller=Join-Path $install 'unins000.exe'
    $process=Start-Process -FilePath $uninstaller -ArgumentList '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART' -WindowStyle Hidden -Wait -PassThru
    Check ($process.ExitCode -eq 0 -and $app.WaitForExit(5000)) 'Uninstall immediately shuts down the running beta'
    Check (!(Test-Path -LiteralPath $exe) -and !(Test-Path -LiteralPath (Join-Path $install 'DesktopGoose2.exe')) -and !(Test-Path -LiteralPath $startup) -and !(Test-Path -LiteralPath $desktop) -and !(Test-Path -LiteralPath $start)) 'Uninstall removes application binaries and all test shortcuts'
    Check ((Test-Path -LiteralPath $settings) -and ([IO.File]::ReadAllText($note) -eq 'Keep my custom honk')) 'Uninstall retains Documents content'
    $freshData=Join-Path $stage 'Fresh Documents/GooseOnTheLoose';$env:DESKTOPGOOSE2_DATA_DIR=$freshData
    Install $setup '' 'fresh-beta.log'
    Check ((Test-Path -LiteralPath (Join-Path $freshData 'settings.json')) -and !(Test-Path -LiteralPath (Join-Path $install 'DesktopGoose2.exe'))) 'Fresh beta seeds its content and does not install the compatibility executable'
    Check (!(Test-Path -LiteralPath $startup) -and !(Test-Path -LiteralPath $desktop)) 'Fresh beta keeps startup and desktop options off by default'
    $process=Start-Process -FilePath $uninstaller -ArgumentList '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART' -WindowStyle Hidden -Wait -PassThru
    Check ($process.ExitCode -eq 0 -and !(Test-Path -LiteralPath $exe) -and (Test-Path -LiteralPath (Join-Path $freshData 'settings.json'))) 'Fresh beta uninstalls while retaining content'
    $report.Add('LIMIT  Isolated AppId and application/data paths on this host; no disposable VM, real known-folder redirection, or reboot/logon test.')
} catch { $report.Add('FAIL  '+$_); throw }
finally {
    $uninstaller=Join-Path $install 'unins000.exe'
    if (Test-Path -LiteralPath $uninstaller) { Start-Process -FilePath $uninstaller -ArgumentList '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART' -WindowStyle Hidden -Wait }
    $env:DESKTOPGOOSE2_DATA_DIR=$previousData
    New-Item -ItemType Directory -Force (Join-Path $ProjectRoot '.work/reports') | Out-Null
    $report | Set-Content -LiteralPath (Join-Path $ProjectRoot '.work/reports/installer-verification.txt')
    $report
    Remove-WorkFolder $stage
    Pop-Location
}
