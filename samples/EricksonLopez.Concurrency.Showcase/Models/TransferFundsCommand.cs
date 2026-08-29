// Copyright © Erickson Lopez. MIT License.
using EricksonLopez.Concurrency.Abstractions;
using EricksonLopez.Concurrency.Mediator;
using EricksonLopez.Result;

namespace EricksonLopez.Concurrency.Showcase.Models;

/// <summary>
/// CQRS command with explicit optimistic concurrency constraints dispatched through EricksonLopez.Mediator.
/// </summary>
public sealed record TransferFundsCommand(
    string SourceAccountId,
    string TargetAccountId,
    decimal Amount,
    ExpectedVersion? ExpectedVersion = null,
    IConcurrencyToken? ConcurrencyToken = null) : IConcurrencyAwareRequest<Result<TransferResult>>;
