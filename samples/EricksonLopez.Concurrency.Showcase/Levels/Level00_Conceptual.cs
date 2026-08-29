// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading.Tasks;

namespace EricksonLopez.Concurrency.Showcase.Levels;

/// <summary>
/// Level 00: Conceptual overview, motivation, comparison, and core design principles.
/// </summary>
public static class Level00_Conceptual
{
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
");

        return Task.CompletedTask;
    }
}
