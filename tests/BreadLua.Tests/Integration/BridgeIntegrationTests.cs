using System;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using BreadPack.NativeLua;
using TUnit.Core;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;

namespace BreadLua.Tests.Integration;

[StructLayout(LayoutKind.Sequential)]
[LuaBridge("test_data")]
public struct TestData
{
    public int id;
    public float value;
    [LuaReadOnly] public bool flag;
    [LuaIgnore] public int hidden;
    [LuaField("custom_name")] public float renamedField;
}

public class BridgeIntegrationTests
{
    [Test]
    public async Task LuaState_DoString_RunsLuaCode()
    {
        using var lua = new LuaState();
        lua.DoString("result = 1 + 2");
        await Task.CompletedTask;
    }

    [Test]
    public async Task LuaState_DoString_Print()
    {
        using var lua = new LuaState();
        lua.DoString("print('integration test: hello from Lua')");
        await Task.CompletedTask;
    }

    [Test]
    public async Task Buffer_SharedMemory_ReadWrite()
    {
        using var buffer = new Buffer<TestData>(10);
        buffer.Count = 1;
        buffer[0] = new TestData { id = 42, value = 3.14f, renamedField = 1.5f };

        await Assert.That(buffer[0].id).IsEqualTo(42);
        await Assert.That(buffer[0].value).IsEqualTo(3.14f);
        await Assert.That(buffer[0].renamedField).IsEqualTo(1.5f);
    }

    [Test]
    public async Task Buffer_BindToLua_SetsGlobals()
    {
        using var lua = new LuaState();
        using var buffer = new Buffer<TestData>(10);
        buffer.Count = 5;

        buffer.BindToLua(lua, "g_test_data");
        lua.DoString("assert(g_test_data_count == 5, 'count should be 5')");
        await Task.CompletedTask;
    }

    [Test]
    public async Task LuaState_CallFunction_Works()
    {
        using var lua = new LuaState();
        lua.DoString("function add() return 1 + 2 end");
        lua.Call("add");
        await Task.CompletedTask;
    }

    [Test]
    public async Task LuaState_InvalidCode_ThrowsLuaException()
    {
        using var lua = new LuaState();
        await Assert.ThrowsAsync<LuaException>(() =>
        {
            lua.DoString("this is not valid lua %%%");
            return Task.CompletedTask;
        });
    }
}
