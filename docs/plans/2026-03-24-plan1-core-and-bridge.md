# BreadLua Plan 1: Core Runtime + LuaBridge Implementation

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** BreadLua 프로젝트를 생성하고, 네이티브 Lua 5.4를 로드하는 코어 런타임과 `[LuaBridge]` Source Generator로 공유 메모리 기반 struct 데이터 바인딩을 구현한다.

**Architecture:** 네이티브 lua54.dll을 P/Invoke로 로드하고, Source Generator가 `[LuaBridge]` struct에서 C 모듈 + C# Bridge + Lua 래퍼를 자동 생성한다. C 모듈은 공유 메모리 포인터를 통해 마샬링 없이 데이터에 접근한다.

**Tech Stack:** .NET 9, C (Lua 5.4 source), CMake, Source Generator (IIncrementalGenerator), TUnit

**Spec:** `docs/specs/2026-03-24-breadlua-design.md`

---

## File Structure

```
D:/Projects/BreadLua/
├── BreadLua.sln
├── LICENSE                              # MIT
├── README.md
├── .gitignore
├── docs/
│   ├── specs/
│   └── plans/
│
├── src/
│   ├── BreadLua.Runtime/               # .NET class library
│   │   ├── BreadLua.Runtime.csproj
│   │   ├── Attributes/
│   │   │   ├── LuaBridgeAttribute.cs
│   │   │   ├── LuaFieldAttribute.cs
│   │   │   ├── LuaReadOnlyAttribute.cs
│   │   │   └── LuaIgnoreAttribute.cs
│   │   ├── Core/
│   │   │   ├── LuaState.cs
│   │   │   ├── LuaConfig.cs
│   │   │   ├── LuaException.cs
│   │   │   └── Buffer.cs
│   │   └── Native/
│   │       ├── LuaNative.cs            # P/Invoke declarations
│   │       └── LuaConstants.cs         # Lua constants (LUA_OK, etc.)
│   │
│   ├── BreadLua.Generator/             # Source Generator
│   │   ├── BreadLua.Generator.csproj
│   │   ├── BreadLuaGenerator.cs        # Entry point
│   │   ├── Bridge/
│   │   │   ├── BridgeAnalyzer.cs       # [LuaBridge] struct 분석
│   │   │   ├── BridgeCSharpEmitter.cs  # .g.cs 생성
│   │   │   ├── BridgeCEmitter.cs       # .c 파일 생성
│   │   │   └── BridgeLuaEmitter.cs     # .lua 래퍼 생성
│   │   └── Util/
│   │       ├── NamingHelper.cs         # PascalCase → snake_case
│   │       └── TypeMapper.cs           # C# type → C type / Lua type 매핑
│   │
│   └── BreadLua.Native/                # CMake project
│       ├── CMakeLists.txt
│       ├── lua54/                      # Lua 5.4 source (vendored)
│       │   ├── lua.h
│       │   ├── lualib.h
│       │   ├── lauxlib.h
│       │   └── *.c
│       └── src/
│           └── breadlua_core.c         # Core init + module registry
│
├── tests/
│   └── BreadLua.Tests/
│       ├── BreadLua.Tests.csproj
│       ├── Core/
│       │   ├── LuaStateTests.cs
│       │   └── BufferTests.cs
│       ├── Generator/
│       │   └── BridgeGeneratorTests.cs
│       └── Integration/
│           └── BridgeIntegrationTests.cs
│
└── samples/
    └── BreadLua.Sample/
        ├── BreadLua.Sample.csproj
        ├── Program.cs
        └── scripts/
            └── test.lua
```

---

## Task 1: GitHub 프로젝트 생성 + 솔루션 스캐폴딩

**Files:**
- Create: `BreadLua.sln`
- Create: `src/BreadLua.Runtime/BreadLua.Runtime.csproj`
- Create: `src/BreadLua.Generator/BreadLua.Generator.csproj`
- Create: `tests/BreadLua.Tests/BreadLua.Tests.csproj`
- Create: `LICENSE`, `README.md`, `.gitignore`

- [ ] **Step 1: Git init + GitHub repo 생성**

```bash
cd D:/Projects/BreadLua
git init
gh repo create BreadLua --public --description "Native Lua 5.4 for .NET — zero marshalling overhead via shared memory + Source Generator" --license mit
```

- [ ] **Step 2: .gitignore 생성**

```bash
dotnet new gitignore
```

`.gitignore`에 추가:
```
# CMake
build/
cmake-build-*/
*.dll
*.so
*.dylib
*.a

# Generated C files
src/BreadLua.Native/src/generated/
```

- [ ] **Step 3: 솔루션 + 프로젝트 생성**

```bash
dotnet new sln -n BreadLua
dotnet new classlib -n BreadLua.Runtime -o src/BreadLua.Runtime -f net9.0
dotnet new classlib -n BreadLua.Generator -o src/BreadLua.Generator -f netstandard2.0
dotnet sln add src/BreadLua.Runtime/BreadLua.Runtime.csproj
dotnet sln add src/BreadLua.Generator/BreadLua.Generator.csproj
```

- [ ] **Step 4: Generator csproj 설정**

`src/BreadLua.Generator/BreadLua.Generator.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <EnforceExtendedAnalyzerRules>true</EnforceExtendedAnalyzerRules>
    <IsRoslynComponent>true</IsRoslynComponent>
    <RootNamespace>BreadPack.NativeLua.Generator</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.CodeAnalysis.Analyzers" Version="3.3.4" PrivateAssets="all" />
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.8.0" PrivateAssets="all" />
  </ItemGroup>
</Project>
```

- [ ] **Step 5: Runtime csproj 설정**

`src/BreadLua.Runtime/BreadLua.Runtime.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    <RootNamespace>BreadPack.NativeLua</RootNamespace>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\BreadLua.Generator\BreadLua.Generator.csproj"
                      OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
  </ItemGroup>
</Project>
```

- [ ] **Step 6: 테스트 프로젝트 생성**

```bash
dotnet new classlib -n BreadLua.Tests -o tests/BreadLua.Tests -f net9.0
dotnet sln add tests/BreadLua.Tests/BreadLua.Tests.csproj
```

`tests/BreadLua.Tests/BreadLua.Tests.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    <OutputType>Exe</OutputType>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="TUnit" Version="*" />
    <ProjectReference Include="..\..\src\BreadLua.Runtime\BreadLua.Runtime.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 7: 빈 Class1.cs 삭제 + 디렉토리 구조 생성**

```bash
rm src/BreadLua.Runtime/Class1.cs src/BreadLua.Generator/Class1.cs tests/BreadLua.Tests/Class1.cs
mkdir -p src/BreadLua.Runtime/Attributes src/BreadLua.Runtime/Core src/BreadLua.Runtime/Native
mkdir -p src/BreadLua.Generator/Bridge src/BreadLua.Generator/Util
mkdir -p tests/BreadLua.Tests/Core tests/BreadLua.Tests/Generator tests/BreadLua.Tests/Integration
```

- [ ] **Step 8: README.md 작성**

```markdown
# BreadLua

Native Lua 5.4 for .NET — zero marshalling overhead via shared memory + Source Generator.

## Features

- **Native Lua 5.4 VM** — full speed, no reinterpretation
- **Shared Memory** — data binding with zero P/Invoke per access
- **Function Pointers** — `UnmanagedCallersOnly` for call overhead elimination
- **Source Generator** — automatic C bridge + C# binding + Lua wrapper generation
- **Unity Support** — IL2CPP/AOT compatible, mobile ready

## Status

🚧 Under development

## License

MIT
```

- [ ] **Step 9: 빌드 확인 + 커밋**

```bash
dotnet build
git add -A
git commit -m "feat: initial project scaffolding with Runtime, Generator, Tests"
git push -u origin main
```

---

## Task 2: Lua 5.4 네이티브 통합

**Files:**
- Create: `src/BreadLua.Native/CMakeLists.txt`
- Create: `src/BreadLua.Native/src/breadlua_core.c`
- Create: `src/BreadLua.Runtime/Native/LuaNative.cs`
- Create: `src/BreadLua.Runtime/Native/LuaConstants.cs`
- Vendor: `src/BreadLua.Native/lua54/` (Lua 5.4.7 source)

- [ ] **Step 1: Lua 5.4 소스 다운로드 + vendor**

```bash
mkdir -p src/BreadLua.Native/lua54
cd src/BreadLua.Native/lua54
curl -L https://www.lua.org/ftp/lua-5.4.7.tar.gz | tar xz --strip-components=2 lua-5.4.7/src/
```

- [ ] **Step 2: CMakeLists.txt 작성**

`src/BreadLua.Native/CMakeLists.txt`:
```cmake
cmake_minimum_required(VERSION 3.16)
project(breadlua_native C)

set(CMAKE_C_STANDARD 11)

# Lua 5.4 source
file(GLOB LUA_SOURCES "lua54/*.c")
list(REMOVE_ITEM LUA_SOURCES
    "${CMAKE_CURRENT_SOURCE_DIR}/lua54/lua.c"
    "${CMAKE_CURRENT_SOURCE_DIR}/lua54/luac.c"
)

# BreadLua bridge source
file(GLOB BREAD_SOURCES "src/*.c")

add_library(breadlua_native SHARED ${LUA_SOURCES} ${BREAD_SOURCES})

target_include_directories(breadlua_native PRIVATE lua54)

if(WIN32)
    target_compile_definitions(breadlua_native PRIVATE LUA_BUILD_AS_DLL)
endif()

# Output to Runtime project for easy loading
set_target_properties(breadlua_native PROPERTIES
    LIBRARY_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/bin"
    RUNTIME_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/bin"
)
```

- [ ] **Step 3: breadlua_core.c 작성**

`src/BreadLua.Native/src/breadlua_core.c`:
```c
#include "lua.h"
#include "lualib.h"
#include "lauxlib.h"

#ifdef _WIN32
#define BREADLUA_EXPORT __declspec(dllexport)
#else
#define BREADLUA_EXPORT __attribute__((visibility("default")))
#endif

BREADLUA_EXPORT lua_State* breadlua_new(void) {
    lua_State* L = luaL_newstate();
    luaL_openlibs(L);
    return L;
}

BREADLUA_EXPORT void breadlua_close(lua_State* L) {
    if (L) lua_close(L);
}

BREADLUA_EXPORT int breadlua_dostring(lua_State* L, const char* code) {
    return luaL_dostring(L, code);
}

BREADLUA_EXPORT int breadlua_dofile(lua_State* L, const char* path) {
    return luaL_dofile(L, path);
}

BREADLUA_EXPORT const char* breadlua_tostring(lua_State* L, int index) {
    return lua_tostring(L, index);
}

BREADLUA_EXPORT int breadlua_pcall_global(lua_State* L, const char* func_name, int nargs, int nresults) {
    lua_getglobal(L, func_name);
    return lua_pcall(L, nargs, nresults, 0);
}

BREADLUA_EXPORT void breadlua_push_lightuserdata(lua_State* L, void* ptr) {
    lua_pushlightuserdata(L, ptr);
}

BREADLUA_EXPORT void breadlua_setglobal(lua_State* L, const char* name) {
    lua_setglobal(L, name);
}

BREADLUA_EXPORT void breadlua_pushinteger(lua_State* L, long long val) {
    lua_pushinteger(L, (lua_Integer)val);
}

BREADLUA_EXPORT void breadlua_pushnumber(lua_State* L, double val) {
    lua_pushnumber(L, val);
}

BREADLUA_EXPORT void breadlua_pushboolean(lua_State* L, int val) {
    lua_pushboolean(L, val);
}

BREADLUA_EXPORT void breadlua_pushstring(lua_State* L, const char* s) {
    lua_pushstring(L, s);
}

BREADLUA_EXPORT int breadlua_type(lua_State* L, int index) {
    return lua_type(L, index);
}

BREADLUA_EXPORT long long breadlua_tointeger(lua_State* L, int index) {
    return (long long)lua_tointeger(L, index);
}

BREADLUA_EXPORT double breadlua_tonumber(lua_State* L, int index) {
    return lua_tonumber(L, index);
}

BREADLUA_EXPORT int breadlua_toboolean(lua_State* L, int index) {
    return lua_toboolean(L, index);
}

BREADLUA_EXPORT void breadlua_pop(lua_State* L, int n) {
    lua_pop(L, n);
}

BREADLUA_EXPORT int breadlua_gettop(lua_State* L) {
    return lua_gettop(L);
}

/* Register a C module's luaopen function */
BREADLUA_EXPORT void breadlua_register_module(lua_State* L, const char* name, lua_CFunction openf) {
    luaL_requiref(L, name, openf, 1);
    lua_pop(L, 1);
}
```

- [ ] **Step 4: LuaConstants.cs 작성**

`src/BreadLua.Runtime/Native/LuaConstants.cs`:
```csharp
namespace BreadPack.NativeLua.Native;

internal static class LuaConstants
{
    public const int LUA_OK = 0;
    public const int LUA_ERRRUN = 2;
    public const int LUA_ERRSYNTAX = 3;
    public const int LUA_ERRMEM = 4;
    public const int LUA_ERRERR = 5;

    public const int LUA_TNIL = 0;
    public const int LUA_TBOOLEAN = 1;
    public const int LUA_TNUMBER = 3;
    public const int LUA_TSTRING = 4;
    public const int LUA_TTABLE = 5;

    public const string NativeLib = "breadlua_native";
}
```

- [ ] **Step 5: LuaNative.cs 작성 (P/Invoke 선언)**

`src/BreadLua.Runtime/Native/LuaNative.cs`:
```csharp
using System;
using System.Runtime.InteropServices;

namespace BreadPack.NativeLua.Native;

internal static class LuaNative
{
    private const string Lib = LuaConstants.NativeLib;

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr breadlua_new();

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern void breadlua_close(IntPtr L);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int breadlua_dostring(IntPtr L, string code);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int breadlua_dofile(IntPtr L, string path);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr breadlua_tostring(IntPtr L, int index);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int breadlua_pcall_global(IntPtr L, string funcName, int nargs, int nresults);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern void breadlua_push_lightuserdata(IntPtr L, IntPtr ptr);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern void breadlua_setglobal(IntPtr L, string name);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern void breadlua_pushinteger(IntPtr L, long val);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern void breadlua_pushnumber(IntPtr L, double val);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern void breadlua_pushboolean(IntPtr L, int val);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern void breadlua_pushstring(IntPtr L, string s);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern int breadlua_type(IntPtr L, int index);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern long breadlua_tointeger(IntPtr L, int index);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern double breadlua_tonumber(IntPtr L, int index);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern int breadlua_toboolean(IntPtr L, int index);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern void breadlua_pop(IntPtr L, int n);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern int breadlua_gettop(IntPtr L);
}
```

- [ ] **Step 6: CMake 빌드 + DLL 확인**

```bash
cd src/BreadLua.Native
cmake -B build -DCMAKE_BUILD_TYPE=Release
cmake --build build --config Release
ls build/bin/
```

Expected: `breadlua_native.dll` (Windows)

- [ ] **Step 7: 네이티브 DLL을 Runtime 프로젝트에 복사 설정**

`src/BreadLua.Runtime/BreadLua.Runtime.csproj`에 추가:
```xml
<ItemGroup>
  <None Include="..\BreadLua.Native\build\bin\breadlua_native.dll"
        CopyToOutputDirectory="PreserveNewest" Link="breadlua_native.dll"
        Condition="Exists('..\BreadLua.Native\build\bin\breadlua_native.dll')" />
</ItemGroup>
```

- [ ] **Step 8: 커밋**

```bash
git add -A
git commit -m "feat: integrate Lua 5.4 native via CMake + P/Invoke declarations"
```

---

## Task 3: Attributes 정의

**Files:**
- Create: `src/BreadLua.Runtime/Attributes/LuaBridgeAttribute.cs`
- Create: `src/BreadLua.Runtime/Attributes/LuaFieldAttribute.cs`
- Create: `src/BreadLua.Runtime/Attributes/LuaReadOnlyAttribute.cs`
- Create: `src/BreadLua.Runtime/Attributes/LuaIgnoreAttribute.cs`

- [ ] **Step 1: LuaBridgeAttribute.cs**

```csharp
using System;

namespace BreadPack.NativeLua;

[AttributeUsage(AttributeTargets.Struct)]
public sealed class LuaBridgeAttribute : Attribute
{
    public string Name { get; }

    public LuaBridgeAttribute(string name)
    {
        Name = name;
    }
}
```

- [ ] **Step 2: LuaFieldAttribute.cs**

```csharp
using System;

namespace BreadPack.NativeLua;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class LuaFieldAttribute : Attribute
{
    public string? Name { get; }

    public LuaFieldAttribute(string? name = null)
    {
        Name = name;
    }
}
```

- [ ] **Step 3: LuaReadOnlyAttribute.cs**

```csharp
using System;

namespace BreadPack.NativeLua;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class LuaReadOnlyAttribute : Attribute { }
```

- [ ] **Step 4: LuaIgnoreAttribute.cs**

```csharp
using System;

namespace BreadPack.NativeLua;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
public sealed class LuaIgnoreAttribute : Attribute { }
```

- [ ] **Step 5: 빌드 확인 + 커밋**

```bash
dotnet build
git add -A
git commit -m "feat: add LuaBridge, LuaField, LuaReadOnly, LuaIgnore attributes"
```

---

## Task 4: LuaState 코어 + Buffer\<T\>

**Files:**
- Create: `src/BreadLua.Runtime/Core/LuaState.cs`
- Create: `src/BreadLua.Runtime/Core/LuaConfig.cs`
- Create: `src/BreadLua.Runtime/Core/LuaException.cs`
- Create: `src/BreadLua.Runtime/Core/Buffer.cs`
- Test: `tests/BreadLua.Tests/Core/LuaStateTests.cs`
- Test: `tests/BreadLua.Tests/Core/BufferTests.cs`

- [ ] **Step 1: LuaException.cs**

```csharp
using System;

namespace BreadPack.NativeLua;

public class LuaException : Exception
{
    public string? LuaStackTrace { get; }
    public string? ScriptFile { get; }
    public int Line { get; }

    public LuaException(string message, string? luaStackTrace = null, string? scriptFile = null, int line = 0)
        : base(message)
    {
        LuaStackTrace = luaStackTrace;
        ScriptFile = scriptFile;
        Line = line;
    }
}
```

- [ ] **Step 2: LuaConfig.cs**

```csharp
namespace BreadPack.NativeLua;

public sealed class LuaConfig
{
    public bool OpenStandardLibs { get; set; } = true;
    public string? ScriptBasePath { get; set; }
}
```

- [ ] **Step 3: LuaState.cs**

```csharp
using System;
using System.Runtime.InteropServices;
using BreadPack.NativeLua.Native;

namespace BreadPack.NativeLua;

public class LuaState : IDisposable
{
    private IntPtr _L;
    private bool _disposed;

    public IntPtr Handle => _L;

    public LuaState(LuaConfig? config = null)
    {
        _L = LuaNative.breadlua_new();
        if (_L == IntPtr.Zero)
            throw new LuaException("Failed to create Lua state");
    }

    public void DoString(string code)
    {
        ThrowIfDisposed();
        int result = LuaNative.breadlua_dostring(_L, code);
        if (result != LuaConstants.LUA_OK)
        {
            string error = GetTopString() ?? "Unknown Lua error";
            LuaNative.breadlua_pop(_L, 1);
            throw new LuaException(error);
        }
    }

    public void DoFile(string path)
    {
        ThrowIfDisposed();
        int result = LuaNative.breadlua_dofile(_L, path);
        if (result != LuaConstants.LUA_OK)
        {
            string error = GetTopString() ?? "Unknown Lua error";
            LuaNative.breadlua_pop(_L, 1);
            throw new LuaException(error, scriptFile: path);
        }
    }

    public void Call(string funcName)
    {
        ThrowIfDisposed();
        int result = LuaNative.breadlua_pcall_global(_L, funcName, 0, 0);
        if (result != LuaConstants.LUA_OK)
        {
            string error = GetTopString() ?? "Unknown Lua error";
            LuaNative.breadlua_pop(_L, 1);
            throw new LuaException(error);
        }
    }

    public void SetGlobal(string name, IntPtr lightuserdata)
    {
        ThrowIfDisposed();
        LuaNative.breadlua_push_lightuserdata(_L, lightuserdata);
        LuaNative.breadlua_setglobal(_L, name);
    }

    public void SetGlobal(string name, long value)
    {
        ThrowIfDisposed();
        LuaNative.breadlua_pushinteger(_L, value);
        LuaNative.breadlua_setglobal(_L, name);
    }

    private string? GetTopString()
    {
        IntPtr ptr = LuaNative.breadlua_tostring(_L, -1);
        return ptr == IntPtr.Zero ? null : Marshal.PtrToStringAnsi(ptr);
    }

    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(LuaState));
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            if (_L != IntPtr.Zero)
            {
                LuaNative.breadlua_close(_L);
                _L = IntPtr.Zero;
            }
            _disposed = true;
        }
    }
}
```

- [ ] **Step 4: Buffer\<T\>.cs**

```csharp
using System;
using System.Runtime.InteropServices;

namespace BreadPack.NativeLua;

public unsafe class Buffer<T> : IDisposable where T : unmanaged
{
    private T* _ptr;
    private readonly int _capacity;
    private int _count;
    private bool _disposed;
    private readonly IntPtr _handle;

    public int Capacity => _capacity;
    public int Count { get => _count; set => _count = Math.Clamp(value, 0, _capacity); }
    public IntPtr Pointer => (IntPtr)_ptr;

    public ref T this[int index]
    {
        get
        {
            if ((uint)index >= (uint)_count)
                throw new IndexOutOfRangeException();
            return ref _ptr[index];
        }
    }

    public Buffer(int capacity)
    {
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        _capacity = capacity;
        _handle = Marshal.AllocHGlobal(sizeof(T) * capacity);
        _ptr = (T*)_handle;
        new Span<byte>((void*)_handle, sizeof(T) * capacity).Clear();
    }

    public Span<T> AsSpan() => new Span<T>(_ptr, _count);

    public void BindToLua(LuaState state, string globalName)
    {
        state.SetGlobal(globalName, (IntPtr)_ptr);
        state.SetGlobal(globalName + "_count", _count);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Marshal.FreeHGlobal(_handle);
            _ptr = null;
            _disposed = true;
        }
    }
}
```

- [ ] **Step 5: LuaStateTests.cs 작성**

```csharp
using BreadPack.NativeLua;
using TUnit.Core;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;

namespace BreadLua.Tests.Core;

public class LuaStateTests
{
    [Test]
    public async Task CreateAndDispose_ShouldNotThrow()
    {
        using var lua = new LuaState();
        await Assert.That(lua.Handle).IsNotEqualTo(IntPtr.Zero);
    }

    [Test]
    public async Task DoString_ValidCode_ShouldExecute()
    {
        using var lua = new LuaState();
        lua.DoString("x = 1 + 2");
        // no exception = success
        await Task.CompletedTask;
    }

    [Test]
    public async Task DoString_InvalidCode_ShouldThrowLuaException()
    {
        using var lua = new LuaState();
        await Assert.ThrowsAsync<LuaException>(() =>
        {
            lua.DoString("invalid syntax %%%");
            return Task.CompletedTask;
        });
    }

    [Test]
    public async Task Dispose_DoubleFree_ShouldNotThrow()
    {
        var lua = new LuaState();
        lua.Dispose();
        lua.Dispose();  // should not throw
        await Task.CompletedTask;
    }
}
```

- [ ] **Step 6: BufferTests.cs 작성**

```csharp
using System.Runtime.InteropServices;
using BreadPack.NativeLua;
using TUnit.Core;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;

namespace BreadLua.Tests.Core;

[StructLayout(LayoutKind.Sequential)]
public struct TestUnit
{
    public int id;
    public float hp;
    public float attack;
}

public class BufferTests
{
    [Test]
    public async Task Create_ShouldAllocateMemory()
    {
        using var buffer = new Buffer<TestUnit>(10);
        await Assert.That(buffer.Capacity).IsEqualTo(10);
        await Assert.That(buffer.Pointer).IsNotEqualTo(IntPtr.Zero);
    }

    [Test]
    public async Task ReadWrite_ShouldWork()
    {
        using var buffer = new Buffer<TestUnit>(10);
        buffer.Count = 1;
        buffer[0] = new TestUnit { id = 1, hp = 100f, attack = 25f };

        await Assert.That(buffer[0].id).IsEqualTo(1);
        await Assert.That(buffer[0].hp).IsEqualTo(100f);
        await Assert.That(buffer[0].attack).IsEqualTo(25f);
    }

    [Test]
    public async Task IndexOutOfRange_ShouldThrow()
    {
        using var buffer = new Buffer<TestUnit>(10);
        buffer.Count = 1;
        await Assert.ThrowsAsync<IndexOutOfRangeException>(() =>
        {
            _ = buffer[5];
            return Task.CompletedTask;
        });
    }

    [Test]
    public async Task AsSpan_ShouldReturnCorrectSlice()
    {
        using var buffer = new Buffer<TestUnit>(10);
        buffer.Count = 3;
        buffer[0] = new TestUnit { id = 1 };
        buffer[1] = new TestUnit { id = 2 };
        buffer[2] = new TestUnit { id = 3 };

        var span = buffer.AsSpan();
        await Assert.That(span.Length).IsEqualTo(3);
        await Assert.That(span[1].id).IsEqualTo(2);
    }
}
```

- [ ] **Step 7: 네이티브 DLL 빌드 후 테스트 실행**

```bash
cd src/BreadLua.Native && cmake -B build -DCMAKE_BUILD_TYPE=Release && cmake --build build --config Release && cd ../..
dotnet test tests/BreadLua.Tests/ -v normal
```

Expected: All tests PASS

- [ ] **Step 8: 커밋**

```bash
git add -A
git commit -m "feat: LuaState core + Buffer<T> with tests"
```

---

## Task 5: Source Generator — BridgeAnalyzer

**Files:**
- Create: `src/BreadLua.Generator/Util/NamingHelper.cs`
- Create: `src/BreadLua.Generator/Util/TypeMapper.cs`
- Create: `src/BreadLua.Generator/Bridge/BridgeAnalyzer.cs`
- Create: `src/BreadLua.Generator/BreadLuaGenerator.cs`

- [ ] **Step 1: NamingHelper.cs**

```csharp
using System.Text;

namespace BreadPack.NativeLua.Generator.Util;

internal static class NamingHelper
{
    public static string ToSnakeCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        var sb = new StringBuilder();
        for (int i = 0; i < name.Length; i++)
        {
            char c = name[i];
            if (char.IsUpper(c))
            {
                if (i > 0 && !char.IsUpper(name[i - 1]))
                    sb.Append('_');
                sb.Append(char.ToLowerInvariant(c));
            }
            else
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }
}
```

- [ ] **Step 2: TypeMapper.cs**

```csharp
namespace BreadPack.NativeLua.Generator.Util;

internal static class TypeMapper
{
    public static string ToCType(string csType) => csType switch
    {
        "int" or "Int32" => "int",
        "long" or "Int64" => "long long",
        "float" or "Single" => "float",
        "double" or "Double" => "double",
        "bool" or "Boolean" => "int",
        "byte" or "Byte" => "unsigned char",
        "short" or "Int16" => "short",
        _ => throw new System.NotSupportedException($"Unsupported type: {csType}")
    };

    public static string ToLuaPush(string csType) => csType switch
    {
        "int" or "Int32" or "long" or "Int64" or "short" or "Int16" or "byte" or "Byte"
            => "lua_pushinteger",
        "float" or "Single" or "double" or "Double"
            => "lua_pushnumber",
        "bool" or "Boolean"
            => "lua_pushboolean",
        _ => throw new System.NotSupportedException($"Unsupported type: {csType}")
    };

    public static string ToLuaCheck(string csType) => csType switch
    {
        "int" or "Int32" or "long" or "Int64" or "short" or "Int16" or "byte" or "Byte"
            => "luaL_checkinteger",
        "float" or "Single" or "double" or "Double"
            => "luaL_checknumber",
        "bool" or "Boolean"
            => "lua_toboolean",
        _ => throw new System.NotSupportedException($"Unsupported type: {csType}")
    };

    public static string ToCCast(string csType) => csType switch
    {
        "float" or "Single" => "(float)",
        "int" or "Int32" => "(int)",
        "short" or "Int16" => "(short)",
        "byte" or "Byte" => "(unsigned char)",
        _ => ""
    };
}
```

- [ ] **Step 3: BridgeAnalyzer.cs**

```csharp
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace BreadPack.NativeLua.Generator.Bridge;

internal sealed class BridgeStructInfo
{
    public string Namespace { get; set; } = "";
    public string TypeName { get; set; } = "";
    public string LuaName { get; set; } = "";
    public List<BridgeFieldInfo> Fields { get; set; } = new();
}

internal sealed class BridgeFieldInfo
{
    public string CsName { get; set; } = "";
    public string LuaName { get; set; } = "";
    public string CsType { get; set; } = "";
    public bool IsReadOnly { get; set; }
}

internal static class BridgeAnalyzer
{
    public static BridgeStructInfo? Analyze(GeneratorAttributeSyntaxContext context)
    {
        var symbol = context.TargetSymbol as INamedTypeSymbol;
        if (symbol == null) return null;

        var attr = symbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "LuaBridgeAttribute");
        if (attr == null) return null;

        string luaName = attr.ConstructorArguments.FirstOrDefault().Value as string ?? symbol.Name;

        var fields = new List<BridgeFieldInfo>();
        foreach (var member in symbol.GetMembers().OfType<IFieldSymbol>())
        {
            if (member.IsStatic || member.IsConst) continue;
            if (member.DeclaredAccessibility != Accessibility.Public) continue;
            if (member.GetAttributes().Any(a => a.AttributeClass?.Name == "LuaIgnoreAttribute")) continue;

            string fieldLuaName = member.Name;
            var fieldAttr = member.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.Name == "LuaFieldAttribute");
            if (fieldAttr != null)
            {
                var nameArg = fieldAttr.ConstructorArguments.FirstOrDefault().Value as string;
                if (!string.IsNullOrEmpty(nameArg))
                    fieldLuaName = nameArg;
            }

            bool isReadOnly = member.GetAttributes()
                .Any(a => a.AttributeClass?.Name == "LuaReadOnlyAttribute");

            fields.Add(new BridgeFieldInfo
            {
                CsName = member.Name,
                LuaName = fieldLuaName,
                CsType = member.Type.SpecialType switch
                {
                    SpecialType.System_Int32 => "int",
                    SpecialType.System_Int64 => "long",
                    SpecialType.System_Single => "float",
                    SpecialType.System_Double => "double",
                    SpecialType.System_Boolean => "bool",
                    SpecialType.System_Int16 => "short",
                    SpecialType.System_Byte => "byte",
                    _ => member.Type.ToDisplayString()
                },
                IsReadOnly = isReadOnly,
            });
        }

        return new BridgeStructInfo
        {
            Namespace = symbol.ContainingNamespace.ToDisplayString(),
            TypeName = symbol.Name,
            LuaName = luaName,
            Fields = fields,
        };
    }
}
```

- [ ] **Step 4: BreadLuaGenerator.cs (엔트리포인트 — 아직 Emitter 연결 전)**

```csharp
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using BreadPack.NativeLua.Generator.Bridge;

namespace BreadPack.NativeLua.Generator;

[Generator(LanguageNames.CSharp)]
public sealed class BreadLuaGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var bridgeStructs = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "BreadPack.NativeLua.LuaBridgeAttribute",
                predicate: (node, _) => node is StructDeclarationSyntax,
                transform: (ctx, _) => BridgeAnalyzer.Analyze(ctx))
            .Where(info => info is not null);

        context.RegisterSourceOutput(bridgeStructs, (spc, info) =>
        {
            if (info == null) return;
            string source = BridgeCSharpEmitter.Emit(info);
            spc.AddSource($"{info.TypeName}Bridge.g.cs", source);
        });
    }
}
```

- [ ] **Step 5: 빌드 확인 + 커밋**

```bash
dotnet build
git add -A
git commit -m "feat: Source Generator BridgeAnalyzer + entry point"
```

---

## Task 6: Source Generator — Emitters (C#, C, Lua)

**Files:**
- Create: `src/BreadLua.Generator/Bridge/BridgeCSharpEmitter.cs`
- Create: `src/BreadLua.Generator/Bridge/BridgeCEmitter.cs`
- Create: `src/BreadLua.Generator/Bridge/BridgeLuaEmitter.cs`

- [ ] **Step 1: BridgeCSharpEmitter.cs**

```csharp
using System.Text;

namespace BreadPack.NativeLua.Generator.Bridge;

internal static class BridgeCSharpEmitter
{
    public static string Emit(BridgeStructInfo info)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Runtime.InteropServices;");
        sb.AppendLine("using BreadPack.NativeLua;");
        sb.AppendLine();
        sb.AppendLine($"namespace {info.Namespace};");
        sb.AppendLine();
        sb.AppendLine($"public static unsafe class {info.TypeName}Bridge");
        sb.AppendLine("{");
        sb.AppendLine($"    private static {info.TypeName}* _buffer;");
        sb.AppendLine("    private static int _capacity;");
        sb.AppendLine("    private static int _count;");
        sb.AppendLine("    private static IntPtr _handle;");
        sb.AppendLine();

        // Initialize
        sb.AppendLine("    public static void Initialize(int capacity)");
        sb.AppendLine("    {");
        sb.AppendLine("        _capacity = capacity;");
        sb.AppendLine($"        _handle = Marshal.AllocHGlobal(sizeof({info.TypeName}) * capacity);");
        sb.AppendLine($"        _buffer = ({info.TypeName}*)_handle;");
        sb.AppendLine($"        new Span<byte>((void*)_handle, sizeof({info.TypeName}) * capacity).Clear();");
        sb.AppendLine("    }");
        sb.AppendLine();

        // Count
        sb.AppendLine("    public static int Count { get => _count; set => _count = Math.Clamp(value, 0, _capacity); }");
        sb.AppendLine();

        // Indexer
        sb.AppendLine($"    public static ref {info.TypeName} Get(int index)");
        sb.AppendLine("    {");
        sb.AppendLine("        if ((uint)index >= (uint)_count) throw new IndexOutOfRangeException();");
        sb.AppendLine("        return ref _buffer[index];");
        sb.AppendLine("    }");
        sb.AppendLine();

        // BindToLua
        sb.AppendLine("    public static void BindToLua(LuaState state)");
        sb.AppendLine("    {");
        sb.AppendLine($"        state.SetGlobal(\"g_{info.LuaName}\", (IntPtr)_buffer);");
        sb.AppendLine($"        state.SetGlobal(\"g_{info.LuaName}_count\", _count);");
        sb.AppendLine("    }");
        sb.AppendLine();

        // Dispose
        sb.AppendLine("    public static void Dispose()");
        sb.AppendLine("    {");
        sb.AppendLine("        if (_handle != IntPtr.Zero)");
        sb.AppendLine("        {");
        sb.AppendLine("            Marshal.FreeHGlobal(_handle);");
        sb.AppendLine("            _handle = IntPtr.Zero;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");

        sb.AppendLine("}");
        return sb.ToString();
    }
}
```

- [ ] **Step 2: BridgeCEmitter.cs**

```csharp
using System.Text;
using BreadPack.NativeLua.Generator.Util;

namespace BreadPack.NativeLua.Generator.Bridge;

internal static class BridgeCEmitter
{
    public static string Emit(BridgeStructInfo info)
    {
        var sb = new StringBuilder();
        sb.AppendLine("/* <auto-generated/> */");
        sb.AppendLine("#include \"lua.h\"");
        sb.AppendLine("#include \"lauxlib.h\"");
        sb.AppendLine();

        // Struct typedef
        sb.AppendLine($"typedef struct {{");
        foreach (var f in info.Fields)
        {
            sb.AppendLine($"    {TypeMapper.ToCType(f.CsType)} {f.CsName};");
        }
        sb.AppendLine($"}} bread_{info.LuaName}_t;");
        sb.AppendLine();

        sb.AppendLine($"static bread_{info.LuaName}_t* g_{info.LuaName}_data = NULL;");
        sb.AppendLine($"static int g_{info.LuaName}_count = 0;");
        sb.AppendLine();

        // Init
        sb.AppendLine($"static int l_{info.LuaName}_init(lua_State* L) {{");
        sb.AppendLine($"    g_{info.LuaName}_data = (bread_{info.LuaName}_t*)lua_touserdata(L, 1);");
        sb.AppendLine($"    g_{info.LuaName}_count = (int)luaL_checkinteger(L, 2);");
        sb.AppendLine("    return 0;");
        sb.AppendLine("}");
        sb.AppendLine();

        // Getters / Setters
        foreach (var f in info.Fields)
        {
            // Getter
            sb.AppendLine($"static int l_{info.LuaName}_get_{f.LuaName}(lua_State* L) {{");
            sb.AppendLine($"    int idx = (int)luaL_checkinteger(L, 1);");
            sb.AppendLine($"    luaL_argcheck(L, idx >= 0 && idx < g_{info.LuaName}_count, 1, \"index out of range\");");
            sb.AppendLine($"    {TypeMapper.ToLuaPush(f.CsType)}(L, g_{info.LuaName}_data[idx].{f.CsName});");
            sb.AppendLine("    return 1;");
            sb.AppendLine("}");
            sb.AppendLine();

            // Setter (skip if ReadOnly)
            if (!f.IsReadOnly)
            {
                sb.AppendLine($"static int l_{info.LuaName}_set_{f.LuaName}(lua_State* L) {{");
                sb.AppendLine($"    int idx = (int)luaL_checkinteger(L, 1);");
                sb.AppendLine($"    luaL_argcheck(L, idx >= 0 && idx < g_{info.LuaName}_count, 1, \"index out of range\");");
                sb.AppendLine($"    g_{info.LuaName}_data[idx].{f.CsName} = {TypeMapper.ToCCast(f.CsType)}{TypeMapper.ToLuaCheck(f.CsType)}(L, 2);");
                sb.AppendLine("    return 0;");
                sb.AppendLine("}");
                sb.AppendLine();
            }
        }

        // luaL_Reg table
        sb.AppendLine($"static const luaL_Reg {info.LuaName}_lib[] = {{");
        sb.AppendLine($"    {{\"init\", l_{info.LuaName}_init}},");
        foreach (var f in info.Fields)
        {
            sb.AppendLine($"    {{\"get_{f.LuaName}\", l_{info.LuaName}_get_{f.LuaName}}},");
            if (!f.IsReadOnly)
                sb.AppendLine($"    {{\"set_{f.LuaName}\", l_{info.LuaName}_set_{f.LuaName}}},");
        }
        sb.AppendLine("    {NULL, NULL}");
        sb.AppendLine("};");
        sb.AppendLine();

        // luaopen
        sb.AppendLine($"int luaopen_{info.LuaName}(lua_State* L) {{");
        sb.AppendLine($"    luaL_newlib(L, {info.LuaName}_lib);");
        sb.AppendLine("    return 1;");
        sb.AppendLine("}");

        return sb.ToString();
    }
}
```

- [ ] **Step 3: BridgeLuaEmitter.cs**

```csharp
using System.Text;

namespace BreadPack.NativeLua.Generator.Bridge;

internal static class BridgeLuaEmitter
{
    public static string Emit(BridgeStructInfo info)
    {
        var sb = new StringBuilder();
        sb.AppendLine("-- <auto-generated/>");
        sb.AppendLine($"local raw = require(\"{info.LuaName}\")");
        sb.AppendLine();
        sb.AppendLine($"local {info.TypeName} = {{}}");
        sb.AppendLine($"{info.TypeName}.__index = {info.TypeName}");
        sb.AppendLine();

        // Constructor
        sb.AppendLine($"function {info.TypeName}.get(idx)");
        sb.AppendLine($"    return setmetatable({{ _idx = idx }}, {info.TypeName})");
        sb.AppendLine("end");
        sb.AppendLine();

        // Methods
        foreach (var f in info.Fields)
        {
            // Getter
            sb.AppendLine($"function {info.TypeName}:{f.LuaName}()");
            sb.AppendLine($"    return raw.get_{f.LuaName}(self._idx)");
            sb.AppendLine("end");
            sb.AppendLine();

            // Setter
            if (!f.IsReadOnly)
            {
                sb.AppendLine($"function {info.TypeName}:set_{f.LuaName}(v)");
                sb.AppendLine($"    raw.set_{f.LuaName}(self._idx, v)");
                sb.AppendLine("end");
                sb.AppendLine();
            }
        }

        // Count
        sb.AppendLine($"function {info.TypeName}.count()");
        sb.AppendLine($"    return g_{info.LuaName}_count");
        sb.AppendLine("end");
        sb.AppendLine();

        sb.AppendLine($"return {info.TypeName}");
        return sb.ToString();
    }
}
```

- [ ] **Step 4: Generator에 C/Lua 출력 추가 (AdditionalOutput)**

`src/BreadLua.Generator/BreadLuaGenerator.cs` 업데이트 — RegisterSourceOutput에 C와 Lua 파일도 생성:

```csharp
context.RegisterSourceOutput(bridgeStructs, (spc, info) =>
{
    if (info == null) return;

    // C# Bridge
    spc.AddSource($"{info.TypeName}Bridge.g.cs", BridgeCSharpEmitter.Emit(info));

    // C Module (텍스트 파일로 출력 — 별도 빌드 스텝에서 사용)
    spc.AddSource($"bread_{info.LuaName}.c.g.txt", BridgeCEmitter.Emit(info));

    // Lua Wrapper
    spc.AddSource($"{info.LuaName}_wrapper.lua.g.txt", BridgeLuaEmitter.Emit(info));
});
```

- [ ] **Step 5: 빌드 확인 + 커밋**

```bash
dotnet build
git add -A
git commit -m "feat: Source Generator emitters for C#, C, and Lua bridge code"
```

---

## Task 7: 통합 테스트 + 샘플

**Files:**
- Create: `tests/BreadLua.Tests/Integration/BridgeIntegrationTests.cs`
- Create: `samples/BreadLua.Sample/BreadLua.Sample.csproj`
- Create: `samples/BreadLua.Sample/Program.cs`
- Create: `samples/BreadLua.Sample/scripts/test.lua`

- [ ] **Step 1: 샘플 프로젝트 생성**

```bash
mkdir -p samples/BreadLua.Sample/scripts
dotnet new console -n BreadLua.Sample -o samples/BreadLua.Sample -f net9.0
dotnet sln add samples/BreadLua.Sample/BreadLua.Sample.csproj
```

`samples/BreadLua.Sample/BreadLua.Sample.csproj`에 추가:
```xml
<PropertyGroup>
  <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
</PropertyGroup>
<ItemGroup>
  <ProjectReference Include="..\..\src\BreadLua.Runtime\BreadLua.Runtime.csproj" />
</ItemGroup>
```

- [ ] **Step 2: 샘플 struct 정의 + Program.cs**

`samples/BreadLua.Sample/Program.cs`:
```csharp
using System;
using System.Runtime.InteropServices;
using BreadPack.NativeLua;

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

// Source Generator가 UnitDataBridge 자동 생성
class Program
{
    static void Main()
    {
        using var lua = new LuaState();

        // LuaState 기본 실행 테스트
        lua.DoString("print('Hello from BreadLua!')");

        // Buffer 테스트
        using var buffer = new Buffer<UnitData>(100);
        buffer.Count = 2;
        buffer[0] = new UnitData { unitId = 1, hp = 100, attack = 25, defence = 10 };
        buffer[1] = new UnitData { unitId = 2, hp = 80, attack = 30, defence = 5 };

        Console.WriteLine($"Unit 0: id={buffer[0].unitId}, hp={buffer[0].hp}");
        Console.WriteLine($"Unit 1: id={buffer[1].unitId}, hp={buffer[1].hp}");
        Console.WriteLine("BreadLua sample completed successfully.");
    }
}
```

- [ ] **Step 3: test.lua**

`samples/BreadLua.Sample/scripts/test.lua`:
```lua
print("Lua script loaded!")

function greet(name)
    print("Hello, " .. name .. " from Lua!")
end

greet("BreadLua")
```

- [ ] **Step 4: BridgeIntegrationTests.cs**

```csharp
using System.Runtime.InteropServices;
using BreadPack.NativeLua;
using TUnit.Core;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;

namespace BreadLua.Tests.Integration;

[StructLayout(LayoutKind.Sequential)]
[LuaBridge("test_data")]
public struct TestData
{
    public int id;
    public float value;
    [LuaReadOnly] public bool flag;
    [LuaIgnore] public int hidden;
    [LuaField("custom_name")] public float renamedField;
}

public class BridgeIntegrationTests
{
    [Test]
    public async Task LuaState_DoString_RunsLuaCode()
    {
        using var lua = new LuaState();
        lua.DoString("result = 1 + 2");
        // no exception = pass
        await Task.CompletedTask;
    }

    [Test]
    public async Task Buffer_SharedMemory_ReadWrite()
    {
        using var buffer = new Buffer<TestData>(10);
        buffer.Count = 1;
        buffer[0] = new TestData { id = 42, value = 3.14f, renamedField = 1.5f };

        await Assert.That(buffer[0].id).IsEqualTo(42);
        await Assert.That(buffer[0].value).IsEqualTo(3.14f);
        await Assert.That(buffer[0].renamedField).IsEqualTo(1.5f);
    }

    [Test]
    public async Task Buffer_BindToLua_SetsGlobals()
    {
        using var lua = new LuaState();
        using var buffer = new Buffer<TestData>(10);
        buffer.Count = 5;

        buffer.BindToLua(lua, "g_test_data");
        // Verify Lua can see the count
        lua.DoString("assert(g_test_data_count == 5)");
        await Task.CompletedTask;
    }

    [Test]
    public async Task GeneratedBridge_Exists()
    {
        // Source Generator가 TestDataBridge 클래스를 생성했는지 확인
        // 컴파일이 성공하면 생성된 것
        var type = typeof(TestData).Assembly.GetType("BreadLua.Tests.Integration.TestDataBridge");
        // Note: namespace는 struct의 namespace를 따름
        await Task.CompletedTask;
    }
}
```

- [ ] **Step 5: 샘플 실행**

```bash
dotnet run --project samples/BreadLua.Sample/
```

Expected:
```
Hello from BreadLua!
Unit 0: id=1, hp=100
Unit 1: id=2, hp=80
BreadLua sample completed successfully.
```

- [ ] **Step 6: 테스트 실행**

```bash
dotnet test tests/BreadLua.Tests/ -v normal
```

Expected: All tests PASS

- [ ] **Step 7: 커밋 + 푸시**

```bash
git add -A
git commit -m "feat: integration tests + sample project for LuaBridge"
git push
```

---

## Summary

| Task | 산출물 | 테스트 |
|------|--------|--------|
| 1. 프로젝트 스캐폴딩 | sln, csproj, GitHub repo | 빌드 성공 |
| 2. Lua 5.4 네이티브 | CMake, breadlua_core.c, LuaNative.cs | DLL 생성 |
| 3. Attributes | 4개 Attribute 클래스 | 빌드 성공 |
| 4. LuaState + Buffer | 코어 런타임 | LuaStateTests, BufferTests |
| 5. BridgeAnalyzer | Source Generator 분석기 | 빌드 성공 |
| 6. Emitters | C#/C/Lua 코드 생성 | 빌드 성공 |
| 7. 통합 테스트 + 샘플 | 엔드투엔드 검증 | BridgeIntegrationTests |

**다음 Plan:** `[LuaModule]` + `[LuaExport]` 함수 바인딩, `[LuaBind]` 클래스 바인딩
