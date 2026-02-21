// Licensed under the Apache License, Version 2.0.
// See LICENSE file in the project root for full license information.

using System;
using System.Net;
using Enyim.Caching.Rendezvous.ElastiCache;
using Xunit;

namespace Enyim.Caching.Rendezvous.Tests
{
    public class ClusterConfigParserTests
    {
        [Fact]
        public void Parse_ValidTwoNodeResponse()
        {
            var response =
                "CONFIG cluster 0 136\r\n" +
                "12\n" +
                "myCluster.pc4ldq.0001.use1.cache.amazonaws.com|10.82.235.120|11211 myCluster.pc4ldq.0002.use1.cache.amazonaws.com|10.80.249.27|11211\n\r\n" +
                "END\r\n";

            var (version, nodes) = ClusterConfigParser.Parse(response);

            Assert.Equal(12, version);
            Assert.Equal(2, nodes.Count);

            Assert.Equal("myCluster.pc4ldq.0001.use1.cache.amazonaws.com", nodes[0].HostName);
            Assert.Equal(IPAddress.Parse("10.82.235.120"), nodes[0].IpAddress);
            Assert.Equal(11211, nodes[0].Port);

            Assert.Equal("myCluster.pc4ldq.0002.use1.cache.amazonaws.com", nodes[1].HostName);
            Assert.Equal(IPAddress.Parse("10.80.249.27"), nodes[1].IpAddress);
            Assert.Equal(11211, nodes[1].Port);
        }

        [Fact]
        public void Parse_SingleNodeResponse()
        {
            var response =
                "CONFIG cluster 0 80\r\n" +
                "1\n" +
                "mycluster.abc123.0001.use1.cache.amazonaws.com|192.168.1.10|11211\n\r\n" +
                "END\r\n";

            var (version, nodes) = ClusterConfigParser.Parse(response);

            Assert.Equal(1, version);
            Assert.Single(nodes);
            Assert.Equal("mycluster.abc123.0001.use1.cache.amazonaws.com", nodes[0].HostName);
            Assert.Equal(IPAddress.Parse("192.168.1.10"), nodes[0].IpAddress);
            Assert.Equal(11211, nodes[0].Port);
        }

        [Fact]
        public void Parse_ThreeNodesWithDifferentPorts()
        {
            var response =
                "CONFIG cluster 0 200\r\n" +
                "5\n" +
                "a.cache.amazonaws.com|10.0.0.1|11211 b.cache.amazonaws.com|10.0.0.2|11212 c.cache.amazonaws.com|10.0.0.3|11213\n\r\n" +
                "END\r\n";

            var (version, nodes) = ClusterConfigParser.Parse(response);

            Assert.Equal(5, version);
            Assert.Equal(3, nodes.Count);
            Assert.Equal(11211, nodes[0].Port);
            Assert.Equal(11212, nodes[1].Port);
            Assert.Equal(11213, nodes[2].Port);
        }

        [Fact]
        public void Parse_EmptyResponse_Throws()
        {
            Assert.Throws<ArgumentException>(() => ClusterConfigParser.Parse(""));
            Assert.Throws<ArgumentException>(() => ClusterConfigParser.Parse(null));
        }

        [Fact]
        public void Parse_MalformedResponse_TooFewLines_Throws()
        {
            var response = "CONFIG cluster 0 10\r\n";
            Assert.Throws<FormatException>(() => ClusterConfigParser.Parse(response));
        }

        [Fact]
        public void Parse_InvalidVersion_Throws()
        {
            var response =
                "CONFIG cluster 0 80\r\n" +
                "not-a-number\n" +
                "a.cache.amazonaws.com|10.0.0.1|11211\n\r\n" +
                "END\r\n";

            Assert.Throws<FormatException>(() => ClusterConfigParser.Parse(response));
        }

        [Fact]
        public void Parse_InvalidNodeFormat_Throws()
        {
            var response =
                "CONFIG cluster 0 80\r\n" +
                "1\n" +
                "a.cache.amazonaws.com|not-an-ip|11211\n\r\n" +
                "END\r\n";

            Assert.Throws<FormatException>(() => ClusterConfigParser.Parse(response));
        }

        [Fact]
        public void Parse_InvalidPort_Throws()
        {
            var response =
                "CONFIG cluster 0 80\r\n" +
                "1\n" +
                "a.cache.amazonaws.com|10.0.0.1|99999\n\r\n" +
                "END\r\n";

            Assert.Throws<FormatException>(() => ClusterConfigParser.Parse(response));
        }

        [Fact]
        public void Parse_MissingPipeSeparator_Throws()
        {
            var response =
                "CONFIG cluster 0 80\r\n" +
                "1\n" +
                "a.cache.amazonaws.com:10.0.0.1:11211\n\r\n" +
                "END\r\n";

            Assert.Throws<FormatException>(() => ClusterConfigParser.Parse(response));
        }

        [Fact]
        public void ClusterNode_Equality()
        {
            var node1 = new ClusterNode("host.com", IPAddress.Parse("10.0.0.1"), 11211);
            var node2 = new ClusterNode("host.com", IPAddress.Parse("10.0.0.1"), 11211);
            var node3 = new ClusterNode("other.com", IPAddress.Parse("10.0.0.2"), 11211);

            Assert.Equal(node1, node2);
            Assert.NotEqual(node1, node3);
            Assert.Equal(node1.GetHashCode(), node2.GetHashCode());
        }

        [Fact]
        public void ClusterNode_ToString()
        {
            var node = new ClusterNode("host.com", IPAddress.Parse("10.0.0.1"), 11211);
            Assert.Equal("host.com|10.0.0.1|11211", node.ToString());
        }
    }
}
