#!/usr/bin/env pwsh
# BreadLua — NuGet Pack Script
# Usage: ./pack.ps1

$version = "0.1.0"
$outputDir = "$PSScriptRoot/packages"

Write-Host "=== Packing BreadLua v$version ===" -ForegroundColor Cyan

# Build Release
dotnet build -c Release

# Pack Generator first (no dependencies)
Write-Host "Packing BreadPack.NativeLua.Generator..." -ForegroundColor Yellow
dotnet pack src/BreadLua.Generator/BreadLua.Generator.csproj `
    -c Release `
    -o $outputDir `
    /p:Version=$version

# Pack Runtime (includes native DLLs)
Write-Host "Packing BreadPack.NativeLua..." -ForegroundColor Yellow
dotnet pack src/BreadLua.Runtime/BreadLua.Runtime.csproj `
    -c Release `
    -o $outputDir `
    /p:Version=$version

Write-Host ""
Write-Host "=== Packages ===" -ForegroundColor Green
Get-ChildItem $outputDir -Filter "*.nupkg" | ForEach-Object { Write-Host "  $_" }

Write-Host ""
Write-Host "To publish: dotnet nuget push packages/*.nupkg --api-key YOUR_KEY --source https://api.nuget.org/v3/index.json" -ForegroundColor Gray
