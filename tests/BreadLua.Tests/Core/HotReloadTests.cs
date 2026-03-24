using System;
using System.IO;
using System.Threading.Tasks;
using BreadPack.NativeLua;
using TUnit.Core;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;

namespace BreadLua.Tests.Core;

public class HotReloadTests
{
    [Test]
    public async Task Reload_ExecutesFileAgain()
    {
        using var lua = new LuaState();
        string tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, "reload_counter = (reload_counter or 0) + 1");
            lua.DoFile(tempFile);
            lua.Reload(tempFile);
            lua.DoString("assert(reload_counter == 2)");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}
