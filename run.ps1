# Cleans and runs the app in dev mode (Debug build via `dotnet run`).
# Killer.Helper.exe (the elevation helper) is a separate project/exe that Killer.exe looks for
# next to itself at runtime - `dotnet run` only builds the project you point it at, so this
# script builds the helper too and copies its output alongside Killer.exe before launching.
$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$mainProject = Join-Path $root 'src\Killer\Killer.csproj'
$helperProject = Join-Path $root 'src\Killer.Helper\Killer.Helper.csproj'
$mainOutDir = Join-Path $root 'src\Killer\bin\Debug\net8.0-windows'
$helperOutDir = Join-Path $root 'src\Killer.Helper\bin\Debug\net8.0-windows'

# The app is single-instance and closing its window only hides it to the tray - so a leftover
# process from a previous run would otherwise just get woken up instead of the freshly built
# code ever starting, making rebuilt changes silently never take effect.
$existing = Get-Process -Name 'Killer', 'Killer.Helper' -ErrorAction SilentlyContinue
if ($existing) {
    Write-Host 'Stopping previous running instance...' -ForegroundColor Cyan
    $existing | Stop-Process -Force
    Start-Sleep -Milliseconds 300
}

Write-Host 'Cleaning previous build output...' -ForegroundColor Cyan
foreach ($dir in @(
    (Join-Path $root 'src\Killer\bin'),
    (Join-Path $root 'src\Killer\obj'),
    (Join-Path $root 'src\Killer.Helper\bin'),
    (Join-Path $root 'src\Killer.Helper\obj')
)) {
    if (Test-Path $dir) { Remove-Item $dir -Recurse -Force }
}

Write-Host 'Building elevation helper...' -ForegroundColor Cyan
dotnet build $helperProject -c Debug
if ($LASTEXITCODE -ne 0) { throw "Building Killer.Helper failed with exit code $LASTEXITCODE" }

Write-Host 'Building main app...' -ForegroundColor Cyan
dotnet build $mainProject -c Debug
if ($LASTEXITCODE -ne 0) { throw "Building Killer failed with exit code $LASTEXITCODE" }

Write-Host 'Placing helper exe next to the main app...' -ForegroundColor Cyan
Copy-Item -Path (Join-Path $helperOutDir '*') -Destination $mainOutDir -Force

Write-Host 'Running in dev mode...' -ForegroundColor Cyan
dotnet run --project $mainProject -c Debug --no-build

exit $LASTEXITCODE
