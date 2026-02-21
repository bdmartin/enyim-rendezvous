# Enyim.Caching.Rendezvous

A **Rendezvous hashing** (Highest Random Weight / HRW) node locator for [EnyimMemcachedCore](https://github.com/cnblogs/EnyimMemcachedCore), with **AWS ElastiCache auto-discovery** support.

## What is Rendezvous Hashing?

Rendezvous hashing is an alternative to consistent hashing (Ketama) for distributing keys across cache nodes. For each key, every node is scored using a hash of the key combined with the node identifier, and the highest-scoring node wins.

**Advantages over consistent hashing:**
- **Simpler** — no hash ring or virtual nodes required
- **Minimal disruption** — when a node is added or removed, only keys mapping to that node are redistributed (same guarantee as consistent hashing)
- **Even distribution** — naturally uniform without tuning virtual node counts
- **Easy to reason about** — the algorithm is straightforward to understand and debug

## Installation

```bash
dotnet add package Enyim.Caching.Rendezvous
```

Or add to your `.csproj`:

```xml
<PackageReference Include="Enyim.Caching.Rendezvous" Version="1.0.0" />
```

## Quick Start

### Basic Usage with EnyimMemcachedCore

```csharp
services.AddEnyimMemcached(options =>
{
    options.Servers.Add(new Server { Address = "10.0.0.1", Port = 11211 });
    options.Servers.Add(new Server { Address = "10.0.0.2", Port = 11211 });
    options.Servers.Add(new Server { Address = "10.0.0.3", Port = 11211 });

    // Use Rendezvous hashing instead of the default Ketama
    options.NodeLocatorFactory = new RendezvousNodeLocatorFactory();
});
```

### Choosing a Hash Algorithm

Three hash algorithms are included. Pass one to the factory constructor:

```csharp
using Enyim.Caching.Rendezvous;
using Enyim.Caching.Rendezvous.Hashing;

// FNV-1a (default) — fast, non-cryptographic, good distribution
options.NodeLocatorFactory = new RendezvousNodeLocatorFactory();

// MurmurHash3 — excellent distribution, very fast
options.NodeLocatorFactory = new RendezvousNodeLocatorFactory(
    new MurmurHash3RendezvousHash());

// SHA-256 — cryptographic quality uniformity, higher CPU cost
options.NodeLocatorFactory = new RendezvousNodeLocatorFactory(
    new Sha256RendezvousHash());
```

### Custom Hash Algorithm

Implement the `IRendezvousHash` interface:

```csharp
public class MyCustomHash : IRendezvousHash
{
    public uint ComputeHash(string key, string node)
    {
        // Combine key + node and return a 32-bit hash score
        // The node with the highest score for a given key wins
    }
}

options.NodeLocatorFactory = new RendezvousNodeLocatorFactory(new MyCustomHash());
```

## AWS ElastiCache Auto-Discovery

When using AWS ElastiCache (Memcached), the library can automatically discover cluster nodes by polling the [configuration endpoint](https://docs.aws.amazon.com/AmazonElastiCache/latest/dg/AutoDiscovery.AddingToYourClientLibrary.html).

```csharp
using Enyim.Caching.Rendezvous.ElastiCache;

var discoveryOptions = new ElastiCacheDiscoveryOptions
{
    ConfigurationEndpoint = new IPEndPoint(
        IPAddress.Parse("10.0.0.1"),  // your cluster config endpoint IP
        11211),
    PollingInterval = TimeSpan.FromSeconds(60),
    ConnectionTimeout = TimeSpan.FromSeconds(5),
    ReceiveTimeout = TimeSpan.FromSeconds(5)
};

var discovery = new ElastiCacheDiscoveryService(discoveryOptions);

// React to node changes
discovery.NodesChanged += (sender, args) =>
{
    Console.WriteLine($"Config version {args.ConfigVersion}: {args.Nodes.Count} node(s)");
    foreach (var node in args.Nodes)
        Console.WriteLine($"  {node.HostName} ({node.IpAddress}:{node.Port})");
};

// Start background polling
discovery.Start();

// Or poll on-demand
var nodes = discovery.Poll();
```

### Parsing Config Responses Directly

If you prefer to manage the connection yourself, use `ClusterConfigParser`:

```csharp
using Enyim.Caching.Rendezvous.ElastiCache;

string response = /* raw response from "config get cluster" command */;
var (version, nodes) = ClusterConfigParser.Parse(response);
```

## Target Framework

- **.NET Standard 2.0** — compatible with .NET Framework 4.6.1+, .NET Core 2.0+, .NET 5+

## Dependencies

- [EnyimMemcachedCore](https://www.nuget.org/packages/EnyimMemcachedCore/) 2.5.4

## How It Works

For a key lookup, the algorithm:

1. Iterates through all alive nodes
2. Computes `score = hash(key, node_endpoint)` for each node
3. Returns the node with the **highest** score

When a node is added, only ~1/N keys (where N is the new total) move to the new node. When a node is removed, only the keys that were on that node get redistributed — all other mappings remain stable.

## License

[Apache License 2.0](LICENSE)
