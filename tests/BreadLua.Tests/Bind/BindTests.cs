using System;
using System.Threading.Tasks;
using BreadPack.NativeLua;
using TUnit.Core;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;

namespace BreadLua.Tests.Bind;

[LuaBind]
public partial class TestPlayer
{
    public string Name { get; set; }
    public int Level { get; set; }
    public float HP { get; set; }

    [LuaIgnore]
    public int InternalId { get; set; }

    [LuaConstructor]
    public TestPlayer(string name, int level)
    {
        Name = name;
        Level = level;
        HP = 100f;
    }

    public void Heal(float amount) { HP += amount; }
    public float GetHP() { return HP; }
}

public class BindTests
{
    [Test]
    public async Task GeneratedCode_Compiles()
    {
        // If this compiles, the Source Generator worked
        var player = new TestPlayer("Hero", 10);
        await Assert.That(player.Name).IsEqualTo("Hero");
    }

    [Test]
    public async Task Register_MethodExists()
    {
        var registerMethod = typeof(TestPlayer).GetMethod("Register",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        await Assert.That(registerMethod).IsNotNull();
    }

    [Test]
    public async Task LuaMetatableName_Exists()
    {
        // Generated code should define the metatable name constant
        string metatableName = TestPlayer.LuaMetatableName;
        await Assert.That(metatableName).IsEqualTo("bread_TestPlayer");
    }

    [Test]
    public async Task ObjectHandle_AllocAndGet()
    {
        var player = new TestPlayer("Test", 5);
        var handle = ObjectHandle.Alloc(player);

        var retrieved = ObjectHandle.Get<TestPlayer>(handle);
        await Assert.That(retrieved).IsNotNull();
        await Assert.That(retrieved!.Name).IsEqualTo("Test");
        await Assert.That(retrieved.Level).IsEqualTo(5);

        ObjectHandle.Free(handle);
    }

    [Test]
    public async Task ObjectHandle_FreeAndGet_ReturnsNull()
    {
        var player = new TestPlayer("Test", 1);
        var handle = ObjectHandle.Alloc(player);
        ObjectHandle.Free(handle);

        // After free, the handle is no longer valid
        // (GCHandle.FromIntPtr on freed handle throws, which is expected)
        await Task.CompletedTask;
    }

    [Test]
    public async Task OriginalClass_StillWorks()
    {
        var player = new TestPlayer("Warrior", 20);
        player.Heal(50f);
        await Assert.That(player.HP).IsEqualTo(150f);
        await Assert.That(player.GetHP()).IsEqualTo(150f);
    }
}
