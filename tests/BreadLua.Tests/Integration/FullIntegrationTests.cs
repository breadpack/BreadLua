using System;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using BreadPack.NativeLua;
using TUnit.Core;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;

namespace BreadLua.Tests.Integration;

public class FullIntegrationTests
{
    [Test]
    public async Task FullWorkflow_CreateState_SetGlobals_Eval()
    {
        using var lua = new LuaState();
        lua.SetGlobal("x", 10L);
        lua.SetGlobal("y", 20L);
        int result = lua.Eval<int>("x + y");
        await Assert.That(result).IsEqualTo(30);
    }

    [Test]
    public async Task FullWorkflow_Tinker_BindAndCall()
    {
        using var lua = new LuaState();
        lua.Tinker.Bind("double_it", (int x, int _) => x * 2);
        int result = lua.Eval<int>("double_it(21, 0)");
        await Assert.That(result).IsEqualTo(42);
    }

    [Test]
    public async Task FullWorkflow_Buffer_BindAndVerify()
    {
        using var lua = new LuaState();
        using var buffer = new Buffer<SimpleData>(10);
        buffer.Count = 3;
        buffer[0] = new SimpleData { value = 100 };
        buffer[1] = new SimpleData { value = 200 };
        buffer[2] = new SimpleData { value = 300 };

        buffer.BindToLua(lua, "g_data");
        lua.DoString("assert(g_data_count == 3, 'expected 3 items')");
        await Task.CompletedTask;
    }

    [Test]
    public async Task FullWorkflow_ComplexLuaScript()
    {
        using var lua = new LuaState();
        lua.DoString(@"
            function fibonacci(n)
                if n <= 1 then return n end
                return fibonacci(n - 1) + fibonacci(n - 2)
            end
        ");
        int fib10 = lua.Call<int>("fibonacci", 10);
        await Assert.That(fib10).IsEqualTo(55);
    }

    [Test]
    public async Task FullWorkflow_ErrorHandling()
    {
        using var lua = new LuaState();
        await Assert.ThrowsAsync<LuaException>(() =>
        {
            lua.DoString("error('intentional error')");
            return Task.CompletedTask;
        });
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct SimpleData
{
    public int value;
}
