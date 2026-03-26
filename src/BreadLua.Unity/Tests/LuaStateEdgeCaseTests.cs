using System;
using NUnit.Framework;
using BreadPack.NativeLua;

namespace BreadPack.NativeLua.Unity.Tests
{
    [TestFixture]
    public class LuaStateEdgeCaseTests
    {
        [Test]
        [Timeout(10000)]
        public void DoString_EmptyString_NoError()
        {
            using var lua = new LuaState();
            lua.DoString("");
        }

        [Test]
        [Timeout(10000)]
        public void DoString_RuntimeError_Throws()
        {
            using var lua = new LuaState();
            Assert.Throws<LuaException>(() => lua.DoString("error('runtime error')"));
        }

        [Test]
        [Timeout(10000)]
        public void DoString_NilAccess_NoError()
        {
            using var lua = new LuaState();
            lua.DoString("x = nil");
        }

        [Test]
        [Timeout(10000)]
        public void Call_NonExistentFunction_Throws()
        {
            using var lua = new LuaState();
            Assert.Throws<LuaException>(() => lua.Call("nonexistent_func"));
        }

        [Test]
        [Timeout(10000)]
        public void Eval_EmptyString_SetsGlobal()
        {
            using var lua = new LuaState();
            lua.SetGlobal("s", "");
            Assert.That(lua.Eval<string>("s"), Is.EqualTo(""));
        }

        [Test]
        [Timeout(10000)]
        public void Eval_NilResult_ReturnsDefault()
        {
            using var lua = new LuaState();
            var result = lua.Eval<int>("nil");
            Assert.That(result, Is.EqualTo(0));
        }

        [Test]
        [Timeout(10000)]
        public void Eval_NilString_ReturnsNull()
        {
            using var lua = new LuaState();
            var result = lua.Eval<string>("nil");
            Assert.That(result, Is.Null);
        }

        [Test]
        [Timeout(10000)]
        public void Dispose_CalledTwice_NoError()
        {
            var lua = new LuaState();
            lua.Dispose();
            lua.Dispose();
        }

        [Test]
        [Timeout(10000)]
        public void DisposedState_Eval_Throws()
        {
            var lua = new LuaState();
            lua.Dispose();
            Assert.Throws<ObjectDisposedException>(() => lua.Eval<int>("1"));
        }

        [Test]
        [Timeout(10000)]
        public void DisposedState_Call_Throws()
        {
            var lua = new LuaState();
            lua.Dispose();
            Assert.Throws<ObjectDisposedException>(() => lua.Call("f"));
        }

        [Test]
        [Timeout(10000)]
        public void DisposedState_SetGlobal_Throws()
        {
            var lua = new LuaState();
            lua.Dispose();
            Assert.Throws<ObjectDisposedException>(() => lua.SetGlobal("x", 1L));
        }

        [Test]
        [Timeout(10000)]
        public void MultipleStates_Independent()
        {
            using var lua1 = new LuaState();
            using var lua2 = new LuaState();

            lua1.DoString("x = 100");
            lua2.DoString("x = 200");

            Assert.That(lua1.Eval<int>("x"), Is.EqualTo(100));
            Assert.That(lua2.Eval<int>("x"), Is.EqualTo(200));
        }

        [Test]
        [Timeout(10000)]
        public void LargeString_Marshalling()
        {
            using var lua = new LuaState();
            var largeStr = new string('A', 100_000);
            lua.SetGlobal("big", largeStr);
            var result = lua.Eval<string>("big");
            Assert.That(result, Is.EqualTo(largeStr));
        }

        [Test]
        [Timeout(10000)]
        public void SetGlobal_IntegerBoundaries()
        {
            using var lua = new LuaState();

            lua.SetGlobal("maxLong", long.MaxValue);
            Assert.That(lua.Eval<long>("maxLong"), Is.EqualTo(long.MaxValue));

            lua.SetGlobal("minLong", long.MinValue);
            Assert.That(lua.Eval<long>("minLong"), Is.EqualTo(long.MinValue));

            lua.SetGlobal("zero", 0L);
            Assert.That(lua.Eval<long>("zero"), Is.EqualTo(0L));
        }

        [Test]
        [Timeout(10000)]
        public void Call_WithManyArgs()
        {
            using var lua = new LuaState();
            lua.DoString("function sum5(a,b,c,d,e) return a+b+c+d+e end");
            var result = lua.Call<int>("sum5", 1, 2, 3, 4, 5);
            Assert.That(result, Is.EqualTo(15));
        }

        [Test]
        [Timeout(10000)]
        public void Eval_StackOverflow_Error()
        {
            using var lua = new LuaState();
            lua.DoString("function recurse(n) if n > 100 then error('too deep') end return recurse(n+1) end");
            Assert.Throws<LuaException>(() => lua.DoString("recurse(0)"));
        }

        [Test]
        [Timeout(10000)]
        public void LuaException_ContainsMessage()
        {
            using var lua = new LuaState();
            var ex = Assert.Throws<LuaException>(() => lua.DoString("error('custom_msg')"));
            Assert.That(ex.Message, Does.Contain("custom_msg"));
        }
    }
}
