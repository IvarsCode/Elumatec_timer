using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Elumatec.Tijdregistratie.Data;
using Elumatec.Tijdregistratie.Models;

namespace Elumatec.Tijdregistratie.ViewModels
{
    public class InterventieOverviewViewModel : INotifyPropertyChanged
    {
        private readonly MainViewModel _main;

        public Medewerker? CurrentUser => _main.CurrentUser;

        public string GebruikerNaam =>
            CurrentUser != null ? CurrentUser.Naam : "Onbekend";

        public ObservableCollection<Interventie> Interventies { get; }
            = new ObservableCollection<Interventie>();

        public ICommand TerugCommand { get; }
        public ICommand NieuweInterventieCommand { get; }
        public ICommand OpenInterventieCommand { get; }

        public InterventieOverviewViewModel(MainViewModel main)
        {
            _main = main;

            TerugCommand = new RelayCommand(() =>
                _main.ShowUserSelection());

            // Ready for later implementation
            NieuweInterventieCommand = new RelayCommand(() =>
            {
                // _main.ShowNieuweInterventie(); 
            });

            OpenInterventieCommand = new RelayCommand<Interventie>(interventie =>
            {
                if (interventie == null)
                    return;

                // _main.ShowInterventieDetail(interventie); (future)
            });

            LoadInterventies();
        }

        private void LoadInterventies()
        {
            Interventies.Clear();

            var interventies = InterventieRepository.GetAll(_main.Db);
            foreach (var interventie in interventies)
                Interventies.Add(interventie);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
