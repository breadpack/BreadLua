# Source Generator Guide

BreadLua Source Generator automatically creates C bridge code, C# bindings, and Lua wrappers at compile time.

## Installation

### NuGet
```bash
dotnet add package BreadPack.NativeLua
dotnet add package BreadPack.NativeLua.Generator
```

The Generator package is a development dependency — it runs at compile time and produces no runtime DLL.

## Attributes

### [LuaBridge] — Struct Data Binding

Maps a C# struct to Lua with zero-copy shared memory via `Buffer<T>`.

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
```

**Requirements:**
- `struct` with `[StructLayout(LayoutKind.Sequential)]`
- Fields must be `unmanaged` types (int, float, bool, etc.)

**Generates:**
- `UnitDataBridge.g.cs` — C# bridge with registration methods
- `bread_unit.c.g.txt` — C native binding code
- `unit_wrapper.lua.g.txt` — Lua accessor wrapper

**Usage with Buffer:**
```csharp
using var buffer = new Buffer<UnitData>(100);
buffer.Count = 1;
buffer[0] = new UnitData { unitId = 1, hp = 100, attack = 25, defence = 10 };
buffer.BindToLua(lua, "g_units");
```

```lua
-- Access from Lua (uses generated wrapper)
local unit = g_units[0]
print(unit.hp)       -- 100
unit.hp = 80         -- writable
print(unit.isAlive)  -- read-only
-- unit.isAlive = false  -- error: read-only
-- unit.internalFlag     -- not visible (LuaIgnore)
print(unit.def)      -- 10 (custom name via LuaField)
```

---

### [LuaModule] — Static Function Binding

Exposes static C# methods as a Lua module.

```csharp
[LuaModule("game")]
public static partial class GameAPI
{
    [LuaExport]
    public static void SpawnEffect(int effectId, float x, float y)
    {
        // Implementation
    }

    [LuaExport("time")]
    public static float GetTime() => Time.time;

    [LuaExport]
    public static string GetPlayerName() => "Hero";

    // Methods without [LuaExport] are not exposed to Lua
    public static void InternalMethod() { }
}
```

**Requirements:**
- `static partial class`
- Methods marked with `[LuaExport]`

**Naming:**
- `[LuaExport]` — auto snake_case (`SpawnEffect` → `spawn_effect`)
- `[LuaExport("name")]` — explicit name

**Generates:**
- `GameAPIModule.g.cs` — C# registration code
- `bread_game_module.c.g.txt` — C module code

**Lua usage:**
```lua
game.spawn_effect(1, 10.0, 20.0)
local t = game.time()
local name = game.get_player_name()
```

---

### [LuaBind] — Class Binding

Binds a C# class to Lua with constructor, methods, and properties via metatables.

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
        Name = name;
        Level = level;
        HP = 100;
    }

    public void Heal(float amount)
    {
        HP = Math.Min(HP + amount, 100);
    }

    public int GetDamage()
    {
        return Level * 10;
    }

    [LuaIgnore]
    public void InternalMethod() { }
}
```

**Requirements:**
- `partial class`
- Exactly one constructor marked with `[LuaConstructor]`

**Generates:**
- `PlayerBind.g.cs` — C# bridge with `Register()` method and `LuaMetatableName` constant
- `bread_Player_bind.c.g.txt` — C binding code
- `Player_wrapper.lua.g.txt` — Lua wrapper

**Registration:**
```csharp
Player.Register(lua);  // Generated static method
```

**Lua usage:**
```lua
local p = Player("Hero", 10)
print(p.Name)      -- "Hero"
print(p.Level)     -- 10
print(p.HP)        -- 100
p:Heal(50)
print(p.HP)        -- 100 (capped)
print(p:GetDamage())  -- 100
```

---

## Field Attributes

### [LuaField("name")]

Custom Lua name for a field or property.

```csharp
[LuaField("def")] public float defence;  // Lua sees "def"
[LuaField("hp")] public float HitPoints { get; set; }  // Lua sees "hp"
```

### [LuaReadOnly]

Field/property is readable but not writable from Lua.

```csharp
[LuaReadOnly] public int MaxLevel;  // Lua can read, cannot write
```

### [LuaIgnore]

Field/property/method is hidden from Lua.

```csharp
[LuaIgnore] public int InternalState;  // Not visible in Lua
[LuaIgnore] public void DebugMethod() { }  // Not callable from Lua
```

## Generated File Locations

Generated code is created at compile time and doesn't appear as physical files in the project. To inspect generated code:

**Visual Studio:** Solution Explorer > Dependencies > Analyzers > BreadPack.NativeLua.Generator

**Rider:** Navigate to generated sources via "Go to Source"

**CLI:**
```bash
dotnet build
# Generated .c and .lua files appear as embedded resources
```

## Best Practices

1. **Use `[LuaBridge]` for data** — structs with `Buffer<T>` give zero-copy performance
2. **Use `[LuaModule]` for utility functions** — static methods grouped by module
3. **Use `[LuaBind]` for game objects** — classes with state and methods
4. **Use `[LuaExport]` without explicit names** — auto snake_case follows Lua convention
5. **Keep bound types simple** — complex generics and inheritance may not work with IL2CPP
