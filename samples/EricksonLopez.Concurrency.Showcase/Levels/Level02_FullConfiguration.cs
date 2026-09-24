// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading.Tasks;
using EricksonLopez.Concurrency.Abstractions;
using EricksonLopez.Concurrency.AspNetCore.DependencyInjection;
using EricksonLopez.Concurrency.Dapper.DependencyInjection;
using EricksonLopez.Concurrency.DependencyInjection;
using EricksonLopez.Concurrency.MariaDb.DependencyInjection;
using EricksonLopez.Concurrency.Mediator.DependencyInjection;
using EricksonLopez.Concurrency.MySql.DependencyInjection;
using EricksonLopez.Concurrency.Oracle.DependencyInjection;
using EricksonLopez.Concurrency.PostgreSql.DependencyInjection;
using EricksonLopez.Concurrency.Showcase.Models;
using EricksonLopez.Concurrency.Showcase.Resolvers;
using EricksonLopez.Concurrency.Sqlite.DependencyInjection;
using EricksonLopez.Concurrency.SqlServer.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace EricksonLopez.Concurrency.Showcase.Levels;

/// <summary>
/// Provides demonstrations of full configuration, options, database dialect registrations, and custom conflict resolvers.
/// </summary>
public static class Level02_FullConfiguration
{
    /// <summary>
    /// Executes the full configuration demonstration.
    /// </summary>
    /// <remarks>
    /// Cookbook: Level 02 — Full Configuration.
    /// Prerequisites: Level01 (basic DI setup).
    /// Concepts: ConcurrencyOptions all properties, custom resolver registration, all dialect DI extensions.
    /// APIs: AddEricksonLopezConcurrency(Action&lt;ConcurrencyOptions&gt;), AddConflictResolver&lt;T,R&gt;(),
    ///        AddEricksonLopezConcurrencyDapper/PostgreSql/SqlServer/MySql/MariaDb/Oracle/Sqlite(),
    ///        AddConcurrencyMediatorBehavior(), AddConcurrencyAspNetCore().
    /// Complexity: Intermediate.
    /// Next: Level03_RealWorldUseCases for domain entity patterns.
    /// </remarks>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static Task RunAsync()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n===============================================================================");
        Console.WriteLine(" LEVEL 02: FULL CONFIGURATION (OPTIONS, EXTENSIONS & ALL DIALECT REGISTRATIONS)");
        Console.WriteLine("===============================================================================");
        Console.ResetColor();

        // 1. Advanced configuration using Action<ConcurrencyOptions>
        var services = new ServiceCollection();

        services.AddEricksonLopezConcurrency(options =>
        {
            options.DefaultResolutionStrategy       = ConflictResolutionStrategy.MergeDomainSpecific;
            options.EnableDiagnostics               = true;
            options.DefaultConflictClassification   = ConcurrencyConflictClassification.Transient;
            options.RecordDetailedActivityTags      = true;
            options.ThrowOnUnresolvedConflict       = false;
            // Lock-acquisition and execution timeouts (prevent deadlocks and thread-pool starvation)
            options.DefaultMaxAcquisitionTimeout    = TimeSpan.FromSeconds(5);
            options.DefaultMaxExecutionTimeout      = TimeSpan.FromSeconds(60);
            // StripeCount controls internal lock partitioning for ExecuteCasAsync.
            // The actual stripe count used is max(configured, ProcessorCount * 8) with a floor of 256.
            // Higher values reduce contention under heavy concurrent load (more independent buckets).
            // Default: 512. Values above Environment.ProcessorCount * 8 have diminishing returns.
            options.StripeCount                     = 512;
        });

        // 2. Register custom typed resolver for ProductInventory
        services.AddConflictResolver<ProductInventory, ShowcaseInventoryConflictResolver>();

        // 3. Register all supported database dialects and integrations into DI
        services.AddEricksonLopezConcurrencyDapper();
        services.AddEricksonLopezConcurrencyPostgreSql();
        services.AddEricksonLopezConcurrencySqlServer();
        services.AddEricksonLopezConcurrencyMySql();
        services.AddEricksonLopezConcurrencyMariaDb();
        services.AddEricksonLopezConcurrencyOracle();
        services.AddEricksonLopezConcurrencySqlite();
        services.AddConcurrencyMediatorBehavior();
        services.AddConcurrencyAspNetCore();

        using ServiceProvider serviceProvider = services.BuildServiceProvider();

        // 4. Verification of registered and injected services
        var optionsInstance = serviceProvider.GetRequiredService<ConcurrencyOptions>();
        var checker = serviceProvider.GetRequiredService<IConcurrencyChecker>();
        var controller = serviceProvider.GetRequiredService<IConcurrencyController>();
        var resolver = serviceProvider.GetRequiredService<IConcurrencyConflictResolver<ProductInventory>>();

        Console.WriteLine("[1] Configured ConcurrencyOptions:");
        Console.WriteLine($"    - DefaultResolutionStrategy:         {optionsInstance.DefaultResolutionStrategy}");
        Console.WriteLine($"    - EnableDiagnostics:                 {optionsInstance.EnableDiagnostics}");
        Console.WriteLine($"    - DefaultConflictClassification:     {optionsInstance.DefaultConflictClassification}");
        Console.WriteLine($"    - RecordDetailedActivityTags:        {optionsInstance.RecordDetailedActivityTags}");
        Console.WriteLine($"    - ThrowOnUnresolvedConflict:         {optionsInstance.ThrowOnUnresolvedConflict}");
        Console.WriteLine($"    - DefaultMaxAcquisitionTimeout:      {optionsInstance.DefaultMaxAcquisitionTimeout.TotalSeconds}s  (lock-wait ceiling before TimeoutException)");
        Console.WriteLine($"    - DefaultMaxExecutionTimeout:        {optionsInstance.DefaultMaxExecutionTimeout.TotalSeconds}s  (mutation-delegate ceiling before cancellation)");
        Console.WriteLine($"    - StripeCount:                       {optionsInstance.StripeCount}  (internal lock partitions = max(256, ProcessorCount * 8))");

        Console.WriteLine("\n[2] Registered and Injected Services:");
        Console.WriteLine($"    - IConcurrencyChecker:               {checker.GetType().Name}");
        Console.WriteLine($"    - IConcurrencyController:            {controller.GetType().Name}");
        Console.WriteLine($"    - IConcurrencyConflictResolver<...>: {resolver.GetType().Name}");

        Console.WriteLine("\n[3] Dialect and Integration Extensions Registered:");
        Console.WriteLine("    ✔ Dapper:       AddEricksonLopezConcurrencyDapper()");
        Console.WriteLine("    ✔ PostgreSQL:   AddEricksonLopezConcurrencyPostgreSql()");
        Console.WriteLine("    ✔ SQL Server:   AddEricksonLopezConcurrencySqlServer()");
        Console.WriteLine("    ✔ MySQL:        AddEricksonLopezConcurrencyMySql()");
        Console.WriteLine("    ✔ MariaDB:      AddEricksonLopezConcurrencyMariaDb()");
        Console.WriteLine("    ✔ Oracle:       AddEricksonLopezConcurrencyOracle()");
        Console.WriteLine("    ✔ SQLite:       AddEricksonLopezConcurrencySqlite()");
        Console.WriteLine("    ✔ Mediator:     AddConcurrencyMediatorBehavior()");
        Console.WriteLine("    ✔ AspNetCore:   AddConcurrencyAspNetCore()");

        return Task.CompletedTask;
    }
}
