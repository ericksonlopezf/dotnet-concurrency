// Copyright © Erickson Lopez. MIT License.
using System;
using AwesomeAssertions;
using EricksonLopez.Concurrency.Abstractions;
using Xunit;

namespace EricksonLopez.Concurrency.Abstractions.Tests;

public sealed class ConcurrencyTokenTests
{
    [Fact]
    public void ConcurrencyToken_NewGuid_ShouldGenerateValidUniqueTokens()
    {
        ConcurrencyToken token1 = ConcurrencyToken.NewGuid();
        ConcurrencyToken token2 = ConcurrencyToken.NewGuid();

        token1.IsEmpty.Should().BeFalse();
        token2.IsEmpty.Should().BeFalse();
        token1.TokenKind.Should().Be("Guid");
        token1.Should().NotBe(token2);
    }

    [Fact]
    public void ConcurrencyToken_FromGuid_ShouldProduceValidToken()
    {
        var guid = Guid.Parse("12345678-1234-1234-1234-123456789abc");
        var token = ConcurrencyToken.From(guid);

        token.Value.Should().Be("12345678123412341234123456789abc");
        token.TokenKind.Should().Be("Guid");
        token.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public void ConcurrencyToken_FromBytes_ShouldProduceHexRepresentation()
    {
        byte[] bytes = [0x00, 0x00, 0x00, 0x01, 0xAA, 0xBB];
        ConcurrencyToken token = ConcurrencyToken.From(bytes);

        token.TokenKind.Should().Be("RowVersion");
        token.Value.Should().Be("00000001AABB");
        token.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public void ConcurrencyToken_FromBytes_Null_ShouldThrowArgumentNullException()
    {
        Action act = () => ConcurrencyToken.From((byte[])null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("bytes");
    }

    [Fact]
    public void ConcurrencyToken_FromString_ShouldProduceValidToken()
    {
        var token = ConcurrencyToken.From("my-version-token", "CustomKind");
        token.Value.Should().Be("my-version-token");
        token.TokenKind.Should().Be("CustomKind");

        var tokenDefaultKind = ConcurrencyToken.From("simple-token");
        tokenDefaultKind.TokenKind.Should().Be("String");
    }

    [Fact]
    public void ConcurrencyToken_Constructor_NullOrWhitespaceHandling()
    {
        var tokenNullValue = new ConcurrencyToken(null!, null!);
        tokenNullValue.Value.Should().Be(string.Empty);
        tokenNullValue.TokenKind.Should().Be("Opaque");

        var tokenWhitespaceKind = new ConcurrencyToken("val", "   ");
        tokenWhitespaceKind.TokenKind.Should().Be("Opaque");
    }

    [Fact]
    public void ConcurrencyToken_None_ShouldBeEmpty()
    {
        var token = ConcurrencyToken.None;
        token.IsEmpty.Should().BeTrue();
        token.TokenKind.Should().Be("None");
        token.Value.Should().Be(string.Empty);
        token.ToString().Should().Be("[EmptyToken]");
    }

    [Fact]
    public void ConcurrencyToken_ToString_NonEmpty_ShouldFormatKindAndValue()
    {
        var token = new ConcurrencyToken("12345", "RowVersion");
        token.ToString().Should().Be("RowVersion:12345");
    }

    [Fact]
    public void ConcurrencyToken_EqualityAndComparisons_ShouldWork()
    {
        var tokenA = new ConcurrencyToken("token-123", "Custom");
        var tokenB = new ConcurrencyToken("token-123", "Custom");
        var tokenC = new ConcurrencyToken("token-456", "Custom");
        var tokenDiffKind = new ConcurrencyToken("token-123", "OtherKind");

        tokenA.Equals((IConcurrencyToken)tokenB).Should().BeTrue();
        tokenA.Equals((IConcurrencyToken)tokenC).Should().BeFalse();
        tokenA.Equals((IConcurrencyToken)tokenDiffKind).Should().BeFalse();
        tokenA.Equals((IConcurrencyToken?)null).Should().BeFalse();

        (tokenA == tokenB).Should().BeTrue();
        (tokenA != tokenC).Should().BeTrue();
        (tokenA != tokenDiffKind).Should().BeTrue();

        (tokenA < tokenC).Should().BeTrue();
        (tokenC < tokenA).Should().BeFalse();
        (tokenA < tokenB).Should().BeFalse(); // Kills < mutated to <=

        (tokenA <= tokenB).Should().BeTrue();
        (tokenA <= tokenC).Should().BeTrue();
        (tokenC <= tokenA).Should().BeFalse();

        (tokenC > tokenA).Should().BeTrue();
        (tokenA > tokenC).Should().BeFalse();
        (tokenA > tokenB).Should().BeFalse(); // Kills > mutated to >=

        (tokenA >= tokenB).Should().BeTrue();
        (tokenC >= tokenA).Should().BeTrue();
        (tokenA >= tokenC).Should().BeFalse();

        tokenA.CompareTo(tokenC).Should().BeLessThan(0);
        tokenC.CompareTo(tokenA).Should().BeGreaterThan(0);
        tokenA.CompareTo(tokenB).Should().Be(0);

        // Same value, different kind
        tokenA.CompareTo(tokenDiffKind).Should().BeLessThan(0); // "Custom" < "OtherKind"
        tokenDiffKind.CompareTo(tokenA).Should().BeGreaterThan(0);

        tokenA.CompareTo(null).Should().Be(1);
        tokenA.CompareTo((object)tokenC).Should().BeLessThan(0);
        tokenA.CompareTo((object)tokenB).Should().Be(0);

        Action act = () => tokenA.CompareTo(12345);
        act.Should().Throw<ArgumentException>()
            .WithMessage("Object must be of type ConcurrencyToken.*")
            .WithParameterName("obj");
    }
}
