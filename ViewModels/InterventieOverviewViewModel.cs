using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Elumatec.Tijdregistratie.Models;

namespace Elumatec.Tijdregistratie.ViewModels
{
    public class InterventieOverviewViewModel : INotifyPropertyChanged
    {
        private readonly MainViewModel _main;

        public Medewerker? CurrentUser => _main.CurrentUser;

        public string GebruikerNaam => CurrentUser != null ? CurrentUser.Naam : "Onbekend";

        public bool HasUser => CurrentUser != null;

        public ICommand TerugCommand { get; }

        public InterventieOverviewViewModel(MainViewModel main)
        {
            _main = main;

            // Only back command for now
            TerugCommand = new RelayCommand(() => _main.ShowUserSelection());
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
