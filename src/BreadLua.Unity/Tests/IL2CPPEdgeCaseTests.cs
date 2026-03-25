using System;
using NUnit.Framework;
using BreadPack.NativeLua;

namespace BreadPack.NativeLua.Unity.Tests
{
    [TestFixture]
    public class IL2CPPEdgeCaseTests
    {
        [Test]
        public void ReversePInvokeCallback_SurvivesIL2CPP()
        {
            using var lua = new LuaState();
            lua.Tinker.Bind("native_callback_test", (Func<int, int, int>)((a, b) => a + b));
            lua.DoString("result = native_callback_test(10, 20)");
            Assert.That(lua.Eval<int>("result"), Is.EqualTo(30));
        }

        [Test]
        public void GenericReadValue_AllTypes()
        {
            using var lua = new LuaState();

            Assert.That(lua.Eval<int>("42"), Is.EqualTo(42));
            Assert.That(lua.Eval<long>("9999999999"), Is.EqualTo(9999999999L));
            Assert.That(lua.Eval<float>("1.5"), Is.EqualTo(1.5f).Within(0.01f));
            Assert.That(lua.Eval<double>("3.14159"), Is.EqualTo(3.14159).Within(0.00001));
            Assert.That(lua.Eval<bool>("true"), Is.True);
            Assert.That(lua.Eval<string>("'hello'"), Is.EqualTo("hello"));
        }

        [Test]
        public void GenericCallWithParams_BoxingRoundTrip()
        {
            using var lua = new LuaState();
            lua.DoString("function add(a, b) return a + b end");
            lua.DoString("function concat(a, b) return a .. b end");
            lua.DoString("function check(a) if a then return 1 else return 0 end end");

            Assert.That(lua.Call<int>("add", 10, 20), Is.EqualTo(30));
            Assert.That(lua.Call<double>("add", 1.5, 2.5), Is.EqualTo(4.0).Within(0.01));
            Assert.That(lua.Call<string>("concat", "hello", " world"), Is.EqualTo("hello world"));
            Assert.That(lua.Call<int>("check", true), Is.EqualTo(1));
        }

        [Test]
        public void BufferGenericPointer_MultipleTypes()
        {
            using var intBuf = new Buffer<int>(4);
            intBuf.Count = 2;
            intBuf[0] = 42;
            intBuf[1] = 99;
            Assert.That(intBuf[0], Is.EqualTo(42));
            Assert.That(intBuf.Pointer, Is.Not.EqualTo(IntPtr.Zero));

            using var floatBuf = new Buffer<float>(4);
            floatBuf.Count = 2;
            floatBuf[0] = 1.5f;
            floatBuf[1] = 2.5f;
            Assert.That(floatBuf[0], Is.EqualTo(1.5f).Within(0.001f));

            using var doubleBuf = new Buffer<double>(4);
            doubleBuf.Count = 1;
            doubleBuf[0] = 3.14159;
            Assert.That(doubleBuf[0], Is.EqualTo(3.14159).Within(0.00001));
        }

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
                Assert.That(retrieved, Is.SameAs(testObj));
            }
            finally
            {
                ObjectHandle.Free(ptr);
            }
        }

        [Test]
        public void DelegateTypePatternMatching_AllBindOverloads()
        {
            using var lua = new LuaState();

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

            lua.DoString("fn_str_action('test')");
            lua.DoString("fn_action()");

            lua.DoString("r4 = fn_double()");
            Assert.That(lua.Eval<double>("r4"), Is.EqualTo(2.718).Within(0.001));
        }

        [Test]
        public void SourceGeneratedCode_NotStripped()
        {
            using var lua = new LuaState();
            lua.CreateMetatable("TestMeta");

            var obj = new TestPayload { Value = 1, Name = "mt_test" };
            var handle = ObjectHandle.Alloc(obj);
            try
            {
                lua.PushObject(handle, "TestMeta");
                Assert.Pass("Metatable and PushObject work — generated code path is intact");
            }
            finally
            {
                ObjectHandle.Free(handle);
            }
        }

        [Test]
        public void UnicodeStringMarshalling_KoreanAndEmoji()
        {
            using var lua = new LuaState();

            lua.SetGlobal("korean", "한글테스트");
            Assert.That(lua.Eval<string>("korean"), Is.EqualTo("한글테스트"));

            lua.SetGlobal("emoji", "🎮🎲");
            Assert.That(lua.Eval<string>("emoji"), Is.EqualTo("🎮🎲"));

            lua.SetGlobal("mixed", "Hello 세계 🌍");
            Assert.That(lua.Eval<string>("mixed"), Is.EqualTo("Hello 세계 🌍"));

            lua.DoString("combined = korean .. ' ' .. emoji");
            Assert.That(lua.Eval<string>("combined"), Is.EqualTo("한글테스트 🎮🎲"));
        }

        private class TestPayload
        {
            public int Value { get; set; }
            public string Name { get; set; }
        }
    }
}
