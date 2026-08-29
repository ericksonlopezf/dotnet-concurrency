// Copyright © Erickson Lopez. MIT License.
using System;

namespace EricksonLopez.Concurrency.Abstractions;

/// <summary>
/// Represents an exception thrown when concurrency options, policies, or resolvers are misconfigured.
/// </summary>
public sealed class ConcurrencyConfigurationException : ConcurrencyException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrencyConfigurationException"/> class with a default error message.
    /// </summary>
    public ConcurrencyConfigurationException()
        : base("Invalid concurrency configuration.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrencyConfigurationException"/> class with a specified message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public ConcurrencyConfigurationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrencyConfigurationException"/> class with a specified message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ConcurrencyConfigurationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
