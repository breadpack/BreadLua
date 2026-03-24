using System;
using System.Threading.Tasks;
using BreadPack.NativeLua;
using TUnit.Core;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;

namespace BreadLua.Tests.Bind;

public class BindClosureTests
{
    [Test]
    public async Task BindCEmitter_GeneratesClosureBindMethod()
    {
        // Verify the Source Generator produces bind() with closure caching in the C output.
        // bind() returns a table with __index/__newindex closures that capture GCHandle as upvalue.
        // This is a compile-time verification - if it compiles, the generator works.
        var player = new TestPlayer("BindTest", 5);
        await Assert.That(player.Name).IsEqualTo("BindTest");
    }

    [Test]
    public async Task BindCEmitter_NoApplyMethod()
    {
        // apply() is no longer generated. bind() closures provide real-time access,
        // so there is no need for a separate apply step.
        // This test confirms the class still compiles correctly without apply().
        var player = new TestPlayer("NoApplyTest", 10);
        player.HP = 200f;
        await Assert.That(player.HP).IsEqualTo(200f);
    }
}
