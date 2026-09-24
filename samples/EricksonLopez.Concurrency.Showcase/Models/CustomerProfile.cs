// Copyright © Erickson Lopez. MIT License.
using EricksonLopez.Concurrency.Abstractions;

namespace EricksonLopez.Concurrency.Showcase.Models;

/// <summary>
/// Represents a customer profile entity using an opaque concurrency token for state validation.
/// </summary>
public sealed class CustomerProfile : IConcurrencyAware
{
    /// <summary>
    /// Gets the unique identifier of the customer.
    /// </summary>
    public string CustomerId { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the email address of the customer.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the full legal name of the customer.
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the opaque concurrency token representing the state of this customer profile.
    /// </summary>
    public IConcurrencyToken ConcurrencyToken { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomerProfile"/> class.
    /// </summary>
    public CustomerProfile()
    {
        ConcurrencyToken = EricksonLopez.Concurrency.Abstractions.ConcurrencyToken.None;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomerProfile"/> class with the specified attributes.
    /// </summary>
    /// <param name="customerId">The unique identifier of the customer.</param>
    /// <param name="email">The email address of the customer.</param>
    /// <param name="fullName">The full legal name of the customer.</param>
    /// <param name="concurrencyToken">The opaque concurrency token.</param>
    public CustomerProfile(string customerId, string email, string fullName, IConcurrencyToken concurrencyToken)
    {
        CustomerId = customerId;
        Email = email;
        FullName = fullName;
        ConcurrencyToken = concurrencyToken;
    }
}
