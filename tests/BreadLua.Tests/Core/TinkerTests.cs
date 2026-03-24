using System;
using System.Threading.Tasks;
using BreadPack.NativeLua;
using TUnit.Core;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;

namespace BreadLua.Tests.Core;

public class TinkerTests
{
    [Test]
    public async Task Bind_IntFunc_CallFromLua()
    {
        using var lua = new LuaState();
        lua.Tinker.Bind("add", (int a, int b) => a + b);
        int result = lua.Eval<int>("add(10, 20)");
        await Assert.That(result).IsEqualTo(30);
    }

    [Test]
    public async Task Bind_StringFunc_CallFromLua()
    {
        using var lua = new LuaState();
        lua.Tinker.Bind("greet", (string name) => "Hello " + name);
        string result = lua.Eval<string>("greet('World')");
        await Assert.That(result).IsEqualTo("Hello World");
    }

    [Test]
    public async Task Bind_Action_CallFromLua()
    {
        using var lua = new LuaState();
        bool called = false;
        lua.Tinker.Bind("notify", () => { called = true; });
        lua.DoString("notify()");
        await Assert.That(called).IsTrue();
    }

    [Test]
    public async Task Bind_StringAction_CallFromLua()
    {
        using var lua = new LuaState();
        string? captured = null;
        lua.Tinker.Bind("log", (string msg) => { captured = msg; });
        lua.DoString("log('test message')");
        await Assert.That(captured).IsEqualTo("test message");
    }
}
