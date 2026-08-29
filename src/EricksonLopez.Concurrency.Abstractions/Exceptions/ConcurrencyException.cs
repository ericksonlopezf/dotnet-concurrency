// Copyright © Erickson Lopez. MIT License.
using System;

namespace EricksonLopez.Concurrency.Abstractions;

/// <summary>
/// Represents the base exception thrown when an unhandled concurrency conflict occurs in an exceptional flow.
/// </summary>
/// <remarks>
/// Under normal operation, concurrency conflicts should be represented via monadic results rather than exceptions.
/// This exception is reserved for scenarios where exceptional throw semantics are strictly required.
/// </remarks>
public class ConcurrencyException : Exception
{
    /// <summary>
    /// Gets the conflict descriptor associated with this exception, if available.
    /// </summary>
    public ConcurrencyConflict? Conflict { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrencyException"/> class with a default error message.
    /// </summary>
    public ConcurrencyException()
        : base("A concurrency conflict occurred.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrencyException"/> class with a specified message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public ConcurrencyException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrencyException"/> class with a specified message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ConcurrencyException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrencyException"/> class with a specified conflict descriptor.
    /// </summary>
    /// <param name="conflict">The conflict descriptor containing detailed diagnostics.</param>
    public ConcurrencyException(ConcurrencyConflict conflict)
        : base(conflict?.Message ?? "A concurrency conflict occurred.")
    {
        Conflict = conflict;
    }
}
