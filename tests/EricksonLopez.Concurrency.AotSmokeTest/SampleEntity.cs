// Copyright © Erickson Lopez. MIT License.
using EricksonLopez.Concurrency.Abstractions;

namespace EricksonLopez.Concurrency.NativeAotTests;

/// <summary>
/// Sample versioned entity for Native AOT validation.
/// </summary>
public sealed record SampleEntity : IVersionedEntity
{
    public string Id { get; init; } = string.Empty;
    public decimal Balance { get; init; }
    public long Version { get; set; }
}
