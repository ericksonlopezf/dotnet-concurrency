// Copyright © Erickson Lopez. MIT License.
using AwesomeAssertions;
using EricksonLopez.Concurrency.Abstractions;
using EricksonLopez.Concurrency.Mediator;
using Xunit;

namespace EricksonLopez.Concurrency.Mediator.Tests;

public sealed class IConcurrencyAwareRequestTests
{
    private sealed class DefaultConcurrencyRequest : IConcurrencyAwareRequest
    {
    }

    private sealed class CustomConcurrencyRequest : IConcurrencyAwareRequest
    {
        public ExpectedVersion? ExpectedVersion => Abstractions.ExpectedVersion.Specific(42);
        public IConcurrencyToken? ConcurrencyToken => new CustomStubToken("token-123");
    }

    private sealed record CustomStubToken(string Value) : IConcurrencyToken
    {
        public string TokenKind => "Stub";
        public bool IsEmpty => string.IsNullOrEmpty(Value);
        public bool Equals(IConcurrencyToken? other) => other is not null && Value == other.Value;
    }

    [Fact]
    public void IConcurrencyAwareRequest_DefaultInterfaceMembers_ShouldReturnNull()
    {
        IConcurrencyAwareRequest req = new DefaultConcurrencyRequest();

        req.ExpectedVersion.Should().BeNull();
        req.ConcurrencyToken.Should().BeNull();
    }

    [Fact]
    public void IConcurrencyAwareRequest_CustomMembers_ShouldReturnAssignedValues()
    {
        IConcurrencyAwareRequest req = new CustomConcurrencyRequest();

        req.ExpectedVersion.Should().Be(ExpectedVersion.Specific(42));
        req.ConcurrencyToken.Should().NotBeNull();
        req.ConcurrencyToken!.Value.Should().Be("token-123");
    }
}
