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
            -- Simulate class objects as tables (plain data)
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

            -- Pattern 1: plain table access (baseline)
            function access_table_pattern()
                local total = 0
                for i = 1, #units_table do
                    local u = units_table[i]
                    total = total + u.hp + u.atk + u.def + u.x + u.y
                end
                return total
            end

            -- Pattern 2: __index metatable (simulates current C getter dispatch via userdata)
            units_meta = {}
            for i = 1, " + UnitCount + @" do
                local data = units_table[i]
                units_meta[i] = setmetatable({}, {
                    __index = function(self, key)
                        return data[key]  -- simulates C getter dispatch via strcmp
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

            -- Pattern 3: bind() closure caching via __index with upvalue
            -- Simulates what bind() produces: a table with __index/__newindex closures
            -- that capture the handle (source data) as upvalue — property syntax, real-time access
            function make_bound_units()
                local bound = {}
                for i = 1, #units_table do
                    local src = units_table[i]  -- captured as upvalue (like GCHandle)
                    bound[i] = setmetatable({}, {
                        __index = function(self, key)
                            return src[key]  -- direct upvalue access, no userdata extraction
                        end,
                        __newindex = function(self, key, value)
                            src[key] = value
                        end,
                    })
                end
                return bound
            end

            bound_units = make_bound_units()

            function access_closure_pattern()
                local total = 0
                for i = 1, #bound_units do
                    local u = bound_units[i]
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

    [Benchmark(Description = "Lua bind() closure access (50 units x 5 fields)")]
    public void LuaBindClosure()
    {
        lua.Call("access_closure_pattern");
    }
}
