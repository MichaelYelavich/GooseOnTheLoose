param([string]$Compiler, [switch]$SkipBuild)
. (Join-Path $PSScriptRoot 'common.ps1')
Push-Location $ProjectRoot
try {
    if (!$SkipBuild) { & ./build.ps1 -Locked }
    $Compiler = Find-Inno $Compiler
    $release = Join-Path $ProjectRoot '.work/release'
    New-Item -ItemType Directory -Force $release | Out-Null
    & $Compiler /Q ("/DReleaseVersion="+$ReleaseVersion) ("/DFileVersion="+$FileVersion) ("/DPackagePrefix="+$PackagePrefix) installer/DesktopGoose2.iss
    if ($LASTEXITCODE -ne 0) { throw 'Installer compilation failed.' }
    $stage = Join-Path $ProjectRoot ('.work/package-'+[guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Force $stage | Out-Null
    try {
        foreach ($item in @('GooseOnTheLoose.exe','GooseOnTheLoose.exe.config','Goose.Core.dll','GooseOnTheLoose.ico','README.md','RELEASE_NOTES.md','RESEARCH.md','VALIDATION.md','Assets','Packs')) { Copy-Item -LiteralPath (Join-Path $ProjectRoot ('dist/'+$item)) -Destination $stage -Recurse }
        Compress-Archive -Path (Join-Path $stage '*') -DestinationPath (Join-Path $release ($PackagePrefix+'-windows.zip')) -Force
        & git archive --format=zip ('--output='+ (Join-Path $release ($PackagePrefix+'-source.zip'))) HEAD
        if ($LASTEXITCODE -ne 0) { throw 'Source archive failed.' }
    } finally { Remove-WorkFolder $stage }
    Get-FileHash -LiteralPath (Join-Path $release ($PackagePrefix+'-Setup.exe')),(Join-Path $release ($PackagePrefix+'-windows.zip')),(Join-Path $release ($PackagePrefix+'-source.zip')) | ForEach-Object { $_.Hash+'  '+[IO.Path]::GetFileName($_.Path) } | Set-Content -LiteralPath (Join-Path $ProjectRoot '.work/checksums.txt')
    Get-Content -LiteralPath (Join-Path $ProjectRoot '.work/checksums.txt')
} finally { Pop-Location }
