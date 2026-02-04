using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Elumatec.Tijdregistratie.Data;
using Elumatec.Tijdregistratie.ViewModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;

namespace Elumatec.Tijdregistratie
{
    public partial class TijdregistratieApp : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // 🔹 SQLite database path
                var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "elumatec.db");

                // 🔹 EF Core options
                var options = new DbContextOptionsBuilder<AppDbContext>()
                    .UseSqlite($"Data Source={dbPath}")
                    .Options;

                // 🔹 Create DbContext
                var dbContext = new AppDbContext(options);

                // 🔹 Create window + ViewModel
                var window = new TijdregistratieWindow
                {
                    DataContext = new UserSelectionViewModel(dbContext)
                };

                desktop.MainWindow = window;
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
