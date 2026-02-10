using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Elumatec.Tijdregistratie.Data;
using Elumatec.Tijdregistratie.Models;

namespace Elumatec.Tijdregistratie.ViewModels
{
    public class InterventieOverviewViewModel : INotifyPropertyChanged
    {
        private readonly AppDbContext _db;
        private readonly Medewerker _currentUser;

        public string GebruikerNaam => _currentUser.Naam;

        public ObservableCollection<InterventieItemViewModel> Interventies { get; }
            = new ObservableCollection<InterventieItemViewModel>();

        // Commands
        public ICommand TerugCommand { get; }
        public ICommand NieuweInterventieCommand { get; }
        public ICommand OpenInterventieCommand { get; }

        // Navigation callbacks (set by MainViewModel)
        public Action? TerugRequested { get; set; }
        public Action? NieuweInterventieRequested { get; set; }
        public Action<Interventie>? OpenInterventieRequested { get; set; }

        public InterventieOverviewViewModel(AppDbContext db, Medewerker currentUser)
        {
            _db = db;
            _currentUser = currentUser;

            // Commands trigger the callbacks
            TerugCommand = new RelayCommand(() => TerugRequested?.Invoke());
            NieuweInterventieCommand = new RelayCommand(() => NieuweInterventieRequested?.Invoke());
            OpenInterventieCommand = new RelayCommand<InterventieItemViewModel>(item =>
            {
                if (item != null)
                    OpenInterventieRequested?.Invoke(item.Interventie);
            });

            LoadInterventies();
        }

        private void LoadInterventies()
        {
            Interventies.Clear();

            var result = InterventieRepository.GetFiltered(
                _db,
                SelectedFilter,
                SearchText,
                FromDate,
                ToDate);

            foreach (var interventie in result)
                Interventies.Add(new InterventieItemViewModel(interventie));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        // Filter options
        public Array FilterTypes => Enum.GetValues(typeof(InterventieFilterType));

        private InterventieFilterType _selectedFilter = InterventieFilterType.Bedrijfsnaam;
        public InterventieFilterType SelectedFilter
        {
            get => _selectedFilter;
            set
            {
                _selectedFilter = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsTextSearch));
                OnPropertyChanged(nameof(IsDateSearch));
                LoadInterventies();
            }
        }

        private string? _searchText;
        public string? SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                LoadInterventies();
            }
        }

        private DateTimeOffset? _fromDate;
        public DateTimeOffset? FromDate
        {
            get => _fromDate;
            set
            {
                _fromDate = value;
                OnPropertyChanged();
                LoadInterventies();
            }
        }

        private DateTimeOffset? _toDate;
        public DateTimeOffset? ToDate
        {
            get => _toDate;
            set
            {
                _toDate = value;
                OnPropertyChanged();
                LoadInterventies();
            }
        }

        public bool IsTextSearch =>
            SelectedFilter == InterventieFilterType.Bedrijfsnaam ||
            SelectedFilter == InterventieFilterType.Machine;

        public bool IsDateSearch =>
            SelectedFilter == InterventieFilterType.Datum;
    }

    public class InterventieItemViewModel
    {
        public Interventie Interventie { get; }

        public InterventieItemViewModel(Interventie interventie)
        {
            Interventie = interventie;
        }

        public string Machine => Interventie.Machine;
        public string Bedrijfsnaam => Interventie.BedrijfNaam;

        public DateTime DatumRecentsteCall
        {
            get
            {
                var mostRecentCall = Interventie.Calls
                    .Where(c => c.StartCall.HasValue)
                    .OrderByDescending(c => c.StartCall)
                    .FirstOrDefault();

                return mostRecentCall?.StartCall ?? DateTime.MinValue;
            }
        }

        public int AantalCalls => Interventie.Calls?.Count ?? 0;

        public string InterneNotitiesKort
        {
            get
            {
                var mostRecentCall = Interventie.Calls?
                    .OrderByDescending(c => c.Id)
                    .FirstOrDefault();
                return Trim(mostRecentCall?.InterneNotities);
            }
        }

        public string ExterneNotitiesKort
        {
            get
            {
                var mostRecentCall = Interventie.Calls?
                    .OrderByDescending(c => c.Id)
                    .FirstOrDefault();
                return Trim(mostRecentCall?.ExterneNotities);
            }
        }

        private static string Trim(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            return text.Length <= 80 ? text : text.Substring(0, 80) + "...";
        }
    }
}