using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Threading;
using Avalonia.Media;
using Elumatec.Tijdregistratie.Data;
using Elumatec.Tijdregistratie.Models;
using Microsoft.EntityFrameworkCore;

namespace Elumatec.Tijdregistratie.ViewModels
{
    public class InterventieFormViewModel : INotifyPropertyChanged
    {
        private readonly AppDbContext _db;
        private readonly DispatcherTimer _timer;
        private readonly Interventie? _existingInterventie;
        private readonly Medewerker _currentUser;

        private TimeSpan _totalTime; // Total time across all calls
        private TimeSpan _currentCallTime; // Time for current call only
        private DateTime? _callStartTime;

        private Bedrijf? _selectedBedrijf;
        private string _bedrijfsnaam = "";
        private string _machine = "";
        private string _interneNotities = "";
        private string _externeNotities = "";

        public bool IsPrefilled { get; }

        // Bedrijven for autocomplete search
        public List<Bedrijf> BedrijvenSuggestions { get; }

        // Selected company from search
        public Bedrijf? SelectedBedrijf
        {
            get => _selectedBedrijf;
            set
            {
                _selectedBedrijf = value;
                if (value != null)
                {
                    Bedrijfsnaam = value.BedrijfNaam;
                }
                OnPropertyChanged();
                UpdateStatusAfterWarning();
            }
        }

        private bool _importantWarningActive = false;
        private bool _nonImportantWarningActive = false;


        private string _statusMessage = "";
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        private bool _isStatusGreen;
        public bool IsStatusGreen
        {
            get => _isStatusGreen;
            private set
            {
                if (_isStatusGreen != value)
                {
                    _isStatusGreen = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(StatusForeground));
                }
            }
        }

        private string _contactpersoonNaam = "";
        public string ContactpersoonNaam
        {
            get => _contactpersoonNaam;
            set { _contactpersoonNaam = value; OnPropertyChanged(); UpdateStatusAfterWarning(); }
        }

        private string _contactpersoonEmail = "";
        public string ContactpersoonEmail
        {
            get => _contactpersoonEmail;
            set { _contactpersoonEmail = value; OnPropertyChanged(); UpdateStatusAfterWarning(); }
        }

        private string _contactpersoonTelefoon = "";
        public string ContactpersoonTelefoon
        {
            get => _contactpersoonTelefoon;
            set { _contactpersoonTelefoon = value; OnPropertyChanged(); UpdateStatusAfterWarning(); }
        }

        public IBrush StatusForeground => IsStatusGreen ? Brushes.Green : Brushes.Red;

        public Action? CloseRequested { get; set; }

        public string Bedrijfsnaam
        {
            get => _bedrijfsnaam;
            set { _bedrijfsnaam = value; OnPropertyChanged(); UpdateStatusAfterWarning(); }
        }

        public string Machine
        {
            get => _machine;
            set { _machine = value; OnPropertyChanged(); UpdateStatusAfterWarning(); }
        }

        public string InterneNotities
        {
            get => _interneNotities;
            set { _interneNotities = value; OnPropertyChanged(); UpdateStatusAfterWarning(); }
        }

        public string ExterneNotities
        {
            get => _externeNotities;
            set { _externeNotities = value; OnPropertyChanged(); UpdateStatusAfterWarning(); }
        }

        // Timer display shows CURRENT CALL time only
        private string _timerDisplay = "00:00:00";
        public string TimerDisplay
        {
            get => _timerDisplay;
            private set { _timerDisplay = value; OnPropertyChanged(); }
        }

        // Total time display shows ALL calls combined
        private string _totalTimeDisplay = "00:00:00";
        public string TotalTimeDisplay
        {
            get => _totalTimeDisplay;
            private set { _totalTimeDisplay = value; OnPropertyChanged(); }
        }

        private bool _pdfDownloaded;
        public bool PdfDownloaded
        {
            get => _pdfDownloaded;
            private set { _pdfDownloaded = value; OnPropertyChanged(); }
        }

        public string Username => _currentUser.Naam;

        public ICommand StopAndSaveCommand { get; }
        public ICommand DownloadPdfCommand { get; }

        public InterventieFormViewModel(
            AppDbContext db,
            Medewerker currentUser,
            Interventie? interventie = null)
        {
            _db = db;
            _currentUser = currentUser;
            _existingInterventie = interventie;

            // Load all bedrijven for autocomplete
            BedrijvenSuggestions = _db.Bedrijven.ToList();

            if (interventie != null)
            {
                // Load the associated bedrijf
                _selectedBedrijf = _db.Bedrijven.FirstOrDefault(b => b.klantId == interventie.KlantId);

                // Load the most recent call for this interventie
                var mostRecentCall = _db.InterventieCalls
                    .Where(c => c.InterventieId == interventie.Id)
                    .OrderByDescending(c => c.Id)
                    .FirstOrDefault();

                if (mostRecentCall != null)
                {
                    ContactpersoonNaam = mostRecentCall.ContactpersoonNaam ?? "";
                    ContactpersoonEmail = mostRecentCall.ContactpersoonEmail ?? "";
                    ContactpersoonTelefoon = mostRecentCall.ContactpersoonTelefoonNummer ?? "";
                    // Leave notes empty for new call
                    InterneNotities = "";
                    ExterneNotities = "";
                }

                IsPrefilled = true;
                Bedrijfsnaam = interventie.BedrijfNaam;
                Machine = interventie.Machine;
                _totalTime = TimeSpan.FromSeconds(interventie.TotaleLooptijd);
                TotalTimeDisplay = _totalTime.ToString(@"hh\:mm\:ss");
            }
            else
            {
                IsPrefilled = false;
                _totalTime = TimeSpan.Zero;
                TotalTimeDisplay = "00:00:00";
            }

            // Current call always starts at zero
            _currentCallTime = TimeSpan.Zero;
            TimerDisplay = "00:00:00";

            // Track when this call started
            _callStartTime = DateTime.Now;

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += (_, _) =>
            {
                _currentCallTime = _currentCallTime.Add(TimeSpan.FromSeconds(1));
                TimerDisplay = _currentCallTime.ToString(@"hh\:mm\:ss");

                // Update total time display
                var combinedTime = _totalTime.Add(_currentCallTime);
                TotalTimeDisplay = combinedTime.ToString(@"hh\:mm\:ss");
            };
            _timer.Start();

            StopAndSaveCommand = new RelayCommand(StopAndSave);
            DownloadPdfCommand = new RelayCommand(async () => await DownloadPdfAsync());
        }

        private void StopAndSave()
        {
            var currentBedrijfsnaam = (Bedrijfsnaam ?? "").Trim();
            var currentMachine = (Machine ?? "").Trim();
            var currentInterneNotities = (InterneNotities ?? "").Trim();
            var currentExterneNotities = (ExterneNotities ?? "").Trim();
            var cpNaam = (ContactpersoonNaam ?? "").Trim();
            var cpEmail = (ContactpersoonEmail ?? "").Trim();
            var cpTelefoon = (ContactpersoonTelefoon ?? "").Trim();

            // If everything is empty, stop timer and close
            if (string.IsNullOrWhiteSpace(currentBedrijfsnaam) &&
                string.IsNullOrWhiteSpace(currentMachine) &&
                string.IsNullOrWhiteSpace(currentExterneNotities) &&
                string.IsNullOrWhiteSpace(cpNaam))
            {
                _timer.Stop();
                CloseRequested?.Invoke();
                return;
            }

            // Important fields warning workflow (will NOT save)
            if (!_importantWarningActive &&
                !IsComplete(currentBedrijfsnaam, currentMachine, currentExterneNotities, cpNaam))
            {
                _importantWarningActive = true;
                StatusMessage = "Contactgegevens zijn niet ingevuld; als u wilt stoppen zonder opslaan klik dan nog een keer";
                IsStatusGreen = false;
                return;
            }

            // Handle important warning confirmation
            if (_importantWarningActive)
            {
                // If still incomplete, close without saving
                if (!IsComplete(currentBedrijfsnaam, currentMachine, currentExterneNotities, cpNaam))
                {
                    _timer.Stop();
                    CloseRequested?.Invoke();
                    return;
                }
                // If now complete, reset warning and continue to save logic
                _importantWarningActive = false;
            }

            // Non-important fields warning workflow (will still save after confirmation)
            if (!_nonImportantWarningActive && !IsComplete(cpEmail, cpTelefoon))
            {
                _nonImportantWarningActive = true;
                StatusMessage = "Email en/of telefoonnummer ontbreken. Klik nog een keer om toch op te slaan";
                IsStatusGreen = false;
                return;
            }

            // If we reach here, save the intervention
            SaveInterventie();

            void SaveInterventie()
            {
                // Validate that a bedrijf is selected
                if (_selectedBedrijf == null && _existingInterventie == null)
                {
                    StatusMessage = "Selecteer een bedrijf voordat u opslaat";
                    IsStatusGreen = false;
                    return;
                }

                var callEndTime = DateTime.Now;

                // Use the repository to save
                InterventieFormRepository.Save(
                    db: _db,
                    existing: _existingInterventie,
                    bedrijfsnaam: currentBedrijfsnaam,
                    machine: currentMachine,
                    klantId: _selectedBedrijf?.Id ?? _existingInterventie!.KlantId,
                    medewerkerId: _currentUser.Id,
                    contactpersoonNaam: cpNaam,
                    contactpersoonEmail: cpEmail,
                    contactpersoonTelefoon: cpTelefoon,
                    interneNotities: currentInterneNotities,
                    externeNotities: currentExterneNotities,
                    callStartTime: _callStartTime,
                    callEndTime: callEndTime
                );

                _timer.Stop();
                CloseRequested?.Invoke();
            }
        }

        // Overload for important fields only
        private bool IsComplete(string bedrijfsnaam, string machine, string externe, string cpNaam)
        {
            return !string.IsNullOrWhiteSpace(bedrijfsnaam) &&
                   !string.IsNullOrWhiteSpace(machine) &&
                   !string.IsNullOrWhiteSpace(externe) &&
                   !string.IsNullOrWhiteSpace(cpNaam);
        }

        // Overload for email and phone only
        private bool IsComplete(string cpEmail, string cpTelefoon)
        {
            return !string.IsNullOrWhiteSpace(cpEmail) &&
                   !string.IsNullOrWhiteSpace(cpTelefoon);
        }

        // Full completion check (all fields)
        private bool IsComplete()
        {
            return !string.IsNullOrWhiteSpace(Bedrijfsnaam) &&
                   !string.IsNullOrWhiteSpace(Machine) &&
                   !string.IsNullOrWhiteSpace(InterneNotities) &&
                   !string.IsNullOrWhiteSpace(ExterneNotities) &&
                   !string.IsNullOrWhiteSpace(ContactpersoonNaam) &&
                   !string.IsNullOrWhiteSpace(ContactpersoonEmail) &&
                   !string.IsNullOrWhiteSpace(ContactpersoonTelefoon);
        }

        private void UpdateStatusAfterWarning()
        {
            if (!_importantWarningActive) return;

            if (IsComplete())
            {
                StatusMessage = "Interventie zal worden opgeslagen door deze knop";
                IsStatusGreen = true;
            }
            else
            {
                StatusMessage = "Door niet alles in te vullen wordt deze informatie niet opgeslagen. Druk nog een keer om dit te bevestigen";
                IsStatusGreen = false;
            }
        }

        private async Task DownloadPdfAsync()
        {
            _timer.Stop();
            PdfDownloaded = true;
            await Task.Delay(1500);
            CloseRequested?.Invoke();
        }

        public void StopTimer()
        {
            _timer.Stop();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}