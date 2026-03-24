using System;
using System.Runtime.InteropServices;
using BreadPack.NativeLua;

[LuaBridge("unit")]
[StructLayout(LayoutKind.Sequential)]
public struct UnitData
{
    public int unitId;
    public float hp;
    public float attack;
    [LuaReadOnly] public bool isAlive;
    [LuaIgnore] public int internalFlag;
    [LuaField("def")] public float defence;
}

class Program
{
    static void Main()
    {
        Console.WriteLine("=== BreadLua Sample ===");

        // LuaState basic test
        using var lua = new LuaState();
        lua.DoString("print('Hello from BreadLua!')");

        // Buffer test
        using var buffer = new Buffer<UnitData>(100);
        buffer.Count = 2;
        buffer[0] = new UnitData { unitId = 1, hp = 100, attack = 25, defence = 10 };
        buffer[1] = new UnitData { unitId = 2, hp = 80, attack = 30, defence = 5 };

        Console.WriteLine("Unit 0: id=" + buffer[0].unitId + ", hp=" + buffer[0].hp + ", atk=" + buffer[0].attack + ", def=" + buffer[0].defence);
        Console.WriteLine("Unit 1: id=" + buffer[1].unitId + ", hp=" + buffer[1].hp + ", atk=" + buffer[1].attack + ", def=" + buffer[1].defence);

        // Bind buffer to Lua
        buffer.BindToLua(lua, "g_unit");
        lua.DoString("assert(g_unit_count == 2, 'unit count should be 2')");
        lua.DoString("print('Lua verified: g_unit_count = ' .. g_unit_count)");

        Console.WriteLine("=== BreadLua Sample completed successfully ===");
    }
}
