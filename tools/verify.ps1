param([switch]$Interactive, [switch]$SkipBuild, [switch]$Format)
. (Join-Path $PSScriptRoot 'common.ps1')
Push-Location $ProjectRoot
$previousReport=$env:GOOSE_REPORT_DIR
try {
    if (!$SkipBuild) { & ./build.ps1 -Locked }
    if ($Format) { Invoke-Dotnet format GooseOnTheLoose.slnx whitespace --no-restore --verify-no-changes }
    $env:GOOSE_REPORT_DIR=Join-Path $ProjectRoot '.work/reports'
    New-Item -ItemType Directory -Force $env:GOOSE_REPORT_DIR | Out-Null
    $executable=Join-Path $ProjectRoot 'tests/bin/Release/Goose.Tests.exe'
    $modes=@('--self-test'); if ($Interactive) { $modes+='--native-smoke' }
    foreach($mode in $modes) {
        $check=Start-Process -FilePath $executable -ArgumentList $mode -WindowStyle Hidden -PassThru
        if (!$check.WaitForExit(120000)) { $check.Kill(); throw ($mode+' exceeded its two-minute limit.') }
        if ($check.ExitCode -ne 0) { throw ($mode+' failed. Read .work/reports.') }
    }
    if (!$Interactive) { 'SKIP  Native modal-dialog tests require an interactive Windows desktop; run tools/verify.ps1 -Interactive.' | Set-Content (Join-Path $env:GOOSE_REPORT_DIR 'interactive-status.txt') }
    Get-Content (Join-Path $env:GOOSE_REPORT_DIR 'desktop-self-test.txt')
    if ($Interactive) { Get-Content (Join-Path $env:GOOSE_REPORT_DIR 'desktop-native-smoke.txt') }
} finally { $env:GOOSE_REPORT_DIR=$previousReport; Pop-Location }
