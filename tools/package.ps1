param([string]$Compiler = (Join-Path $PSScriptRoot '..\artifacts\build-tools\Inno Setup\ISCC.exe'))
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
Set-Location $root
& .\build.ps1
if (!(Test-Path -LiteralPath $Compiler)) { throw 'Inno Setup compiler is required. Pass -Compiler with the path to ISCC.exe.' }
& $Compiler /Q installer\DesktopGoose2.iss
if ($LASTEXITCODE -ne 0) { throw 'Installer compilation failed.' }
$portable=Join-Path $root ('artifacts\package-smoke\'+[guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force $portable | Out-Null
foreach ($item in @('DesktopGoose2.exe','DesktopGoose2.ico','README.md','RESEARCH.md','VALIDATION.md','RELEASE_NOTES.md','Assets','Packs')) { Copy-Item -LiteralPath (Join-Path $root ('dist\'+$item)) -Destination $portable -Recurse }
Compress-Archive -Path (Join-Path $portable '*') -DestinationPath artifacts\DesktopGoose2-0.3.1-windows.zip -Force
$source=@('src','Assets','Packs','installer','tools','build.ps1','app.manifest','README.md','RESEARCH.md','VALIDATION.md','RELEASE_NOTES.md','.gitignore')
$stage=Join-Path $root ('artifacts\source-bundle\'+[guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force $stage,(Join-Path $stage 'research'),(Join-Path $stage 'artifacts') | Out-Null
foreach ($item in $source) { Copy-Item -LiteralPath (Join-Path $root $item) -Destination $stage -Recurse }
foreach ($item in @('sources.json','github-search.json','decompiled-forks.json','fork-comparison.json')) { $path=Join-Path $root ('research\'+$item); if (Test-Path -LiteralPath $path) { Copy-Item -LiteralPath $path -Destination (Join-Path $stage 'research') } }
foreach ($item in @('desktop-self-test.txt','desktop-native-smoke.txt','installer-verification.txt')) { $path=Join-Path $root ('artifacts\'+$item); if (Test-Path -LiteralPath $path) { Copy-Item -LiteralPath $path -Destination (Join-Path $stage 'artifacts') } }
Compress-Archive -Path (Join-Path $stage '*') -DestinationPath artifacts\DesktopGoose2-0.3.1-source.zip -Force
Get-FileHash artifacts\DesktopGoose2-Setup.exe,artifacts\DesktopGoose2-0.3.1-windows.zip,artifacts\DesktopGoose2-0.3.1-source.zip | ForEach-Object { $_.Hash+'  '+[IO.Path]::GetFileName($_.Path) }
