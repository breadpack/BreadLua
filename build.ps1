#!/usr/bin/env pwsh
# BreadLua — Local Cross-Compile Build Script
# Usage: ./build.ps1 [platform]
# Platforms: windows, android, all, clean

param(
    [string]$Platform = "windows"
)

$NativeDir = "$PSScriptRoot/src/BreadLua.Native"
$PluginDir = "$PSScriptRoot/src/BreadLua.Unity/Plugins"

function Build-Windows {
    Write-Host "=== Building Windows x64 ===" -ForegroundColor Cyan
    Push-Location $NativeDir
    cmake -B build-windows -DCMAKE_BUILD_TYPE=Release -A x64
    cmake --build build-windows --config Release

    # Copy to Unity plugins
    New-Item -ItemType Directory -Force -Path "$PluginDir/Windows" | Out-Null
    Copy-Item "build-windows/bin/Release/breadlua_native.dll" "$PluginDir/Windows/" -Force
    Write-Host "OK: $PluginDir/Windows/breadlua_native.dll" -ForegroundColor Green
    Pop-Location
}

function Build-Android {
    Write-Host "=== Building Android ===" -ForegroundColor Cyan

    # Find Android NDK
    $ndkPath = $env:ANDROID_NDK_HOME
    if (-not $ndkPath) {
        $ndkPath = $env:ANDROID_NDK_ROOT
    }
    if (-not $ndkPath) {
        # Common paths
        $candidates = @(
            "$env:LOCALAPPDATA/Android/Sdk/ndk/*",
            "$env:USERPROFILE/AppData/Local/Android/Sdk/ndk/*",
            "C:/Microsoft/AndroidNDK*/*"
        )
        foreach ($c in $candidates) {
            $found = Get-Item $c -ErrorAction SilentlyContinue | Sort-Object -Descending | Select-Object -First 1
            if ($found) { $ndkPath = $found.FullName; break }
        }
    }

    if (-not $ndkPath) {
        Write-Host "ERROR: Android NDK not found. Set ANDROID_NDK_HOME." -ForegroundColor Red
        return
    }

    $toolchain = "$ndkPath/build/cmake/android.toolchain.cmake"
    Write-Host "NDK: $ndkPath" -ForegroundColor Gray

    Push-Location $NativeDir

    # arm64-v8a
    Write-Host "  Building arm64-v8a..." -ForegroundColor Yellow
    cmake -B build-android-arm64 `
        -DCMAKE_TOOLCHAIN_FILE="$toolchain" `
        -DANDROID_ABI=arm64-v8a `
        -DANDROID_PLATFORM=android-21 `
        -DCMAKE_BUILD_TYPE=Release
    cmake --build build-android-arm64 --config Release

    New-Item -ItemType Directory -Force -Path "$PluginDir/Android/arm64-v8a" | Out-Null
    $so64 = Get-ChildItem "build-android-arm64" -Recurse -Filter "libbreadlua_native.so" | Select-Object -First 1
    if ($so64) { Copy-Item $so64.FullName "$PluginDir/Android/arm64-v8a/" -Force }

    # armeabi-v7a
    Write-Host "  Building armeabi-v7a..." -ForegroundColor Yellow
    cmake -B build-android-armv7 `
        -DCMAKE_TOOLCHAIN_FILE="$toolchain" `
        -DANDROID_ABI=armeabi-v7a `
        -DANDROID_PLATFORM=android-21 `
        -DCMAKE_BUILD_TYPE=Release
    cmake --build build-android-armv7 --config Release

    New-Item -ItemType Directory -Force -Path "$PluginDir/Android/armeabi-v7a" | Out-Null
    $so32 = Get-ChildItem "build-android-armv7" -Recurse -Filter "libbreadlua_native.so" | Select-Object -First 1
    if ($so32) { Copy-Item $so32.FullName "$PluginDir/Android/armeabi-v7a/" -Force }

    Write-Host "OK: Android builds complete" -ForegroundColor Green
    Pop-Location
}

function Build-Clean {
    Write-Host "=== Cleaning all builds ===" -ForegroundColor Cyan
    Push-Location $NativeDir
    Remove-Item -Recurse -Force build-* -ErrorAction SilentlyContinue
    Pop-Location
    Write-Host "OK: Cleaned" -ForegroundColor Green
}

# Main
switch ($Platform.ToLower()) {
    "windows" { Build-Windows }
    "android" { Build-Android }
    "all" {
        Build-Windows
        Build-Android
        Write-Host ""
        Write-Host "=== macOS/iOS require Mac build ===" -ForegroundColor Yellow
        Write-Host "  On Mac: ./build.sh macos" -ForegroundColor Yellow
        Write-Host "  On Mac: ./build.sh ios" -ForegroundColor Yellow
    }
    "clean" { Build-Clean }
    default {
        Write-Host "Usage: ./build.ps1 [windows|android|all|clean]"
    }
}
