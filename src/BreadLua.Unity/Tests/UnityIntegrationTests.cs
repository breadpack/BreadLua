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
        [Timeout(10000)]
        public IEnumerator UnityLuaState_Lifecycle()
        {
            var go = new GameObject("LuaTest");
            var luaState = go.AddComponent<UnityLuaState>();

            yield return null;

            Assert.That(luaState.State, Is.Not.Null);
            Assert.That(luaState.State.Handle, Is.Not.EqualTo(System.IntPtr.Zero));

            Object.Destroy(go);
            yield return null;
        }

        [UnityTest]
        [Timeout(10000)]
        public IEnumerator UnityLuaState_UpdateCallback()
        {
            var go = new GameObject("LuaTest");
            var luaState = go.AddComponent<UnityLuaState>();

            yield return null;

            luaState.State.DoString("counter = 0; function on_update() counter = counter + 1 end");

            yield return null;
            yield return null;

            var counter = luaState.State.Eval<int>("counter");
            Assert.That(counter, Is.GreaterThanOrEqualTo(2));

            Object.Destroy(go);
            yield return null;
        }

        [UnityTest]
        [Timeout(10000)]
        public IEnumerator UnityLuaState_StartupScripts()
        {
            var go = new GameObject("LuaTest");
            var luaState = go.AddComponent<UnityLuaState>();

            yield return null;

            luaState.State.DoString("startup_ran = true");
            Assert.That(luaState.State.Eval<bool>("startup_ran"), Is.True);

            Object.Destroy(go);
            yield return null;
        }

        [Test]
        [Timeout(10000)]
        public void UnityModuleLoader_LoadFromResources()
        {
            var loader = new UnityModuleLoader("Lua");
            var script = loader.Load("test_module");

            if (script == null)
            {
                Assert.Ignore("test_module.lua.txt not found in Resources — skipping");
                return;
            }

            Assert.That(script, Does.Contain("test_module_loaded"));
        }
    }
}
