// Copyright © Erickson Lopez. MIT License.

namespace EricksonLopez.Concurrency.Showcase.Models;

/// <summary>
/// Represents the result data for a funds transfer operation.
/// </summary>
/// <param name="TransactionId">The unique identifier of the transfer transaction.</param>
/// <param name="SourceAccountId">The identifier of the source bank account.</param>
/// <param name="TargetAccountId">The identifier of the destination bank account.</param>
/// <param name="Amount">The transferred monetary amount.</param>
/// <param name="NewVersion">The new concurrency version of the source account.</param>
public sealed record TransferResult(string TransactionId, string SourceAccountId, string TargetAccountId, decimal Amount, long NewVersion);
