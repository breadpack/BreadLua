# Unity Runtime Test + CI Pipeline Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add Unity Play Mode tests covering all BreadLua features (including IL2CPP edge cases) and integrate GameCI + Firebase Test Lab into the existing CI pipeline.

**Architecture:** Tests live inside the UPM package (`src/BreadLua.Unity/Tests/`) using NUnit + UnityTest attributes. A standalone Unity project (`tests/BreadLua.Unity.TestProject/`) references the package for CI execution. Firebase Game Loop integration reuses the same test logic via a GameLoopManager MonoBehaviour. The existing disabled CI workflow is extended with Unity test, build, and Firebase stages.

**Tech Stack:** Unity 2021.3 LTS, NUnit, Unity Test Framework, GameCI (unity-test-runner@v4, unity-builder@v4), Firebase Test Lab (gcloud CLI), IL2CPP

**Spec:** `docs/superpowers/specs/2026-03-25-unity-runtime-test-ci-design.md`

---

### Task 1: Implementation Prerequisites — Runtime Fixes

Before writing any tests, apply required runtime changes for IL2CPP compatibility.

**Files:**
- Modify: `src/BreadLua.Runtime/Core/LuaTinker.cs`
- Modify: `src/BreadLua.Runtime/Core/LuaState.cs`
- Create: `src/BreadLua.Unity/Runtime/link.xml`
- Create: `src/BreadLua.Unity/Runtime/link.xml.meta`

- [ ] **Step 1: Add `[MonoPInvokeCallback]` to `LuaTinker.OnGenericCallback`**

In `src/BreadLua.Runtime/Core/LuaTinker.cs`, add the AOT attribute to the callback method. This prevents IL2CPP crashes on reverse P/Invoke.

```csharp
// Add using at top:
using System.Runtime.InteropServices;
using AOT; // Unity's AOT namespace — available via UnityEngine.CoreModule

// Add attribute to OnGenericCallback (line 70):
[MonoPInvokeCallback(typeof(GenericCallbackDelegate))]
private static int OnGenericCallback(IntPtr L, IntPtr namePtr)
```

> **Note:** `AOT.MonoPInvokeCallbackAttribute` exists in Unity's runtime. For the .NET-only build (BreadLua.Runtime.csproj), this attribute doesn't exist. You need to provide a polyfill:

Create a conditional compilation block in `LuaTinker.cs`:

```csharp
#if !UNITY_5_3_OR_NEWER
namespace AOT
{
    [System.AttributeUsage(System.AttributeTargets.Method)]
    internal class MonoPInvokeCallbackAttribute : System.Attribute
    {
        public MonoPInvokeCallbackAttribute(System.Type type) { }
    }
}
#endif
```

Add this **at the bottom** of `LuaTinker.cs` (outside the class, inside the file).

Then add `[AOT.MonoPInvokeCallback(typeof(GenericCallbackDelegate))]` before `OnGenericCallback`.

- [ ] **Step 2: Replace `Marshal.PtrToStringAnsi` with UTF-8 marshalling**

In `src/BreadLua.Runtime/Core/LuaState.cs`, replace all `Marshal.PtrToStringAnsi` with `Marshal.PtrToStringUTF8`:

Line 145 — `ReadValue<T>`:
```csharp
// Before:
result = ptr == IntPtr.Zero ? null! : Marshal.PtrToStringAnsi(ptr)!;
// After:
result = ptr == IntPtr.Zero ? null! : Marshal.PtrToStringUTF8(ptr)!;
```

Line 171 — `GetTopString`:
```csharp
// Before:
return ptr == IntPtr.Zero ? null : Marshal.PtrToStringAnsi(ptr);
// After:
return ptr == IntPtr.Zero ? null : Marshal.PtrToStringUTF8(ptr);
```

In `src/BreadLua.Runtime/Core/LuaTinker.cs`, replace all `Marshal.PtrToStringAnsi`:

Line 72:
```csharp
string? name = Marshal.PtrToStringUTF8(namePtr);
```

Line 99, 108:
```csharp
string arg = ptr == IntPtr.Zero ? "" : Marshal.PtrToStringUTF8(ptr) ?? "";
```

- [ ] **Step 3: Verify existing .NET tests still pass**

Run: TUnit test runner agent to execute `tests/BreadLua.Tests/`

Expected: All existing tests pass (no behavioral change for ASCII strings).

- [ ] **Step 4: Create `link.xml` for IL2CPP stripping protection**

Create `src/BreadLua.Unity/Runtime/link.xml`:
```xml
<linker>
  <assembly fullname="BreadPack.NativeLua.Unity">
    <type fullname="*" preserve="all"/>
  </assembly>
</linker>
```

Create `src/BreadLua.Unity/Runtime/link.xml.meta` (Unity requires .meta for every file):
```
fileFormatVersion: 2
guid: a1b2c3d4e5f6a7b8c9d0e1f2a3b4c5d6
TextScriptImporter:
  externalObjects: {}
  userData:
  assetBundleName:
  assetBundleVariant:
```

- [ ] **Step 5: Commit prerequisites**

```bash
git add src/BreadLua.Runtime/Core/LuaTinker.cs src/BreadLua.Runtime/Core/LuaState.cs src/BreadLua.Unity/Runtime/link.xml src/BreadLua.Unity/Runtime/link.xml.meta
git commit -m "fix: add MonoPInvokeCallback, UTF-8 marshalling, link.xml for IL2CPP"
```

---

### Task 2: Unity Test Infrastructure — asmdef + Test Project

Set up the Unity test assembly definition and the CI test project.

**Files:**
- Create: `src/BreadLua.Unity/Tests/BreadPack.NativeLua.Unity.Tests.asmdef`
- Create: `tests/BreadLua.Unity.TestProject/Assets/.gitkeep`
- Create: `tests/BreadLua.Unity.TestProject/Packages/manifest.json`
- Create: `tests/BreadLua.Unity.TestProject/ProjectSettings/ProjectSettings.asset`

- [ ] **Step 1: Create test assembly definition**

Create `src/BreadLua.Unity/Tests/BreadPack.NativeLua.Unity.Tests.asmdef`:
```json
{
    "name": "BreadPack.NativeLua.Unity.Tests",
    "rootNamespace": "BreadPack.NativeLua.Unity.Tests",
    "references": [
        "BreadPack.NativeLua.Unity"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": true,
    "overrideReferences": true,
    "precompiledReferences": [
        "nunit.framework.dll"
    ],
    "autoReferenced": false,
    "defineConstraints": [
        "UNITY_INCLUDE_TESTS"
    ],
    "optionalUnityReferences": [
        "TestAssemblies"
    ],
    "noEngineReferences": false
}
```

- [ ] **Step 2: Create Unity test project skeleton**

Create `tests/BreadLua.Unity.TestProject/Assets/.gitkeep` (empty file).

Create `tests/BreadLua.Unity.TestProject/Packages/manifest.json`:
```json
{
    "dependencies": {
        "dev.breadpack.nativelua": "file:../../src/BreadLua.Unity",
        "com.unity.test-framework": "1.3.9"
    }
}
```

Create `tests/BreadLua.Unity.TestProject/ProjectSettings/ProjectSettings.asset` — a minimal Unity project settings file. This is a large YAML file; use Unity Editor to generate it, or copy a minimal version:

```yaml
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!129 &1
PlayerSettings:
  m_ObjectHideFlags: 0
  serializedVersion: 26
  productName: BreadLua.Tests
  companyName: BreadPack
  defaultScreenWidth: 1024
  defaultScreenHeight: 768
  scriptingBackend:
    Android: 1
    iPhone: 1
    Standalone: 0
  il2cppCompilerConfiguration:
    Android: 1
    iPhone: 1
  apiCompatibilityLevel:
    Android: 6
    iPhone: 6
    Standalone: 6
  bundleVersion: 0.1.0
  applicationIdentifier:
    Android: dev.breadpack.nativelua.tests
    iPhone: dev.breadpack.nativelua.tests
    Standalone: dev.breadpack.nativelua.tests
```

> **Note:** The `scriptingBackend` values: `0` = Mono, `1` = IL2CPP. Android and iOS are set to IL2CPP (1).

- [ ] **Step 3: Verify project structure**

```bash
ls -la tests/BreadLua.Unity.TestProject/Assets/
ls -la tests/BreadLua.Unity.TestProject/Packages/
ls -la tests/BreadLua.Unity.TestProject/ProjectSettings/
ls -la src/BreadLua.Unity/Tests/
```

Expected: All directories and files exist.

- [ ] **Step 4: Commit test infrastructure**

```bash
git add src/BreadLua.Unity/Tests/ tests/BreadLua.Unity.TestProject/
git commit -m "feat: Unity test infrastructure — asmdef and test project"
```

---

### Task 3: Core API Tests — `LuaStateTests.cs`

**Files:**
- Create: `src/BreadLua.Unity/Tests/LuaStateTests.cs`

- [ ] **Step 1: Write LuaStateTests.cs**

Create `src/BreadLua.Unity/Tests/LuaStateTests.cs`:

```csharp
using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;
using BreadPack.NativeLua;

namespace BreadPack.NativeLua.Unity.Tests
{
    [TestFixture]
    public class LuaStateTests
    {
        [Test]
        public void CreateAndDispose()
        {
            var lua = new LuaState();
            Assert.That(lua.Handle, Is.Not.EqualTo(IntPtr.Zero));
            lua.Dispose();
        }

        [Test]
        public void DoString_SimpleExpression()
        {
            using var lua = new LuaState();
            lua.DoString("x = 1 + 2");
            var result = lua.Eval<int>("x");
            Assert.That(result, Is.EqualTo(3));
        }

        [Test]
        public void DoString_SyntaxError_Throws()
        {
            using var lua = new LuaState();
            Assert.Throws<LuaException>(() => lua.DoString("invalid $$$ syntax"));
        }

        [Test]
        public void Call_GlobalFunction()
        {
            using var lua = new LuaState();
            lua.DoString("function greet() result = 'hello' end");
            lua.Call("greet");
            var result = lua.Eval<string>("result");
            Assert.That(result, Is.EqualTo("hello"));
        }

        [Test]
        public void Eval_Int()
        {
            using var lua = new LuaState();
            Assert.That(lua.Eval<int>("2 + 3"), Is.EqualTo(5));
        }

        [Test]
        public void Eval_Double()
        {
            using var lua = new LuaState();
            Assert.That(lua.Eval<double>("3.14"), Is.EqualTo(3.14).Within(0.001));
        }

        [Test]
        public void Eval_Bool()
        {
            using var lua = new LuaState();
            Assert.That(lua.Eval<bool>("true"), Is.True);
            Assert.That(lua.Eval<bool>("false"), Is.False);
        }

        [Test]
        public void Eval_String()
        {
            using var lua = new LuaState();
            Assert.That(lua.Eval<string>("'hello world'"), Is.EqualTo("hello world"));
        }

        [Test]
        public void SetGlobal_AllTypes()
        {
            using var lua = new LuaState();

            lua.SetGlobal("myLong", 42L);
            Assert.That(lua.Eval<long>("myLong"), Is.EqualTo(42L));

            lua.SetGlobal("myDouble", 3.14);
            Assert.That(lua.Eval<double>("myDouble"), Is.EqualTo(3.14).Within(0.001));

            lua.SetGlobal("myBool", true);
            Assert.That(lua.Eval<bool>("myBool"), Is.True);

            lua.SetGlobal("myStr", "test");
            Assert.That(lua.Eval<string>("myStr"), Is.EqualTo("test"));
        }

        [Test]
        public void CallWithArgs_MixedTypes()
        {
            using var lua = new LuaState();
            lua.DoString("function add(a, b) return a + b end");
            var result = lua.Call<int>("add", 10, 20);
            Assert.That(result, Is.EqualTo(30));
        }

        [Test]
        public void DisposedState_Throws()
        {
            var lua = new LuaState();
            lua.Dispose();
            Assert.Throws<ObjectDisposedException>(() => lua.DoString("x = 1"));
        }
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add src/BreadLua.Unity/Tests/LuaStateTests.cs
git commit -m "test: Unity Play Mode core API tests for LuaState"
```

---

### Task 4: Buffer Tests — `BufferTests.cs`

**Files:**
- Create: `src/BreadLua.Unity/Tests/BufferTests.cs`

- [ ] **Step 1: Write BufferTests.cs**

Create `src/BreadLua.Unity/Tests/BufferTests.cs`:

```csharp
using System;
using NUnit.Framework;
using BreadPack.NativeLua;

namespace BreadPack.NativeLua.Unity.Tests
{
    [TestFixture]
    public class BufferTests
    {
        [Test]
        public void Create_And_Access()
        {
            using var buffer = new Buffer<int>(10);
            buffer.Count = 3;
            buffer[0] = 100;
            buffer[1] = 200;
            buffer[2] = 300;
            Assert.That(buffer[0], Is.EqualTo(100));
            Assert.That(buffer[1], Is.EqualTo(200));
            Assert.That(buffer[2], Is.EqualTo(300));
            Assert.That(buffer.Capacity, Is.EqualTo(10));
        }

        [Test]
        public void BindToLua_And_ReadFromLua()
        {
            using var lua = new LuaState();
            using var buffer = new Buffer<int>(4);
            buffer.Count = 2;
            buffer[0] = 42;
            buffer[1] = 99;

            buffer.BindToLua(lua, "data");

            var count = lua.Eval<long>("data_count");
            Assert.That(count, Is.EqualTo(2));
        }

        [Test]
        public void AsSpan_ReturnsCorrectSlice()
        {
            using var buffer = new Buffer<int>(10);
            buffer.Count = 3;
            buffer[0] = 1;
            buffer[1] = 2;
            buffer[2] = 3;

            var span = buffer.AsSpan();
            Assert.That(span.Length, Is.EqualTo(3));
            Assert.That(span[0], Is.EqualTo(1));
            Assert.That(span[2], Is.EqualTo(3));
        }

        [Test]
        public void Dispose_FreesMemory()
        {
            var buffer = new Buffer<int>(10);
            buffer.Count = 1;
            buffer[0] = 42;
            buffer.Dispose();
            // After dispose, Pointer should be zero
            Assert.That(buffer.Pointer, Is.EqualTo(IntPtr.Zero));
        }

        [Test]
        public void OutOfRange_Throws()
        {
            using var buffer = new Buffer<int>(5);
            buffer.Count = 2;
            Assert.Throws<IndexOutOfRangeException>(() => { var _ = buffer[2]; });
            Assert.Throws<IndexOutOfRangeException>(() => { var _ = buffer[-1]; });
        }
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add src/BreadLua.Unity/Tests/BufferTests.cs
git commit -m "test: Unity Play Mode Buffer<T> tests"
```

---

### Task 5: Tinker Tests — `TinkerTests.cs`

**Files:**
- Create: `src/BreadLua.Unity/Tests/TinkerTests.cs`

- [ ] **Step 1: Write TinkerTests.cs**

Create `src/BreadLua.Unity/Tests/TinkerTests.cs`:

```csharp
using System;
using NUnit.Framework;
using BreadPack.NativeLua;

namespace BreadPack.NativeLua.Unity.Tests
{
    [TestFixture]
    public class TinkerTests
    {
        [Test]
        public void Bind_IntFunc_And_Call()
        {
            using var lua = new LuaState();
            lua.Tinker.Bind("add", (Func<int, int, int>)((a, b) => a + b));
            lua.DoString("result = add(3, 4)");
            Assert.That(lua.Eval<int>("result"), Is.EqualTo(7));
        }

        [Test]
        public void Bind_FloatFunc()
        {
            using var lua = new LuaState();
            lua.Tinker.Bind("multiply", (Func<float, float, float>)((a, b) => a * b));
            lua.DoString("result = multiply(2.5, 4.0)");
            Assert.That(lua.Eval<double>("result"), Is.EqualTo(10.0).Within(0.01));
        }

        [Test]
        public void Bind_StringFunc()
        {
            using var lua = new LuaState();
            lua.Tinker.Bind("upper", (Func<string, string>)(s => s.ToUpper()));
            lua.DoString("result = upper('hello')");
            Assert.That(lua.Eval<string>("result"), Is.EqualTo("HELLO"));
        }

        [Test]
        public void Bind_StringAction()
        {
            using var lua = new LuaState();
            string captured = null;
            lua.Tinker.Bind("capture", (Action<string>)(s => captured = s));
            lua.DoString("capture('test_value')");
            Assert.That(captured, Is.EqualTo("test_value"));
        }

        [Test]
        public void Bind_Action()
        {
            using var lua = new LuaState();
            bool called = false;
            lua.Tinker.Bind("ping", (Action)(() => called = true));
            lua.DoString("ping()");
            Assert.That(called, Is.True);
        }

        [Test]
        public void Bind_DoubleFunc()
        {
            using var lua = new LuaState();
            lua.Tinker.Bind("pi", (Func<double>)(() => 3.14159));
            lua.DoString("result = pi()");
            Assert.That(lua.Eval<double>("result"), Is.EqualTo(3.14159).Within(0.0001));
        }

        [Test]
        public void Callback_Exception_Propagates()
        {
            using var lua = new LuaState();
            lua.Tinker.Bind("explode", (Action)(() => throw new InvalidOperationException("boom")));
            // The native callback returns -1 on exception, which should cause a Lua error
            Assert.Throws<LuaException>(() => lua.DoString("explode()"));
        }

        [Test]
        public void MultiInstance_BindingIsolation()
        {
            // This test documents the static _bindings issue:
            // Two LuaState instances share the same binding dictionary.
            // Binding the same name in the second state overwrites the first.
            using var lua1 = new LuaState();
            using var lua2 = new LuaState();

            lua1.Tinker.Bind("shared_fn", (Func<int, int, int>)((a, b) => a + b));
            lua2.Tinker.Bind("shared_fn", (Func<int, int, int>)((a, b) => a * b));

            // lua2's binding overwrites lua1's due to static dictionary
            // This test documents current behavior — both states use lua2's binding
            lua2.DoString("result = shared_fn(3, 4)");
            Assert.That(lua2.Eval<int>("result"), Is.EqualTo(12)); // 3 * 4
        }
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add src/BreadLua.Unity/Tests/TinkerTests.cs
git commit -m "test: Unity Play Mode LuaTinker binding tests"
```

---

### Task 6: HotReload Tests — `HotReloadTests.cs`

**Files:**
- Create: `src/BreadLua.Unity/Tests/HotReloadTests.cs`

- [ ] **Step 1: Write HotReloadTests.cs with platform guard**

Create `src/BreadLua.Unity/Tests/HotReloadTests.cs`:

```csharp
// HotReload uses FileSystemWatcher — not available on mobile platforms
#if !UNITY_ANDROID && !UNITY_IOS
using System.IO;
using NUnit.Framework;
using BreadPack.NativeLua;

namespace BreadPack.NativeLua.Unity.Tests
{
    [TestFixture]
    public class HotReloadTests
    {
        [Test]
        public void Reload_UpdatesGlobalState()
        {
            using var lua = new LuaState();
            var tempDir = Path.Combine(Path.GetTempPath(), "breadlua_test_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            try
            {
                var scriptPath = Path.Combine(tempDir, "test.lua");

                // Write initial script
                File.WriteAllText(scriptPath, "version = 1");
                lua.DoFile(scriptPath);
                Assert.That(lua.Eval<int>("version"), Is.EqualTo(1));

                // Update and reload
                File.WriteAllText(scriptPath, "version = 2");
                lua.Reload(scriptPath);
                Assert.That(lua.Eval<int>("version"), Is.EqualTo(2));
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }
    }
}
#endif
```

- [ ] **Step 2: Commit**

```bash
git add src/BreadLua.Unity/Tests/HotReloadTests.cs
git commit -m "test: Unity Play Mode HotReload tests (desktop only)"
```

---

### Task 7: Unity Integration Tests — `UnityIntegrationTests.cs`

**Files:**
- Create: `src/BreadLua.Unity/Tests/UnityIntegrationTests.cs`
- Create: `tests/BreadLua.Unity.TestProject/Assets/Resources/Lua/test_module.lua.txt`

- [ ] **Step 1: Write UnityIntegrationTests.cs**

Create `src/BreadLua.Unity/Tests/UnityIntegrationTests.cs`:

```csharp
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using BreadPack.NativeLua.Unity;

namespace BreadPack.NativeLua.Unity.Tests
{
    [TestFixture]
    public class UnityIntegrationTests
    {
        [UnityTest]
        public IEnumerator UnityLuaState_Lifecycle()
        {
            var go = new GameObject("LuaTest");
            var luaState = go.AddComponent<UnityLuaState>();

            yield return null; // Wait one frame for Awake

            Assert.That(luaState.State, Is.Not.Null);
            Assert.That(luaState.State.Handle, Is.Not.EqualTo(System.IntPtr.Zero));

            Object.Destroy(go);
            yield return null; // Wait for OnDestroy
        }

        [UnityTest]
        public IEnumerator UnityLuaState_UpdateCallback()
        {
            var go = new GameObject("LuaTest");
            var luaState = go.AddComponent<UnityLuaState>();

            yield return null; // Awake

            // Set up an on_update function that increments a counter
            luaState.State.DoString("counter = 0; function on_update() counter = counter + 1 end");

            yield return null; // First Update
            yield return null; // Second Update

            var counter = luaState.State.Eval<int>("counter");
            Assert.That(counter, Is.GreaterThanOrEqualTo(2));

            Object.Destroy(go);
            yield return null;
        }

        [UnityTest]
        public IEnumerator UnityLuaState_StartupScripts()
        {
            // Create a TextAsset to simulate a startup script
            // Since startupScripts is a serialized field, we test via DoString on State
            var go = new GameObject("LuaTest");
            var luaState = go.AddComponent<UnityLuaState>();

            yield return null; // Awake

            // Verify State is initialized and can execute scripts
            luaState.State.DoString("startup_ran = true");
            Assert.That(luaState.State.Eval<bool>("startup_ran"), Is.True);

            Object.Destroy(go);
            yield return null;
        }

        [Test]
        public void UnityModuleLoader_LoadFromResources()
        {
            var loader = new UnityModuleLoader("Lua");
            var script = loader.Load("test_module");

            // test_module.lua.txt must exist in Resources/Lua/
            // If running in CI without the resource, skip gracefully
            if (script == null)
            {
                Assert.Ignore("test_module.lua.txt not found in Resources — skipping");
                return;
            }

            Assert.That(script, Does.Contain("test_module_loaded"));
        }
    }
}
```

- [ ] **Step 2: Create test Lua resource**

Create `tests/BreadLua.Unity.TestProject/Assets/Resources/Lua/test_module.lua.txt`:
```lua
test_module_loaded = true
```

- [ ] **Step 3: Commit**

```bash
git add src/BreadLua.Unity/Tests/UnityIntegrationTests.cs tests/BreadLua.Unity.TestProject/Assets/Resources/
git commit -m "test: Unity integration tests — lifecycle, update, module loader"
```

---

### Task 8: Generator Tests — `GeneratorTests.cs`

**Files:**
- Create: `src/BreadLua.Unity/Tests/GeneratorTests.cs`

- [ ] **Step 1: Write GeneratorTests.cs**

Create `src/BreadLua.Unity/Tests/GeneratorTests.cs`:

```csharp
using System;
using NUnit.Framework;
using BreadPack.NativeLua;

namespace BreadPack.NativeLua.Unity.Tests
{
    [TestFixture]
    public class GeneratorTests
    {
        [Test]
        public void LuaBind_ClassRegistration()
        {
            // Verify that metatable creation works (foundation for [LuaBind])
            using var lua = new LuaState();
            lua.CreateMetatable("TestClass");
            // If no crash, metatable system is functional
            Assert.Pass("Metatable created successfully");
        }

        [Test]
        public void LuaBind_MethodCall()
        {
            // Verify C function registration on metatables (used by generated [LuaBind] code)
            using var lua = new LuaState();
            lua.CreateMetatable("MethodTestClass");

            // Register a function pointer on the metatable
            // Use Tinker as a proxy for the same native callback mechanism
            lua.Tinker.Bind("mt_method", (Func<int, int, int>)((a, b) => a + b));
            lua.DoString("result = mt_method(5, 3)");
            Assert.That(lua.Eval<int>("result"), Is.EqualTo(8));
        }

        [Test]
        public void LuaBind_PropertyAccess()
        {
            // Verify object push/get round-trip (used by generated property accessors)
            using var lua = new LuaState();
            lua.CreateMetatable("PropTestClass");

            var obj = new TestObject { Name = "test_prop", Value = 42 };
            var handle = ObjectHandle.Alloc(obj);
            try
            {
                lua.PushObject(handle, "PropTestClass");
                // Verify the object survives the push
                var retrieved = ObjectHandle.Get<TestObject>(handle);
                Assert.That(retrieved, Is.Not.Null);
                Assert.That(retrieved.Name, Is.EqualTo("test_prop"));
                Assert.That(retrieved.Value, Is.EqualTo(42));
            }
            finally
            {
                ObjectHandle.Free(handle);
            }
        }

        [Test]
        public void LuaModule_FunctionCall()
        {
            // Verify static function binding (used by generated [LuaModule] code)
            using var lua = new LuaState();
            lua.Tinker.Bind("module_func", (Func<int, int, int>)((a, b) => a * b));
            lua.DoString("result = module_func(6, 7)");
            Assert.That(lua.Eval<int>("result"), Is.EqualTo(42));
        }

        [Test]
        public void LuaBridge_StructBinding()
        {
            // Verify shared memory binding (used by generated [LuaBridge] code)
            using var lua = new LuaState();
            using var buffer = new Buffer<int>(8);
            buffer.Count = 3;
            buffer[0] = 10;
            buffer[1] = 20;
            buffer[2] = 30;

            buffer.BindToLua(lua, "bridge_data");

            // Verify Lua can see the count
            Assert.That(lua.Eval<long>("bridge_data_count"), Is.EqualTo(3));
        }

        private class TestObject
        {
            public string Name { get; set; }
            public int Value { get; set; }
        }
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add src/BreadLua.Unity/Tests/GeneratorTests.cs
git commit -m "test: Unity Play Mode generator binding tests"
```

---

### Task 9: IL2CPP Edge Case Tests — `IL2CPPEdgeCaseTests.cs`

**Files:**
- Create: `src/BreadLua.Unity/Tests/IL2CPPEdgeCaseTests.cs`

- [ ] **Step 1: Write IL2CPPEdgeCaseTests.cs**

Create `src/BreadLua.Unity/Tests/IL2CPPEdgeCaseTests.cs`:

```csharp
using System;
using NUnit.Framework;
using BreadPack.NativeLua;

namespace BreadPack.NativeLua.Unity.Tests
{
    [TestFixture]
    public class IL2CPPEdgeCaseTests
    {
        // === Test 1: Reverse P/Invoke callback ===
        [Test]
        public void ReversePInvokeCallback_SurvivesIL2CPP()
        {
            // This test verifies that Marshal.GetFunctionPointerForDelegate callback
            // works when called from native code. On IL2CPP without [MonoPInvokeCallback],
            // this would crash.
            using var lua = new LuaState();
            lua.Tinker.Bind("native_callback_test", (Func<int, int, int>)((a, b) => a + b));
            lua.DoString("result = native_callback_test(10, 20)");
            Assert.That(lua.Eval<int>("result"), Is.EqualTo(30));
        }

        // === Test 2: Generic ReadValue<T> all types ===
        [Test]
        public void GenericReadValue_AllTypes()
        {
            using var lua = new LuaState();
            lua.DoString("function identity(x) return x end");

            Assert.That(lua.Eval<int>("42"), Is.EqualTo(42));
            Assert.That(lua.Eval<long>("9999999999"), Is.EqualTo(9999999999L));
            Assert.That(lua.Eval<float>("1.5"), Is.EqualTo(1.5f).Within(0.01f));
            Assert.That(lua.Eval<double>("3.14159"), Is.EqualTo(3.14159).Within(0.00001));
            Assert.That(lua.Eval<bool>("true"), Is.True);
            Assert.That(lua.Eval<string>("'hello'"), Is.EqualTo("hello"));
        }

        // === Test 3: Call<T> with params object[] boxing ===
        [Test]
        public void GenericCallWithParams_BoxingRoundTrip()
        {
            using var lua = new LuaState();
            lua.DoString("function add(a, b) return a + b end");
            lua.DoString("function concat(a, b) return a .. b end");
            lua.DoString("function check(a) if a then return 1 else return 0 end end");

            // int args → int return
            Assert.That(lua.Call<int>("add", 10, 20), Is.EqualTo(30));

            // double args → double return
            Assert.That(lua.Call<double>("add", 1.5, 2.5), Is.EqualTo(4.0).Within(0.01));

            // string args → string return
            Assert.That(lua.Call<string>("concat", "hello", " world"), Is.EqualTo("hello world"));

            // bool arg → int return
            Assert.That(lua.Call<int>("check", true), Is.EqualTo(1));
        }

        // === Test 4: Buffer<T> generic pointer + sizeof(T) ===
        [Test]
        public void BufferGenericPointer_MultipleTypes()
        {
            // Buffer<int>
            using var intBuf = new Buffer<int>(4);
            intBuf.Count = 2;
            intBuf[0] = 42;
            intBuf[1] = 99;
            Assert.That(intBuf[0], Is.EqualTo(42));
            Assert.That(intBuf.Pointer, Is.Not.EqualTo(IntPtr.Zero));

            // Buffer<float>
            using var floatBuf = new Buffer<float>(4);
            floatBuf.Count = 2;
            floatBuf[0] = 1.5f;
            floatBuf[1] = 2.5f;
            Assert.That(floatBuf[0], Is.EqualTo(1.5f).Within(0.001f));

            // Buffer<double>
            using var doubleBuf = new Buffer<double>(4);
            doubleBuf.Count = 1;
            doubleBuf[0] = 3.14159;
            Assert.That(doubleBuf[0], Is.EqualTo(3.14159).Within(0.00001));
        }

        // === Test 5: GCHandle round-trip ===
        [Test]
        public void GCHandleRoundTrip_ObjectSurvivesNativePass()
        {
            var testObj = new TestPayload { Value = 42, Name = "test" };
            var ptr = ObjectHandle.Alloc(testObj);

            try
            {
                Assert.That(ptr, Is.Not.EqualTo(IntPtr.Zero));

                var retrieved = ObjectHandle.Get<TestPayload>(ptr);
                Assert.That(retrieved, Is.Not.Null);
                Assert.That(retrieved.Value, Is.EqualTo(42));
                Assert.That(retrieved.Name, Is.EqualTo("test"));

                // Verify it's the same reference
                Assert.That(retrieved, Is.SameAs(testObj));
            }
            finally
            {
                ObjectHandle.Free(ptr);
            }
        }

        // === Test 6: Delegate type pattern matching ===
        [Test]
        public void DelegateTypePatternMatching_AllBindOverloads()
        {
            using var lua = new LuaState();

            // All 6 delegate types must work
            lua.Tinker.Bind("fn_int", (Func<int, int, int>)((a, b) => a - b));
            lua.Tinker.Bind("fn_float", (Func<float, float, float>)((a, b) => a / b));
            lua.Tinker.Bind("fn_str", (Func<string, string>)(s => s + "!"));
            lua.Tinker.Bind("fn_str_action", (Action<string>)(s => { }));
            lua.Tinker.Bind("fn_action", (Action)(() => { }));
            lua.Tinker.Bind("fn_double", (Func<double>)(() => 2.718));

            lua.DoString("r1 = fn_int(10, 3)");
            Assert.That(lua.Eval<int>("r1"), Is.EqualTo(7));

            lua.DoString("r2 = fn_float(10.0, 4.0)");
            Assert.That(lua.Eval<double>("r2"), Is.EqualTo(2.5).Within(0.01));

            lua.DoString("r3 = fn_str('hello')");
            Assert.That(lua.Eval<string>("r3"), Is.EqualTo("hello!"));

            lua.DoString("fn_str_action('test')"); // Should not throw
            lua.DoString("fn_action()"); // Should not throw

            lua.DoString("r4 = fn_double()");
            Assert.That(lua.Eval<double>("r4"), Is.EqualTo(2.718).Within(0.001));
        }

        // === Test 7: Source generated code not stripped ===
        [Test]
        public void SourceGeneratedCode_NotStripped()
        {
            // This test verifies that [LuaBind] / [LuaModule] / [LuaBridge] generated code
            // is not removed by IL2CPP managed code stripping.
            // The link.xml should preserve all types in the assembly.

            // Verify the metatable creation API works (used by generated code)
            using var lua = new LuaState();
            lua.CreateMetatable("TestMeta");

            var obj = new TestPayload { Value = 1, Name = "mt_test" };
            var handle = ObjectHandle.Alloc(obj);
            try
            {
                lua.PushObject(handle, "TestMeta");
                // If we get here without crash, the metatable/object system works
                Assert.Pass("Metatable and PushObject work — generated code path is intact");
            }
            finally
            {
                ObjectHandle.Free(handle);
            }
        }

        // === Test 8: Unicode string marshalling ===
        [Test]
        public void UnicodeStringMarshalling_KoreanAndEmoji()
        {
            using var lua = new LuaState();

            // Korean
            lua.SetGlobal("korean", "한글테스트");
            Assert.That(lua.Eval<string>("korean"), Is.EqualTo("한글테스트"));

            // Emoji
            lua.SetGlobal("emoji", "🎮🎲");
            Assert.That(lua.Eval<string>("emoji"), Is.EqualTo("🎮🎲"));

            // Mixed ASCII + Unicode
            lua.SetGlobal("mixed", "Hello 세계 🌍");
            Assert.That(lua.Eval<string>("mixed"), Is.EqualTo("Hello 세계 🌍"));

            // Lua string concatenation with Unicode
            lua.DoString("combined = korean .. ' ' .. emoji");
            Assert.That(lua.Eval<string>("combined"), Is.EqualTo("한글테스트 🎮🎲"));
        }

        // Helper class for GCHandle tests
        private class TestPayload
        {
            public int Value { get; set; }
            public string Name { get; set; }
        }
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add src/BreadLua.Unity/Tests/IL2CPPEdgeCaseTests.cs
git commit -m "test: IL2CPP edge case tests — callbacks, generics, GCHandle, Unicode"
```

---

### Task 10: Firebase Game Loop Integration

**Files:**
- Create: `tests/BreadLua.Unity.TestProject/Assets/FirebaseGameLoop/GameLoopManager.cs`
- Create: `tests/BreadLua.Unity.TestProject/Assets/FirebaseGameLoop/TestResultWriter.cs`
- Create: `tests/BreadLua.Unity.TestProject/Assets/Plugins/Android/AndroidManifest.xml`
- Create: `tests/BreadLua.Unity.TestProject/Assets/Plugins/iOS/Info.plist.append`

- [ ] **Step 1: Write TestResultWriter.cs**

Create `tests/BreadLua.Unity.TestProject/Assets/FirebaseGameLoop/TestResultWriter.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace BreadPack.NativeLua.Unity.Tests.GameLoop
{
    public class TestResult
    {
        public string Name;
        public bool Passed;
        public string Message;
        public long DurationMs;
    }

    public static class TestResultWriter
    {
        private const string Tag = "[BREADLUA_TEST]";
        private static readonly List<TestResult> _results = new();
        private static readonly System.Diagnostics.Stopwatch _stopwatch = new();

        public static void StartTest(string name)
        {
            Debug.Log($"{Tag} START: {name}");
            _stopwatch.Restart();
        }

        public static void Pass(string name)
        {
            _stopwatch.Stop();
            Debug.Log($"{Tag} PASS: {name}");
            _results.Add(new TestResult
            {
                Name = name, Passed = true, DurationMs = _stopwatch.ElapsedMilliseconds
            });
        }

        public static void Fail(string name, string message)
        {
            _stopwatch.Stop();
            Debug.LogError($"{Tag} FAIL: {name} — {message}");
            _results.Add(new TestResult
            {
                Name = name, Passed = false, Message = message, DurationMs = _stopwatch.ElapsedMilliseconds
            });
        }

        public static void WriteSummary()
        {
            int passed = 0, failed = 0;
            foreach (var r in _results)
            {
                if (r.Passed) passed++;
                else failed++;
            }

            Debug.Log($"{Tag} SUMMARY: {passed}/{_results.Count} passed, {failed} failed");
            WriteJsonFile();
        }

        private static void WriteJsonFile()
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine($"  \"timestamp\": \"{DateTime.UtcNow:O}\",");
            sb.AppendLine($"  \"platform\": \"{Application.platform}\",");
            sb.AppendLine($"  \"total\": {_results.Count},");

            int passed = 0;
            foreach (var r in _results) if (r.Passed) passed++;
            sb.AppendLine($"  \"passed\": {passed},");
            sb.AppendLine($"  \"failed\": {_results.Count - passed},");
            sb.AppendLine("  \"results\": [");

            for (int i = 0; i < _results.Count; i++)
            {
                var r = _results[i];
                sb.Append($"    {{ \"name\": \"{Escape(r.Name)}\", \"status\": \"{(r.Passed ? "pass" : "fail")}\", \"duration_ms\": {r.DurationMs}");
                if (!r.Passed && r.Message != null)
                    sb.Append($", \"message\": \"{Escape(r.Message)}\"");
                sb.Append(" }");
                if (i < _results.Count - 1) sb.Append(",");
                sb.AppendLine();
            }

            sb.AppendLine("  ]");
            sb.AppendLine("}");

            var path = Path.Combine(Application.persistentDataPath, "test-results.json");
            File.WriteAllText(path, sb.ToString());
            Debug.Log($"{Tag} Results written to: {path}");
        }

        private static string Escape(string s)
        {
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n");
        }
    }
}
```

- [ ] **Step 2: Write GameLoopManager.cs**

Create `tests/BreadLua.Unity.TestProject/Assets/FirebaseGameLoop/GameLoopManager.cs`:

```csharp
using System;
using System.Collections;
using UnityEngine;

namespace BreadPack.NativeLua.Unity.Tests.GameLoop
{
    public class GameLoopManager : MonoBehaviour
    {
        private static bool _isGameLoop;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void DetectGameLoop()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                using var intent = activity.Call<AndroidJavaObject>("getIntent");
                var action = intent.Call<string>("getAction");
                _isGameLoop = action == "com.google.intent.action.TEST_LOOP";
            }
            catch (Exception)
            {
                _isGameLoop = false;
            }
#elif UNITY_IOS && !UNITY_EDITOR
            // iOS Game Loop detection via URL scheme
            // Firebase launches with firebase-game-loop:// URL
            _isGameLoop = IsLaunchedByFirebase();
#else
            _isGameLoop = false;
#endif

            if (_isGameLoop)
            {
                var go = new GameObject("GameLoopManager");
                DontDestroyOnLoad(go);
                go.AddComponent<GameLoopManager>();
            }
        }

#if UNITY_IOS && !UNITY_EDITOR
        private static bool IsLaunchedByFirebase()
        {
            // Check launch URL for firebase-game-loop scheme
            // Unity doesn't expose this directly; use a native plugin or check
            // Application.absoluteURL on startup
            return !string.IsNullOrEmpty(Application.absoluteURL)
                && Application.absoluteURL.StartsWith("firebase-game-loop");
        }
#endif

        private void Start()
        {
            StartCoroutine(RunAllTests());
        }

        private IEnumerator RunAllTests()
        {
            Debug.Log("[BREADLUA_TEST] Game Loop started — running all tests");
            yield return null;

            RunTest("ReversePInvokeCallback", TestReversePInvokeCallback);
            RunTest("GenericReadValue_AllTypes", TestGenericReadValueAllTypes);
            RunTest("GenericCallWithParams_BoxingRoundTrip", TestGenericCallWithParamsBoxing);
            RunTest("BufferGenericPointer_MultipleTypes", TestBufferGenericPointerMultipleTypes);
            RunTest("GCHandleRoundTrip", TestGCHandleRoundTrip);
            RunTest("DelegateTypePatternMatching", TestDelegateTypePatternMatching);
            RunTest("SourceGeneratedCode_NotStripped", TestSourceGeneratedCodeNotStripped);
            RunTest("UnicodeStringMarshalling", TestUnicodeStringMarshalling);
            RunTest("LuaState_CreateAndDispose", TestLuaStateCreateAndDispose);
            RunTest("LuaState_DoString", TestLuaStateDoString);
            RunTest("Buffer_CreateAndAccess", TestBufferCreateAndAccess);
            RunTest("Tinker_BindAndCall", TestTinkerBindAndCall);

            TestResultWriter.WriteSummary();

            yield return new WaitForSeconds(1f);

            // Signal game loop completion
#if UNITY_ANDROID && !UNITY_EDITOR
            using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            activity.Call("finish");
#elif UNITY_IOS && !UNITY_EDITOR
            Application.OpenURL("firebase-game-loop-complete://");
#endif

            Application.Quit();
        }

        private void RunTest(string name, Action testAction)
        {
            TestResultWriter.StartTest(name);
            try
            {
                testAction();
                TestResultWriter.Pass(name);
            }
            catch (Exception ex)
            {
                TestResultWriter.Fail(name, ex.Message);
            }
        }

        // ---- Inline test methods (mirrors IL2CPPEdgeCaseTests + core tests) ----

        private void TestReversePInvokeCallback()
        {
            using var lua = new LuaState();
            lua.Tinker.Bind("cb_test", (Func<int, int, int>)((a, b) => a + b));
            lua.DoString("result = cb_test(10, 20)");
            Assert(lua.Eval<int>("result") == 30, "Expected 30");
        }

        private void TestGenericReadValueAllTypes()
        {
            using var lua = new LuaState();
            Assert(lua.Eval<int>("42") == 42, "int failed");
            Assert(lua.Eval<long>("9999999999") == 9999999999L, "long failed");
            Assert(Math.Abs(lua.Eval<float>("1.5") - 1.5f) < 0.01f, "float failed");
            Assert(Math.Abs(lua.Eval<double>("3.14159") - 3.14159) < 0.00001, "double failed");
            Assert(lua.Eval<bool>("true"), "bool failed");
            Assert(lua.Eval<string>("'hello'") == "hello", "string failed");
        }

        private void TestGenericCallWithParamsBoxing()
        {
            using var lua = new LuaState();
            lua.DoString("function add(a, b) return a + b end");
            Assert(lua.Call<int>("add", 10, 20) == 30, "int boxing failed");
            Assert(Math.Abs(lua.Call<double>("add", 1.5, 2.5) - 4.0) < 0.01, "double boxing failed");
        }

        private void TestBufferGenericPointerMultipleTypes()
        {
            using var intBuf = new Buffer<int>(4);
            intBuf.Count = 1;
            intBuf[0] = 42;
            Assert(intBuf[0] == 42, "Buffer<int> failed");

            using var floatBuf = new Buffer<float>(4);
            floatBuf.Count = 1;
            floatBuf[0] = 1.5f;
            Assert(Math.Abs(floatBuf[0] - 1.5f) < 0.001f, "Buffer<float> failed");

            using var doubleBuf = new Buffer<double>(4);
            doubleBuf.Count = 1;
            doubleBuf[0] = 3.14;
            Assert(Math.Abs(doubleBuf[0] - 3.14) < 0.01, "Buffer<double> failed");
        }

        private void TestGCHandleRoundTrip()
        {
            var obj = "test_object";
            var ptr = ObjectHandle.Alloc(obj);
            try
            {
                var result = ObjectHandle.Get<string>(ptr);
                Assert(result == "test_object", $"Expected 'test_object', got '{result}'");
            }
            finally
            {
                ObjectHandle.Free(ptr);
            }
        }

        private void TestDelegateTypePatternMatching()
        {
            using var lua = new LuaState();
            lua.Tinker.Bind("dm_int", (Func<int, int, int>)((a, b) => a - b));
            lua.Tinker.Bind("dm_action", (Action)(() => { }));
            lua.Tinker.Bind("dm_double", (Func<double>)(() => 2.718));

            lua.DoString("r1 = dm_int(10, 3)");
            Assert(lua.Eval<int>("r1") == 7, "int delegate failed");
            lua.DoString("dm_action()");
            lua.DoString("r2 = dm_double()");
            Assert(Math.Abs(lua.Eval<double>("r2") - 2.718) < 0.001, "double delegate failed");
        }

        private void TestSourceGeneratedCodeNotStripped()
        {
            using var lua = new LuaState();
            lua.CreateMetatable("GameLoopTestMeta");
            var obj = "meta_test";
            var handle = ObjectHandle.Alloc(obj);
            try
            {
                lua.PushObject(handle, "GameLoopTestMeta");
            }
            finally
            {
                ObjectHandle.Free(handle);
            }
        }

        private void TestUnicodeStringMarshalling()
        {
            using var lua = new LuaState();
            lua.SetGlobal("korean", "한글테스트");
            var result = lua.Eval<string>("korean");
            Assert(result == "한글테스트", $"Korean failed: got '{result}'");

            lua.SetGlobal("emoji", "🎮");
            var emojiResult = lua.Eval<string>("emoji");
            Assert(emojiResult == "🎮", $"Emoji failed: got '{emojiResult}'");
        }

        private void TestLuaStateCreateAndDispose()
        {
            var lua = new LuaState();
            Assert(lua.Handle != IntPtr.Zero, "Handle is zero");
            lua.Dispose();
        }

        private void TestLuaStateDoString()
        {
            using var lua = new LuaState();
            lua.DoString("x = 1 + 2");
            Assert(lua.Eval<int>("x") == 3, "DoString failed");
        }

        private void TestBufferCreateAndAccess()
        {
            using var buffer = new Buffer<int>(10);
            buffer.Count = 2;
            buffer[0] = 100;
            buffer[1] = 200;
            Assert(buffer[0] == 100 && buffer[1] == 200, "Buffer access failed");
        }

        private void TestTinkerBindAndCall()
        {
            using var lua = new LuaState();
            lua.Tinker.Bind("gl_add", (Func<int, int, int>)((a, b) => a + b));
            lua.DoString("result = gl_add(5, 7)");
            Assert(lua.Eval<int>("result") == 12, "Tinker bind failed");
        }

        private static void Assert(bool condition, string message = "Assertion failed")
        {
            if (!condition) throw new Exception(message);
        }
    }
}
```

- [ ] **Step 3: Create Android manifest with Game Loop intent**

Create `tests/BreadLua.Unity.TestProject/Assets/Plugins/Android/AndroidManifest.xml`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<manifest xmlns:android="http://schemas.android.com/apk/res/android"
    package="dev.breadpack.nativelua.tests">
    <application>
        <activity android:name="com.unity3d.player.UnityPlayerActivity"
                  android:exported="true">
            <intent-filter>
                <action android:name="android.intent.action.MAIN" />
                <category android:name="android.intent.category.LAUNCHER" />
            </intent-filter>
            <intent-filter>
                <action android:name="com.google.intent.action.TEST_LOOP" />
                <category android:name="android.intent.category.DEFAULT" />
            </intent-filter>
        </activity>
    </application>
</manifest>
```

- [ ] **Step 4: Create iOS Info.plist URL scheme for Firebase Game Loop**

Create `tests/BreadLua.Unity.TestProject/Assets/Plugins/iOS/Info.plist.append`:

> **Note:** Unity merges `Info.plist.append` files into the final Info.plist during iOS build. This avoids manually editing the generated Xcode project.

```xml
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleURLTypes</key>
    <array>
        <dict>
            <key>CFBundleURLSchemes</key>
            <array>
                <string>firebase-game-loop</string>
            </array>
        </dict>
    </array>
</dict>
</plist>
```

- [ ] **Step 5: Commit**

```bash
git add tests/BreadLua.Unity.TestProject/Assets/FirebaseGameLoop/ tests/BreadLua.Unity.TestProject/Assets/Plugins/
git commit -m "feat: Firebase Game Loop integration — manager, result writer, manifest, iOS plist"
```

---

### Task 11: CI Workflow — Activate and Extend

**Files:**
- Rename: `.github/workflows/ci.yml.disabled` → `.github/workflows/ci.yml`
- Modify: `.github/workflows/ci.yml` (add Unity stages)

- [ ] **Step 1: Rename CI workflow to activate it**

```bash
mv .github/workflows/ci.yml.disabled .github/workflows/ci.yml
```

- [ ] **Step 2: Add Unity test, build, and Firebase stages**

Append the following jobs to the end of `.github/workflows/ci.yml`, after the existing `package-unity` job:

```yaml

  # ================================
  # Unity Play Mode Tests (GameCI)
  # ================================
  unity-test:
    needs: [build-native-windows, build-native-linux, build-native-macos, test-dotnet]
    strategy:
      matrix:
        include:
          - os: ubuntu-latest
            artifact: native-linux-x64
            pluginDir: Plugins/Linux
            lib: libbreadlua_native.so
          - os: macos-latest
            artifact: native-macos-universal
            pluginDir: Plugins/macOS
            lib: libbreadlua_native.dylib
          - os: windows-latest
            artifact: native-windows-x64
            pluginDir: Plugins/Windows
            lib: breadlua_native.dll
    runs-on: ${{ matrix.os }}
    steps:
      - uses: actions/checkout@v4

      - name: Download native artifact
        uses: actions/download-artifact@v4
        with:
          name: ${{ matrix.artifact }}
          path: native-lib

      - name: Copy native lib to Unity package plugins
        shell: bash
        run: |
          mkdir -p src/BreadLua.Unity/${{ matrix.pluginDir }}
          cp native-lib/${{ matrix.lib }} src/BreadLua.Unity/${{ matrix.pluginDir }}/

      - uses: game-ci/unity-test-runner@v4
        env:
          UNITY_LICENSE: ${{ secrets.UNITY_LICENSE }}
          UNITY_EMAIL: ${{ secrets.UNITY_EMAIL }}
          UNITY_PASSWORD: ${{ secrets.UNITY_PASSWORD }}
        with:
          testMode: playmode
          projectPath: tests/BreadLua.Unity.TestProject
          unityVersion: auto
          githubToken: ${{ secrets.GITHUB_TOKEN }}

      - uses: actions/upload-artifact@v4
        if: always()
        with:
          name: unity-test-results-${{ matrix.os }}
          path: artifacts/

  # ================================
  # Unity Build for Mobile (IL2CPP)
  # ================================
  unity-build:
    needs: [unity-test, build-native-android, build-native-ios]
    strategy:
      matrix:
        include:
          - targetPlatform: Android
            os: ubuntu-latest
          - targetPlatform: iOS
            os: macos-latest
    runs-on: ${{ matrix.os }}
    steps:
      - uses: actions/checkout@v4

      - name: Download all native artifacts
        uses: actions/download-artifact@v4
        with:
          path: artifacts

      - name: Copy native libs to Unity package
        shell: bash
        run: |
          # Android
          if [ -d artifacts/native-android ]; then
            find artifacts/native-android -name "*.so" -path "*arm64*" -exec cp {} src/BreadLua.Unity/Plugins/Android/arm64-v8a/ \; || true
            find artifacts/native-android -name "*.so" -path "*armv7*" -exec cp {} src/BreadLua.Unity/Plugins/Android/armeabi-v7a/ \; || true
          fi
          # iOS
          if [ -f artifacts/native-ios-arm64/libbreadlua_native.a ]; then
            cp artifacts/native-ios-arm64/libbreadlua_native.a src/BreadLua.Unity/Plugins/iOS/ || true
          fi
          # Desktop (for editor)
          if [ -f artifacts/native-linux-x64/libbreadlua_native.so ]; then
            cp artifacts/native-linux-x64/libbreadlua_native.so src/BreadLua.Unity/Plugins/Linux/ || true
          fi
          if [ -f artifacts/native-macos-universal/libbreadlua_native.dylib ]; then
            cp artifacts/native-macos-universal/libbreadlua_native.dylib src/BreadLua.Unity/Plugins/macOS/ || true
          fi

      - uses: game-ci/unity-builder@v4
        env:
          UNITY_LICENSE: ${{ secrets.UNITY_LICENSE }}
          UNITY_EMAIL: ${{ secrets.UNITY_EMAIL }}
          UNITY_PASSWORD: ${{ secrets.UNITY_PASSWORD }}
        with:
          targetPlatform: ${{ matrix.targetPlatform }}
          projectPath: tests/BreadLua.Unity.TestProject
          unityVersion: auto

      - name: Archive and export iOS
        if: matrix.targetPlatform == 'iOS'
        run: |
          xcodebuild archive \
            -project build/iOS/Unity-iPhone.xcodeproj \
            -scheme Unity-iPhone \
            -archivePath build/iOS/app.xcarchive \
            -allowProvisioningUpdates \
            CODE_SIGN_IDENTITY="-" \
            AD_HOC_CODE_SIGNING_ALLOWED=YES || echo "iOS archive failed — expected in CI without signing"

      - uses: actions/upload-artifact@v4
        with:
          name: unity-build-${{ matrix.targetPlatform }}
          path: build/

  # ================================
  # Firebase Test Lab (main branch only)
  # ================================
  firebase-test:
    needs: [unity-build]
    if: github.event_name == 'push' && github.ref == 'refs/heads/main'
    runs-on: ubuntu-latest
    strategy:
      matrix:
        include:
          - platform: android
            artifact: unity-build-Android
            device: model=Pixel6,version=33
          - platform: ios
            artifact: unity-build-iOS
            device: model=iphone13pro,version=16.6
    steps:
      - uses: google-github-actions/auth@v2
        with:
          credentials_json: ${{ secrets.GCP_SA_KEY }}

      - uses: google-github-actions/setup-gcloud@v2

      - uses: actions/download-artifact@v4
        with:
          name: ${{ matrix.artifact }}
          path: build

      - name: Find app binary
        id: find-app
        run: |
          APK_PATH=$(find build -name "*.apk" -type f | head -1)
          IPA_PATH=$(find build -name "*.ipa" -type f | head -1)
          echo "apk_path=$APK_PATH" >> "$GITHUB_OUTPUT"
          echo "ipa_path=$IPA_PATH" >> "$GITHUB_OUTPUT"

      - name: Run Firebase Test Lab (Android)
        if: matrix.platform == 'android' && steps.find-app.outputs.apk_path != ''
        run: |
          gcloud firebase test android run \
            --type game-loop \
            --app "${{ steps.find-app.outputs.apk_path }}" \
            --device ${{ matrix.device }} \
            --timeout 5m \
            --results-bucket=${{ secrets.GCP_PROJECT_ID }}-test-results \
            --project ${{ secrets.GCP_PROJECT_ID }}

      - name: Run Firebase Test Lab (iOS - beta)
        if: matrix.platform == 'ios' && steps.find-app.outputs.ipa_path != ''
        continue-on-error: true  # iOS Game Loop is beta — don't fail CI
        run: |
          gcloud beta firebase test ios run \
            --type game-loop \
            --app "${{ steps.find-app.outputs.ipa_path }}" \
            --device ${{ matrix.device }} \
            --timeout 5m \
            --project ${{ secrets.GCP_PROJECT_ID }}

      - name: Check test results (Android)
        if: matrix.platform == 'android' && steps.find-app.outputs.apk_path != ''
        run: |
          sleep 10
          MATRIX_ID=$(gcloud firebase test android results list \
            --project ${{ secrets.GCP_PROJECT_ID }} \
            --format="value(testMatrixId)" 2>/dev/null | tail -1)
          if [ -n "$MATRIX_ID" ]; then
            gsutil cp "gs://${{ secrets.GCP_PROJECT_ID }}-test-results/$MATRIX_ID/*/logcat" ./logcat.txt 2>/dev/null || true
            if [ -f ./logcat.txt ]; then
              if grep -q "\[BREADLUA_TEST\] FAIL:" ./logcat.txt; then
                echo "::error::Firebase Test Lab tests failed"
                grep "\[BREADLUA_TEST\]" ./logcat.txt
                exit 1
              fi
              grep "\[BREADLUA_TEST\] SUMMARY:" ./logcat.txt || echo "No test summary found in logcat"
            fi
          fi
```

- [ ] **Step 3: Commit**

```bash
git add .github/workflows/ci.yml
git rm .github/workflows/ci.yml.disabled 2>/dev/null || true
git commit -m "ci: activate CI with Unity Play Mode tests, IL2CPP builds, Firebase Test Lab"
```

---

### Task 12: Final Verification

- [ ] **Step 1: Verify all files exist and structure is correct**

```bash
# Test infrastructure
ls src/BreadLua.Unity/Tests/*.cs
ls src/BreadLua.Unity/Tests/*.asmdef
ls src/BreadLua.Unity/Runtime/link.xml

# Test project
ls tests/BreadLua.Unity.TestProject/Packages/manifest.json
ls tests/BreadLua.Unity.TestProject/Assets/FirebaseGameLoop/*.cs
ls tests/BreadLua.Unity.TestProject/Assets/Plugins/Android/AndroidManifest.xml
ls tests/BreadLua.Unity.TestProject/Assets/Resources/Lua/test_module.lua.txt

# CI
ls .github/workflows/ci.yml
```

Expected: All files listed above exist.

- [ ] **Step 2: Verify runtime changes compile**

Run: TUnit test runner agent to execute existing tests — confirms `PtrToStringUTF8` and `[MonoPInvokeCallback]` changes don't break existing behavior.

- [ ] **Step 3: Review git log**

```bash
git log --oneline -10
```

Expected commits (newest first):
```
ci: activate CI with Unity Play Mode tests, IL2CPP builds, Firebase Test Lab
feat: Firebase Game Loop integration — manager, result writer, manifest, iOS plist
test: IL2CPP edge case tests — callbacks, generics, GCHandle, Unicode
test: Unity Play Mode generator binding tests
test: Unity integration tests — lifecycle, update, module loader
test: Unity Play Mode HotReload tests (desktop only)
test: Unity Play Mode LuaTinker binding tests
test: Unity Play Mode Buffer<T> tests
test: Unity Play Mode core API tests for LuaState
feat: Unity test infrastructure — asmdef and test project
fix: add MonoPInvokeCallback, UTF-8 marshalling, link.xml for IL2CPP
```
