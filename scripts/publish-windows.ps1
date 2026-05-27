$ErrorActionPreference = "Stop"

$Root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$Out  = Join-Path $Root "dist\win-x64"
$Zip  = Join-Path $Root "dist\EssSimulator-win-x64.zip"

Push-Location $Root
try {
    Write-Host "==> Publishing EssSimulator for Windows x64 (self-contained)..."
    dotnet publish EssSimulator.csproj `
        -c Release `
        -r win-x64 `
        --self-contained true `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -p:EnableCompressionInSingleFile=true `
        -o $Out

    Copy-Item (Join-Path $Root "scripts\windows\start.bat") (Join-Path $Out "start.bat") -Force
    Copy-Item (Join-Path $Root "scripts\windows\README-Windows.txt") (Join-Path $Out "README-Windows.txt") -Force

    if (Test-Path $Zip) { Remove-Item $Zip -Force }
    Compress-Archive -Path (Join-Path $Out "*") -DestinationPath $Zip -Force

    Write-Host "Done."
    Write-Host "  Folder: $Out"
    Write-Host "  Zip:    $Zip"
}
finally {
    Pop-Location
}
