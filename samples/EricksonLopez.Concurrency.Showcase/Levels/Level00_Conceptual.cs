// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading.Tasks;

namespace EricksonLopez.Concurrency.Showcase.Levels;

/// <summary>
/// Provides conceptual demonstrations covering motivation, comparison, and core design principles.
/// </summary>
public static class Level00_Conceptual
{
    /// <summary>
    /// Executes the conceptual demonstration covering motivation, comparison, and design principles.
    /// </summary>
    /// <remarks>
    /// Cookbook: Level 00 — Conceptual Foundations.
    /// Prerequisites: None.
    /// Concepts: Optimistic vs Pessimistic concurrency, zero-allocation structs, ADR-001 (SoC).
    /// APIs: None (narrative only — no library calls).
    /// Complexity: Introductory.
    /// Next: Level01_QuickStart for the first live API call.
    /// </remarks>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static Task RunAsync()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n===============================================================================");
        Console.WriteLine(" LEVEL 00: CONCEPTUAL FOUNDATIONS & ARCHITECTURAL DECISIONS");
        Console.WriteLine("===============================================================================");
        Console.ResetColor();

        Console.WriteLine(@"
1. What is EricksonLopez.Concurrency?
   A high-performance framework for optimistic concurrency control, deterministic conflict
   arbitration, and state synchronization across the .NET 10 and Native AOT ecosystem.

2. What problems does it solve?
   - Prevents Lost Updates in microservices, CQRS commands, and distributed systems.
   - Eliminates the latency and deadlocks of heavy distributed locking mechanisms (Redis Redlock/Mutex).
   - Provides standardized database error classification for PostgreSQL, SQL Server, MySQL, MariaDB, Oracle, and SQLite.
   - Offers zero-allocation readonly record struct value types across the entire critical path.

3. Why Optimistic Concurrency instead of Distributed Locks?
   - Distributed locks introduce network roundtrips, lease expiry hazards (GC pauses), and single points of failure.
   - Optimistic concurrency assumes conflicts are infrequent and delegates atomic state checks
     to the database engine (WHERE version = @ExpectedVersion) or in-memory CAS.

4. Comparison Matrix:
   +------------------------------+--------------------+---------------------+
   | Dimension                    | Optimistic (.NET)  | Distributed Locks   |
   +------------------------------+--------------------+---------------------+
   | Throughput                   | Ultra High         | Medium / Low        |
   | Operation Latency            | Sub-microsecond    | 2ms - 50ms (network)|
   | Heap Allocations             | 0 bytes (structs)  | Multiple objects    |
   | Infrastructure Dependencies  | Zero (Native DB)   | Redis / Consul / ZK |
   | Failure Mode Safety          | Transactional DB   | Deadlock / Leaks    |
   +------------------------------+--------------------+---------------------+

5. Architectural Invariants:
   - Zero-allocation: ConcurrencyVersion, ExpectedVersion, ActualVersion, ConcurrencyToken are value structs.
   - Native AOT First: Trim-analyzed, zero reflection overhead.
   - Strict Separation of Concerns: Detection and classification here; retry policies in Resilience.

6. ADR-001 — Separation of Concerns between Concurrency and Resilience:
   This library is responsible for DETECTING and CLASSIFYING conflicts
   (e.g., Transient vs NonRetryable, VersionMismatch vs Deadlock).
   It is NOT responsible for retry backoff, jitter, circuit-breaking, or fallback orchestration.
   Those concerns belong to Resilience/Polly or application-level orchestration policies.
   This avoids circular coupling and keeps each layer independently testable.
   -> See Level10 for a concrete CQRS architectural demarcation demonstration.
");

        return Task.CompletedTask;
    }
}
