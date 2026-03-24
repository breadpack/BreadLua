#!/usr/bin/env pwsh
# BreadLua — Unity Package Preparation
# Usage: ./pack-unity.ps1
# Copies built DLLs and Runtime managed DLL to Unity package

$unityPkg = "$PSScriptRoot/src/BreadLua.Unity"
$runtimeBin = "$PSScriptRoot/src/BreadLua.Runtime/bin/Release/net9.0"

Write-Host "=== Preparing Unity Package ===" -ForegroundColor Cyan

# Build Runtime
dotnet build src/BreadLua.Runtime -c Release

# Copy managed DLL to Unity package
$managedDir = "$unityPkg/Runtime/Managed"
New-Item -ItemType Directory -Force -Path $managedDir | Out-Null

$dll = "$runtimeBin/BreadLua.Runtime.dll"
if (Test-Path $dll) {
    Copy-Item $dll "$managedDir/BreadPack.NativeLua.dll" -Force
    Write-Host "  Copied: BreadPack.NativeLua.dll" -ForegroundColor Green
} else {
    Write-Host "  WARNING: Runtime DLL not found at $dll" -ForegroundColor Red
}

# Report native plugin status
Write-Host ""
Write-Host "=== Native Plugins Status ===" -ForegroundColor Cyan
$platforms = @(
    @{ Name = "Windows"; Path = "$unityPkg/Plugins/Windows/breadlua_native.dll" },
    @{ Name = "Android arm64"; Path = "$unityPkg/Plugins/Android/arm64-v8a/libbreadlua_native.so" },
    @{ Name = "Android armv7"; Path = "$unityPkg/Plugins/Android/armeabi-v7a/libbreadlua_native.so" },
    @{ Name = "macOS"; Path = "$unityPkg/Plugins/macOS/libbreadlua_native.dylib" },
    @{ Name = "iOS"; Path = "$unityPkg/Plugins/iOS/libbreadlua_native.a" },
    @{ Name = "Linux"; Path = "$unityPkg/Plugins/Linux/libbreadlua_native.so" }
)

foreach ($p in $platforms) {
    if (Test-Path $p.Path) {
        Write-Host "  OK $($p.Name)" -ForegroundColor Green
    } else {
        Write-Host "  MISSING $($p.Name) — run ./build.ps1 or ./build.sh" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "=== Unity Installation ===" -ForegroundColor Cyan
Write-Host "Add to Packages/manifest.json:" -ForegroundColor Gray
Write-Host "  ""dev.breadpack.nativelua"": ""file:../../src/BreadLua.Unity""" -ForegroundColor White
Write-Host ""
Write-Host "Or via git URL:" -ForegroundColor Gray
Write-Host "  ""dev.breadpack.nativelua"": ""https://github.com/breadpack/BreadLua.git?path=src/BreadLua.Unity""" -ForegroundColor White
