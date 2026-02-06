using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Threading;
using Avalonia.Media;
using Elumatec.Tijdregistratie.Data;
using Elumatec.Tijdregistratie.Models;

namespace Elumatec.Tijdregistratie.ViewModels
{
    public class InterventieFormViewModel : INotifyPropertyChanged
    {
        private readonly AppDbContext _db;
        private readonly DispatcherTimer _timer;
        private readonly Interventie? _existingInterventie;
        private readonly Medewerker _currentUser;

        private TimeSpan _currentTime;

        private string _bedrijfsnaam = "";
        private string _machine = "";
        private DateTimeOffset _datumLaatsteCall = DateTimeOffset.Now;
        private int _aantalCalls = 1;
        private string _interneNotities = "";
        private string _externeNotities = "";

        public bool IsPrefilled { get; }

        private bool _warningActive = false;

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
                    OnPropertyChanged(nameof(StatusForeground)); // Trigger UI update
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

        // Navigation callback 
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

        public DateTimeOffset DatumLaatsteCall
        {
            get => _datumLaatsteCall;
            set { _datumLaatsteCall = value; OnPropertyChanged(); }
        }

        public int AantalCalls
        {
            get => _aantalCalls;
            set { _aantalCalls = value; OnPropertyChanged(); }
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

        private string _timerDisplay = "00:00:00";
        public string TimerDisplay
        {
            get => _timerDisplay;
            private set { _timerDisplay = value; OnPropertyChanged(); }
        }

        private bool _pdfDownloaded;
        public bool PdfDownloaded
        {
            get => _pdfDownloaded;
            private set { _pdfDownloaded = value; OnPropertyChanged(); }
        }

        public string Username => _currentUser.Naam;

        // Commands for buttons
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

            if (interventie != null)
            {
                var contact = _db.Contactpersonen.Find(interventie.ContactpersoonId);
                if (contact != null)
                {
                    ContactpersoonNaam = contact.Naam ?? "";
                    ContactpersoonEmail = contact.Email ?? "";
                    ContactpersoonTelefoon = contact.TelefoonNummer ?? "";
                }


                IsPrefilled = true;

                Bedrijfsnaam = interventie.Bedrijfsnaam;
                Machine = interventie.Machine;

                DatumLaatsteCall = interventie.DatumRecentsteCall != null
                    ? new DateTimeOffset(interventie.DatumRecentsteCall.Value)
                    : DateTimeOffset.Now;

                AantalCalls = interventie.AantalCalls;
                _currentTime = TimeSpan.FromSeconds(interventie.TotaleLooptijd);

                InterneNotities = interventie.InterneNotities ?? "";
                ExterneNotities = interventie.ExterneNotities ?? "";
            }
            else
            {
                IsPrefilled = false;
                _currentTime = TimeSpan.Zero;
                DatumLaatsteCall = DateTimeOffset.Now;
            }

            TimerDisplay = _currentTime.ToString(@"hh\:mm\:ss");

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += (_, _) =>
            {
                _currentTime = _currentTime.Add(TimeSpan.FromSeconds(1));
                TimerDisplay = _currentTime.ToString(@"hh\:mm\:ss");
            };
            _timer.Start();

            StopAndSaveCommand = new RelayCommand(StopAndSave);
            DownloadPdfCommand = new RelayCommand(async () => await DownloadPdfAsync());
        }

        private void StopAndSave()
        {
            _timer.Stop();

            var currentBedrijfsnaam = (Bedrijfsnaam ?? "").Trim();
            var currentMachine = (Machine ?? "").Trim();
            var currentInterneNotities = (InterneNotities ?? "").Trim();
            var currentExterneNotities = (ExterneNotities ?? "").Trim();
            var currentDatum = DatumLaatsteCall != default ? DatumLaatsteCall : DateTimeOffset.Now;

            var cpNaam = (ContactpersoonNaam ?? "").Trim();
            var cpEmail = (ContactpersoonEmail ?? "").Trim();
            var cpTelefoon = (ContactpersoonTelefoon ?? "").Trim();

            if (string.IsNullOrWhiteSpace(currentBedrijfsnaam) &&
                string.IsNullOrWhiteSpace(currentMachine) &&
                string.IsNullOrWhiteSpace(currentInterneNotities) &&
                string.IsNullOrWhiteSpace(currentExterneNotities) &&
                string.IsNullOrWhiteSpace(cpNaam) &&
                string.IsNullOrWhiteSpace(cpEmail) &&
                string.IsNullOrWhiteSpace(cpTelefoon))
            {
                CloseRequested?.Invoke();
                return;
            }

            if (!_warningActive && !IsComplete(currentBedrijfsnaam, currentMachine, currentInterneNotities, currentExterneNotities, cpNaam, cpEmail, cpTelefoon))
            {
                _warningActive = true;
                StatusMessage = "Door niet alles in te vullen wordt deze informatie niet opgeslagen. Druk nog een keer om dit te bevestigen";
                IsStatusGreen = false;
                return;
            }

            // Helper: Determine ContactpersoonId for new interventie
            int DetermineContactpersoonId()
            {
                if (_existingInterventie != null)
                {
                    // Updating existing interventie, just use existing ContactpersoonId
                    return _existingInterventie.ContactpersoonId;
                }

                // Try to find a full match
                var existingCP = _db.Contactpersonen.FirstOrDefault(c =>
                    c.Naam == cpNaam &&
                    c.Email == cpEmail &&
                    c.TelefoonNummer == cpTelefoon);

                if (existingCP != null)
                    return existingCP.Id;

                // No match → return -1 so repo can create new
                return -1;
            }

            void SaveInterventie()
            {
                var contactpersoonId = DetermineContactpersoonId();

                InterventieFormRepository.Save(
                    _db,
                    _existingInterventie,
                    currentBedrijfsnaam,
                    currentMachine,
                    currentDatum,
                    AantalCalls,
                    (int)_currentTime.TotalSeconds,
                    currentInterneNotities,
                    currentExterneNotities,
                    contactpersoonId,
                    cpNaam,
                    cpEmail,
                    cpTelefoon
                );

                CloseRequested?.Invoke();
            }

            if (_warningActive)
            {
                if (!IsComplete(currentBedrijfsnaam, currentMachine, currentInterneNotities, currentExterneNotities, cpNaam, cpEmail, cpTelefoon))
                {
                    CloseRequested?.Invoke();
                    return;
                }

                StatusMessage = "Interventie zal worden opgeslagen door deze knop";
                IsStatusGreen = true;

                Dispatcher.UIThread.Post(SaveInterventie);
                return;
            }

            if (IsComplete(currentBedrijfsnaam, currentMachine, currentInterneNotities, currentExterneNotities, cpNaam, cpEmail, cpTelefoon))
            {
                SaveInterventie();
                return;
            }
        }



        private bool IsComplete(string bedrijfsnaam, string machine, string interne, string externe,
                        string cpNaam, string cpEmail, string cpTelefoon)
        {
            return !string.IsNullOrWhiteSpace(bedrijfsnaam) &&
                   !string.IsNullOrWhiteSpace(machine) &&
                   !string.IsNullOrWhiteSpace(interne) &&
                   !string.IsNullOrWhiteSpace(externe) &&
                   !string.IsNullOrWhiteSpace(cpNaam) &&
                   !string.IsNullOrWhiteSpace(cpEmail) &&
                   !string.IsNullOrWhiteSpace(cpTelefoon);
        }

        private bool IsComplete()
        {
            return !string.IsNullOrWhiteSpace(Bedrijfsnaam)
                   && !string.IsNullOrWhiteSpace(Machine)
                   && !string.IsNullOrWhiteSpace(InterneNotities)
                   && !string.IsNullOrWhiteSpace(ExterneNotities);
        }

        private void UpdateStatusAfterWarning()
        {
            if (!_warningActive)
                return;

            if (IsComplete())
            {
                StatusMessage = "Interventie zal worden opgeslagen door deze knop";
                IsStatusGreen = true;
            }
            else
            {
                StatusMessage =
                    "Door niet alles in te vullen wordt deze informatie niet opgeslagen. Druk nog een keer om dit te bevestigen";
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
