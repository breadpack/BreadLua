using BenchmarkDotNet.Running;

namespace BreadLua.Benchmarks;

class Program
{
    static void Main(string[] args)
    {
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}
