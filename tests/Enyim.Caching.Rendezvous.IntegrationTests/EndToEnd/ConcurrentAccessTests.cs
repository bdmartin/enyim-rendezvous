// Licensed under the Apache License, Version 2.0.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Enyim.Caching.Memcached;
using Moq;
using Xunit;

namespace Enyim.Caching.Rendezvous.IntegrationTests.EndToEnd
{
    public class ConcurrentAccessTests
    {
        private static IMemcachedNode CreateMockNode(string address, int port, bool isAlive = true)
        {
            var mock = new Mock<IMemcachedNode>();
            mock.Setup(n => n.EndPoint).Returns(new IPEndPoint(IPAddress.Parse(address), port));
            mock.Setup(n => n.IsAlive).Returns(isAlive);
            return mock.Object;
        }

        [Fact]
        public void ConcurrentLocate_NoExceptions()
        {
            var nodes = new List<IMemcachedNode>();
            for (int i = 1; i <= 5; i++)
                nodes.Add(CreateMockNode($"10.0.0.{i}", 11211));

            var locator = new RendezvousNodeLocator();
            locator.Initialize(nodes);

            var exceptions = new List<Exception>();
            var threads = new Thread[10];

            for (int t = 0; t < 10; t++)
            {
                var threadIndex = t;
                threads[t] = new Thread(() =>
                {
                    try
                    {
                        var rng = new Random(threadIndex);
                        for (int i = 0; i < 10000; i++)
                        {
                            locator.Locate($"key-{rng.Next(100000)}");
                        }
                    }
                    catch (Exception ex)
                    {
                        lock (exceptions) { exceptions.Add(ex); }
                    }
                });
            }

            foreach (var thread in threads) thread.Start();
            foreach (var thread in threads) thread.Join();

            Assert.Empty(exceptions);
        }

        [Fact]
        public void ConcurrentLocateAndReInitialize_NoExceptions()
        {
            var nodeSets = new List<IMemcachedNode>[3];
            for (int s = 0; s < 3; s++)
            {
                nodeSets[s] = new List<IMemcachedNode>();
                for (int i = 1; i <= 3 + s; i++)
                    nodeSets[s].Add(CreateMockNode($"10.{s}.0.{i}", 11211));
            }

            var locator = new RendezvousNodeLocator();
            locator.Initialize(nodeSets[0]);

            var exceptions = new List<Exception>();
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

            // 5 reader threads
            var readers = new Thread[5];
            for (int t = 0; t < 5; t++)
            {
                var threadIndex = t;
                readers[t] = new Thread(() =>
                {
                    try
                    {
                        var rng = new Random(threadIndex);
                        while (!cts.Token.IsCancellationRequested)
                        {
                            locator.Locate($"key-{rng.Next(100000)}");
                        }
                    }
                    catch (Exception ex)
                    {
                        lock (exceptions) { exceptions.Add(ex); }
                    }
                });
            }

            // 2 writer threads
            var writers = new Thread[2];
            for (int t = 0; t < 2; t++)
            {
                var threadIndex = t;
                writers[t] = new Thread(() =>
                {
                    try
                    {
                        int idx = threadIndex;
                        while (!cts.Token.IsCancellationRequested)
                        {
                            idx = (idx + 1) % nodeSets.Length;
                            locator.Initialize(nodeSets[idx]);
                            Thread.Sleep(1); // small yield
                        }
                    }
                    catch (Exception ex)
                    {
                        lock (exceptions) { exceptions.Add(ex); }
                    }
                });
            }

            foreach (var thread in readers) thread.Start();
            foreach (var thread in writers) thread.Start();

            cts.Token.WaitHandle.WaitOne();

            foreach (var thread in readers) thread.Join(TimeSpan.FromSeconds(5));
            foreach (var thread in writers) thread.Join(TimeSpan.FromSeconds(5));

            Assert.Empty(exceptions);
        }

        [Fact]
        public void DisposeWhileLocating_ThrowsObjectDisposedOrCompletes()
        {
            var nodes = new List<IMemcachedNode>();
            for (int i = 1; i <= 5; i++)
                nodes.Add(CreateMockNode($"10.0.0.{i}", 11211));

            var locator = new RendezvousNodeLocator();
            locator.Initialize(nodes);

            var exceptions = new List<Exception>();
            var startBarrier = new ManualResetEventSlim(false);

            // Reader threads that locate keys
            var readers = new Thread[5];
            for (int t = 0; t < 5; t++)
            {
                readers[t] = new Thread(() =>
                {
                    startBarrier.Wait();
                    for (int i = 0; i < 10000; i++)
                    {
                        try
                        {
                            locator.Locate($"key-{i}");
                        }
                        catch (ObjectDisposedException)
                        {
                            // Expected after dispose
                        }
                        catch (Exception ex)
                        {
                            lock (exceptions) { exceptions.Add(ex); }
                        }
                    }
                });
            }

            foreach (var thread in readers) thread.Start();

            // Let readers start, then dispose
            startBarrier.Set();
            Thread.Sleep(1);
            locator.Dispose();

            foreach (var thread in readers) thread.Join(TimeSpan.FromSeconds(5));

            Assert.Empty(exceptions);
        }
    }
}
