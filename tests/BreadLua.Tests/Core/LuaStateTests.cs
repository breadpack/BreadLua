using System;
using System.Threading.Tasks;
using BreadPack.NativeLua;
using TUnit.Core;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;

namespace BreadLua.Tests.Core;

public class LuaStateTests
{
    [Test]
    public async Task CreateAndDispose_ShouldNotThrow()
    {
        using var lua = new LuaState();
        await Assert.That(lua.Handle).IsNotEqualTo(IntPtr.Zero);
    }

    [Test]
    public async Task DoString_ValidCode_ShouldExecute()
    {
        using var lua = new LuaState();
        lua.DoString("x = 1 + 2");
        await Task.CompletedTask;
    }

    [Test]
    public async Task DoString_InvalidCode_ShouldThrowLuaException()
    {
        using var lua = new LuaState();
        await Assert.ThrowsAsync<LuaException>(() =>
        {
            lua.DoString("invalid syntax %%%");
            return Task.CompletedTask;
        });
    }

    [Test]
    public async Task Call_DefinedFunction_ShouldExecute()
    {
        using var lua = new LuaState();
        lua.DoString("function hello() end");
        lua.Call("hello");
        await Task.CompletedTask;
    }

    [Test]
    public async Task Call_UndefinedFunction_ShouldThrow()
    {
        using var lua = new LuaState();
        await Assert.ThrowsAsync<LuaException>(() =>
        {
            lua.Call("nonexistent_func");
            return Task.CompletedTask;
        });
    }

    [Test]
    public async Task Dispose_DoubleFree_ShouldNotThrow()
    {
        var lua = new LuaState();
        lua.Dispose();
        lua.Dispose();
        await Task.CompletedTask;
    }
}
