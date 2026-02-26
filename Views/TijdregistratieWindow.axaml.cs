using Avalonia.Controls;
using Elumatec.Tijdregistratie.Data;
using Elumatec.Tijdregistratie.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace Elumatec.Tijdregistratie.Views
{
    public partial class TijdregistratieWindow : Window
    {
        public TijdregistratieWindow()
        {
            InitializeComponent();

            var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "elumatec.db");
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={dbPath}")
                .Options;

            var dbContext = new AppDbContext(options);

            DataContext = new MainViewModel(dbContext);

            Closing += OnWindowClosing;
        }

        private void OnWindowClosing(object? sender, WindowClosingEventArgs e)
        {
            if (DataContext is MainViewModel mainVM &&
                mainVM.CurrentView is IClosingGuard guard)
            {
                if (!guard.OnWindowCloseRequested())
                    e.Cancel = true;
            }
        }
    }
}