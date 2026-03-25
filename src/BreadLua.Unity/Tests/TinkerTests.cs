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
            Assert.Throws<LuaException>(() => lua.DoString("explode()"));
        }

        [Test]
        public void MultiInstance_BindingIsolation()
        {
            using var lua1 = new LuaState();
            using var lua2 = new LuaState();

            lua1.Tinker.Bind("shared_fn", (Func<int, int, int>)((a, b) => a + b));
            lua2.Tinker.Bind("shared_fn", (Func<int, int, int>)((a, b) => a * b));

            lua2.DoString("result = shared_fn(3, 4)");
            Assert.That(lua2.Eval<int>("result"), Is.EqualTo(12));
        }
    }
}
