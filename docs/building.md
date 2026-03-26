# Building from Source

## Prerequisites

- .NET 9.0 SDK
- CMake 3.16+
- C compiler (MSVC, GCC, or Clang)
- Platform-specific: Android NDK, Xcode (iOS/macOS)

## Clone

```bash
git clone https://github.com/breadpack/BreadLua.git
cd BreadLua
```

## Native Library

### Windows

```powershell
cd src/BreadLua.Native
cmake -B build -DCMAKE_BUILD_TYPE=Release -A x64
cmake --build build --config Release
# Output: build/bin/Release/breadlua_native.dll
```

### Linux

```bash
cd src/BreadLua.Native
cmake -B build -DCMAKE_BUILD_TYPE=Release
cmake --build build --config Release
# Output: build/bin/libbreadlua_native.so
```

### macOS (Universal)

```bash
cd src/BreadLua.Native
cmake -B build -DCMAKE_BUILD_TYPE=Release -DCMAKE_OSX_ARCHITECTURES="x86_64;arm64"
cmake --build build --config Release
# Output: build/bin/libbreadlua_native.dylib
```

### Android

```bash
cd src/BreadLua.Native

# arm64-v8a
cmake -B build-arm64 \
  -DCMAKE_TOOLCHAIN_FILE=$ANDROID_NDK_HOME/build/cmake/android.toolchain.cmake \
  -DANDROID_ABI=arm64-v8a \
  -DANDROID_PLATFORM=android-21 \
  -DCMAKE_BUILD_TYPE=Release
cmake --build build-arm64 --config Release

# armeabi-v7a
cmake -B build-armv7 \
  -DCMAKE_TOOLCHAIN_FILE=$ANDROID_NDK_HOME/build/cmake/android.toolchain.cmake \
  -DANDROID_ABI=armeabi-v7a \
  -DANDROID_PLATFORM=android-21 \
  -DCMAKE_BUILD_TYPE=Release
cmake --build build-armv7 --config Release
```

### iOS (Static)

```bash
cd src/BreadLua.Native
cmake -B build-ios \
  -DCMAKE_SYSTEM_NAME=iOS \
  -DCMAKE_OSX_ARCHITECTURES=arm64 \
  -DCMAKE_OSX_DEPLOYMENT_TARGET=12.0 \
  -DCMAKE_BUILD_TYPE=Release \
  -DBUILD_SHARED_LIBS=OFF
cmake --build build-ios --config Release
# Output: build-ios/bin/libbreadlua_native.a
```

### Build Scripts

Convenience scripts are provided in the repository root:

```bash
./build-macos.sh    # macOS universal
./build-android.sh  # Android arm64 + armv7
./build-ios.sh      # iOS static
./build-linux.sh    # Linux x64
```

Windows:
```powershell
./build.ps1
```

## .NET Build

```bash
dotnet build
```

## Run Tests

```bash
dotnet run --project tests/BreadLua.Tests/
```

## NuGet Package

```bash
# Build native libraries for all platforms first, then:
dotnet pack src/BreadLua.Runtime/ -c Release
dotnet pack src/BreadLua.Generator/ -c Release
```

Or use the provided script:
```powershell
./pack.ps1
```

## Unity Package

Build the Runtime DLL for netstandard2.1:
```bash
dotnet build src/BreadLua.Runtime/ -f netstandard2.1 -c Release
cp src/BreadLua.Runtime/bin/Release/netstandard2.1/BreadLua.Runtime.dll src/BreadLua.Unity/Plugins/
```

Copy native plugins to `src/BreadLua.Unity/Plugins/{Platform}/`.

Or use the provided script:
```powershell
./pack-unity.ps1
```

## CI/CD

The project uses GitHub Actions for CI:

```
Native Build (5 platforms) → .NET Tests (3 OS) → Unity Editor Tests
                                                      ↓
                                              Unity IL2CPP Build (Android/iOS)
                                                      ↓
                                              Firebase Test Lab (real device)
```

CI is triggered manually via `workflow_dispatch`. Run from the Actions tab or:
```bash
gh workflow run CI --ref main
```
