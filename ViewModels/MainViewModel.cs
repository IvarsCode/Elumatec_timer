using System;
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
            CurrentView = new UserSelectionViewModel(Db, this);
        }

        public void UserSelected(Medewerker user)
        {
            CurrentUser = user;
            ShowInterventieOverview();
        }

        public void ShowInterventieOverview()
        {
            if (CurrentUser == null)
                throw new InvalidOperationException("No current user set.");

            var overviewVM = new InterventieOverviewViewModel(Db, CurrentUser);

            overviewVM.TerugRequested = ShowUserSelection;
            overviewVM.NieuweInterventieRequested = () => ShowInterventieForm(null);
            overviewVM.OpenInterventieRequested = interventie =>
                ShowInterventieForm(interventie);

            CurrentView = overviewVM;
        }

        public void ShowInterventieForm(Interventie? interventie)
        {
            if (CurrentUser == null)
                throw new InvalidOperationException("No current user set.");

            var formVM = new InterventieFormViewModel(Db, CurrentUser, interventie);

            formVM.CloseRequested = ShowInterventieOverview;

            CurrentView = formVM;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
