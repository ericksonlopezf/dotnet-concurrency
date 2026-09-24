// Copyright © Erickson Lopez. MIT License.
using EricksonLopez.Concurrency.Abstractions;

namespace EricksonLopez.Concurrency.Showcase.Models;

/// <summary>
/// Represents a bank account with explicit optimistic version checking.
/// </summary>
public sealed class BankAccount : IVersionedEntity<BankAccount>, IMutableVersionedEntity
{
    /// <summary>
    /// Gets the unique identifier of the bank account.
    /// </summary>
    public string AccountId { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the owner name of the bank account.
    /// </summary>
    public string Owner { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the monetary balance of the bank account.
    /// </summary>
    public decimal Balance { get; set; }

    /// <summary>
    /// Gets or sets the optimistic concurrency version.
    /// </summary>
    public long Version { get; set; }

    /// <summary>
    /// Gets the strongly-typed concurrency version of this bank account.
    /// </summary>
    public ConcurrencyVersion<BankAccount> TypedVersion => new(Version);

    /// <summary>
    /// Initializes a new instance of the <see cref="BankAccount"/> class.
    /// </summary>
    public BankAccount()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BankAccount"/> class with the specified attributes and version.
    /// </summary>
    /// <param name="accountId">The unique identifier of the bank account.</param>
    /// <param name="owner">The owner name of the bank account.</param>
    /// <param name="balance">The monetary balance of the bank account.</param>
    /// <param name="version">The optimistic concurrency version.</param>
    public BankAccount(string accountId, string owner, decimal balance, long version)
    {
        AccountId = accountId;
        Owner = owner;
        Balance = balance;
        Version = version;
    }
}
