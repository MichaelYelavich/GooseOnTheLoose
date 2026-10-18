$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$stage=Join-Path $root ('artifacts\test-packages-'+[guid]::NewGuid().ToString('N'))
$portable=Join-Path $stage 'Portable With Spaces'
$source=Join-Path $stage 'Source With Spaces'
$report=[Collections.Generic.List[string]]::new()
try {
    Expand-Archive -LiteralPath (Join-Path $root 'artifacts\DesktopGoose2-0.3.1-windows.zip') -DestinationPath $portable
    Expand-Archive -LiteralPath (Join-Path $root 'artifacts\DesktopGoose2-0.3.1-source.zip') -DestinationPath $source
    $exe=Join-Path $portable 'DesktopGoose2.exe'
    if ((Get-FileHash -LiteralPath $exe).Hash -ne (Get-FileHash -LiteralPath (Join-Path $root 'dist\DesktopGoose2.exe')).Hash) { throw 'Portable executable differs from release build.' }
    if ((Get-Item -LiteralPath $exe).VersionInfo.FileVersion -ne '0.3.1.0') { throw 'Wrong release version.' }
    $report.Add('PASS  Portable archive extracts into a path with spaces and matches the 0.3.1 release executable.')
    foreach ($mode in @('--self-test','--native-smoke')) {
        $p=Start-Process -FilePath $exe -ArgumentList $mode -WindowStyle Hidden -Wait -PassThru
        if ($p.ExitCode -ne 0) { throw ('Extracted portable test failed: '+$mode) }
        $report.Add('PASS  Extracted portable '+$mode+' succeeds with its bundled assets.')
    }
    & (Join-Path $source 'build.ps1') | Out-Null
    if (!(Test-Path -LiteralPath (Join-Path $source 'dist\DesktopGoose2.exe'))) { throw 'Source archive did not rebuild.' }
    foreach ($path in @('research\sources.json','research\decompiled-forks.json','installer\DesktopGoose2.iss')) { if (!(Test-Path -LiteralPath (Join-Path $source $path))) { throw ('Missing source evidence: '+$path) } }
    $report.Add('PASS  Extracted source archive rebuilds in a path with spaces and includes installer source and research metadata.')
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    foreach ($name in @('DesktopGoose2-0.3.1-windows.zip','DesktopGoose2-0.3.1-source.zip')) {
        $zip=[IO.Compression.ZipFile]::OpenRead((Join-Path $root ('artifacts\'+$name)))
        try { if ($zip.Entries.FullName -match 'superseded-prototype|build-tools|live-test-data|test-installer') { throw 'Private build/test directories were packaged.' }; $report.Add('PASS  '+$name+': '+$zip.Entries.Count+' entries; no superseded prototype, compiler, or test user data.') } finally { $zip.Dispose() }
    }
} catch { $report.Add('FAIL  '+$_); throw }
finally { Set-Location $root; $report | Set-Content -LiteralPath (Join-Path $root 'artifacts\package-verification.txt'); $report }
