// Copyright © Erickson Lopez. MIT License.
using System;
using AwesomeAssertions;
using EricksonLopez.Concurrency.Abstractions;
using EricksonLopez.Concurrency.SqlServer;
using Xunit;

namespace EricksonLopez.Concurrency.SqlServer.Tests;

public sealed class SqlServerRowVersionTokenTests
{
    private sealed class CustomTokenStub : IConcurrencyToken
    {
        public string Value { get; init; } = string.Empty;
        public string TokenKind { get; init; } = string.Empty;
        public bool IsEmpty => string.IsNullOrEmpty(Value);
        public bool Equals(IConcurrencyToken? other) => other is not null && Value == other.Value;
    }

    [Fact]
    public void SqlServerRowVersionToken_ByteArrayConstructor_ShouldInitializeProperly()
    {
        byte[] raw = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x07, 0xD0 };
        var token = new SqlServerRowVersionToken(raw);

        token.IsEmpty.Should().BeFalse();
        token.Value.Should().Be("00000000000007D0");
        token.TokenKind.Should().Be("SqlServer.RowVersion");
        token.ToString().Should().Be("[RowVersion:0x00000000000007D0]");

        byte[] returnedBytes = token.ToByteArray();
        returnedBytes.Should().Equal(raw);

        // Mutating returned array should not affect token
        returnedBytes[0] = 0xFF;
        token.Value.Should().Be("00000000000007D0");
    }

    [Fact]
    public void SqlServerRowVersionToken_SpanConstructor_ShouldInitializeProperly()
    {
        ReadOnlySpan<byte> span = stackalloc byte[] { 0x01, 0x02, 0x03, 0x04 };
        var token = new SqlServerRowVersionToken(span);

        token.IsEmpty.Should().BeFalse();
        token.Value.Should().Be("01020304");

        var emptySpanToken = new SqlServerRowVersionToken(ReadOnlySpan<byte>.Empty);
        emptySpanToken.IsEmpty.Should().BeTrue();
        emptySpanToken.Value.Should().Be(string.Empty);
        emptySpanToken.ToByteArray().Should().BeEmpty();
    }

    [Fact]
    public void SqlServerRowVersionToken_NullOrEmptyConstructor_ShouldBeEmpty()
    {
        var nullToken = new SqlServerRowVersionToken((byte[]?)null);
        nullToken.IsEmpty.Should().BeTrue();
        nullToken.Value.Should().Be(string.Empty);
        nullToken.ToByteArray().Should().BeEmpty();
        nullToken.ToString().Should().Be("[RowVersion:Empty]");

        var emptyToken = new SqlServerRowVersionToken(Array.Empty<byte>());
        emptyToken.IsEmpty.Should().BeTrue();
        emptyToken.ToByteArray().Should().BeEmpty();

        var defaultToken = default(SqlServerRowVersionToken);
        defaultToken.IsEmpty.Should().BeTrue();
        defaultToken.Value.Should().Be(string.Empty);
        defaultToken.ToByteArray().Should().BeEmpty();
        defaultToken.ToString().Should().Be("[RowVersion:Empty]");

        var noneToken = SqlServerRowVersionToken.None;
        noneToken.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void Parse_ValidHexStrings_ShouldParseCorrectly()
    {
        var tokenWith0x = SqlServerRowVersionToken.Parse("0x00000000000007D0");
        tokenWith0x.Value.Should().Be("00000000000007D0");

        var tokenWithout0x = SqlServerRowVersionToken.Parse("00000000000007D0");
        tokenWithout0x.Value.Should().Be("00000000000007D0");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_NullOrWhiteSpace_ShouldThrowArgumentException(string? hex)
    {
        Action act = () => SqlServerRowVersionToken.Parse(hex!);
        act.Should().Throw<ArgumentException>()
            .WithParameterName("hexString");
    }

    [Fact]
    public void Equals_WithIConcurrencyTokenAndTyped_ShouldEvaluateAllBranches()
    {
        var t1 = SqlServerRowVersionToken.Parse("0x01");
        var t2 = SqlServerRowVersionToken.Parse("0x01");
        var t3 = SqlServerRowVersionToken.Parse("0x02");

        t1.Equals((IConcurrencyToken?)null).Should().BeFalse();
        t1.Equals((IConcurrencyToken)t2).Should().BeTrue();
        t1.Equals((IConcurrencyToken)t3).Should().BeFalse();

        var customStub = new CustomTokenStub { Value = "01" };
        t1.Equals((IConcurrencyToken)customStub).Should().BeTrue();

        t1.Equals(t2).Should().BeTrue();
        t1.Equals(t3).Should().BeFalse();

        t1.Equals((object?)null).Should().BeFalse();
        t1.Equals((object)t2).Should().BeTrue();
        t1.Equals("01").Should().BeFalse();

        t1.GetHashCode().Should().Be(string.GetHashCode("01", StringComparison.Ordinal));
    }

    [Fact]
    public void CompareTo_AndOperators_ShouldWorkAccurately()
    {
        var t1 = SqlServerRowVersionToken.Parse("0x01");
        var t2 = SqlServerRowVersionToken.Parse("0x01");
        var t3 = SqlServerRowVersionToken.Parse("0x02");

        t1.CompareTo(t2).Should().Be(0);
        t1.CompareTo(t3).Should().BeLessThan(0);
        t3.CompareTo(t1).Should().BeGreaterThan(0);

        t1.CompareTo((object?)null).Should().Be(1);
        t1.CompareTo((object)t2).Should().Be(0);

        Action act = () => t1.CompareTo("invalid");
        act.Should().Throw<ArgumentException>()
            .WithParameterName("obj")
            .WithMessage("*Object must be of type SqlServerRowVersionToken.*");

        (t1 == t2).Should().BeTrue();
        (t1 == t3).Should().BeFalse();
        (t1 != t3).Should().BeTrue();
        (t1 != t2).Should().BeFalse();

        (t1 < t3).Should().BeTrue();
        (t3 < t1).Should().BeFalse();
        (t1 < t2).Should().BeFalse();
        (t2 < t1).Should().BeFalse();

        (t1 <= t3).Should().BeTrue();
        (t1 <= t2).Should().BeTrue();
        (t3 <= t1).Should().BeFalse();

        (t3 > t1).Should().BeTrue();
        (t1 > t3).Should().BeFalse();
        (t1 > t2).Should().BeFalse();
        (t2 > t1).Should().BeFalse();

        (t3 >= t1).Should().BeTrue();
        (t1 >= t2).Should().BeTrue();
        (t1 >= t3).Should().BeFalse();
    }
}
