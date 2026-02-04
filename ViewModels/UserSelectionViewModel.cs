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
        private readonly AppDbContext _db;

        public ObservableCollection<Medewerker> FilteredUsers { get; } = new();

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText == value) return;
                _searchText = value;
                OnPropertyChanged();
                FilterUsers();
            }
        }

        private Medewerker? _selectedUser;
        public Medewerker? SelectedUser
        {
            get => _selectedUser;
            set
            {
                if (_selectedUser == value) return;
                _selectedUser = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasSelectedUser));
            }
        }

        public bool HasSelectedUser => SelectedUser != null;

        private Medewerker? _recentUser;
        public Medewerker? RecentUser
        {
            get => _recentUser;
            set
            {
                if (_recentUser == value) return;
                _recentUser = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasRecentUser));
            }
        }

        public bool HasRecentUser => RecentUser != null;

        public ICommand SelectUserCommand { get; }

        public UserSelectionViewModel(AppDbContext db)
        {
            _db = db;

            SelectUserCommand = new RelayCommand<Medewerker?>(SelectUser);

            // 🔹 LOAD recent user from AppState
            RecentUser = MedewerkerRepository.GetRecentUser(_db);
        }

        private void FilterUsers()
        {
            FilteredUsers.Clear();

            if (string.IsNullOrWhiteSpace(SearchText))
                return;

            try
            {
                // 🔹 Use repository Search method (top 4 matches)
                var results = MedewerkerRepository.Search(_db, SearchText);

                foreach (var user in results)
                    FilteredUsers.Add(user);
            }
            catch (Exception ex)
            {
                // Optional: log for debugging
                Console.WriteLine($"[FilterUsers] Exception: {ex}");
            }
        }

        private void SelectUser(Medewerker? user)
        {
            if (user == null) return;

            SelectedUser = user;
            RecentUser = user;

            // 🔹 SAVE recent user to AppState
            MedewerkerRepository.SaveRecentUser(_db, user.Id);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
