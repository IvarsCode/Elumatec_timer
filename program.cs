using Avalonia;
using System;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Elumatec.Tijdregistratie.Data;

namespace Elumatec.Tijdregistratie
{
    internal class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            var dbPath = Path.Combine(Environment.CurrentDirectory, "elumatec.db");
            Console.WriteLine($"=== DATABASE INITIALIZATION ===");
            Console.WriteLine($"Database path: {dbPath}");
            Console.WriteLine($"Database exists: {File.Exists(dbPath)}");

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={dbPath}")
                .EnableSensitiveDataLogging()
                .LogTo(Console.WriteLine, Microsoft.Extensions.Logging.LogLevel.Information)
                .Options;

            try
            {
                using (var db = new AppDbContext(options))
                {
                    Console.WriteLine("\n=== RUNNING MIGRATIONS ===");
                    db.Database.Migrate();
                    Console.WriteLine("Migrations completed successfully.");

                    Console.WriteLine("\n=== LOADING CSV DATA ===");
                    BedrijvenLaden.LoadBedrijvenCsvToDb(db);
                    Console.WriteLine("CSV load completed successfully.");
                }

                Console.WriteLine("\n=== DATABASE INITIALIZATION COMPLETE ===\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine("\n=== ERROR OCCURRED ===");
                Console.WriteLine($"Error Type: {ex.GetType().Name}");
                Console.WriteLine($"Message: {ex.Message}");
                Console.WriteLine($"\nStack Trace:\n{ex.StackTrace}");

                if (ex.InnerException != null)
                {
                    Console.WriteLine($"\n=== INNER EXCEPTION ===");
                    Console.WriteLine($"Type: {ex.InnerException.GetType().Name}");
                    Console.WriteLine($"Message: {ex.InnerException.Message}");
                    Console.WriteLine($"Stack Trace:\n{ex.InnerException.StackTrace}");
                }

                Console.WriteLine("\n=== Press any key to exit ===");
                Console.ReadKey();
                Environment.Exit(1);
            }

            Console.WriteLine("Starting Avalonia UI...");
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<TijdregistratieApp>()
                         .UsePlatformDetect()
                         .LogToTrace();
    }
}

// using Avalonia;
// using System;
// using System.IO;
// using Microsoft.EntityFrameworkCore;
// using Elumatec.Tijdregistratie.Data;

// namespace Elumatec.Tijdregistratie
// {
//     internal class Program
//     {
//         [STAThread]
//         public static void Main(string[] args)
//         {


//             var dbPath = Path.Combine(Environment.CurrentDirectory, "elumatec.db");
//             var options = new DbContextOptionsBuilder<AppDbContext>()
//                 .UseSqlite($"Data Source={dbPath}")
//                 .Options;

//             using (var db = new AppDbContext(options))
//             {
//                 // Will apply migrations if present; otherwise create DB
//                 db.Database.Migrate();

//                 // ONLY do this when heving an updated bedrijven.csv in data, if so delete the other so there is only 1
//                 BedrijvenLaden.LoadBedrijvenCsvToDb(db);
//             }

//             BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

//         }

//         public static AppBuilder BuildAvaloniaApp()
//             => AppBuilder.Configure<TijdregistratieApp>()
//                          .UsePlatformDetect()
//                          .LogToTrace();

//     }
// }
