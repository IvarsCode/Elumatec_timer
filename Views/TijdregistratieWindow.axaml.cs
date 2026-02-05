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

            // 🔹 Create DbContext options
            var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "elumatec.db");
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={dbPath}")
                .Options;

            // 🔹 Create DbContext
            var dbContext = new AppDbContext(options);

            // 🔹 Assign MainViewModel as DataContext for navigation
            DataContext = new MainViewModel(dbContext);
        }
    }
}
