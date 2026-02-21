// Licensed under the Apache License, Version 2.0.
// See LICENSE file in the project root for full license information.

namespace Enyim.Caching.Rendezvous.HashProfiler.Analysis;

public sealed class MonotonicityResult
{
    public required int MovedToNewNode { get; init; }
    public required int MovedBetweenExisting { get; init; }
    public required int SpuriousMoves { get; init; }
    public required double ActualMovePercent { get; init; }
    public required double ExpectedMovePercent { get; init; }
    public required bool Passed { get; init; }
}

public static class MonotonicityAnalyzer
{
    public static MonotonicityResult Analyze(IRendezvousHash hash, int keyCount, int nodeCount)
    {
        var originalNodes = new string[nodeCount];
        for (int i = 0; i < nodeCount; i++)
            originalNodes[i] = $"10.0.0.{i + 1}:11211";

        string newNode = $"10.0.0.{nodeCount + 1}:11211";

        var expandedNodes = new string[nodeCount + 1];
        Array.Copy(originalNodes, expandedNodes, nodeCount);
        expandedNodes[nodeCount] = newNode;

        // Map keys with original nodes
        var originalMapping = new int[keyCount];
        for (int k = 0; k < keyCount; k++)
            originalMapping[k] = FindBestNode(hash, $"key-{k}", originalNodes);

        // Map keys with expanded nodes (original + new node)
        var expandedMapping = new int[keyCount];
        for (int k = 0; k < keyCount; k++)
            expandedMapping[k] = FindBestNode(hash, $"key-{k}", expandedNodes);

        int movedToNew = 0;
        int movedBetweenExisting = 0;

        for (int k = 0; k < keyCount; k++)
        {
            if (expandedMapping[k] != originalMapping[k])
            {
                if (expandedNodes[expandedMapping[k]] == newNode)
                    movedToNew++;
                else
                    movedBetweenExisting++;
            }
        }

        // Test node removal: remove the last original node
        var reducedNodes = new string[nodeCount - 1];
        Array.Copy(originalNodes, reducedNodes, nodeCount - 1);
        string removedNode = originalNodes[nodeCount - 1];

        var reducedMapping = new int[keyCount];
        for (int k = 0; k < keyCount; k++)
            reducedMapping[k] = FindBestNode(hash, $"key-{k}", reducedNodes);

        int spuriousMoves = 0;
        for (int k = 0; k < keyCount; k++)
        {
            // Only check keys that were NOT on the removed node
            if (originalNodes[originalMapping[k]] != removedNode)
            {
                // This key should stay on the same node among survivors
                if (reducedNodes[reducedMapping[k]] != originalNodes[originalMapping[k]])
                    spuriousMoves++;
            }
        }

        double actualMovePercent = (double)movedToNew / keyCount * 100.0;
        double expectedMovePercent = 1.0 / (nodeCount + 1) * 100.0;

        return new MonotonicityResult
        {
            MovedToNewNode = movedToNew,
            MovedBetweenExisting = movedBetweenExisting,
            SpuriousMoves = spuriousMoves,
            ActualMovePercent = actualMovePercent,
            ExpectedMovePercent = expectedMovePercent,
            Passed = movedBetweenExisting == 0 && spuriousMoves == 0
        };
    }

    private static int FindBestNode(IRendezvousHash hash, string key, string[] nodes)
    {
        uint bestScore = 0;
        int bestNode = 0;

        for (int n = 0; n < nodes.Length; n++)
        {
            uint score = hash.ComputeHash(key, nodes[n]);
            if (score > bestScore || n == 0)
            {
                bestScore = score;
                bestNode = n;
            }
        }

        return bestNode;
    }
}
