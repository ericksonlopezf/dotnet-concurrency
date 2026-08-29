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
/// Level 02: Full Configuration — ConcurrencyOptions, all database dialect registrations, and custom conflict resolvers.
/// </summary>
public static class Level02_FullConfiguration
{
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
            options.DefaultResolutionStrategy = ConflictResolutionStrategy.MergeDomainSpecific;
            options.EnableDiagnostics = true;
            options.DefaultConflictClassification = ConcurrencyConflictClassification.Transient;
            options.RecordDetailedActivityTags = true;
            options.ThrowOnUnresolvedConflict = false;
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
