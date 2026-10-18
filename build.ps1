$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (!(Test-Path -LiteralPath $compiler)) { throw '.NET Framework 4.x compiler was not found.' }
New-Item -ItemType Directory -Force dist,dist\Packs | Out-Null
$sourceFiles = @(Get-ChildItem -LiteralPath src -Filter '*.cs' | ForEach-Object { $_.FullName })
$compilerOptions = @('/nologo','/target:winexe','/platform:anycpu','/optimize+','/utf8output','/out:dist\DesktopGoose2.exe','/win32manifest:app.manifest','/reference:System.Drawing.dll','/reference:System.Windows.Forms.dll','/reference:System.Web.Extensions.dll','/resource:tools\fixtures\stock-honk-0.3.png,DesktopGoose2.Tests.StockHonk')
& $compiler $compilerOptions $sourceFiles
if ($LASTEXITCODE -ne 0) { throw 'Compilation failed.' }
Copy-Item -LiteralPath Packs\goose-notes.json -Destination dist\Packs\goose-notes.json -Force
Copy-Item -LiteralPath Assets -Destination dist -Recurse -Force
$assets=Start-Process -FilePath (Join-Path $PSScriptRoot 'dist\DesktopGoose2.exe') -ArgumentList '--make-assets' -Wait -PassThru -WindowStyle Hidden
if ($assets.ExitCode -ne 0) { throw 'Asset generation failed.' }
& $compiler $compilerOptions /win32icon:dist\DesktopGoose2.ico $sourceFiles
if ($LASTEXITCODE -ne 0) { throw 'Final compilation failed.' }
Copy-Item -LiteralPath README.md -Destination dist\README.md -Force
Copy-Item -LiteralPath RESEARCH.md,VALIDATION.md,RELEASE_NOTES.md -Destination dist -Force
Write-Output 'Built dist\DesktopGoose2.exe — desktop-only runtime'
