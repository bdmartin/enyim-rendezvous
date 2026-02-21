// Licensed under the Apache License, Version 2.0.
// See LICENSE file in the project root for full license information.

using Enyim.Caching.Rendezvous;
using Enyim.Caching.Rendezvous.Hashing;
using Enyim.Caching.Rendezvous.HashProfiler.Analysis;
using Enyim.Caching.Rendezvous.HashProfiler.Reporting;

// Defaults
string outputDir = Directory.GetCurrentDirectory();
string format = "console";
int keyCount = 100_000;
int nodeCount = 10;
int avalancheIterations = 50_000;

// Parse arguments
for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--output-dir" when i + 1 < args.Length:
            outputDir = args[++i];
            break;
        case "--format" when i + 1 < args.Length:
            format = args[++i];
            break;
        case "--key-count" when i + 1 < args.Length:
            keyCount = int.Parse(args[++i]);
            break;
        case "--node-count" when i + 1 < args.Length:
            nodeCount = int.Parse(args[++i]);
            break;
        case "--avalanche-iterations" when i + 1 < args.Length:
            avalancheIterations = int.Parse(args[++i]);
            break;
        default:
            Console.Error.WriteLine($"Unknown argument: {args[i]}");
            Console.Error.WriteLine("Usage: HashProfiler [--output-dir DIR] [--format console|csv|json] [--key-count N] [--node-count N] [--avalanche-iterations N]");
            return 1;
    }
}

IReportWriter writer = format switch
{
    "console" => new ConsoleReportWriter(),
    "csv" => new CsvReportWriter(),
    "json" => new JsonReportWriter(),
    _ => throw new ArgumentException($"Unknown format: {format}. Use console, csv, or json.")
};

var algorithms = new (string Name, IRendezvousHash Hash)[]
{
    ("FNV-1a", new FnvRendezvousHash()),
    ("MurmurHash3", new MurmurHash3RendezvousHash()),
    ("SHA-256", new Sha256RendezvousHash())
};

var report = new ProfileReport();

foreach (var (name, hash) in algorithms)
{
    Console.Write($"Profiling {name}...");

    var distribution = DistributionAnalyzer.Analyze(hash, keyCount, nodeCount);
    var avalanche = AvalancheAnalyzer.Analyze(hash, avalancheIterations);
    var monotonicity = MonotonicityAnalyzer.Analyze(hash, keyCount, nodeCount);
    var collision = CollisionAnalyzer.Analyze(hash, keyCount);

    report.Results[name] = new AlgorithmResults
    {
        Distribution = distribution,
        Avalanche = avalanche,
        Monotonicity = monotonicity,
        Collision = collision
    };

    Console.WriteLine(" done");
}

writer.Write(report, outputDir);

return report.PassesAllThresholds() ? 0 : 1;
