using Avalonia.Controls;
using Elumatec.Tijdregistratie.Data;
using Elumatec.Tijdregistratie.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Elumatec.Tijdregistratie
{
    public partial class TijdregistratieWindow : Window
    {
        public TijdregistratieWindow()
        {
            InitializeComponent();

            // Create DbContext options
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite("Data Source=elumatec.db")
                .Options;

            // Create DbContext
            var dbContext = new AppDbContext(options);

            // Assign ViewModel
            DataContext = new UserSelectionViewModel(dbContext);
        }
    }
}
