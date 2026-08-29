// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.Concurrency.Abstractions;

namespace EricksonLopez.Concurrency.Testing;

/// <summary>
/// Represents a recorded invocation of <see cref="IConcurrencyController.ExecuteCasAsync{TEntity}"/>.
/// </summary>
/// <param name="Entity">The entity instance passed to the CAS operation.</param>
/// <param name="Expected">The expected version constraint.</param>
/// <param name="EntityId">The unique identifier of the entity.</param>
/// <param name="Timestamp">The timestamp when the CAS operation was invoked.</param>
public sealed record ExecuteCasInvocation(object Entity, ExpectedVersion Expected, string EntityId, DateTimeOffset Timestamp);
