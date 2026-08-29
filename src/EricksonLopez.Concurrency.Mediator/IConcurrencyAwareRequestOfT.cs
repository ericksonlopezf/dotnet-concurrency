// Copyright © Erickson Lopez. MIT License.
using EricksonLopez.Mediator;

namespace EricksonLopez.Concurrency.Mediator;

/// <summary>
/// Defines a strongly-typed contract for mediator commands returning <typeparamref name="TResponse"/> that declare explicit concurrency constraints.
/// </summary>
/// <typeparam name="TResponse">The response type returned by the command handler.</typeparam>
public interface IConcurrencyAwareRequest<TResponse> : ICommand<TResponse>, IConcurrencyAwareRequest
{
}
