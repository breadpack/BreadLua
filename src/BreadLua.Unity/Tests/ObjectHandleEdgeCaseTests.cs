using System;
using NUnit.Framework;
using BreadPack.NativeLua;

namespace BreadPack.NativeLua.Unity.Tests
{
    [TestFixture]
    public class ObjectHandleEdgeCaseTests
    {
        [Test]
        [Timeout(10000)]
        public void Get_WithZeroPtr_ReturnsNull()
        {
            var result = ObjectHandle.Get<string>(IntPtr.Zero);
            Assert.That(result, Is.Null);
        }

        [Test]
        [Timeout(10000)]
        public void Free_WithZeroPtr_NoError()
        {
            ObjectHandle.Free(IntPtr.Zero);
        }

        [Test]
        [Timeout(10000)]
        public void Get_WrongType_ReturnsNull()
        {
            var obj = "hello";
            var ptr = ObjectHandle.Alloc(obj);
            try
            {
                var result = ObjectHandle.Get<System.Text.StringBuilder>(ptr);
                Assert.That(result, Is.Null);
            }
            finally
            {
                ObjectHandle.Free(ptr);
            }
        }

        [Test]
        [Timeout(10000)]
        public void Alloc_MultipleObjects_UniquePointers()
        {
            var ptr1 = ObjectHandle.Alloc("a");
            var ptr2 = ObjectHandle.Alloc("b");
            try
            {
                Assert.That(ptr1, Is.Not.EqualTo(ptr2));
                Assert.That(ObjectHandle.Get<string>(ptr1), Is.EqualTo("a"));
                Assert.That(ObjectHandle.Get<string>(ptr2), Is.EqualTo("b"));
            }
            finally
            {
                ObjectHandle.Free(ptr1);
                ObjectHandle.Free(ptr2);
            }
        }
    }
}
