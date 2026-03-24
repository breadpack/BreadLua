using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using BreadPack.NativeLua;
using TUnit.Core;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;

namespace BreadLua.Tests.Core;

[StructLayout(LayoutKind.Sequential)]
public struct TestUnit
{
    public int id;
    public float hp;
    public float attack;
}

public class BufferTests
{
    [Test]
    public async Task Create_ShouldAllocateMemory()
    {
        using var buffer = new Buffer<TestUnit>(10);
        await Assert.That(buffer.Capacity).IsEqualTo(10);
        await Assert.That(buffer.Pointer).IsNotEqualTo(IntPtr.Zero);
    }

    [Test]
    public async Task ReadWrite_ShouldWork()
    {
        using var buffer = new Buffer<TestUnit>(10);
        buffer.Count = 1;
        buffer[0] = new TestUnit { id = 1, hp = 100f, attack = 25f };

        await Assert.That(buffer[0].id).IsEqualTo(1);
        await Assert.That(buffer[0].hp).IsEqualTo(100f);
        await Assert.That(buffer[0].attack).IsEqualTo(25f);
    }

    [Test]
    public async Task IndexOutOfRange_ShouldThrow()
    {
        using var buffer = new Buffer<TestUnit>(10);
        buffer.Count = 1;
        await Assert.ThrowsAsync<IndexOutOfRangeException>(() =>
        {
            _ = buffer[5];
            return Task.CompletedTask;
        });
    }

    [Test]
    public async Task AsSpan_ShouldReturnCorrectSlice()
    {
        using var buffer = new Buffer<TestUnit>(10);
        buffer.Count = 3;
        buffer[0] = new TestUnit { id = 1 };
        buffer[1] = new TestUnit { id = 2 };
        buffer[2] = new TestUnit { id = 3 };

        var span = buffer.AsSpan();
        int spanLength = span.Length;
        int spanItem1Id = span[1].id;
        await Assert.That(spanLength).IsEqualTo(3);
        await Assert.That(spanItem1Id).IsEqualTo(2);
    }
}
