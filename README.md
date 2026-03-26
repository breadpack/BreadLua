# BreadLua

**Native Lua 5.4 for .NET — zero marshalling overhead via shared memory + Source Generator.**

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![CI](https://github.com/breadpack/BreadLua/actions/workflows/ci.yml/badge.svg)](https://github.com/breadpack/BreadLua/actions/workflows/ci.yml)

## Why BreadLua?

Existing .NET Lua libraries have fundamental performance issues:

| Library | Problem |
|---------|---------|
| NLua | Native Lua VM, but 4 P/Invoke calls per field access. C# interop kills performance. |
| MoonSharp | Lua VM reimplemented in C# — interpreter is always slower than native. |
| Lua-CSharp | Improved C# interpreter, still slower than native Lua. |

**BreadLua's approach:**
- **Native Lua 5.4 VM** — full speed, no reinterpretation
- **Shared Memory** — `Buffer<T>` gives C# and Lua zero-copy access to the same data
- **Function Pointers** — `UnmanagedCallersOnly` eliminates call overhead
- **Source Generator** — automatic C bridge + C# binding + Lua wrapper generation

## Benchmark

```
BenchmarkDotNet v0.15.8, Windows 11, Intel Core i5-14600KF, .NET 9.0

| Method                                          |     Mean | Ratio |
|-------------------------------------------------|---------:|------:|
| C# Direct Array Access (100 units x 5 fields)   |  158.0ns |  1.00 |
| BreadLua Buffer<T> (100 units x 5 fields)        |  157.7ns |  1.00 |
| Traditional P/Invoke (100 units x 5 fields)      | 3192.9ns | 20.21 |
```

**Buffer\<T\> matches pure C# speed. 20x faster than traditional P/Invoke.**

## Installation

### NuGet (.NET)

```bash
dotnet add package BreadPack.NativeLua
dotnet add package BreadPack.NativeLua.Generator
```

Native library is included automatically for Windows/Linux/macOS.

### Unity (UPM)

**Option 1 — Git URL (recommended):**

Window > Package Manager > + > Add package from git URL:
```
https://github.com/breadpack/BreadLua.git?path=src/BreadLua.Unity
```

**Option 2 — Local path:**

Edit `Packages/manifest.json`:
```json
{
  "dependencies": {
    "dev.breadpack.nativelua": "file:path/to/BreadLua/src/BreadLua.Unity"
  }
}
```

**Option 3 — Download release:**

1. Download `breadlua-unity-plugins` artifact from [Releases](https://github.com/breadpack/BreadLua/releases)
2. Copy `Plugins/` folder to your Unity project's `Assets/Plugins/`
3. Copy `BreadLua.Runtime.dll` to `Assets/Plugins/`

> **Note:** Unity requires native plugins (`.dll`, `.so`, `.dylib`, `.a`) in the `Plugins/` directory with platform-specific `.meta` files. See [docs/unity-setup.md](docs/unity-setup.md) for details.

## Quick Start

```csharp
using BreadPack.NativeLua;

using var lua = new LuaState();

// Execute Lua code
lua.DoString("print('Hello from Lua!')");

// Evaluate expressions
int result = lua.Eval<int>("10 + 20");  // 30

// Call Lua functions
lua.DoString("function add(a, b) return a + b end");
int sum = lua.Call<int>("add", 3, 7);  // 10

// Set global variables
lua.SetGlobal("playerName", "Hero");
lua.SetGlobal("hp", 100L);

// Runtime binding (C# functions callable from Lua)
lua.Tinker.Bind("multiply", (int a, int b) => a * b);
int product = lua.Eval<int>("multiply(6, 7)");  // 42

// Shared memory buffer (zero-copy)
using var buffer = new Buffer<UnitData>(100);
buffer.Count = 1;
buffer[0] = new UnitData { hp = 100, attack = 25 };
buffer.BindToLua(lua, "g_unit");  // Lua accesses same memory pointer
```

## Source Generator

### Data Binding — `[LuaBridge]`

```csharp
[LuaBridge("unit")]
[StructLayout(LayoutKind.Sequential)]
public struct UnitData
{
    public int unitId;
    public float hp;
    public float attack;
    [LuaReadOnly] public bool isAlive;
    [LuaIgnore] public int internalFlag;
    [LuaField("def")] public float defence;
}
// Generates: UnitDataBridge.g.cs + bread_unit.c + unit_wrapper.lua
```

### Function Binding — `[LuaModule]`

```csharp
[LuaModule("game")]
public static partial class GameAPI
{
    [LuaExport]  // Auto snake_case: "spawn_effect"
    public static void SpawnEffect(int effectId, float x, float y) { }

    [LuaExport("time")]  // Explicit name
    public static float GetTime() => Time.time;
}
// Lua: game.spawn_effect(1, 10, 20)
```

### Class Binding — `[LuaBind]`

```csharp
[LuaBind]
public partial class Player
{
    public string Name { get; set; }
    public int Level { get; set; }
    public float HP { get; set; }

    [LuaConstructor]
    public Player(string name, int level)
    {
        Name = name; Level = level; HP = 100;
    }

    public void Heal(float amount) { HP += amount; }
}
```

```lua
local p = Player("Hero", 10)
print(p.Name)    -- "Hero"
p:Heal(50)
print(p.HP)      -- 150
```

## Unity Integration

```csharp
using BreadPack.NativeLua;
using BreadPack.NativeLua.Unity;
using UnityEngine;

public class LuaDemo : MonoBehaviour
{
    void Start()
    {
        // Option 1: Direct LuaState
        using var lua = new LuaState();
        lua.Tinker.Bind("log", (string msg) => Debug.Log(msg));
        lua.DoString("log('Hello from Lua in Unity!')");

        // Option 2: UnityLuaState component
        // Attach UnityLuaState to a GameObject in the Inspector
        // Set startup scripts (TextAsset) and it auto-manages lifecycle
    }
}
```

### UnityLuaState Component

Attach `UnityLuaState` to a GameObject. It manages the Lua lifecycle automatically:

- **Awake** — Creates LuaState, executes startup scripts
- **Update** — Calls Lua `on_update()` if defined
- **OnDestroy** — Disposes LuaState

### Loading Lua Scripts from Resources

```csharp
var loader = new UnityModuleLoader("Lua");  // Resources/Lua/
string script = loader.Load("my_module");   // Loads Resources/Lua/my_module.lua.txt
lua.DoString(script);
```

> Unity requires Lua scripts to have `.lua.txt` extension to be recognized as TextAsset.

## Features

| Feature | Description |
|---------|-------------|
| LuaState | DoString, DoFile, Call, Eval with type-safe returns |
| Buffer\<T\> | Zero-copy shared memory between C# and Lua |
| LuaTinker | Runtime C# function binding to Lua |
| [LuaBridge] | Source Generator for struct data binding |
| [LuaModule] | Source Generator for static function binding |
| [LuaBind] | Source Generator for class binding with metatable |
| Hot Reload | Watch and reload Lua scripts on file change |
| REPL | Interactive Lua console |
| Unity | UPM package with MonoBehaviour lifecycle integration |

## Supported Platforms

| Platform | Architecture | Library |
|----------|-------------|---------|
| Windows | x64 | `breadlua_native.dll` |
| Linux | x64 | `libbreadlua_native.so` |
| macOS | x64, arm64 (Universal) | `libbreadlua_native.dylib` |
| Android | arm64-v8a, armeabi-v7a | `libbreadlua_native.so` |
| iOS | arm64 (static) | `libbreadlua_native.a` |

## Documentation

- [API Reference](docs/api-reference.md)
- [Unity Setup Guide](docs/unity-setup.md)
- [Source Generator Guide](docs/source-generator.md)
- [Building from Source](docs/building.md)

## Architecture

```
Developer Code ([LuaBridge] struct, [LuaBind] class, [LuaModule] static class)
        |
        | Source Generator (compile time)
        v
C# Bridge (.g.cs) + C Module (.c) + Lua Wrapper (.lua)
        |
        | runtime
        v
C# App <--shared memory--> C Module <--native--> lua54 VM
         (pointer)          (bridge)
```

## License

MIT
