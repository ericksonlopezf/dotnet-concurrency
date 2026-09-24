// Copyright © Erickson Lopez. MIT License.
using EricksonLopez.Concurrency.Abstractions;

namespace EricksonLopez.Concurrency.Showcase.Models;

/// <summary>
/// Represents product inventory with strongly-typed optimistic concurrency versioning.
/// </summary>
public sealed class ProductInventory : IVersionedEntity<ProductInventory>, IMutableVersionedEntity
{
    /// <summary>
    /// Gets the unique identifier of the product inventory item.
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the stock keeping unit (SKU) code.
    /// </summary>
    public string Sku { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the quantity of available stock.
    /// </summary>
    public int AvailableStock { get; set; }

    /// <summary>
    /// Gets or sets the quantity of reserved stock.
    /// </summary>
    public int ReservedStock { get; set; }

    /// <summary>
    /// Gets or sets the optimistic concurrency version.
    /// </summary>
    public long Version { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProductInventory"/> class.
    /// </summary>
    public ProductInventory()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProductInventory"/> class with the specified properties.
    /// </summary>
    /// <param name="id">The unique identifier of the product.</param>
    /// <param name="sku">The stock keeping unit code.</param>
    /// <param name="availableStock">The available stock count.</param>
    /// <param name="reservedStock">The reserved stock count.</param>
    /// <param name="version">The optimistic concurrency version.</param>
    public ProductInventory(string id, string sku, int availableStock, int reservedStock, long version)
    {
        Id = id;
        Sku = sku;
        AvailableStock = availableStock;
        ReservedStock = reservedStock;
        Version = version;
    }

    /// <summary>
    /// Creates a copy of this inventory item with an updated version number.
    /// </summary>
    /// <param name="nextVersion">The new concurrency version to assign.</param>
    /// <returns>A new <see cref="ProductInventory"/> instance with the updated version.</returns>
    public ProductInventory CloneWithVersion(long nextVersion)
    {
        return new ProductInventory(Id, Sku, AvailableStock, ReservedStock, nextVersion);
    }
}
