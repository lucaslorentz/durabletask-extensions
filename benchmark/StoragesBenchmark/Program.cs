using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace StoragesBenchmark
{
    class Program
    {
        static void Main(string[] args)
        {
            var summary = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly)
                .Run(args, ManualConfig
                    .Create(DefaultConfig.Instance)
                    .WithOption(ConfigOptions.DisableOptimizationsValidator, true));
        }
    }
}
