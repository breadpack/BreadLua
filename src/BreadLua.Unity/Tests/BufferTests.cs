using System;
using NUnit.Framework;
using BreadPack.NativeLua;

namespace BreadPack.NativeLua.Unity.Tests
{
    [TestFixture]
    public class BufferTests
    {
        [Test]
        [Timeout(10000)]
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
        [Timeout(10000)]
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
        [Timeout(10000)]
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
        [Timeout(10000)]
        public void Dispose_FreesMemory()
        {
            var buffer = new Buffer<int>(10);
            buffer.Count = 1;
            buffer[0] = 42;
            buffer.Dispose();
            Assert.That(buffer.Pointer, Is.EqualTo(IntPtr.Zero));
        }

        [Test]
        [Timeout(10000)]
        public void OutOfRange_Throws()
        {
            using var buffer = new Buffer<int>(5);
            buffer.Count = 2;
            Assert.Throws<IndexOutOfRangeException>(() => { var _ = buffer[2]; });
            Assert.Throws<IndexOutOfRangeException>(() => { var _ = buffer[-1]; });
        }
    }
}
