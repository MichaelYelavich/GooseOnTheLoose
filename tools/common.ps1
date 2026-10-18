$ErrorActionPreference = 'Stop'
$ProjectRoot = Split-Path $PSScriptRoot -Parent
$Dotnet = (Get-Command dotnet -ErrorAction SilentlyContinue).Source
if (!$Dotnet) { $Dotnet = Join-Path $env:LOCALAPPDATA 'GooseBuildTools/dotnet/dotnet.exe' }
if (!(Test-Path -LiteralPath $Dotnet)) { throw 'Install the .NET SDK version listed in global.json.' }
[xml]$VersionDocument = Get-Content -Raw (Join-Path $ProjectRoot 'Version.props')
$ReleaseVersion = [string]$VersionDocument.Project.PropertyGroup.Version
$FileVersion = [string]$VersionDocument.Project.PropertyGroup.FileVersion
$PackagePrefix = [string]$VersionDocument.Project.PropertyGroup.PackagePrefix
function Invoke-Dotnet { & $Dotnet @args; if ($LASTEXITCODE -ne 0) { throw ('dotnet failed: ' + ($args -join ' ')) } }
function Remove-WorkFolder([string]$Path) {
    $absolute = [IO.Path]::GetFullPath($Path)
    $workRoot = [IO.Path]::GetFullPath((Join-Path $ProjectRoot '.work'))
    if (!$absolute.StartsWith($workRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Cleanup path must be inside .work.' }
    if (Test-Path -LiteralPath $absolute) { Remove-Item -LiteralPath $absolute -Recurse -Force }
}
function Find-Inno([string]$Compiler) {
    if ($Compiler -and (Test-Path -LiteralPath $Compiler)) { return $Compiler }
    $candidates = @($env:INNO_COMPILER, (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6/ISCC.exe'), (Join-Path $env:LOCALAPPDATA 'GooseBuildTools/Inno Setup/ISCC.exe'))
    foreach ($path in $candidates) { if ($path -and (Test-Path -LiteralPath $path)) { return $path } }
    throw 'Inno Setup 6 is required. Set INNO_COMPILER or pass -Compiler.'
}
