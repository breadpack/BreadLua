using BenchmarkDotNet.Running;

namespace BreadLua.Benchmarks;

class Program
{
    static void Main(string[] args)
    {
        BenchmarkRunner.Run<DataAccessBenchmark>();
    }
}
