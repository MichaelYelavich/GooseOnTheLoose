$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
Set-Location $projectRoot
& (Join-Path $projectRoot 'build.ps1')
$executable = Join-Path $projectRoot 'dist\DesktopGoose2.exe'
foreach ($mode in @('--self-test','--native-smoke')) {
    $check = Start-Process -FilePath $executable -ArgumentList $mode -Wait -PassThru -WindowStyle Hidden
    if ($check.ExitCode -ne 0) { throw "$mode failed. Read artifacts\desktop-self-test.txt or artifacts\desktop-native-smoke.txt." }
}
Get-Content -LiteralPath (Join-Path $projectRoot 'artifacts\desktop-self-test.txt')
Get-Content -LiteralPath (Join-Path $projectRoot 'artifacts\desktop-native-smoke.txt')
