// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.Concurrency.Abstractions;

namespace EricksonLopez.Concurrency.Testing;

/// <summary>
/// Represents a recorded invocation of <see cref="IConcurrencyController.VerifyToken{TEntity}"/>.
/// </summary>
/// <param name="Entity">The entity instance passed to verification.</param>
/// <param name="Expected">The expected token constraint.</param>
/// <param name="EntityId">The unique identifier of the entity.</param>
/// <param name="Timestamp">The timestamp when the verification was invoked.</param>
public sealed record VerifyTokenInvocation(object Entity, IConcurrencyToken Expected, string EntityId, DateTimeOffset Timestamp);
