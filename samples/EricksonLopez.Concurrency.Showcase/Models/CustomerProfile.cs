// Copyright © Erickson Lopez. MIT License.
using EricksonLopez.Concurrency.Abstractions;

namespace EricksonLopez.Concurrency.Showcase.Models;

/// <summary>
/// Domain entity using an opaque concurrency token for state validation.
/// </summary>
public sealed class CustomerProfile : IConcurrencyAware
{
    public string CustomerId { get; init; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public IConcurrencyToken ConcurrencyToken { get; set; }

    public CustomerProfile()
    {
        ConcurrencyToken = EricksonLopez.Concurrency.Abstractions.ConcurrencyToken.None;
    }

    public CustomerProfile(string customerId, string email, string fullName, IConcurrencyToken concurrencyToken)
    {
        CustomerId = customerId;
        Email = email;
        FullName = fullName;
        ConcurrencyToken = concurrencyToken;
    }
}
