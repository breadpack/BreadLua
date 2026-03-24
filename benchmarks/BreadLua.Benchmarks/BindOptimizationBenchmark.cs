using System;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BreadPack.NativeLua;

namespace BreadLua.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class BindOptimizationBenchmark
{
    private LuaState lua = null!;
    private const int UnitCount = 50;
    private const int PropsPerUnit = 5;

    [GlobalSetup]
    public void Setup()
    {
        lua = new LuaState();

        lua.DoString(@"
            -- Simulate class objects as tables (bind() result)
            units_table = {}
            for i = 1, " + UnitCount + @" do
                units_table[i] = {
                    hp = 100.0 + i,
                    atk = 25.0 + i * 0.5,
                    def = 10.0 + i * 0.3,
                    x = i * 1.5,
                    y = i * 2.0,
                }
            end

            -- Pattern 1: table access (simulates bind() snapshot)
            function access_table_pattern()
                local total = 0
                for i = 1, #units_table do
                    local u = units_table[i]
                    total = total + u.hp + u.atk + u.def + u.x + u.y
                end
                return total
            end

            -- Pattern 2: function call per field (simulates current __index)
            -- Each getter is a function call (simulates C function overhead)
            function make_getter(t, key)
                return function() return t[key] end
            end

            units_meta = {}
            for i = 1, " + UnitCount + @" do
                local data = units_table[i]
                units_meta[i] = setmetatable({}, {
                    __index = function(self, key)
                        return data[key]  -- simulates C getter dispatch
                    end
                })
            end

            function access_meta_pattern()
                local total = 0
                for i = 1, #units_meta do
                    local u = units_meta[i]
                    total = total + u.hp + u.atk + u.def + u.x + u.y
                end
                return total
            end

            -- Pattern 3: bind once, access table
            function bind_then_access_pattern()
                local total = 0
                for i = 1, #units_meta do
                    -- bind(): copy metatable obj to plain table (simulated)
                    local u = {}
                    local src = units_table[i]
                    u.hp = src.hp
                    u.atk = src.atk
                    u.def = src.def
                    u.x = src.x
                    u.y = src.y
                    -- now access plain table
                    total = total + u.hp + u.atk + u.def + u.x + u.y
                end
                return total
            end
        ");
    }

    [GlobalCleanup]
    public void Cleanup() => lua?.Dispose();

    [Benchmark(Baseline = true, Description = "Lua plain table access (50 units x 5 fields)")]
    public void LuaTableDirect()
    {
        lua.Call("access_table_pattern");
    }

    [Benchmark(Description = "Lua __index metatable (50 units x 5 fields)")]
    public void LuaMetatableIndex()
    {
        lua.Call("access_meta_pattern");
    }

    [Benchmark(Description = "Lua bind() snapshot + table access (50 units x 5 fields)")]
    public void LuaBindSnapshot()
    {
        lua.Call("bind_then_access_pattern");
    }
}
