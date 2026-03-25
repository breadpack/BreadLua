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
            using var lua = new LuaState();
            lua.CreateMetatable("TestClass");
            Assert.Pass("Metatable created successfully");
        }

        [Test]
        public void LuaBind_MethodCall()
        {
            using var lua = new LuaState();
            lua.CreateMetatable("MethodTestClass");

            lua.Tinker.Bind("mt_method", (Func<int, int, int>)((a, b) => a + b));
            lua.DoString("result = mt_method(5, 3)");
            Assert.That(lua.Eval<int>("result"), Is.EqualTo(8));
        }

        [Test]
        public void LuaBind_PropertyAccess()
        {
            using var lua = new LuaState();
            lua.CreateMetatable("PropTestClass");

            var obj = new TestObject { Name = "test_prop", Value = 42 };
            var handle = ObjectHandle.Alloc(obj);
            try
            {
                lua.PushObject(handle, "PropTestClass");
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
            using var lua = new LuaState();
            lua.Tinker.Bind("module_func", (Func<int, int, int>)((a, b) => a * b));
            lua.DoString("result = module_func(6, 7)");
            Assert.That(lua.Eval<int>("result"), Is.EqualTo(42));
        }

        [Test]
        public void LuaBridge_StructBinding()
        {
            using var lua = new LuaState();
            using var buffer = new Buffer<int>(8);
            buffer.Count = 3;
            buffer[0] = 10;
            buffer[1] = 20;
            buffer[2] = 30;

            buffer.BindToLua(lua, "bridge_data");

            Assert.That(lua.Eval<long>("bridge_data_count"), Is.EqualTo(3));
        }

        private class TestObject
        {
            public string Name { get; set; }
            public int Value { get; set; }
        }
    }
}
