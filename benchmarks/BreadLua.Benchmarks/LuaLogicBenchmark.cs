using System;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BreadPack.NativeLua;
using BreadPack.NativeLua.Native;

namespace BreadLua.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class LuaLogicBenchmark
{
    private LuaState lua = null!;

    // C# side data for pure C# benchmarks
    private float[] unitHp = null!;
    private float[] unitAtk = null!;
    private float[] unitDef = null!;
    private bool[] unitAlive = null!;
    private const int UnitCount = 50;
    private const float DefenseConstant = 150f;

    [GlobalSetup]
    public void Setup()
    {
        lua = new LuaState();

        // Initialize C# arrays
        unitHp = new float[UnitCount];
        unitAtk = new float[UnitCount];
        unitDef = new float[UnitCount];
        unitAlive = new bool[UnitCount];

        var rng = new Random(42);
        for (int i = 0; i < UnitCount; i++)
        {
            unitHp[i] = 100f + rng.Next(200);
            unitAtk[i] = 20f + rng.Next(30);
            unitDef[i] = 5f + rng.Next(20);
            unitAlive[i] = true;
        }

        // Load Lua-side data and logic
        lua.DoString(@"
            -- Unit data (mirrors C# arrays)
            units = {}
            math.randomseed(42)
            for i = 1, " + UnitCount + @" do
                units[i] = {
                    hp = 100 + math.random(200),
                    atk = 20 + math.random(30),
                    def = 5 + math.random(20),
                    alive = true
                }
            end

            DEF_CONSTANT = 150.0

            -- Damage calculation (same formula as C#)
            function calc_damage(atk, def)
                return atk * (1.0 - def / (def + DEF_CONSTANT))
            end

            -- Simple AI: find nearest alive target with lowest HP
            function find_best_target(attacker_idx)
                local best_idx = -1
                local best_hp = 999999
                for i = 1, #units do
                    if i ~= attacker_idx and units[i].alive and units[i].hp < best_hp then
                        best_hp = units[i].hp
                        best_idx = i
                    end
                end
                return best_idx
            end

            -- Full battle frame: each unit attacks best target
            function battle_frame()
                local total_damage = 0
                for i = 1, #units do
                    if units[i].alive then
                        local target = find_best_target(i)
                        if target > 0 then
                            local dmg = calc_damage(units[i].atk, units[target].def)
                            units[target].hp = units[target].hp - dmg
                            total_damage = total_damage + dmg
                            if units[target].hp <= 0 then
                                units[target].alive = false
                            end
                        end
                    end
                end
                return total_damage
            end

            -- Damage calculation only (no AI, no state mutation)
            function damage_calc_loop()
                local total = 0
                for i = 1, #units do
                    for j = 1, #units do
                        if i ~= j then
                            total = total + calc_damage(units[i].atk, units[j].def)
                        end
                    end
                end
                return total
            end

            -- Reset units for next benchmark iteration
            function reset_units()
                math.randomseed(42)
                for i = 1, #units do
                    units[i].hp = 100 + math.random(200)
                    units[i].atk = 20 + math.random(30)
                    units[i].def = 5 + math.random(20)
                    units[i].alive = true
                end
            end

            -- Fibonacci (pure computation, no data access)
            function fib(n)
                if n <= 1 then return n end
                return fib(n-1) + fib(n-2)
            end

            -- String manipulation
            function string_work()
                local result = ''
                for i = 1, 100 do
                    result = result .. 'unit_' .. tostring(i) .. ','
                end
                return #result
            end
        ");
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        lua?.Dispose();
    }

    // ==========================================
    // Damage Calculation: Lua vs C#
    // ==========================================

    [Benchmark(Baseline = true, Description = "C# damage calc (50x50 pairs)")]
    public float CSharp_DamageCalcLoop()
    {
        float total = 0;
        for (int i = 0; i < UnitCount; i++)
        {
            for (int j = 0; j < UnitCount; j++)
            {
                if (i != j)
                {
                    total += unitAtk[i] * (1f - unitDef[j] / (unitDef[j] + DefenseConstant));
                }
            }
        }
        return total;
    }

    [Benchmark(Description = "Lua damage calc (50x50 pairs, single pcall)")]
    public void Lua_DamageCalcLoop()
    {
        lua.Call("damage_calc_loop");
    }

    // ==========================================
    // Battle Frame: AI + Damage + State Mutation
    // ==========================================

    [Benchmark(Description = "C# battle frame (50 units, AI + damage)")]
    public float CSharp_BattleFrame()
    {
        // Reset
        var rng = new Random(42);
        for (int i = 0; i < UnitCount; i++)
        {
            unitHp[i] = 100f + rng.Next(200);
            unitAtk[i] = 20f + rng.Next(30);
            unitDef[i] = 5f + rng.Next(20);
            unitAlive[i] = true;
        }

        float totalDamage = 0;
        for (int i = 0; i < UnitCount; i++)
        {
            if (!unitAlive[i]) continue;

            // Find best target (lowest HP alive)
            int bestIdx = -1;
            float bestHp = float.MaxValue;
            for (int j = 0; j < UnitCount; j++)
            {
                if (j != i && unitAlive[j] && unitHp[j] < bestHp)
                {
                    bestHp = unitHp[j];
                    bestIdx = j;
                }
            }

            if (bestIdx >= 0)
            {
                float dmg = unitAtk[i] * (1f - unitDef[bestIdx] / (unitDef[bestIdx] + DefenseConstant));
                unitHp[bestIdx] -= dmg;
                totalDamage += dmg;
                if (unitHp[bestIdx] <= 0)
                    unitAlive[bestIdx] = false;
            }
        }
        return totalDamage;
    }

    [Benchmark(Description = "Lua battle frame (50 units, AI + damage, single pcall)")]
    public void Lua_BattleFrame()
    {
        lua.Call("reset_units");
        lua.Call("battle_frame");
    }

    // ==========================================
    // Pure Computation: Fibonacci
    // ==========================================

    [Benchmark(Description = "C# fibonacci(25)")]
    public int CSharp_Fibonacci()
    {
        return Fib(25);
    }

    [Benchmark(Description = "Lua fibonacci(25) (single pcall)")]
    public void Lua_Fibonacci()
    {
        lua.DoString("fib(25)");
    }

    private static int Fib(int n)
    {
        if (n <= 1) return n;
        return Fib(n - 1) + Fib(n - 2);
    }

    // ==========================================
    // String Work
    // ==========================================

    [Benchmark(Description = "C# string concat x100")]
    public int CSharp_StringWork()
    {
        string result = "";
        for (int i = 1; i <= 100; i++)
        {
            result += "unit_" + i.ToString() + ",";
        }
        return result.Length;
    }

    [Benchmark(Description = "Lua string concat x100 (single pcall)")]
    public void Lua_StringWork()
    {
        lua.Call("string_work");
    }
}
