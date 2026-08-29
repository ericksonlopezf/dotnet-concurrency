// Copyright © Erickson Lopez. MIT License.
using EricksonLopez.Concurrency.Abstractions;

namespace EricksonLopez.Concurrency.Showcase.Models;

/// <summary>
/// Domain entity representing product inventory with strongly-typed optimistic concurrency versioning.
/// </summary>
public sealed class ProductInventory : IVersionedEntity<ProductInventory>
{
    public string Id { get; init; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public int AvailableStock { get; set; }
    public int ReservedStock { get; set; }
    public long Version { get; set; }

    public ProductInventory()
    {
    }

    public ProductInventory(string id, string sku, int availableStock, int reservedStock, long version)
    {
        Id = id;
        Sku = sku;
        AvailableStock = availableStock;
        ReservedStock = reservedStock;
        Version = version;
    }

    public ProductInventory CloneWithVersion(long nextVersion)
    {
        return new ProductInventory(Id, Sku, AvailableStock, ReservedStock, nextVersion);
    }
}
