using System;
using NUnit.Framework;
using BreadPack.NativeLua;

namespace BreadPack.NativeLua.Unity.Tests
{
    [TestFixture]
    public class TinkerEdgeCaseTests
    {
        [Test]
        [Timeout(10000)]
        public void Callback_Exception_Propagates_AsLuaException()
        {
            using var lua = new LuaState();
            lua.Tinker.Bind("explode", (Action)(() => throw new InvalidOperationException("boom")));
            var ex = Assert.Throws<LuaException>(() => lua.DoString("explode()"));
            Assert.That(ex.Message, Does.Contain("boom"));
        }

        [Test]
        [Timeout(10000)]
        public void Callback_Exception_StateRemainsUsable()
        {
            using var lua = new LuaState();
            lua.Tinker.Bind("explode", (Action)(() => throw new Exception("fail")));

            try { lua.DoString("explode()"); } catch (LuaException) { }

            lua.DoString("x = 42");
            Assert.That(lua.Eval<int>("x"), Is.EqualTo(42));
        }

        [Test]
        [Timeout(10000)]
        public void Rebind_SameName_UsesLatest()
        {
            using var lua = new LuaState();
            lua.Tinker.Bind("calc", (Func<int, int, int>)((a, b) => a + b));
            lua.DoString("r1 = calc(3, 4)");
            Assert.That(lua.Eval<int>("r1"), Is.EqualTo(7));

            lua.Tinker.Bind("calc", (Func<int, int, int>)((a, b) => a * b));
            lua.DoString("r2 = calc(3, 4)");
            Assert.That(lua.Eval<int>("r2"), Is.EqualTo(12));
        }

        [Test]
        [Timeout(10000)]
        public void Callback_EmptyString_Arg()
        {
            using var lua = new LuaState();
            string captured = null;
            lua.Tinker.Bind("capture", (Action<string>)(s => captured = s));
            lua.DoString("capture('')");
            Assert.That(captured, Is.EqualTo(""));
        }

        [Test]
        [Timeout(10000)]
        public void Callback_NilString_DefaultsToEmpty()
        {
            using var lua = new LuaState();
            string captured = null;
            lua.Tinker.Bind("capture", (Action<string>)(s => captured = s));
            lua.DoString("capture(nil)");
            Assert.That(captured, Is.EqualTo(""));
        }

        [Test]
        [Timeout(10000)]
        public void Callback_LargeIntegerValues()
        {
            using var lua = new LuaState();
            lua.Tinker.Bind("mirror", (Func<int, int, int>)((a, b) => a + b));
            lua.DoString("result = mirror(2147483640, 7)");
            Assert.That(lua.Eval<int>("result"), Is.EqualTo(2147483647));
        }

        [Test]
        [Timeout(10000)]
        public void Callback_FloatPrecision()
        {
            using var lua = new LuaState();
            lua.Tinker.Bind("precise", (Func<float, float, float>)((a, b) => a + b));
            lua.DoString("result = precise(0.1, 0.2)");
            Assert.That(lua.Eval<double>("result"), Is.EqualTo(0.3f).Within(0.001));
        }

        [Test]
        [Timeout(10000)]
        public void Callback_ReturnString_WithUnicode()
        {
            using var lua = new LuaState();
            lua.Tinker.Bind("greet", (Func<string, string>)(name => "안녕 " + name + " 🎮"));
            lua.DoString("result = greet('세계')");
            Assert.That(lua.Eval<string>("result"), Is.EqualTo("안녕 세계 🎮"));
        }

        [Test]
        [Timeout(10000)]
        public void MultipleBindTypes_SameState()
        {
            using var lua = new LuaState();
            lua.Tinker.Bind("fn_int", (Func<int, int, int>)((a, b) => a + b));
            lua.Tinker.Bind("fn_str", (Func<string, string>)(s => s.ToUpper()));
            lua.Tinker.Bind("fn_act", (Action)(() => { }));
            lua.Tinker.Bind("fn_dbl", (Func<double>)(() => 3.14));

            lua.DoString("r1 = fn_int(1, 2)");
            lua.DoString("r2 = fn_str('hello')");
            lua.DoString("fn_act()");
            lua.DoString("r3 = fn_dbl()");

            Assert.That(lua.Eval<int>("r1"), Is.EqualTo(3));
            Assert.That(lua.Eval<string>("r2"), Is.EqualTo("HELLO"));
            Assert.That(lua.Eval<double>("r3"), Is.EqualTo(3.14).Within(0.001));
        }
    }
}
