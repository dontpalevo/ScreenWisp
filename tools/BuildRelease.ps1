param([Parameter(Mandatory=$true)][string]$IsccPath)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
Push-Location $root
try {
 & .\build.ps1 -PublishDirectory artifacts/rc3-about-payload
 if($LASTEXITCODE){throw 'Build/test/publish failed'}
 $payload=Join-Path $root 'artifacts\rc3-about-payload';$out=Join-Path $root 'release\rc3-about'
 New-Item -ItemType Directory -Force $out | Out-Null
 Copy-Item -LiteralPath (Join-Path $root 'COPYRIGHT.md') -Destination $payload -Force
 foreach($readme in @('README.md','README.ru.md')){Copy-Item -LiteralPath (Join-Path $root $readme) -Destination $payload -Force}
 $packages=if($env:NUGET_PACKAGES){$env:NUGET_PACKAGES}else{Join-Path $env:USERPROFILE '.nuget\packages'}
 $net=Get-ChildItem (Join-Path $packages 'microsoft.netcore.app.runtime.win-x64') -Directory | Sort-Object Name -Descending | Select-Object -First 1
 $desktop=Get-ChildItem (Join-Path $packages 'microsoft.windowsdesktop.app.runtime.win-x64') -Directory | Sort-Object Name -Descending | Select-Object -First 1
 $licenses=Join-Path $payload 'runtime-licenses';New-Item -ItemType Directory -Force $licenses | Out-Null
 Copy-Item (Join-Path $net.FullName 'LICENSE.TXT') (Join-Path $licenses 'dotnet-LICENSE.txt') -Force
 Copy-Item (Join-Path $net.FullName 'THIRD-PARTY-NOTICES.TXT') (Join-Path $licenses 'dotnet-THIRD-PARTY-NOTICES.txt') -Force
 Copy-Item (Join-Path $desktop.FullName 'LICENSE') (Join-Path $licenses 'windowsdesktop-LICENSE.txt') -Force
 # No installer marker or PDB in the portable distribution.
 $stage=Join-Path $root 'artifacts\rc3-about-portable-package';New-Item -ItemType Directory -Force $stage | Out-Null
 foreach($name in @('ScreenWisp.exe','LICENSE','LICENSE-SCREENWISP','COPYRIGHT.md','THIRD-PARTY-NOTICES.md','README.md','README.ru.md')){Copy-Item -LiteralPath (Join-Path $payload $name) -Destination $stage -Force}
 Copy-Item -LiteralPath $licenses -Destination $stage -Recurse -Force
 foreach($base in @($payload,$stage)){
  New-Item -ItemType Directory -Force (Join-Path $base 'docs\screenshots'),(Join-Path $base 'branding\png') | Out-Null
  Copy-Item -Path (Join-Path $root 'docs\screenshots\*.png') -Destination (Join-Path $base 'docs\screenshots') -Force
  foreach($guide in @('DEMO-RECORDING.md','DEVELOPMENT.md','PUBLIC-ARCHITECTURE.md','RELEASE-VALIDATION.md','LOCALIZATION.md')){Copy-Item -LiteralPath (Join-Path $root ('docs\'+$guide)) -Destination (Join-Path $base 'docs') -Force}
  Copy-Item -LiteralPath (Join-Path $root 'branding\png\ScreenWisp-128.png') -Destination (Join-Path $base 'branding\png') -Force
 }

 Compress-Archive -Path (Join-Path $stage '*') -DestinationPath (Join-Path $out 'ScreenWisp-Portable.zip') -Force -CompressionLevel Optimal
 & $IsccPath ('/DPayloadDir='+$payload) ('/DReleaseDir='+$out) (Join-Path $root 'installer\ScreenWisp.iss')
 if($LASTEXITCODE){throw 'Inno Setup compile failed'}
 $lines=foreach($name in @('ScreenWisp-Setup.exe','ScreenWisp-Portable.zip')){(Get-FileHash -LiteralPath (Join-Path $out $name) -Algorithm SHA256).Hash.ToLowerInvariant()+'  '+$name}
 [IO.File]::WriteAllLines((Join-Path $out 'SHA256SUMS.txt'),$lines,[Text.UTF8Encoding]::new($false))
} finally{Pop-Location}
