// Copyright © Erickson Lopez. MIT License.
using System;

namespace EricksonLopez.Concurrency.Abstractions;

/// <summary>
/// Represents an exception thrown when a strict concurrency token comparison fails.
/// </summary>
public sealed class ConcurrencyTokenMismatchException : ConcurrencyException
{
    /// <summary>
    /// Gets the expected concurrency token constraint.
    /// </summary>
    public IConcurrencyToken? ExpectedToken { get; }

    /// <summary>
    /// Gets the actual concurrency token found in storage.
    /// </summary>
    public IConcurrencyToken? ActualToken { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrencyTokenMismatchException"/> class with a default error message.
    /// </summary>
    public ConcurrencyTokenMismatchException()
        : base("Concurrency token mismatch.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrencyTokenMismatchException"/> class with a specified message.
    /// </summary>
    /// <param name="message">The message describing the error.</param>
    public ConcurrencyTokenMismatchException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrencyTokenMismatchException"/> class with a specified message and inner exception.
    /// </summary>
    /// <param name="message">The message describing the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ConcurrencyTokenMismatchException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrencyTokenMismatchException"/> class with the expected and actual tokens.
    /// </summary>
    /// <param name="expectedToken">The expected concurrency token constraint.</param>
    /// <param name="actualToken">The actual concurrency token found in storage.</param>
    public ConcurrencyTokenMismatchException(IConcurrencyToken expectedToken, IConcurrencyToken actualToken)
        : base($"Concurrency token mismatch. Expected: '{expectedToken?.Value}', Actual: '{actualToken?.Value}'.")
    {
        ExpectedToken = expectedToken;
        ActualToken = actualToken;
    }
}
