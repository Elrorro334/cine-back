using System.Net;
using Cine.Application.Helpers;
using FluentAssertions;
using Xunit;

namespace cine_back.Tests.Helpers;

public class IpAddressHelperTests
{
    [Theory]
    [InlineData("192.168.1.200, 10.0.0.1", null, "192.168.1.200")]
    [InlineData("192.168.1.50:54321", null, "192.168.1.50")]
    [InlineData("[2001:db8::1]:8443", null, "2001:db8::1")]
    [InlineData("::ffff:192.168.1.99", null, "192.168.1.99")]
    [InlineData(null, "192.168.1.77", "192.168.1.77")]
    [InlineData(null, null, "127.0.0.1")]
    public void ExtractClientIp_ResolvesExpectedAddress(string? forwardedFor, string? remoteIp, string expected)
    {
        IPAddress? remote = remoteIp != null ? IPAddress.Parse(remoteIp) : null;
        var result = IpAddressHelper.ExtractClientIp(forwardedFor, remote);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("192.168.1.10", "192.168.1.200", true)]
    [InlineData("192.168.2.10", "192.168.1.200", false)]
    [InlineData("2001:db8:85a3:0::1", "2001:db8:85a3:0::99", true)]
    [InlineData("2001:db8:85a3:1::1", "2001:db8:85a3:0::99", false)]
    [InlineData("192.168.1.10", "2001:db8::1", false)]
    [InlineData("invalida", "192.168.1.10", false)]
    public void IsSameSubnet_EvaluatesIpv4AndIpv6Correctly(string ipA, string ipB, bool expected)
    {
        IpAddressHelper.IsSameSubnet(ipA, ipB).Should().Be(expected);
    }

    [Theory]
    [InlineData("2001:0db8:0000:0000:0000:0000:0000:0001", "2001:db8::1", true)]
    [InlineData("::ffff:192.168.1.200", "192.168.1.200", true)]
    [InlineData("192.168.1.200", "192.168.1.201", false)]
    public void AreSameIp_ComparesCanonically(string ipA, string ipB, bool expected)
    {
        IpAddressHelper.AreSameIp(ipA, ipB).Should().Be(expected);
    }
}
