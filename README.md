# BreadLua

**Native Lua 5.4 for .NET — zero marshalling overhead via shared memory + Source Generator.**

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

## Why BreadLua?

Existing .NET Lua libraries have fundamental performance issues:

| Library | Problem |
|---------|---------|
| NLua | Native Lua VM, but 4 P/Invoke calls per field access. C# interop kills performance. |
| MoonSharp | Lua VM reimplemented in C# — interpreter is always slower than JIT. |
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

Zero managed allocations across all operations.

## Quick Start

```csharp
using BreadPack.NativeLua;

using var lua = new LuaState();

// Execute Lua code
lua.DoString("print('Hello from Lua!')");

// Evaluate expressions
int result = lua.Eval<int>("10 + 20");

// Call Lua functions with arguments
lua.DoString("function add(a, b) return a + b end");
int sum = lua.Call<int>("add", 3, 7);  // 10

// Runtime binding (LuaTinker)
lua.Tinker.Bind("multiply", (int a, int b) => a * b);
int product = lua.Eval<int>("multiply(6, 7)");  // 42

// Shared memory buffer (zero-copy)
using var buffer = new Buffer<UnitData>(100);
buffer.Count = 1;
buffer[0] = new UnitData { hp = 100, attack = 25 };
buffer.BindToLua(lua, "g_unit");  // Lua accesses same memory
```

## Source Generator

### Data Binding — `[LuaBridge]`

```csharp
[LuaBridge("unit")]
[StructLayout(LayoutKind.Sequential)]
public struct UnitData
{
    public int unitId;         // Auto-exposed to Lua
    public float hp;
    public float attack;
    [LuaReadOnly] public bool isAlive;    // Getter only
    [LuaIgnore] public int internalFlag;  // Hidden from Lua
    [LuaField("def")] public float defence;  // Custom Lua name
}
// Source Generator creates: UnitDataBridge.g.cs + bread_unit.c + unit.lua
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

## Features

| Feature | Status |
|---------|--------|
| Native Lua 5.4 VM | Done |
| LuaState (DoString, DoFile, Call, Eval) | Done |
| Buffer\<T\> shared memory | Done |
| Source Generator — [LuaBridge] | Done |
| Source Generator — [LuaModule] | Done |
| Source Generator — [LuaBind] | Done |
| LuaTinker — runtime Bind() | Done |
| Hot Reload | Done |
| REPL | Done |
| Unity package | Done |
| Benchmarks | Done |

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

## Unity

Install via UPM (Package Manager):
```json
{ "dev.breadpack.nativelua": "file:path/to/BreadLua.Unity" }
```

Place native plugins in `Plugins/{Windows,Android,iOS,macOS}/`.

## License

MIT
