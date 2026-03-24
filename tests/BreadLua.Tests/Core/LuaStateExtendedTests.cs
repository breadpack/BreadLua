using System;
using System.Threading.Tasks;
using BreadPack.NativeLua;
using TUnit.Core;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;

namespace BreadLua.Tests.Core;

public class LuaStateExtendedTests
{
    [Test]
    public async Task SetGlobal_Double()
    {
        using var lua = new LuaState();
        lua.SetGlobal("pi", 3.14);
        lua.DoString("assert(math.abs(pi - 3.14) < 0.001)");
        await Task.CompletedTask;
    }

    [Test]
    public async Task SetGlobal_Bool()
    {
        using var lua = new LuaState();
        lua.SetGlobal("flag", true);
        lua.DoString("assert(flag == true)");
        await Task.CompletedTask;
    }

    [Test]
    public async Task SetGlobal_String()
    {
        using var lua = new LuaState();
        lua.SetGlobal("name", "BreadLua");
        lua.DoString("assert(name == 'BreadLua')");
        await Task.CompletedTask;
    }

    [Test]
    public async Task Eval_Int()
    {
        using var lua = new LuaState();
        int result = lua.Eval<int>("1 + 2 + 3");
        await Assert.That(result).IsEqualTo(6);
    }

    [Test]
    public async Task Eval_Double()
    {
        using var lua = new LuaState();
        double result = lua.Eval<double>("3.14 * 2");
        await Assert.That(Math.Abs(result - 6.28) < 0.01).IsTrue();
    }

    [Test]
    public async Task Eval_String()
    {
        using var lua = new LuaState();
        string result = lua.Eval<string>("'hello' .. ' ' .. 'world'");
        await Assert.That(result).IsEqualTo("hello world");
    }

    [Test]
    public async Task Eval_Bool()
    {
        using var lua = new LuaState();
        bool result = lua.Eval<bool>("10 > 5");
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task Call_WithArgs_ReturnsInt()
    {
        using var lua = new LuaState();
        lua.DoString("function add(a, b) return a + b end");
        int result = lua.Call<int>("add", 10, 20);
        await Assert.That(result).IsEqualTo(30);
    }

    [Test]
    public async Task Call_WithStringArgs()
    {
        using var lua = new LuaState();
        lua.DoString("function greet(name) return 'Hello ' .. name end");
        string result = lua.Call<string>("greet", "BreadLua");
        await Assert.That(result).IsEqualTo("Hello BreadLua");
    }

    [Test]
    public async Task Call_WithMixedArgs()
    {
        using var lua = new LuaState();
        lua.DoString("function calc(a, b, factor) return (a + b) * factor end");
        double result = lua.Call<double>("calc", 3, 7, 2.5);
        await Assert.That(Math.Abs(result - 25.0) < 0.01).IsTrue();
    }
}
