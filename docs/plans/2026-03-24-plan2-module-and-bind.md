# BreadLua Plan 2: LuaModule + LuaExport + LuaBind Implementation

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task.

**Goal:** `[LuaModule]`/`[LuaExport]`로 C# 정적 함수를 Lua에 노출하고, `[LuaBind]`로 C# 클래스를 Lua에서 생성/사용 가능하게 한다. 함수 포인터(`UnmanagedCallersOnly`) 기반으로 P/Invoke 없이 호출한다.

**Architecture:** Source Generator가 `[LuaModule]` 클래스에서 UnmanagedCallersOnly 네이티브 진입점 + C 모듈 코드를 생성한다. `[LuaBind]` 클래스는 GCHandle 기반 userdata로 Lua에서 C# 객체를 생성/접근한다.

**Tech Stack:** .NET 9, C, Source Generator, UnmanagedCallersOnly, GCHandle, TUnit

**Spec:** `docs/specs/2026-03-24-breadlua-design.md`

---

## File Structure (신규/변경)

```
src/BreadLua.Runtime/
├── Attributes/
│   ├── LuaModuleAttribute.cs          # NEW
│   ├── LuaExportAttribute.cs          # NEW
│   ├── LuaBindAttribute.cs            # NEW
│   └── LuaConstructorAttribute.cs     # NEW
├── Core/
│   ├── LuaState.cs                    # MODIFY: RegisterModule 추가
│   └── ObjectHandle.cs                # NEW: GCHandle 래퍼

src/BreadLua.Generator/
├── Module/
│   ├── ModuleAnalyzer.cs              # NEW
│   ├── ModuleCSharpEmitter.cs         # NEW
│   └── ModuleCEmitter.cs              # NEW
├── Bind/
│   ├── BindAnalyzer.cs                # NEW
│   ├── BindCSharpEmitter.cs           # NEW
│   ├── BindCEmitter.cs                # NEW
│   └── BindLuaEmitter.cs             # NEW
├── BreadLuaGenerator.cs               # MODIFY: Module/Bind 등록

src/BreadLua.Native/src/
├── breadlua_core.c                    # MODIFY: 함수 포인터 등록 API 추가
├── breadlua_object.c                  # NEW: GCHandle userdata 지원

tests/BreadLua.Tests/
├── Module/
│   └── ModuleTests.cs                 # NEW
├── Bind/
│   └── BindTests.cs                   # NEW
```

---

## Task 1: 신규 Attributes 추가

**Files:**
- Create: `src/BreadLua.Runtime/Attributes/LuaModuleAttribute.cs`
- Create: `src/BreadLua.Runtime/Attributes/LuaExportAttribute.cs`
- Create: `src/BreadLua.Runtime/Attributes/LuaBindAttribute.cs`
- Create: `src/BreadLua.Runtime/Attributes/LuaConstructorAttribute.cs`

- [ ] **Step 1: LuaModuleAttribute.cs**
```csharp
using System;

namespace BreadPack.NativeLua;

[AttributeUsage(AttributeTargets.Class)]
public sealed class LuaModuleAttribute : Attribute
{
    public string Name { get; }
    public LuaModuleAttribute(string name) { Name = name; }
}
```

- [ ] **Step 2: LuaExportAttribute.cs**
```csharp
using System;

namespace BreadPack.NativeLua;

[AttributeUsage(AttributeTargets.Method)]
public sealed class LuaExportAttribute : Attribute
{
    public string? Name { get; }
    public LuaExportAttribute(string? name = null) { Name = name; }
}
```

- [ ] **Step 3: LuaBindAttribute.cs**
```csharp
using System;

namespace BreadPack.NativeLua;

[AttributeUsage(AttributeTargets.Class)]
public sealed class LuaBindAttribute : Attribute { }
```

- [ ] **Step 4: LuaConstructorAttribute.cs**
```csharp
using System;

namespace BreadPack.NativeLua;

[AttributeUsage(AttributeTargets.Constructor)]
public sealed class LuaConstructorAttribute : Attribute { }
```

- [ ] **Step 5: Build + commit**
```bash
dotnet build && git add -A && git commit -m "feat: add LuaModule, LuaExport, LuaBind, LuaConstructor attributes" && git push
```

---

## Task 2: 네이티브 함수 포인터 + GCHandle 지원

**Files:**
- Modify: `src/BreadLua.Native/src/breadlua_core.c`
- Create: `src/BreadLua.Native/src/breadlua_object.c`
- Create: `src/BreadLua.Runtime/Core/ObjectHandle.cs`
- Modify: `src/BreadLua.Runtime/Native/LuaNative.cs`
- Modify: `src/BreadLua.Runtime/Core/LuaState.cs`

- [ ] **Step 1: breadlua_core.c에 함수 포인터 등록 API 추가**

Append to `src/BreadLua.Native/src/breadlua_core.c`:
```c
/* Function pointer registration for [LuaModule] */
typedef void* (*bread_fn_ptr)(void);

#define BREAD_MAX_FN 256
static struct {
    const char* name;
    void* fn_ptr;
} g_fn_registry[BREAD_MAX_FN];
static int g_fn_count = 0;

BREADLUA_EXPORT void breadlua_register_fn(const char* name, void* fn_ptr) {
    if (g_fn_count < BREAD_MAX_FN) {
        g_fn_registry[g_fn_count].name = name;
        g_fn_registry[g_fn_count].fn_ptr = fn_ptr;
        g_fn_count++;
    }
}

BREADLUA_EXPORT void* breadlua_get_fn(const char* name) {
    for (int i = 0; i < g_fn_count; i++) {
        if (strcmp(g_fn_registry[i].name, name) == 0) {
            return g_fn_registry[i].fn_ptr;
        }
    }
    return NULL;
}
```

- [ ] **Step 2: breadlua_object.c (GCHandle userdata)**

Create `src/BreadLua.Native/src/breadlua_object.c`:
```c
#include "lua.h"
#include "lauxlib.h"
#include <string.h>

#ifdef _WIN32
#define BREADLUA_EXPORT __declspec(dllexport)
#else
#define BREADLUA_EXPORT __attribute__((visibility("default")))
#endif

/* GCHandle stored as lightuserdata in a full userdata wrapper */
typedef struct {
    void* gc_handle;  /* GCHandle IntPtr from C# */
} bread_object_t;

typedef void (*bread_release_fn)(void* gc_handle);
static bread_release_fn g_release_fn = NULL;

BREADLUA_EXPORT void breadlua_set_release_fn(bread_release_fn fn) {
    g_release_fn = fn;
}

static int bread_object_gc(lua_State* L) {
    bread_object_t* obj = (bread_object_t*)luaL_checkudata(L, 1, "bread_object");
    if (obj->gc_handle && g_release_fn) {
        g_release_fn(obj->gc_handle);
        obj->gc_handle = NULL;
    }
    return 0;
}

BREADLUA_EXPORT void breadlua_push_object(lua_State* L, void* gc_handle, const char* metatable_name) {
    bread_object_t* obj = (bread_object_t*)lua_newuserdata(L, sizeof(bread_object_t));
    obj->gc_handle = gc_handle;
    if (luaL_getmetatable(L, metatable_name) == 0) {
        lua_pop(L, 1);
        /* Create metatable if it doesn't exist */
        luaL_newmetatable(L, metatable_name);
        lua_pushcfunction(L, bread_object_gc);
        lua_setfield(L, -2, "__gc");
    }
    lua_setmetatable(L, -2);
}

BREADLUA_EXPORT void* breadlua_get_object(lua_State* L, int index) {
    bread_object_t* obj = (bread_object_t*)lua_touserdata(L, index);
    if (obj == NULL) return NULL;
    return obj->gc_handle;
}

BREADLUA_EXPORT void breadlua_create_metatable(lua_State* L, const char* name) {
    luaL_newmetatable(L, name);
    lua_pushvalue(L, -1);
    lua_setfield(L, -2, "__index");
    lua_pushcfunction(L, bread_object_gc);
    lua_setfield(L, -2, "__gc");
}

BREADLUA_EXPORT void breadlua_set_metatable_fn(lua_State* L, const char* mt_name, const char* fn_name, lua_CFunction fn) {
    luaL_getmetatable(L, mt_name);
    lua_pushcfunction(L, fn);
    lua_setfield(L, -2, fn_name);
    lua_pop(L, 1);
}
```

- [ ] **Step 3: LuaNative.cs에 새 P/Invoke 추가**

Append to `src/BreadLua.Runtime/Native/LuaNative.cs`:
```csharp
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern void breadlua_register_fn(string name, IntPtr fnPtr);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr breadlua_get_fn(string name);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern void breadlua_set_release_fn(IntPtr releaseFn);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern void breadlua_push_object(IntPtr L, IntPtr gcHandle, string metatableName);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr breadlua_get_object(IntPtr L, int index);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern void breadlua_create_metatable(IntPtr L, string name);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern void breadlua_set_metatable_fn(IntPtr L, string mtName, string fnName, IntPtr fn);
```

- [ ] **Step 4: ObjectHandle.cs**

Create `src/BreadLua.Runtime/Core/ObjectHandle.cs`:
```csharp
using System;
using System.Runtime.InteropServices;

namespace BreadPack.NativeLua;

public static class ObjectHandle
{
    public static IntPtr Alloc(object obj)
    {
        var handle = GCHandle.Alloc(obj);
        return GCHandle.ToIntPtr(handle);
    }

    public static T? Get<T>(IntPtr ptr) where T : class
    {
        if (ptr == IntPtr.Zero) return null;
        var handle = GCHandle.FromIntPtr(ptr);
        return handle.Target as T;
    }

    public static void Free(IntPtr ptr)
    {
        if (ptr == IntPtr.Zero) return;
        var handle = GCHandle.FromIntPtr(ptr);
        if (handle.IsAllocated) handle.Free();
    }
}
```

- [ ] **Step 5: LuaState.cs에 RegisterModule 추가**

Add method to `src/BreadLua.Runtime/Core/LuaState.cs`:
```csharp
    public void RegisterCFunction(string metatableName, string funcName, IntPtr cFunction)
    {
        ThrowIfDisposed();
        LuaNative.breadlua_set_metatable_fn(_L, metatableName, funcName, cFunction);
    }

    public void CreateMetatable(string name)
    {
        ThrowIfDisposed();
        LuaNative.breadlua_create_metatable(_L, name);
    }

    public void PushObject(IntPtr gcHandle, string metatableName)
    {
        ThrowIfDisposed();
        LuaNative.breadlua_push_object(_L, gcHandle, metatableName);
    }
```

- [ ] **Step 6: CMake rebuild + dotnet build + commit**
```bash
cd src/BreadLua.Native && cmake --build build --config Release && cd ../..
dotnet build
git add -A && git commit -m "feat: native function pointer registry + GCHandle object support" && git push
```

---

## Task 3: Source Generator — ModuleAnalyzer + ModuleEmitters

**Files:**
- Create: `src/BreadLua.Generator/Module/ModuleAnalyzer.cs`
- Create: `src/BreadLua.Generator/Module/ModuleCSharpEmitter.cs`
- Create: `src/BreadLua.Generator/Module/ModuleCEmitter.cs`
- Modify: `src/BreadLua.Generator/BreadLuaGenerator.cs`

ModuleAnalyzer가 [LuaModule] 클래스의 [LuaExport] 메서드를 분석한다.
ModuleCSharpEmitter가 UnmanagedCallersOnly 래퍼 + 등록 코드를 생성한다.
ModuleCEmitter가 Lua에서 호출 가능한 C 함수를 생성한다.

주요 규칙:
- [LuaExport]에 이름 미지정 시 메서드명을 snake_case로 변환
- 지원 파라미터 타입: int, long, float, double, bool, string (void 반환 포함)
- UnmanagedCallersOnly는 static 메서드 + blittable 타입만 지원. string은 IntPtr(byte*)로 전달.

- [ ] Write ModuleAnalyzer, ModuleCSharpEmitter, ModuleCEmitter
- [ ] Update BreadLuaGenerator to include Module pipeline
- [ ] Build + commit

---

## Task 4: Source Generator — BindAnalyzer + BindEmitters

**Files:**
- Create: `src/BreadLua.Generator/Bind/BindAnalyzer.cs`
- Create: `src/BreadLua.Generator/Bind/BindCSharpEmitter.cs`
- Create: `src/BreadLua.Generator/Bind/BindCEmitter.cs`
- Create: `src/BreadLua.Generator/Bind/BindLuaEmitter.cs`
- Modify: `src/BreadLua.Generator/BreadLuaGenerator.cs`

BindAnalyzer가 [LuaBind] 클래스의 프로퍼티, 메서드, [LuaConstructor]를 분석한다.
BindCSharpEmitter가 Factory, getter/setter 래퍼를 생성한다.
BindCEmitter가 메타테이블 + __index/__newindex + 생성자를 생성한다.

- [ ] Write BindAnalyzer, BindCSharpEmitter, BindCEmitter, BindLuaEmitter
- [ ] Update BreadLuaGenerator to include Bind pipeline
- [ ] Build + commit

---

## Task 5: Module/Bind 통합 테스트

**Files:**
- Create: `tests/BreadLua.Tests/Module/ModuleTests.cs`
- Create: `tests/BreadLua.Tests/Bind/BindTests.cs`

테스트 시나리오:
- [LuaModule] 함수가 Lua에서 호출 가능한지
- [LuaBind] 클래스를 Lua에서 생성 가능한지
- 프로퍼티 읽기/쓰기
- 메서드 호출
- GC 시 GCHandle 해제

- [ ] Write ModuleTests.cs
- [ ] Write BindTests.cs
- [ ] Run tests + commit

---

## Task 6: 벤치마크 추가 — 함수 호출 비교

**Files:**
- Modify: `benchmarks/BreadLua.Benchmarks/DataAccessBenchmark.cs`

추가 벤치마크:
- BreadLua 함수 포인터 호출 vs 전통 P/Invoke 함수 호출
- [LuaBind] 객체 생성 + 메서드 호출 성능

- [ ] Add function call benchmarks
- [ ] Run benchmarks + commit
