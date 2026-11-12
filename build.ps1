param([string]$PublishDirectory='artifacts/win-x64')
$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot
try {
 dotnet build ScreenWisp.slnx -c Release
 if ($LASTEXITCODE) { throw 'Build failed' }
 dotnet run --project tests/ScreenWisp.Tests -c Release --no-build
 if ($LASTEXITCODE) { throw 'Tests failed' }
 dotnet run --project tests/ScreenWisp.Tests -c Release --no-build -- --lifecycle
 if ($LASTEXITCODE) { throw 'Lifecycle test failed' }
 dotnet publish src/ScreenWisp -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o $PublishDirectory
 if ($LASTEXITCODE) { throw 'Publish failed' }
} finally { Pop-Location }
