using System;
using NUnit.Framework;
using BreadPack.NativeLua;

namespace BreadPack.NativeLua.Unity.Tests
{
    [TestFixture]
    public class BufferEdgeCaseTests
    {
        [Test]
        [Timeout(10000)]
        public void Constructor_ZeroCapacity_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Buffer<int>(0));
        }

        [Test]
        [Timeout(10000)]
        public void Constructor_NegativeCapacity_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Buffer<int>(-1));
        }

        [Test]
        [Timeout(10000)]
        public void Count_ClampedToCapacity()
        {
            using var buffer = new Buffer<int>(5);
            buffer.Count = 100;
            Assert.That(buffer.Count, Is.EqualTo(5));
        }

        [Test]
        [Timeout(10000)]
        public void Count_ClampedToZero()
        {
            using var buffer = new Buffer<int>(5);
            buffer.Count = -10;
            Assert.That(buffer.Count, Is.EqualTo(0));
        }

        [Test]
        [Timeout(10000)]
        public void Count_SetToZero_EmptySpan()
        {
            using var buffer = new Buffer<int>(5);
            buffer.Count = 3;
            buffer.Count = 0;
            Assert.That(buffer.AsSpan().Length, Is.EqualTo(0));
        }

        [Test]
        [Timeout(10000)]
        public void Dispose_CalledTwice_NoError()
        {
            var buffer = new Buffer<int>(5);
            buffer.Dispose();
            buffer.Dispose();
        }

        [Test]
        [Timeout(10000)]
        public void Dispose_PointerBecomesZero()
        {
            var buffer = new Buffer<int>(5);
            Assert.That(buffer.Pointer, Is.Not.EqualTo(IntPtr.Zero));
            buffer.Dispose();
            Assert.That(buffer.Pointer, Is.EqualTo(IntPtr.Zero));
        }

        [Test]
        [Timeout(10000)]
        public void Buffer_ByteType()
        {
            using var buffer = new Buffer<byte>(256);
            buffer.Count = 3;
            buffer[0] = 0xFF;
            buffer[1] = 0x00;
            buffer[2] = 0x7F;
            Assert.That(buffer[0], Is.EqualTo(0xFF));
            Assert.That(buffer[1], Is.EqualTo(0x00));
            Assert.That(buffer[2], Is.EqualTo(0x7F));
        }

        [Test]
        [Timeout(10000)]
        public void Buffer_LongType()
        {
            using var buffer = new Buffer<long>(4);
            buffer.Count = 2;
            buffer[0] = long.MaxValue;
            buffer[1] = long.MinValue;
            Assert.That(buffer[0], Is.EqualTo(long.MaxValue));
            Assert.That(buffer[1], Is.EqualTo(long.MinValue));
        }
    }
}
