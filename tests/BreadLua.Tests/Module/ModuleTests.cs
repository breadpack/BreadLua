using System;
using System.Threading.Tasks;
using BreadPack.NativeLua;
using TUnit.Core;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;

namespace BreadLua.Tests.Module;

[LuaModule("test_module")]
public static partial class TestModuleAPI
{
    [LuaExport]
    public static int Add(int a, int b) => a + b;

    [LuaExport("multiply")]
    public static float Multiply(float a, float b) => a * b;

    [LuaExport]
    public static void DoNothing() { }

    [LuaExport]
    public static bool IsPositive(int value) => value > 0;
}

public class ModuleTests
{
    [Test]
    public async Task GeneratedCode_Compiles()
    {
        // If this test compiles and runs, the Source Generator worked
        await Task.CompletedTask;
    }

    [Test]
    public async Task Register_MethodExists()
    {
        // Source Generator should create a static Register() method
        var registerMethod = typeof(TestModuleAPI).GetMethod("Register",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        await Assert.That(registerMethod).IsNotNull();
    }

    [Test]
    public async Task OriginalMethods_StillWork()
    {
        // Original methods should still be callable
        await Assert.That(TestModuleAPI.Add(3, 4)).IsEqualTo(7);
        await Assert.That(TestModuleAPI.Multiply(2.5f, 4f)).IsEqualTo(10f);
        await Assert.That(TestModuleAPI.IsPositive(5)).IsTrue();
    }
}
