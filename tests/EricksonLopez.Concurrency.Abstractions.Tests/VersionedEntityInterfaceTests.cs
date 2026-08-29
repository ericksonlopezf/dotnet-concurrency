// Copyright © Erickson Lopez. MIT License.
using AwesomeAssertions;
using EricksonLopez.Concurrency.Abstractions;
using Xunit;

namespace EricksonLopez.Concurrency.Abstractions.Tests;

public sealed class VersionedEntityInterfaceTests
{
    private sealed class InvoiceAggregate : IVersionedEntity<InvoiceAggregate>
    {
        public long Version { get; set; } = 42;
    }

    [Fact]
    public void IVersionedEntityOfT_DefaultTypedVersion_ShouldConstructTypedVersionFromLong()
    {
        var invoice = new InvoiceAggregate { Version = 99 };
        IVersionedEntity<InvoiceAggregate> versioned = invoice;

        ConcurrencyVersion<InvoiceAggregate> typedVersion = versioned.TypedVersion;

        typedVersion.Value.Should().Be(99);
        typedVersion.IsNone.Should().BeFalse();
    }
}
