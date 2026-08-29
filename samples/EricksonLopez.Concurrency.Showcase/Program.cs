// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using System.Threading.Tasks;
using EricksonLopez.Concurrency.Showcase.Levels;

namespace EricksonLopez.Concurrency.Showcase;

/// <summary>
/// Executable Showcase entry point orchestrating progressive architectural learning levels.
/// </summary>
public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        PrintBanner();

        bool runInteractive = args.Contains("--menu") || args.Contains("-i");
        bool runAll = !runInteractive || args.Contains("--run-all") || !Environment.UserInteractive || Console.IsInputRedirected;

        if (runAll)
        {
            Console.WriteLine("Executing full Progressive Showcase suite (Levels 00 to 10)...\n");
            await RunAllLevelsAsync();
            return 0;
        }

        while (true)
        {
            Console.WriteLine("\nSelect the level you wish to execute:");
            Console.WriteLine("  [0] Level 00: Conceptual & Design Principles");
            Console.WriteLine("  [1] Level 01: Quick Start (DI & First Verification)");
            Console.WriteLine("  [2] Level 02: Full Configuration (Options & Custom Resolvers)");
            Console.WriteLine("  [3] Level 03: Real-World Use Cases (Typed Versions & ETags)");
            Console.WriteLine("  [4] Level 04: Advanced Integration (Dapper & Result Monad)");
            Console.WriteLine("  [5] Level 05: Processing & Concurrency (CAS & Race Conditions)");
            Console.WriteLine("  [6] Level 06: Error Handling & DB Classification (PostgreSQL, SQL Server, etc.)");
            Console.WriteLine("  [7] Level 07: Scalability & Throughput (Zero-Allocation & OpenTelemetry)");
            Console.WriteLine("  [8] Level 08: Customization & Extensibility (Conflict Resolvers)");
            Console.WriteLine("  [9] Level 09: Specialized Tokens & Pessimistic Locking Hints");
            Console.WriteLine(" [10] Level 10: Enterprise Architecture (CQRS, Mediator & Multi-Tenancy)");
            Console.WriteLine("  [A] Run ALL levels sequentially");
            Console.WriteLine("  [Q] Quit");
            Console.Write("\nOption: ");

            string? input = Console.ReadLine()?.Trim().ToUpperInvariant();

            if (input == "Q")
            {
                break;
            }

            try
            {
                switch (input)
                {
                    case "0":
                        await Level00_Conceptual.RunAsync();
                        break;
                    case "1":
                        await Level01_QuickStart.RunAsync();
                        break;
                    case "2":
                        await Level02_FullConfiguration.RunAsync();
                        break;
                    case "3":
                        await Level03_RealWorldUseCases.RunAsync();
                        break;
                    case "4":
                        await Level04_AdvancedIntegration.RunAsync();
                        break;
                    case "5":
                        await Level05_ProcessingAndConcurrency.RunAsync();
                        break;
                    case "6":
                        await Level06_ErrorHandlingAndClassification.RunAsync();
                        break;
                    case "7":
                        await Level07_ScalabilityAndThroughput.RunAsync();
                        break;
                    case "8":
                        await Level08_CustomizationAndExtensibility.RunAsync();
                        break;
                    case "9":
                        await Level09_SpecializedTokensAndLocking.RunAsync();
                        break;
                    case "10":
                        await Level10_EnterpriseArchitecture.RunAsync();
                        break;
                    case "A":
                        await RunAllLevelsAsync();
                        break;
                    default:
                        Console.WriteLine("Invalid option. Please try again.");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n[ERROR] An exception occurred during level execution:\n{ex}");
                Console.ResetColor();
            }
        }

        return 0;
    }

    private static async Task RunAllLevelsAsync()
    {
        await Level00_Conceptual.RunAsync();
        await Level01_QuickStart.RunAsync();
        await Level02_FullConfiguration.RunAsync();
        await Level03_RealWorldUseCases.RunAsync();
        await Level04_AdvancedIntegration.RunAsync();
        await Level05_ProcessingAndConcurrency.RunAsync();
        await Level06_ErrorHandlingAndClassification.RunAsync();
        await Level07_ScalabilityAndThroughput.RunAsync();
        await Level08_CustomizationAndExtensibility.RunAsync();
        await Level09_SpecializedTokensAndLocking.RunAsync();
        await Level10_EnterpriseArchitecture.RunAsync();

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n===============================================================================");
        Console.WriteLine(" ALL SHOWCASE LEVELS EXECUTED SUCCESSFULLY.");
        Console.WriteLine("===============================================================================");
        Console.ResetColor();
    }

    private static void PrintBanner()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(@"
╔═══════════════════════════════════════════════════════════════════════════════╗
║                   ERICKSONLOPEZ.CONCURRENCY SHOWCASE                          ║
║         Official Reference Implementation and Executable Documentation        ║
╚═══════════════════════════════════════════════════════════════════════════════╝");
        Console.ResetColor();
    }
}
