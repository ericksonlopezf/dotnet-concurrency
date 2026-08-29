// Copyright © Erickson Lopez. MIT License.
using EricksonLopez.Concurrency.Abstractions;

namespace EricksonLopez.Concurrency.Showcase.Models;

/// <summary>
/// Domain entity representing a bank account with explicit optimistic version checking.
/// </summary>
public sealed class BankAccount : IVersionedEntity<BankAccount>
{
    public string AccountId { get; init; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public long Version { get; set; }

    /// <summary>
    /// Gets the strongly-typed concurrency version of this bank account.
    /// </summary>
    public ConcurrencyVersion<BankAccount> TypedVersion => new(Version);

    public BankAccount()
    {
    }

    public BankAccount(string accountId, string owner, decimal balance, long version)
    {
        AccountId = accountId;
        Owner = owner;
        Balance = balance;
        Version = version;
    }
}
