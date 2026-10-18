# Pinned official compiler, stored outside release artifacts.
$ErrorActionPreference='Stop'
$version='6.7.3'
$expected='9C73C3BAE7ED48D44112A0F48E66742C00090BDB5BEF71D9D3C056C66E97B732'
$toolsRoot=Join-Path $env:LOCALAPPDATA 'GooseBuildTools'
New-Item -ItemType Directory -Force $toolsRoot | Out-Null
$download=Join-Path $toolsRoot ('innosetup-'+$version+'.exe')
Invoke-WebRequest -Uri ('https://github.com/jrsoftware/issrc/releases/download/is-6_7_3/innosetup-'+$version+'.exe') -OutFile $download
if ((Get-FileHash -LiteralPath $download).Hash -ne $expected) { throw 'Inno Setup download checksum mismatch.' }
$signature=Get-AuthenticodeSignature -FilePath $download
if ($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Subject -notlike '*O=Pyrsys B.V.*') { throw 'Inno Setup publisher signature is invalid.' }
$destination=Join-Path $toolsRoot 'Inno Setup'
$process=Start-Process -FilePath $download -ArgumentList @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART','/CURRENTUSER',('/DIR="'+$destination+'"')) -WindowStyle Hidden -Wait -PassThru
if ($process.ExitCode -ne 0) { throw 'Inno Setup installation failed.' }
Write-Output (Join-Path $destination 'ISCC.exe')
