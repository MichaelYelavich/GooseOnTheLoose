param([string]$ReleaseFolder)
. (Join-Path $PSScriptRoot 'common.ps1')
if (!$ReleaseFolder) { $ReleaseFolder=Join-Path $ProjectRoot '.work/release' }
$stage=Join-Path $ProjectRoot ('.work/packages-'+[guid]::NewGuid().ToString('N'))
$portable=Join-Path $stage 'Portable With Spaces';$source=Join-Path $stage 'Source With Spaces'
$report=[Collections.Generic.List[string]]::new()
$previousData=$env:DESKTOPGOOSE2_DATA_DIR
try {
    Expand-Archive -LiteralPath (Join-Path $ReleaseFolder ($PackagePrefix+'-windows.zip')) -DestinationPath $portable
    Expand-Archive -LiteralPath (Join-Path $ReleaseFolder ($PackagePrefix+'-source.zip')) -DestinationPath $source
    $exe=Join-Path $portable 'GooseOnTheLoose.exe'
    if ((Get-FileHash -LiteralPath $exe).Hash -ne (Get-FileHash -LiteralPath (Join-Path $ProjectRoot 'dist/GooseOnTheLoose.exe')).Hash) { throw 'Portable binary differs from the release build.' }
    if ((Get-Item -LiteralPath $exe).VersionInfo.FileVersion -ne $FileVersion) { throw 'Incorrect portable version.' }
    $report.Add('PASS  Portable archive matches the verified beta executable and extracts in a path containing spaces.')
    $env:DESKTOPGOOSE2_DATA_DIR=Join-Path $stage 'Portable Content With Spaces'
    $process=Start-Process -FilePath $exe -ArgumentList '--seed-content' -WindowStyle Hidden -Wait -PassThru
    if ($process.ExitCode -ne 0 -or !(Test-Path -LiteralPath (Join-Path $env:DESKTOPGOOSE2_DATA_DIR 'Pictures/honk.png'))) { throw 'Portable seeding failed.' }
    $app=Start-Process -FilePath $exe -WindowStyle Hidden -PassThru
    Start-Sleep -Milliseconds 500
    $quit=Start-Process -FilePath $exe -ArgumentList '--quit' -WindowStyle Hidden -Wait -PassThru
    if ($quit.ExitCode -ne 0 -or !$app.WaitForExit(5000)) { throw 'Portable start/quit failed.' }
    $report.Add('PASS  Extracted portable application seeds, runs and quits with its bundled files.')
    & (Join-Path $source 'build.ps1') -Locked | Out-Null
    if (!(Test-Path -LiteralPath (Join-Path $source 'dist/GooseOnTheLoose.exe'))) { throw 'Source archive did not rebuild.' }
    & (Join-Path $source 'tools/verify.ps1') -SkipBuild | Out-Null
    $report.Add('PASS  Extracted source rebuilds with locked dependencies and passes all application regressions in a path containing spaces.')
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    foreach($suffix in @('-windows.zip','-source.zip')) {
        $zip=[IO.Compression.ZipFile]::OpenRead((Join-Path $ReleaseFolder ($PackagePrefix+$suffix)))
        try {
            if ($zip.Entries.FullName -match '(^|/)(\.work|artifacts|bin|obj|\.git)/|error\.log|runtime\.json') { throw 'Generated or private files were packaged.' }
            if ($suffix -eq '-windows.zip' -and ($zip.Entries.FullName -match 'Goose.Tests|StockHonk|stock-honk|AssetGenerator')) { throw 'Tests were included in the portable game.' }
            $report.Add('PASS  '+$suffix+' contains no private/generated files or unwanted test runtime.')
        } finally { $zip.Dispose() }
    }
} catch { $report.Add('FAIL  '+$_); throw }
finally {
    $env:DESKTOPGOOSE2_DATA_DIR=$previousData
    New-Item -ItemType Directory -Force (Join-Path $ProjectRoot '.work/reports') | Out-Null
    $report | Set-Content -LiteralPath (Join-Path $ProjectRoot '.work/reports/package-verification.txt')
    $report
    Remove-WorkFolder $stage
}
