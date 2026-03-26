# Unity Setup Guide

## Requirements

- Unity 2021.3 LTS or later
- Native plugins for target platforms

## Installation

### Via Git URL (recommended)

1. Open **Window > Package Manager**
2. Click **+** > **Add package from git URL**
3. Enter:
```
https://github.com/breadpack/BreadLua.git?path=src/BreadLua.Unity
```

### Via Local Path

If you cloned the repository locally:

Edit `Packages/manifest.json`:
```json
{
  "dependencies": {
    "dev.breadpack.nativelua": "file:../path/to/BreadLua/src/BreadLua.Unity"
  }
}
```

### Manual Installation

1. Download `BreadLua.Runtime.dll` (netstandard2.1 build)
2. Download native plugins for your target platforms
3. Copy to your project:

```
Assets/
  Plugins/
    BreadLua.Runtime.dll
    Windows/
      breadlua_native.dll
    Linux/
      libbreadlua_native.so
    macOS/
      libbreadlua_native.dylib
    Android/
      arm64-v8a/
        libbreadlua_native.so
      armeabi-v7a/
        libbreadlua_native.so
    iOS/
      libbreadlua_native.a
```

## Native Plugin .meta Files

Each native plugin needs a `.meta` file that tells Unity which platform it targets. The UPM package includes these automatically. For manual installation, ensure each plugin has correct platform settings in the Inspector:

| Plugin | Platform | CPU |
|--------|----------|-----|
| `breadlua_native.dll` | Windows x64 | x86_64 |
| `libbreadlua_native.so` (Linux) | Linux | x86_64 |
| `libbreadlua_native.dylib` | macOS | AnyCPU |
| `libbreadlua_native.so` (Android arm64) | Android | ARM64 |
| `libbreadlua_native.so` (Android armv7) | Android | ARMv7 |
| `libbreadlua_native.a` | iOS | ARM64 |

## Basic Usage

### Direct LuaState

```csharp
using BreadPack.NativeLua;
using UnityEngine;

public class LuaExample : MonoBehaviour
{
    private LuaState _lua;

    void Awake()
    {
        _lua = new LuaState();
        _lua.Tinker.Bind("log", (Action<string>)(msg => Debug.Log(msg)));
        _lua.DoString("log('Lua initialized!')");
    }

    void Update()
    {
        _lua.DoString("log('frame update')");
    }

    void OnDestroy()
    {
        _lua?.Dispose();
    }
}
```

### UnityLuaState Component

1. Attach `UnityLuaState` component to a GameObject
2. Set **Startup Scripts** in Inspector (TextAsset files)
3. The component manages the Lua lifecycle:

```
Awake   → LuaState created, startup scripts executed
Update  → Calls Lua on_update() function (if defined)
Destroy → LuaState disposed
```

Lua startup script example (`Assets/Resources/Lua/init.lua.txt`):
```lua
function on_update()
    -- called every frame
end
```

### Loading Scripts from Resources

```csharp
var loader = new UnityModuleLoader("Lua");
string script = loader.Load("game_logic");  // Resources/Lua/game_logic.lua.txt
lua.DoString(script);
```

> Lua files must use `.lua.txt` extension for Unity to recognize them as TextAsset.

## IL2CPP / Mobile

BreadLua is tested on IL2CPP builds for Android and iOS. Key considerations:

### link.xml

The UPM package includes `link.xml` to prevent code stripping:
```xml
<linker>
  <assembly fullname="BreadPack.NativeLua.Unity" preserve="all"/>
</linker>
```

### MonoPInvokeCallback

Callback functions from native to managed code require `[MonoPInvokeCallback]` for IL2CPP. BreadLua handles this internally.

### Supported Types

All value types work on IL2CPP:
- `int`, `long`, `float`, `double`, `bool`, `string`
- `Buffer<T>` with any `unmanaged` struct
- `ObjectHandle` for reference type marshalling

### Unicode

String marshalling uses UTF-8. Korean, Japanese, Chinese, and emoji are fully supported:
```csharp
lua.SetGlobal("msg", "Hello 세계 🎮");
string result = lua.Eval<string>("msg");  // "Hello 세계 🎮"
```

## Building Native Plugins Locally

### Prerequisites
- CMake 3.16+
- Platform-specific toolchains (Xcode for iOS, Android NDK for Android)

### Build Scripts

```bash
# macOS
./build-macos.sh

# Windows (PowerShell)
./build.ps1

# Android (requires ANDROID_NDK_HOME)
./build-android.sh

# iOS
./build-ios.sh

# Linux
./build-linux.sh
```

Built libraries are output to `src/BreadLua.Native/build-{platform}/bin/`.

## Troubleshooting

### DllNotFoundException

Native library not found. Check:
1. Plugin file exists in `Plugins/{Platform}/`
2. `.meta` file has correct platform settings
3. For Android: correct ABI folder (`arm64-v8a` or `armeabi-v7a`)

### Multiple plugins with the same name

Native plugins should only be in the UPM package `Plugins/` folder, not duplicated in `Assets/Plugins/`. Remove duplicates.

### IL2CPP build fails with stripping

Ensure `link.xml` exists in the package Runtime folder and is not excluded from build.
