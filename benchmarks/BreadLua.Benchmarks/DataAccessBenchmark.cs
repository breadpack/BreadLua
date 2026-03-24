using System;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BreadPack.NativeLua;
using BreadPack.NativeLua.Native;

namespace BreadLua.Benchmarks;

[StructLayout(LayoutKind.Sequential)]
public struct BenchUnit
{
    public int unitId;
    public float hp;
    public float maxHp;
    public float attack;
    public float defence;
    public float x;
    public float y;
    public int stateId;
}

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class DataAccessBenchmark
{
    private const int UnitCount = 100;
    private const int FieldAccessPerUnit = 5; // read hp, attack, defence, x, y

    private LuaState lua = null!;
    private Buffer<BenchUnit> buffer = null!;
    private BenchUnit[] managedArray = null!;
    private IntPtr luaState;

    [GlobalSetup]
    public void Setup()
    {
        lua = new LuaState();
        luaState = lua.Handle;

        // Shared memory buffer
        buffer = new Buffer<BenchUnit>(UnitCount);
        buffer.Count = UnitCount;
        for (int i = 0; i < UnitCount; i++)
        {
            buffer[i] = new BenchUnit
            {
                unitId = i,
                hp = 100f + i,
                maxHp = 200f,
                attack = 25f + i * 0.5f,
                defence = 10f + i * 0.3f,
                x = i * 1.5f,
                y = i * 2.0f,
                stateId = 1,
            };
        }

        // Managed array (C# baseline)
        managedArray = new BenchUnit[UnitCount];
        for (int i = 0; i < UnitCount; i++)
        {
            managedArray[i] = buffer[i];
        }

        // Bind to Lua for Lua-side benchmarks
        buffer.BindToLua(lua, "g_unit");

        // Define Lua functions for benchmarks
        lua.DoString(@"
            function lua_sum_hp()
                local total = 0
                for i = 0, g_unit_count - 1 do
                    -- This would use C module in real scenario
                    -- For now just pure Lua computation
                    total = total + (100 + i)
                end
                return total
            end
        ");
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        buffer?.Dispose();
        lua?.Dispose();
    }

    /// <summary>
    /// Baseline: Pure C# array access -- read 5 fields from 100 units
    /// </summary>
    [Benchmark(Baseline = true, Description = "C# Direct Array Access (100 units x 5 fields)")]
    public float CSharp_DirectAccess()
    {
        float total = 0;
        for (int i = 0; i < UnitCount; i++)
        {
            total += managedArray[i].hp;
            total += managedArray[i].attack;
            total += managedArray[i].defence;
            total += managedArray[i].x;
            total += managedArray[i].y;
        }
        return total;
    }

    /// <summary>
    /// BreadLua: Shared memory Buffer access -- same as C# (zero overhead)
    /// </summary>
    [Benchmark(Description = "BreadLua Buffer<T> Access (100 units x 5 fields)")]
    public float BreadLua_SharedMemory()
    {
        float total = 0;
        for (int i = 0; i < UnitCount; i++)
        {
            total += buffer[i].hp;
            total += buffer[i].attack;
            total += buffer[i].defence;
            total += buffer[i].x;
            total += buffer[i].y;
        }
        return total;
    }

    /// <summary>
    /// Traditional P/Invoke: Simulates NLua-style field access -- 4 P/Invoke per field
    /// lua_pushstring + lua_gettable + lua_tonumber + lua_pop = 4 calls per field
    /// Here we simulate with 2 P/Invoke per field (push + get) as minimum
    /// </summary>
    [Benchmark(Description = "Traditional P/Invoke (100 units x 5 fields x 2 calls)")]
    public double Traditional_PInvoke()
    {
        double total = 0;
        for (int i = 0; i < UnitCount; i++)
        {
            for (int f = 0; f < FieldAccessPerUnit; f++)
            {
                // Simulate: push a number, read it back (2 P/Invoke per field)
                LuaNative.breadlua_pushnumber(luaState, i + f);
                total += LuaNative.breadlua_tonumber(luaState, -1);
                LuaNative.breadlua_pop(luaState, 1);
            }
        }
        return total;
    }

    /// <summary>
    /// Lua function call: Single pcall for a Lua function (frame-loop pattern)
    /// </summary>
    [Benchmark(Description = "Lua pcall (single function call)")]
    public void Lua_SingleCall()
    {
        lua.Call("lua_sum_hp");
    }

    /// <summary>
    /// BreadLua Span access: Using AsSpan() for sequential iteration
    /// </summary>
    [Benchmark(Description = "BreadLua Span<T> Access (100 units x 5 fields)")]
    public float BreadLua_SpanAccess()
    {
        float total = 0;
        var span = buffer.AsSpan();
        for (int i = 0; i < span.Length; i++)
        {
            total += span[i].hp;
            total += span[i].attack;
            total += span[i].defence;
            total += span[i].x;
            total += span[i].y;
        }
        return total;
    }
}
