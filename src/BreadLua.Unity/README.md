# BreadLua Unity Package

## Installation

Add to your Unity project's `Packages/manifest.json`:
```json
{
  "dependencies": {
    "com.breadpack.nativelua": "file:../../src/BreadLua.Unity"
  }
}
```

## Usage

1. Add `UnityLuaState` component to a GameObject
2. Assign Lua scripts (.lua.txt TextAssets) to the startup scripts array
3. Define `on_update()` in your Lua scripts for per-frame logic

## Native Plugins

Place platform-specific native libraries in `Plugins/`:
- Windows: `Plugins/Windows/breadlua_native.dll`
- Android: `Plugins/Android/libbreadlua_native.so`
- iOS: `Plugins/iOS/libbreadlua_native.a`
- macOS: `Plugins/macOS/libbreadlua_native.dylib`
