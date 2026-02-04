using Avalonia;
using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Elumatec.Tijdregistratie.Data;

namespace Elumatec.Tijdregistratie;

internal sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // tiny file logger to help diagnose headless startup issues
        void Log(string message)
        {
            try
            {
                var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Elumatec");
                Directory.CreateDirectory(folder);
                var p = Path.Combine(folder, "startup.log");
                File.AppendAllText(p, $"[{DateTime.Now:O}] {message}\n");
            }
            catch { }
        }

        Log($"Before DB migration (basedir={AppContext.BaseDirectory})");

        // ensure database exists and apply migrations
        var dbPath = Path.Combine(Environment.CurrentDirectory, "elumatec.db");
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            .Options;

        using (var db = new AppDbContext(options))
        {
            // Will apply migrations if present; otherwise create DB
            db.Database.Migrate();
        }

        Log("After DB migration");

        try
        {
            Log("Before attach exception handlers"); AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                Console.Error.WriteLine($"Unhandled exception: {e.ExceptionObject}");
            };

            System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                Console.Error.WriteLine($"Unobserved task exception: {e.Exception}");
            };

            // Forward Trace output to console for Avalonia logs
            System.Diagnostics.Trace.AutoFlush = true;
            if (System.Diagnostics.Trace.Listeners.Count == 0 || !(System.Diagnostics.Trace.Listeners[0] is System.Diagnostics.ConsoleTraceListener))
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
}