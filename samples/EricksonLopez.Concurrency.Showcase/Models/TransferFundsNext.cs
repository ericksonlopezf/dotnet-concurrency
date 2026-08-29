// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading.Tasks;
using EricksonLopez.Concurrency.Abstractions;
using EricksonLopez.Concurrency.Showcase.Models;
using EricksonLopez.Mediator;
using EricksonLopez.Result;

namespace EricksonLopez.Concurrency.Showcase.Models;

/// <summary>
/// Next delegate struct executing the command handler downstream.
/// </summary>
public readonly struct TransferFundsNext : INext<Result<TransferResult>>
{
    private readonly TransferFundsCommand _command;
    private readonly IConcurrencyController _controller;

    public TransferFundsNext(TransferFundsCommand command, IConcurrencyController controller)
    {
        _command = command;
        _controller = controller;
    }

    public async ValueTask<Result<TransferResult>> InvokeAsync()
    {
        TransferFundsCommand cmd = _command;
        IConcurrencyController ctrl = _controller;
        decimal amount = cmd.Amount;

        // Simulate source bank account loaded from persistence
        var sourceAccount = new BankAccount(cmd.SourceAccountId, "Source Owner", 1000.00m, version: 1);

        // If the command declares an explicit version constraint, verify it
        if (cmd.ExpectedVersion.HasValue)
        {
            ConcurrencyConflict? conflict = ctrl.VerifyVersion(sourceAccount, cmd.ExpectedVersion.Value, cmd.SourceAccountId);
            if (conflict is not null)
            {
                return Result<TransferResult>.Failure(
                    Error.Conflict("Concurrency.Conflict", conflict.Message));
            }
        }

        // Execute in-memory CAS on the source bank account
        CasResult<BankAccount> casOutcome = await ctrl.ExecuteCasAsync(
            entity: sourceAccount,
            expected: cmd.ExpectedVersion ?? ExpectedVersion.Specific(sourceAccount.Version),
            entityId: sourceAccount.AccountId,
            mutate: (acc, ct) =>
            {
                acc.Balance -= amount;
                return ValueTask.FromResult(acc);
            });

        if (!casOutcome.IsSuccess)
        {
            return Result<TransferResult>.Failure(
                Error.Conflict("Concurrency.CasFailed", casOutcome.Conflict?.Message ?? "CAS failed."));
        }

        var result = new TransferResult(
            TransactionId: $"TX-{Guid.NewGuid():N}"[..12],
            SourceAccountId: cmd.SourceAccountId,
            TargetAccountId: cmd.TargetAccountId,
            Amount: amount,
            NewVersion: casOutcome.NewVersion?.Value ?? sourceAccount.Version + 1);

        return Result<TransferResult>.Success(result);
    }
}
