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
        [Timeout(10000)]
        public void CreateAndDispose()
        {
            var lua = new LuaState();
            Assert.That(lua.Handle, Is.Not.EqualTo(IntPtr.Zero));
            lua.Dispose();
        }

        [Test]
        [Timeout(10000)]
        public void DoString_SimpleExpression()
        {
            using var lua = new LuaState();
            lua.DoString("x = 1 + 2");
            var result = lua.Eval<int>("x");
            Assert.That(result, Is.EqualTo(3));
        }

        [Test]
        [Timeout(10000)]
        public void DoString_SyntaxError_Throws()
        {
            using var lua = new LuaState();
            Assert.Throws<LuaException>(() => lua.DoString("invalid $$$ syntax"));
        }

        [Test]
        [Timeout(10000)]
        public void Call_GlobalFunction()
        {
            using var lua = new LuaState();
            lua.DoString("function greet() result = 'hello' end");
            lua.Call("greet");
            var result = lua.Eval<string>("result");
            Assert.That(result, Is.EqualTo("hello"));
        }

        [Test]
        [Timeout(10000)]
        public void Eval_Int()
        {
            using var lua = new LuaState();
            Assert.That(lua.Eval<int>("2 + 3"), Is.EqualTo(5));
        }

        [Test]
        [Timeout(10000)]
        public void Eval_Double()
        {
            using var lua = new LuaState();
            Assert.That(lua.Eval<double>("3.14"), Is.EqualTo(3.14).Within(0.001));
        }

        [Test]
        [Timeout(10000)]
        public void Eval_Bool()
        {
            using var lua = new LuaState();
            Assert.That(lua.Eval<bool>("true"), Is.True);
            Assert.That(lua.Eval<bool>("false"), Is.False);
        }

        [Test]
        [Timeout(10000)]
        public void Eval_String()
        {
            using var lua = new LuaState();
            Assert.That(lua.Eval<string>("'hello world'"), Is.EqualTo("hello world"));
        }

        [Test]
        [Timeout(10000)]
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
        [Timeout(10000)]
        public void CallWithArgs_MixedTypes()
        {
            using var lua = new LuaState();
            lua.DoString("function add(a, b) return a + b end");
            var result = lua.Call<int>("add", 10, 20);
            Assert.That(result, Is.EqualTo(30));
        }

        [Test]
        [Timeout(10000)]
        public void DisposedState_Throws()
        {
            var lua = new LuaState();
            lua.Dispose();
            Assert.Throws<ObjectDisposedException>(() => lua.DoString("x = 1"));
        }
    }
}
