using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Elumatec.Tijdregistratie.Data;
using Elumatec.Tijdregistratie.Models;

namespace Elumatec.Tijdregistratie.ViewModels
{
    public class UserSelectionViewModel : INotifyPropertyChanged
    {
        // Observable collection bound to ItemsControl in XAML
        public ObservableCollection<Medewerker> FilteredUsers { get; } = new();

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (value == _searchText) return;
                _searchText = value;
                OnPropertyChanged();
                FilterUsers(); // search database whenever text changes
            }
        }

        private Medewerker? _selectedUser;
        public Medewerker? SelectedUser
        {
            get => _selectedUser;
            set
            {
                _selectedUser = value;
                OnPropertyChanged();
            }
        }

        private Medewerker? _recentUser;
        public Medewerker? RecentUser
        {
            get => _recentUser;
            set
            {
                _recentUser = value;
                OnPropertyChanged();
            }
        }

        public ICommand SelectUserCommand { get; }

        public UserSelectionViewModel()
        {
            // Command that will be executed when user clicks a result button
            SelectUserCommand = new RelayCommand<Medewerker?>(SelectUser);

            // Optional: load recent user from settings here if needed
        }

        // Filters users using the MedewerkerRepository.Search method (partial match)
        private void FilterUsers()
        {
            FilteredUsers.Clear();

            if (string.IsNullOrWhiteSpace(SearchText))
                return;

            var results = MedewerkerRepository.Search(SearchText);
            foreach (var user in results)
                FilteredUsers.Add(user);
        }

        private void SelectUser(Medewerker? user)
        {
            if (user == null) return;

            SelectedUser = user;
            RecentUser = user;

            // Optional: persist RecentUser in settings or database
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
