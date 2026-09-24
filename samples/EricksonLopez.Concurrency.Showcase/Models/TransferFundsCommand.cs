// Copyright © Erickson Lopez. MIT License.
using EricksonLopez.Concurrency.Abstractions;
using EricksonLopez.Concurrency.Mediator;
using EricksonLopez.Result;

namespace EricksonLopez.Concurrency.Showcase.Models;

/// <summary>
/// Represents a CQRS command with explicit optimistic concurrency constraints dispatched through EricksonLopez.Mediator.
/// </summary>
/// <param name="SourceAccountId">The identifier of the source bank account.</param>
/// <param name="TargetAccountId">The identifier of the destination bank account.</param>
/// <param name="Amount">The monetary amount to transfer.</param>
/// <param name="ExpectedVersion">The optional expected numeric version of the source account.</param>
/// <param name="ConcurrencyToken">The optional concurrency token of the source account.</param>
public sealed record TransferFundsCommand(
    string SourceAccountId,
    string TargetAccountId,
    decimal Amount,
    ExpectedVersion? ExpectedVersion = null,
    IConcurrencyToken? ConcurrencyToken = null) : IConcurrencyAwareRequest<Result<TransferResult>>;
