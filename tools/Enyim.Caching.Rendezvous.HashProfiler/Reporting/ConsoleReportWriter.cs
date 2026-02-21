// Licensed under the Apache License, Version 2.0.
// See LICENSE file in the project root for full license information.

namespace Enyim.Caching.Rendezvous.HashProfiler.Reporting;

public sealed class ConsoleReportWriter : IReportWriter
{
    public void Write(ProfileReport report, string outputDir)
    {
        Console.WriteLine();
        Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║               Hash Quality Profile Results                  ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");

        foreach (var (name, results) in report.Results)
        {
            Console.WriteLine();
            Console.WriteLine($"  Algorithm: {name}");
            Console.WriteLine("  ─────────────────────────────────────────────────────");

            WriteStatus("Distribution", results.Distribution.Passed,
                $"χ²={results.Distribution.ChiSquared:F2}  p={results.Distribution.PValue:F4}  max-dev={results.Distribution.MaxDeviationPercent:F1}%");

            WriteStatus("Avalanche", results.Avalanche.Passed,
                $"mean-flip={results.Avalanche.MeanBitFlipPercent:F1}%  σ={results.Avalanche.StdDev:F2}  range=[{results.Avalanche.MinBitsFlipped},{results.Avalanche.MaxBitsFlipped}]");

            WriteStatus("Monotonicity", results.Monotonicity.Passed,
                $"moved-to-new={results.Monotonicity.MovedToNewNode}  between-existing={results.Monotonicity.MovedBetweenExisting}  spurious={results.Monotonicity.SpuriousMoves}");

            WriteStatus("Collisions", results.Collision.Passed,
                $"actual={results.Collision.Collisions}  expected≈{results.Collision.ExpectedCollisions:F1}  unique={results.Collision.UniqueHashes}/{results.Collision.TotalKeys}");
        }

        Console.WriteLine();
        Console.WriteLine("  ═══════════════════════════════════════════════════════");
        var allPassed = report.PassesAllThresholds();
        var saved = Console.ForegroundColor;
        Console.ForegroundColor = allPassed ? ConsoleColor.Green : ConsoleColor.Red;
        Console.WriteLine($"  Overall: {(allPassed ? "ALL PASSED" : "SOME FAILED")}");
        Console.ForegroundColor = saved;
        Console.WriteLine();
    }

    private static void WriteStatus(string testName, bool passed, string details)
    {
        var saved = Console.ForegroundColor;
        Console.ForegroundColor = passed ? ConsoleColor.Green : ConsoleColor.Red;
        string indicator = passed ? "PASS" : "FAIL";
        Console.Write($"    [{indicator}] ");
        Console.ForegroundColor = saved;
        Console.WriteLine($"{testName,-15} {details}");
    }
}
