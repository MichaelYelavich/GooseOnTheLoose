param([switch]$Locked)
. (Join-Path $PSScriptRoot 'tools/common.ps1')
Push-Location $ProjectRoot
try {
    $restore = @('restore', 'GooseOnTheLoose.slnx')
    if ($Locked) { $restore += '--locked-mode' }
    Invoke-Dotnet @restore
    Invoke-Dotnet build GooseOnTheLoose.slnx -c Release --no-restore
    $generated = Join-Path $ProjectRoot '.work/generated'
    New-Item -ItemType Directory -Force $generated | Out-Null
    & tools/AssetGenerator/bin/Release/Goose.AssetGenerator.exe $generated
    if ($LASTEXITCODE -ne 0) { throw 'Asset generation failed.' }
    Invoke-Dotnet build GooseOnTheLoose.slnx -c Release --no-restore
    New-Item -ItemType Directory -Force dist | Out-Null
    foreach ($name in @('GooseOnTheLoose.exe','GooseOnTheLoose.exe.config','Goose.Core.dll')) { Copy-Item -LiteralPath (Join-Path $ProjectRoot ('src/bin/Release/' + $name)) -Destination dist -Force }
    Copy-Item -LiteralPath (Join-Path $generated 'GooseOnTheLoose.ico') -Destination dist -Force
    foreach ($destination in @('dist','tests/bin/Release')) {
        foreach ($folder in @('Assets','Packs')) { Copy-Item -LiteralPath $folder -Destination $destination -Recurse -Force }
        Copy-Item -LiteralPath (Join-Path $generated 'Packs/Images') -Destination (Join-Path $destination 'Packs') -Recurse -Force
    }
    Copy-Item -LiteralPath README.md,CONTRIBUTING.md,RELEASE_NOTES.md,RESEARCH.md,VALIDATION.md,global.json -Destination dist -Force
    Copy-Item -LiteralPath docs -Destination dist -Recurse -Force
} finally { Pop-Location }
