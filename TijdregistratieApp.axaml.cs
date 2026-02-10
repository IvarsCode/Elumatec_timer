using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Elumatec.Tijdregistratie.Data;
using Elumatec.Tijdregistratie.ViewModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using Elumatec.Tijdregistratie.Views;

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
                var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "elumatec.db");

                var options = new DbContextOptionsBuilder<AppDbContext>()
                    .UseSqlite($"Data Source={dbPath}")
                    .Options;

                var dbContext = new AppDbContext(options);

                var mainViewModel = new MainViewModel(dbContext);

                var window = new TijdregistratieWindow
                {
                    DataContext = mainViewModel
                };

                desktop.MainWindow = window;
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
