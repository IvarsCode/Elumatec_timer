using Avalonia;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Elumatec.Tijdregistratie.Data;
using Elumatec.Tijdregistratie.Models;
using Elumatec.Tijdregistratie.Pdf;

namespace Elumatec.Tijdregistratie;

internal sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Check if test mode is requested
        bool testMode = args.Contains("--test-pdf");

        void Log(string message)
        {
            try
            {
                var folder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Elumatec");
                Directory.CreateDirectory(folder);
                var p = Path.Combine(folder, "startup.log");
                File.AppendAllText(p, $"[{DateTime.Now:O}] {message}\n");
            }
            catch { }
        }

        Log($"Before DB migration (basedir={AppContext.BaseDirectory})");

        var dbPath = Path.Combine(Environment.CurrentDirectory, "elumatec.db");

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            .Options;

        using (var db = new AppDbContext(options))
        {
            db.Database.Migrate();
        }

        Log("After DB migration");

        // 🔽 TEST PDF GENERATION (only if --test-pdf argument is passed)
        if (testMode)
        {
            GenerateTestPdf(options);
            return; // Exit application after PDF generation
        }

        try
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                Console.Error.WriteLine($"Unhandled exception: {e.ExceptionObject}");
            };

            System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                Console.Error.WriteLine($"Unobserved task exception: {e.Exception}");
            };

            System.Diagnostics.Trace.AutoFlush = true;
            if (System.Diagnostics.Trace.Listeners.Count == 0 ||
                !(System.Diagnostics.Trace.Listeners[0] is System.Diagnostics.ConsoleTraceListener))
            {
                System.Diagnostics.Trace.Listeners.Add(new System.Diagnostics.ConsoleTraceListener());
            }

            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Startup exception: {ex}");
            throw;
        }
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<TijdregistratieApp>()
            .UsePlatformDetect()
            .LogToTrace();

    private static void GenerateTestPdf(DbContextOptions<AppDbContext> options)
    {
        try
        {
            using (var db = new AppDbContext(options))
            {
                // Find all interventies with 4 calls
                var interventiesWithFourCalls = db.Interventies
                    .Where(i => i.AantalCalls == 4)
                    .OrderBy(i => i.Bedrijfsnaam)
                    .ToList();

                if (interventiesWithFourCalls.Count == 0)
                {
                    Console.WriteLine("❌ No interventies found with 4 calls. Please add some data first.");
                    Console.Out.Flush();
                    return;
                }

                Console.WriteLine($"\n📋 Found {interventiesWithFourCalls.Count} interventie(s) with 4 calls:\n");

                for (int i = 0; i < interventiesWithFourCalls.Count; i++)
                {
                    var interventie = interventiesWithFourCalls[i];
                    Console.WriteLine($"  [{i + 1}] {interventie.Bedrijfsnaam} - {interventie.Machine}");
                }

                Console.Write($"\n🔹 Enter the number of the interventie to generate PDF (1-{interventiesWithFourCalls.Count}), or press Enter to skip: ");
                Console.Out.Flush();

                var input = Console.ReadLine()?.Trim();

                if (string.IsNullOrEmpty(input))
                {
                    Console.WriteLine("Skipping PDF generation.\n");
                    Console.Out.Flush();
                    return;
                }

                if (!int.TryParse(input, out int selectedIndex) || selectedIndex < 1 || selectedIndex > interventiesWithFourCalls.Count)
                {
                    Console.WriteLine($"❌ Invalid selection. Please enter a number between 1 and {interventiesWithFourCalls.Count}.\n");
                    Console.Out.Flush();
                    return;
                }

                var selected = interventiesWithFourCalls[selectedIndex - 1];

                // Generate PDF with single interventie
                var singlePdfPath = Path.Combine(Environment.CurrentDirectory, $"Test_Interventie_{selected.Bedrijfsnaam.Replace(" ", "_")}.pdf");

                InterventiesPdfExporter.ExportSingleInterventie(selected, singlePdfPath);

                Console.WriteLine($"✅ PDF generated successfully: {Path.GetFileName(singlePdfPath)}");
                Console.WriteLine($"   Location: {singlePdfPath}\n");
                Console.Out.Flush();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error generating test PDF: {ex.Message}\n");
            Console.Out.Flush();
        }
    }
}
