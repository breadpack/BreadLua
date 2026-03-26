# BreadLua API Reference

## Namespace: `BreadPack.NativeLua`

### LuaState

Lua VM instance. Implements `IDisposable`.

```csharp
public class LuaState : IDisposable
```

#### Constructor

```csharp
var lua = new LuaState();
var lua = new LuaState(new LuaConfig { OpenStandardLibs = true });
```

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Handle` | `IntPtr` | Native Lua state pointer |
| `Tinker` | `LuaTinker` | Runtime function binding interface |

#### Methods

| Method | Description |
|--------|-------------|
| `DoString(string code)` | Execute Lua code string |
| `DoFile(string path)` | Execute Lua file |
| `Call(string funcName)` | Call Lua function (no return) |
| `Call<T>(string funcName, params object[] args)` | Call with arguments, return typed result |
| `Eval<T>(string expression)` | Evaluate Lua expression, return typed result |
| `SetGlobal(string name, long value)` | Set integer global |
| `SetGlobal(string name, double value)` | Set number global |
| `SetGlobal(string name, bool value)` | Set boolean global |
| `SetGlobal(string name, string value)` | Set string global |
| `SetGlobal(string name, IntPtr lightuserdata)` | Set lightuserdata global |
| `CreateMetatable(string name)` | Create Lua metatable |
| `PushObject(IntPtr gcHandle, string metatableName)` | Push C# object to Lua stack |
| `RegisterCFunction(string mtName, string fnName, IntPtr fn)` | Register C function on metatable |
| `Reload(string path)` | Reload a Lua file |
| `WatchAndReload(string directory, string filter)` | Watch directory for changes |
| `StopWatching()` | Stop file watching |
| `StartRepl()` | Start interactive REPL |
| `Dispose()` | Close Lua state and free resources |

#### Supported Types for Eval/Call

| C# Type | Lua Type |
|---------|----------|
| `int` | integer |
| `long` | integer |
| `float` | number |
| `double` | number |
| `bool` | boolean |
| `string` | string |

```csharp
int i = lua.Eval<int>("42");
long l = lua.Eval<long>("9999999999");
double d = lua.Eval<double>("3.14");
bool b = lua.Eval<bool>("true");
string s = lua.Eval<string>("'hello'");
```

---

### LuaTinker

Runtime C# function binding. Accessed via `lua.Tinker`.

```csharp
// Bind overloads
lua.Tinker.Bind("name", (Func<int, int, int>)((a, b) => a + b));
lua.Tinker.Bind("name", (Func<float, float, float>)((a, b) => a * b));
lua.Tinker.Bind("name", (Func<string, string>)(s => s.ToUpper()));
lua.Tinker.Bind("name", (Func<double>)(() => 3.14));
lua.Tinker.Bind("name", (Action<string>)(s => Console.WriteLine(s)));
lua.Tinker.Bind("name", (Action)(() => Console.WriteLine("called")));
```

Bound functions are immediately callable from Lua:

```csharp
lua.Tinker.Bind("greet", (Func<string, string>)(name => $"Hello {name}!"));
string result = lua.Eval<string>("greet('World')");  // "Hello World!"
```

Exceptions thrown in bound functions propagate as `LuaException`:

```csharp
lua.Tinker.Bind("fail", (Action)(() => throw new Exception("oops")));
// lua.DoString("fail()") throws LuaException with message containing "oops"
```

---

### Buffer\<T\>

Zero-copy shared memory buffer. `T` must be `unmanaged` (value type without references).

```csharp
public unsafe class Buffer<T> : IDisposable where T : unmanaged
```

#### Usage

```csharp
using var buffer = new Buffer<int>(capacity: 100);
buffer.Count = 3;
buffer[0] = 10;
buffer[1] = 20;
buffer[2] = 30;

Span<int> span = buffer.AsSpan();  // Length == Count

buffer.BindToLua(lua, "data");
// Creates globals: data (IntPtr), data_count (long)
```

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Capacity` | `int` | Maximum elements |
| `Count` | `int` | Current element count (clamped to 0..Capacity) |
| `Pointer` | `IntPtr` | Raw memory pointer |

#### With structs

```csharp
[StructLayout(LayoutKind.Sequential)]
public struct UnitData
{
    public int id;
    public float hp;
    public float attack;
}

using var units = new Buffer<UnitData>(1000);
units.Count = 1;
units[0] = new UnitData { id = 1, hp = 100, attack = 25 };
units.BindToLua(lua, "g_units");
```

---

### ObjectHandle

GCHandle wrapper for passing C# objects through native code.

```csharp
var obj = new MyClass();
IntPtr ptr = ObjectHandle.Alloc(obj);

MyClass retrieved = ObjectHandle.Get<MyClass>(ptr);  // Same instance

ObjectHandle.Free(ptr);  // Must free to avoid memory leak
```

- `Alloc(object)` — Pin object, return handle
- `Get<T>(IntPtr)` — Retrieve object (returns null for IntPtr.Zero or type mismatch)
- `Free(IntPtr)` — Release handle (safe to call with IntPtr.Zero)

---

### LuaException

```csharp
try
{
    lua.DoString("error('something went wrong')");
}
catch (LuaException ex)
{
    Console.WriteLine(ex.Message);        // Error message
    Console.WriteLine(ex.LuaStackTrace);  // Lua stack trace
    Console.WriteLine(ex.ScriptFile);     // Source file (if DoFile)
    Console.WriteLine(ex.Line);           // Line number
}
```

---

### LuaConfig

```csharp
var lua = new LuaState(new LuaConfig
{
    OpenStandardLibs = true,   // Load standard Lua libraries (default: true)
    ScriptBasePath = "scripts" // Base path for DoFile (optional)
});
```

---

### Hot Reload

```csharp
using var lua = new LuaState();

// Reload single file
lua.DoFile("scripts/game.lua");
lua.Reload("scripts/game.lua");

// Watch directory for changes
lua.WatchAndReload("scripts/", "*.lua");
// Files are automatically reloaded on save

lua.StopWatching();
```

---

### REPL

```csharp
using var lua = new LuaState();
lua.Tinker.Bind("add", (Func<int, int, int>)((a, b) => a + b));
lua.StartRepl();
// > print(add(3, 4))
// 7
// > exit
```
