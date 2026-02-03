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

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<TijdregistratieApp>()
            .UsePlatformDetect()
            .LogToTrace();
}