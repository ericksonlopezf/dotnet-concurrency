// Copyright © Erickson Lopez. MIT License.

namespace EricksonLopez.Concurrency.Showcase.Models;

/// <summary>
/// Result data for a funds transfer operation.
/// </summary>
public sealed record TransferResult(string TransactionId, string SourceAccountId, string TargetAccountId, decimal Amount, long NewVersion);
