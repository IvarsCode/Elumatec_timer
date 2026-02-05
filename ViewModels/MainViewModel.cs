using System.ComponentModel;
using System.Runtime.CompilerServices;
using Elumatec.Tijdregistratie.Data;
using Elumatec.Tijdregistratie.Models;

namespace Elumatec.Tijdregistratie.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private object? _currentView;
        public object? CurrentView
        {
            get => _currentView;
            set
            {
                _currentView = value;
                OnPropertyChanged();
            }
        }

        public AppDbContext Db { get; }
        public Medewerker? CurrentUser { get; private set; }

        public MainViewModel(AppDbContext db)
        {
            Db = db;
            ShowUserSelection();
        }

        public void ShowUserSelection()
        {
            // Pass "this" so UserSelectionViewModel can call UserSelected()
            CurrentView = new UserSelectionViewModel(Db, this);
        }

        public void UserSelected(Medewerker user)
        {
            CurrentUser = user;
            ShowInterventieOverview();
        }

        public void ShowInterventieOverview()
        {
            CurrentView = new InterventieOverviewViewModel(this);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
