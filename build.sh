#!/bin/bash
# BreadLua — Mac/Linux Build Script
# Usage: ./build.sh [platform]
# Platforms: macos, ios, linux, all, clean
# Note: On Mac/Linux, run: chmod +x build.sh

PLATFORM=${1:-macos}
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
NATIVE_DIR="$SCRIPT_DIR/src/BreadLua.Native"
PLUGIN_DIR="$SCRIPT_DIR/src/BreadLua.Unity/Plugins"

build_macos() {
    echo "=== Building macOS Universal ==="
    cd "$NATIVE_DIR"
    cmake -B build-macos \
        -DCMAKE_BUILD_TYPE=Release \
        -DCMAKE_OSX_ARCHITECTURES="x86_64;arm64"
    cmake --build build-macos --config Release

    mkdir -p "$PLUGIN_DIR/macOS"
    cp build-macos/bin/libbreadlua_native.dylib "$PLUGIN_DIR/macOS/" 2>/dev/null || true
    echo "OK: $PLUGIN_DIR/macOS/libbreadlua_native.dylib"
}

build_ios() {
    echo "=== Building iOS arm64 (static) ==="
    cd "$NATIVE_DIR"
    cmake -B build-ios \
        -DCMAKE_SYSTEM_NAME=iOS \
        -DCMAKE_OSX_ARCHITECTURES=arm64 \
        -DCMAKE_OSX_DEPLOYMENT_TARGET=12.0 \
        -DCMAKE_BUILD_TYPE=Release \
        -DBUILD_SHARED_LIBS=OFF
    cmake --build build-ios --config Release

    mkdir -p "$PLUGIN_DIR/iOS"
    find build-ios -name "*.a" -exec cp {} "$PLUGIN_DIR/iOS/libbreadlua_native.a" \;
    echo "OK: $PLUGIN_DIR/iOS/libbreadlua_native.a"
}

build_linux() {
    echo "=== Building Linux x64 ==="
    cd "$NATIVE_DIR"
    cmake -B build-linux -DCMAKE_BUILD_TYPE=Release
    cmake --build build-linux --config Release

    mkdir -p "$PLUGIN_DIR/Linux"
    cp build-linux/bin/libbreadlua_native.so "$PLUGIN_DIR/Linux/" 2>/dev/null || true
    echo "OK: $PLUGIN_DIR/Linux/libbreadlua_native.so"
}

build_clean() {
    echo "=== Cleaning ==="
    cd "$NATIVE_DIR"
    rm -rf build-*
    echo "OK: Cleaned"
}

case "$PLATFORM" in
    macos)   build_macos ;;
    ios)     build_ios ;;
    linux)   build_linux ;;
    all)
        build_macos
        build_ios
        build_linux
        ;;
    clean)   build_clean ;;
    *)       echo "Usage: ./build.sh [macos|ios|linux|all|clean]" ;;
esac
