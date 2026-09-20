# Builds slim, framework-dependent, single-file production exes for both Killer.exe and its
# elevation helper, Killer.Helper.exe, into the same publish folder. "Framework-dependent" means
# the target machine needs the .NET 8 Desktop Runtime already installed - that's what keeps the
# output small instead of bundling the whole runtime.
$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$mainProject = Join-Path $root 'src\Killer\Killer.csproj'
$helperProject = Join-Path $root 'src\Killer.Helper\Killer.Helper.csproj'
$publishDir = Join-Path $root 'publish'

Write-Host 'Cleaning previous build output...' -ForegroundColor Cyan
foreach ($dir in @(
    (Join-Path $root 'src\Killer\bin'),
    (Join-Path $root 'src\Killer\obj'),
    (Join-Path $root 'src\Killer.Helper\bin'),
    (Join-Path $root 'src\Killer.Helper\obj'),
    $publishDir
)) {
    if (Test-Path $dir) { Remove-Item $dir -Recurse -Force }
}

function Publish-Project([string]$project, [string]$label) {
    Write-Host "Publishing $label..." -ForegroundColor Cyan
    dotnet publish $project `
        -c Release `
        -r win-x64 `
        --self-contained false `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -o $publishDir

    if ($LASTEXITCODE -ne 0) { throw "Publishing $label failed with exit code $LASTEXITCODE" }
}

Publish-Project $mainProject 'Killer'
Publish-Project $helperProject 'Killer.Helper'

Write-Host ''
Write-Host "Done: $publishDir\Killer.exe" -ForegroundColor Green
Write-Host "      $publishDir\Killer.Helper.exe (used automatically when admin rights are needed)" -ForegroundColor Green
