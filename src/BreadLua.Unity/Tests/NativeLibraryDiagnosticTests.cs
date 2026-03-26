using System;
using System.Runtime.InteropServices;
using NUnit.Framework;
using UnityEngine;

namespace BreadPack.NativeLua.Unity.Tests
{
    [TestFixture]
    public class NativeLibraryDiagnosticTests
    {
        [Test]
        [Order(0)]
        [Timeout(10000)]
        public void NativeLibrary_CanLoad()
        {
            try
            {
                var lua = new LuaState();
                Assert.That(lua.Handle, Is.Not.EqualTo(IntPtr.Zero),
                    "LuaState created but Handle is zero");
                lua.Dispose();
                Debug.Log("[BREADLUA_DIAG] Native library loaded successfully");
            }
            catch (DllNotFoundException ex)
            {
                Debug.LogError($"[BREADLUA_DIAG] DllNotFoundException: {ex.Message}");
                Assert.Fail($"Native library 'breadlua_native' not found. " +
                    $"Ensure the library is in the correct Plugins/ directory with a proper .meta file. " +
                    $"Details: {ex.Message}");
            }
            catch (EntryPointNotFoundException ex)
            {
                Debug.LogError($"[BREADLUA_DIAG] EntryPointNotFoundException: {ex.Message}");
                Assert.Fail($"Native library found but entry point missing: {ex.Message}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BREADLUA_DIAG] Unexpected error: {ex.GetType().Name}: {ex.Message}");
                Assert.Fail($"Unexpected error loading native library: {ex.GetType().Name}: {ex.Message}");
            }
        }

        [Test]
        [Timeout(5000)]
        public void NativeLibrary_DoString_Works()
        {
            using var lua = new LuaState();
            lua.DoString("x = 1 + 1");
            var result = lua.Eval<int>("x");
            Assert.That(result, Is.EqualTo(2));
        }
    }
}
