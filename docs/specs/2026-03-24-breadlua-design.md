# BreadLua Design Spec

## Overview

BreadLua는 네이티브 Lua 5.4 VM을 .NET에서 마샬링 오버헤드 없이 사용하는 라이브러리다.
Source Generator가 C 브릿지 모듈 + C# 바인딩 + Lua 래퍼를 자동 생성한다.

- 라이선스: MIT
- 타겟: .NET 5+ / Unity (IL2CPP/AOT 포함, 모바일)
- Lua: 5.4 (네이티브)

## Problem

기존 .NET Lua 라이브러리의 한계:

| 라이브러리 | 문제 |
|-----------|------|
| NLua | 네이티브 Lua VM이지만 필드 1개 접근에 P/Invoke 4회. C# 연동 시 성능 폭락 |
| MoonSharp | C#으로 Lua VM 재구현 → 인터프리터가 JIT 코드보다 항상 느림 |
| Lua-CSharp | 개선됐지만 여전히 C# 인터프리터. 네이티브 Lua 대비 느림 |

## Solution

```
네이티브 Lua VM (최고 성능)
+ 공유 메모리 (데이터 전달 비용 0)
+ 함수 포인터 (UnmanagedCallersOnly, 마샬링 없는 함수 호출)
+ Source Generator (모든 glue 코드 자동 생성)
+ P/Invoke는 제어 흐름만 (프레임당 1~5회)
```

## Architecture

```
┌─────────────────────────────────────────────┐
│  Developer Code                              │
│  [LuaBridge] struct, [LuaBind] class        │
│  [LuaModule] static class                   │
└──────────────┬──────────────────────────────┘
               │ Source Generator (compile time)
               ▼
┌──────────────────────────────────────────────┐
│  BreadLua.Generator                          │
│  → C# Bridge (.g.cs)                        │
│  → C Module  (.c → .dll/.so/.a)             │
│  → Lua Wrapper (.lua)                       │
└──────────────┬──────────────────────────────┘
               │ runtime
               ▼
┌──────────────────────────────────────────────┐
│  BreadLua.Runtime                            │
│  ┌──────────┐  shared mem   ┌─────────────┐ │
│  │ C# App   │◄────────────►│ C Module    │ │
│  └────┬─────┘  (pointer)    └──────┬──────┘ │
│       │ P/Invoke minimal          │ native  │
│       ▼                          ▼         │
│  ┌──────────────────────────────────┐      │
│  │       lua54 (native VM)           │      │
│  └──────────────────────────────────┘      │
└──────────────────────────────────────────────┘
```

## Projects

| Project | Type | Role |
|---------|------|------|
| `BreadLua.Runtime` | .NET class library | LuaState, attributes, runtime core |
| `BreadLua.Generator` | Source Generator | C/C#/Lua code generation |
| `BreadLua.Native` | CMake C project | lua54 + generated C modules build |
| `BreadLua.Unity` | Unity package | native plugin loading, module loaders |
| `BreadLua.Tests` | TUnit tests | unit/integration tests |

## Namespace

```
BreadPack.NativeLua
```

Class names do not carry the "Bread" prefix.

## API Design

### Data Binding (`[LuaBridge]`)

Unmanaged struct shared via pointer. All public fields exposed by default.

```csharp
using BreadPack.NativeLua;

[LuaBridge("unit")]
[StructLayout(LayoutKind.Sequential)]
public struct UnitData
{
    public int unitId;             // auto-exposed as "unitId"
    public float hp;
    public float attack;
    [LuaReadOnly] public bool isAlive;
    [LuaIgnore]   public int internalFlag;
    [LuaField("def")] public float defence;  // rename only when needed
}
```

Generated:
- `UnitDataBridge.g.cs` — buffer alloc/free, Lua binding
- `bread_unit.c` — getter/setter/bulk access
- `unit.lua` — OOP wrapper

### Function Binding (`[LuaModule]` + `[LuaExport]`)

C# static methods exposed to Lua via function pointers. No P/Invoke per call.

```csharp
[LuaModule("game")]
public static partial class GameAPI
{
    [LuaExport]  // name omitted → auto snake_case: "spawn_effect"
    public static void SpawnEffect(int effectId, float x, float y) { }

    [LuaExport("time")]
    public static float GetTime() => Time.time;
}
```

Generated:
- `GameAPI.g.cs` — `UnmanagedCallersOnly` entry points + function pointer registration
- `bread_game.c` — Lua-callable C functions using function pointers

### Class Binding (`[LuaBind]`)

Bidirectional binding. Lua can create C# objects and call methods.

```csharp
[LuaBind]
public partial class Player
{
    public string Name { get; set; }
    public int Level { get; set; }
    public float HP { get; set; }

    [LuaIgnore]
    public int InternalId { get; set; }

    [LuaConstructor]
    public Player(string name, int level)
    {
        Name = name;
        Level = level;
        HP = 100;
    }

    public void Heal(float amount) { HP += amount; }
}
```

```lua
-- Create C# object from Lua
local p = Player("Hero", 10)
print(p.Name)
p:Heal(50)

-- Bulk create from table
local p2 = Player.from({ Name = "Archer", Level = 5 })
```

Reverse binding (Lua table → C# object):
```lua
local config = { Name = "Goblin", Level = 5 }
game.register_enemy(config)  -- received as C# object
```

### LuaTinker (Lightweight Binding + REPL)

```csharp
var lua = new LuaState();

// Quick bind (signature auto-inferred)
lua.Bind("add", (int a, int b) => a + b);
lua.Bind("log", (string msg) => Console.WriteLine(msg));

// Quick call
int result = lua.Call<int>("add", 10, 20);

// Hot reload
lua.Reload("scripts/battle.lua");

// REPL
lua.StartRepl();
```

### Runtime Core

```csharp
namespace BreadPack.NativeLua
{
    public class LuaState : IDisposable
    {
        public LuaState(LuaConfig config = null);

        // Script execution
        public void DoFile(string path);
        public void DoString(string code);
        public T Eval<T>(string expression);

        // Function call (1 P/Invoke per call)
        public void Call(string funcName, params object[] args);
        public T Call<T>(string funcName, params object[] args);

        // LuaTinker
        public void Bind(string name, Delegate func);
        public void Bind<T>(string name, T value);

        // Data buffer
        public Buffer<T> CreateBuffer<T>(int capacity) where T : unmanaged;

        // Module loader
        public void AddModuleLoader(IModuleLoader loader);

        // Hot reload
        public void Reload(string path);

        // REPL
        public void StartRepl();
    }
}
```

### Attributes

```csharp
namespace BreadPack.NativeLua
{
    [LuaBridge("name")]     // unmanaged struct → shared memory
    [LuaBind]               // managed class → GCHandle + userdata
    [LuaModule("name")]     // static class → function module
    [LuaExport("name")]     // method → Lua function (name optional, auto snake_case)
    [LuaConstructor]        // constructor → Lua callable
    [LuaField("name")]      // field/property rename (optional, all public exposed by default)
    [LuaReadOnly]           // getter only
    [LuaIgnore]             // exclude from Lua
}
```

## Internal Mechanisms

### Shared Memory Lifecycle

```
Initialize           Frame Loop            Dispose
AllocHGlobal  →  C#/C direct read/write  →  FreeHGlobal
BindToLua (pointer transfer, 1 P/Invoke)
```

### Function Pointer Mechanism

```
Init (once):
  [UnmanagedCallersOnly] C# method → function pointer → stored in C module

Runtime (per frame):
  Lua script → C module → function pointer → C# direct (no P/Invoke)
```

### LuaBind Object Lifecycle

```
Lua: Player("Hero", 10)
  → C module l_player_new()
  → function pointer → C# PlayerFactory.Create()
  → GCHandle.Alloc(player) → IntPtr
  → Lua userdata stores IntPtr

Lua: p:Heal(50)
  → C module → function pointer → C# Heal()

Lua GC: __gc metamethod → C module → GCHandle.Free()
```

### String Handling

- Numbers/bool: zero cost (blittable)
- Strings: UTF-8 byte* passed via function pointer, C# does Encoding.UTF8.GetString()
- P/Invoke count still minimized

### Error Handling

```csharp
try { lua.Call("on_frame"); }
catch (LuaException ex)
{
    // ex.LuaStackTrace, ex.ScriptFile, ex.Line
}
```

```lua
local ok, err = pcall(function()
    game.spawn_effect(-1, 0, 0)
end)
```

### Source Generator Pipeline

```
Compile time:
  1. [LuaBridge] struct → bread_{name}.c + {Name}Bridge.g.cs + {name}.lua
  2. [LuaModule] class  → bread_{name}.c + {Name}Module.g.cs
  3. [LuaBind] class    → bread_{name}.c + {Name}Bind.g.cs + {name}.lua

Build time:
  4. Collect generated .c files
  5. lua54 source + bread_*.c → CMake → breadlua_native.dll/.so/.a

Runtime:
  6. LuaState loads breadlua_native
  7. Each Bridge/Module/Bind auto-initializes
```

### Naming Convention

| C# | Lua auto-conversion |
|----|---------------------|
| `SpawnEffect` (method) | `spawn_effect` (snake_case) |
| `HP` (property) | `HP` (as-is) |
| `unitId` (field) | `unitId` (as-is) |
| `[LuaField("def")]` | `def` (explicit) |
| `[LuaExport("time")]` | `time` (explicit) |

## Unity Integration

```
BreadLua.Unity/
├── Plugins/
│   ├── Windows/breadlua_native.dll
│   ├── Android/libbreadlua_native.so  (arm64, armv7)
│   ├── iOS/libbreadlua_native.a       (static)
│   └── macOS/libbreadlua_native.dylib
├── Runtime/
│   ├── UnityModuleLoader.cs     (Resources/Addressables)
│   └── LuaAssetImporter.cs     (.lua file import)
└── package.json
```

## Performance Target

| Metric | Target |
|--------|--------|
| P/Invoke per frame | ≤ 5 |
| Data access (shared memory) | 0 overhead |
| C# function call from Lua | function pointer (no marshalling) |
| Pure Lua execution | native Lua 5.4 speed |
