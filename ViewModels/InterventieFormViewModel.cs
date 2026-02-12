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
using Elumatec.Tijdregistratie.Pdf.ConvertInterventieToPDF;
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
        private Machine? _selectedMachine;
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

        public List<Machine> MachineSuggestions { get; }

        // Selected machine from search
        public Machine? SelectedMachine
        {
            get => _selectedMachine;
            set
            {
                _selectedMachine = value;
                if (value != null)
                {
                    Machine = value.MachineNaam;
                }
                OnPropertyChanged();
                UpdateStatusAfterWarning();
            }
        }

        private bool _importantWarningActive = false;
        private bool _nonImportantWarningActive = false;
        private bool _cancelWarningActive = false;

        private string _statusMessage = "";
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        private string _statusColor = "Red"; // Red, Yellow, or Green
        public string StatusColor
        {
            get => _statusColor;
            private set
            {
                if (_statusColor != value)
                {
                    _statusColor = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(StatusBackground));
                    OnPropertyChanged(nameof(StatusBorderBrush));
                    OnPropertyChanged(nameof(StatusForeground));
                }
            }
        }

        // Color properties for binding
        public string StatusBackground => _statusColor switch
        {
            "Green" => "#E8F5E9",
            "Yellow" => "#FFF9C4",
            _ => "#FFEBEE"
        };

        public string StatusBorderBrush => _statusColor switch
        {
            "Green" => "#4CAF50",
            "Yellow" => "#FFC107",
            _ => "#F44336"
        };

        public string StatusForeground => _statusColor switch
        {
            "Green" => "#2E7D32",
            "Yellow" => "#F57F17",
            _ => "#C62828"
        };

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

        // Previous calls list
        private List<InterventieCall> _previousCalls = new List<InterventieCall>();
        public List<InterventieCall> PreviousCalls
        {
            get => _previousCalls;
            private set { _previousCalls = value; OnPropertyChanged(); }
        }

        private InterventieCall? _currentlyLoadedCall;
        public InterventieCall? CurrentlyLoadedCall
        {
            get => _currentlyLoadedCall;
            private set
            {
                _currentlyLoadedCall = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsReadOnly));
            }
        }

        private InterventieCall? _pendingCallToLoad;

        // Hover state for tooltip
        private InterventieCall? _hoveredCall;
        public InterventieCall? HoveredCall
        {
            get => _hoveredCall;
            set
            {
                _hoveredCall = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsHoverInfoVisible));
            }
        }

        public bool IsHoverInfoVisible => HoveredCall != null;

        public string Username => _currentUser.Naam;

        // Fields are read-only when viewing a previous call
        public bool IsReadOnly => CurrentlyLoadedCall != null;

        public ICommand StopAndSaveCommand { get; }
        public ICommand DownloadPdfCommand { get; }
        public ICommand LoadPreviousCallCommand { get; }
        public ICommand CancelCommand { get; }

        public InterventieFormViewModel(
            AppDbContext db,
            Medewerker currentUser,
            Interventie? interventie = null,
            InterventieCall? callToLoad = null)
        {
            _db = db;
            _currentUser = currentUser;
            _existingInterventie = interventie;

            // Load all bedrijven for autocomplete
            BedrijvenSuggestions = _db.Bedrijven.ToList();
            MachineSuggestions = _db.Machines.ToList();

            if (interventie != null)
            {
                // Load the associated bedrijf
                _selectedBedrijf = _db.Bedrijven.FirstOrDefault(b => b.klantId == interventie.KlantId);

                // Load all previous calls for this interventie
                PreviousCalls = _db.InterventieCalls
                    .Where(c => c.InterventieId == interventie.Id)
                    .OrderByDescending(c => c.StartCall)
                    .ToList();

                // Check if we're loading a specific previous call
                if (callToLoad != null)
                {
                    // Load the specific call data
                    CurrentlyLoadedCall = callToLoad;
                    ContactpersoonNaam = callToLoad.ContactpersoonNaam ?? "";
                    ContactpersoonEmail = callToLoad.ContactpersoonEmail ?? "";
                    ContactpersoonTelefoon = callToLoad.ContactpersoonTelefoonNummer ?? "";
                    InterneNotities = callToLoad.InterneNotities ?? "";
                    ExterneNotities = callToLoad.ExterneNotities ?? "";
                }
                else
                {
                    // Load the most recent call for prefilling contact info
                    var mostRecentCall = PreviousCalls.FirstOrDefault();

                    if (mostRecentCall != null)
                    {
                        ContactpersoonNaam = mostRecentCall.ContactpersoonNaam ?? "";
                        ContactpersoonEmail = mostRecentCall.ContactpersoonEmail ?? "";
                        ContactpersoonTelefoon = mostRecentCall.ContactpersoonTelefoonNummer ?? "";
                        // Leave notes empty for new call
                        InterneNotities = "";
                        ExterneNotities = "";
                    }
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
                PreviousCalls = new List<InterventieCall>();
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

            // Only start timer if we're NOT loading a previous call
            if (callToLoad == null)
            {
                _timer.Start();
            }

            StopAndSaveCommand = new RelayCommand(StopAndSave);
            DownloadPdfCommand = new RelayCommand(async () => await DownloadPdfAsync());
            LoadPreviousCallCommand = new RelayCommand<InterventieCall>(LoadPreviousCall);
            CancelCommand = new RelayCommand(Cancel);
        }

        private void Cancel()
        {
            // If warning is already active, close without saving
            if (_cancelWarningActive)
            {
                _timer.Stop();
                CloseRequested?.Invoke();
                return;
            }

            // Show warning
            _cancelWarningActive = true;
            StatusMessage = "Weet u zeker dat u wilt annuleren?";
            StatusColor = "Red";
        }

        private void StopAndSave()
        {
            // If we're currently viewing a previous call (not actively recording), just close
            if (CurrentlyLoadedCall != null)
            {
                _timer.Stop();
                CloseRequested?.Invoke();
                return;
            }

            var currentBedrijfsnaam = (Bedrijfsnaam ?? "").Trim();
            var currentMachine = (Machine ?? "").Trim();
            var currentInterneNotities = (InterneNotities ?? "").Trim();
            var currentExterneNotities = (ExterneNotities ?? "").Trim();
            var cpNaam = (ContactpersoonNaam ?? "").Trim();
            var cpEmail = (ContactpersoonEmail ?? "").Trim();
            var cpTelefoon = (ContactpersoonTelefoon ?? "").Trim();

            // Check if all MUST FILL fields are empty
            bool allMustFillEmpty = string.IsNullOrWhiteSpace(currentBedrijfsnaam) &&
                                    string.IsNullOrWhiteSpace(currentMachine) &&
                                    string.IsNullOrWhiteSpace(currentExterneNotities) &&
                                    string.IsNullOrWhiteSpace(cpNaam);

            // If all must-fill fields are empty, just close
            if (allMustFillEmpty)
            {
                _timer.Stop();
                CloseRequested?.Invoke();
                return;
            }

            // Check if all MUST FILL fields are complete
            bool allMustFillComplete = !string.IsNullOrWhiteSpace(currentBedrijfsnaam) &&
                                       !string.IsNullOrWhiteSpace(currentMachine) &&
                                       !string.IsNullOrWhiteSpace(currentExterneNotities) &&
                                       !string.IsNullOrWhiteSpace(cpNaam);

            // Check if optional fields are complete
            bool optionalFieldsComplete = !string.IsNullOrWhiteSpace(cpEmail) &&
                                          !string.IsNullOrWhiteSpace(cpTelefoon);

            // CASE 1: Some but not all must-fill fields are filled
            if (!allMustFillComplete && !_importantWarningActive)
            {
                _importantWarningActive = true;
                StatusMessage = "Niet alle velden zijn ingevuld; als u wilt stoppen zonder opslaan klik dan nog een keer";
                StatusColor = "Red";
                return;
            }

            // CASE 2: Important warning is active - user clicked again
            if (_importantWarningActive)
            {
                // If still incomplete, close without saving
                if (!allMustFillComplete)
                {
                    _timer.Stop();
                    CloseRequested?.Invoke();
                    return;
                }
                // If now complete, reset warning and continue to check optional fields
                _importantWarningActive = false;
            }

            // CASE 3: All must-fill complete, but optional fields missing
            if (allMustFillComplete && !optionalFieldsComplete && !_nonImportantWarningActive)
            {
                _nonImportantWarningActive = true;
                StatusMessage = "Niet alle contactgegevens zijn ingevuld. Als u wilt opslaan zonder deze gegevens klik dan nog een keer";
                StatusColor = "Yellow";
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
                    StatusColor = "Red";
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

        private void LoadPreviousCall(InterventieCall? call)
        {
            if (call == null) return;

            // If we're currently viewing a previous call (not actively recording), just load the new call
            if (CurrentlyLoadedCall != null)
            {
                LoadCallData(call);
                return;
            }

            // First, go through the same validation as "Pauzeren"
            var currentBedrijfsnaam = (Bedrijfsnaam ?? "").Trim();
            var currentMachine = (Machine ?? "").Trim();
            var currentExterneNotities = (ExterneNotities ?? "").Trim();
            var cpNaam = (ContactpersoonNaam ?? "").Trim();
            var cpEmail = (ContactpersoonEmail ?? "").Trim();
            var cpTelefoon = (ContactpersoonTelefoon ?? "").Trim();

            // Check if all MUST FILL fields are empty
            bool allMustFillEmpty = string.IsNullOrWhiteSpace(currentBedrijfsnaam) &&
                                    string.IsNullOrWhiteSpace(currentMachine) &&
                                    string.IsNullOrWhiteSpace(currentExterneNotities) &&
                                    string.IsNullOrWhiteSpace(cpNaam);

            // If all must-fill fields are empty, just load the call
            if (allMustFillEmpty)
            {
                LoadCallData(call);
                return;
            }

            // Check if all MUST FILL fields are complete
            bool allMustFillComplete = !string.IsNullOrWhiteSpace(currentBedrijfsnaam) &&
                                       !string.IsNullOrWhiteSpace(currentMachine) &&
                                       !string.IsNullOrWhiteSpace(currentExterneNotities) &&
                                       !string.IsNullOrWhiteSpace(cpNaam);

            // Check if optional fields are complete
            bool optionalFieldsComplete = !string.IsNullOrWhiteSpace(cpEmail) &&
                                          !string.IsNullOrWhiteSpace(cpTelefoon);

            // CASE 1: Some but not all must-fill fields are filled
            if (!allMustFillComplete && !_importantWarningActive)
            {
                _importantWarningActive = true;
                StatusMessage = "Niet alle velden zijn ingevuld; als u wilt stoppen zonder opslaan klik dan nog een keer";
                StatusColor = "Red";

                // Store the call to load after confirmation
                _pendingCallToLoad = call;
                return;
            }

            // CASE 2: Important warning is active - user clicked again
            if (_importantWarningActive && _pendingCallToLoad == call)
            {
                // If still incomplete, load without saving
                if (!allMustFillComplete)
                {
                    LoadCallData(call);
                    return;
                }
                // If now complete, reset warning and continue to check optional fields
                _importantWarningActive = false;
                _pendingCallToLoad = null;
            }

            // CASE 3: All must-fill complete, but optional fields missing
            if (allMustFillComplete && !optionalFieldsComplete && !_nonImportantWarningActive)
            {
                _nonImportantWarningActive = true;
                StatusMessage = "Niet alle contactgegevens zijn ingevuld. Als u wilt opslaan zonder deze gegevens klik dan nog een keer";
                StatusColor = "Yellow";

                // Store the call to load after confirmation
                _pendingCallToLoad = call;
                return;
            }

            // CASE 4: Non-important warning is active - save and then load
            if (_nonImportantWarningActive && _pendingCallToLoad == call)
            {
                SaveCurrentCallThenLoad(call);
                return;
            }

            // If all fields complete, save and load
            if (allMustFillComplete && optionalFieldsComplete)
            {
                SaveCurrentCallThenLoad(call);
            }
        }

        private void SaveCurrentCallThenLoad(InterventieCall callToLoad)
        {
            // Validate that a bedrijf is selected
            if (_selectedBedrijf == null && _existingInterventie == null)
            {
                StatusMessage = "Selecteer een bedrijf voordat u opslaat";
                StatusColor = "Red";
                return;
            }

            var currentBedrijfsnaam = (Bedrijfsnaam ?? "").Trim();
            var currentMachine = (Machine ?? "").Trim();
            var currentInterneNotities = (InterneNotities ?? "").Trim();
            var currentExterneNotities = (ExterneNotities ?? "").Trim();
            var cpNaam = (ContactpersoonNaam ?? "").Trim();
            var cpEmail = (ContactpersoonEmail ?? "").Trim();
            var cpTelefoon = (ContactpersoonTelefoon ?? "").Trim();

            var callEndTime = DateTime.Now;

            // Save current call
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

            // Reload the previous calls list to include the newly saved call
            if (_existingInterventie != null)
            {
                PreviousCalls = _db.InterventieCalls
                    .Where(c => c.InterventieId == _existingInterventie.Id)
                    .OrderByDescending(c => c.StartCall)
                    .ToList();
            }

            // Reset warnings
            _importantWarningActive = false;
            _nonImportantWarningActive = false;
            _pendingCallToLoad = null;
            StatusMessage = "";

            // Load the selected call
            LoadCallData(callToLoad);
        }

        private void LoadCallData(InterventieCall call)
        {
            _timer.Stop();

            CurrentlyLoadedCall = call;
            ContactpersoonNaam = call.ContactpersoonNaam ?? "";
            ContactpersoonEmail = call.ContactpersoonEmail ?? "";
            ContactpersoonTelefoon = call.ContactpersoonTelefoonNummer ?? "";
            InterneNotities = call.InterneNotities ?? "";
            ExterneNotities = call.ExterneNotities ?? "";

            // Reset warnings
            _importantWarningActive = false;
            _nonImportantWarningActive = false;
            _cancelWarningActive = false;
            _pendingCallToLoad = null;
            StatusMessage = "";

            // Reset call start time since we're viewing a previous call
            _callStartTime = null;
            _currentCallTime = TimeSpan.Zero;
            TimerDisplay = "00:00:00";
        }

        private void UpdateStatusAfterWarning()
        {
            // Reset cancel warning when user types
            if (_cancelWarningActive)
            {
                _cancelWarningActive = false;
                StatusMessage = "";
            }

            // Only update if a warning is currently active
            if (!_importantWarningActive && !_nonImportantWarningActive) return;

            var currentBedrijfsnaam = (Bedrijfsnaam ?? "").Trim();
            var currentMachine = (Machine ?? "").Trim();
            var currentExterneNotities = (ExterneNotities ?? "").Trim();
            var cpNaam = (ContactpersoonNaam ?? "").Trim();
            var cpEmail = (ContactpersoonEmail ?? "").Trim();
            var cpTelefoon = (ContactpersoonTelefoon ?? "").Trim();

            bool allMustFillComplete = !string.IsNullOrWhiteSpace(currentBedrijfsnaam) &&
                                       !string.IsNullOrWhiteSpace(currentMachine) &&
                                       !string.IsNullOrWhiteSpace(currentExterneNotities) &&
                                       !string.IsNullOrWhiteSpace(cpNaam);

            bool optionalFieldsComplete = !string.IsNullOrWhiteSpace(cpEmail) &&
                                          !string.IsNullOrWhiteSpace(cpTelefoon);

            bool allFieldsComplete = allMustFillComplete && optionalFieldsComplete;

            // If important warning is active
            if (_importantWarningActive)
            {
                if (allFieldsComplete)
                {
                    StatusMessage = "Alle velden ingevuld, door te pauzeren wordt de interventie opgeslagen";
                    StatusColor = "Green";
                }
                else if (allMustFillComplete)
                {
                    StatusMessage = "Niet alle contactgegevens zijn ingevuld. Als u wilt opslaan zonder deze gegevens klik dan nog een keer";
                    StatusColor = "Yellow";
                }
                else
                {
                    StatusMessage = "Contactgegevens zijn niet ingevuld; als u wilt stoppen zonder opslaan klik dan nog een keer";
                    StatusColor = "Red";
                }
            }

            // If non-important warning is active
            if (_nonImportantWarningActive)
            {
                if (allFieldsComplete)
                {
                    StatusMessage = "Alle velden ingevuld, door te pauzeren wordt de interventie opgeslagen";
                    StatusColor = "Green";
                }
                else
                {
                    StatusMessage = "Niet alle contactgegevens zijn ingevuld. Als u wilt opslaan zonder deze gegevens klik dan nog een keer";
                    StatusColor = "Yellow";
                }
            }
        }

        private async Task DownloadPdfAsync()
        {
            _timer.Stop();

            try
            {
                var pdfGenerator = new ServiceBonPdf();
                string pdfPath = pdfGenerator.GeneratePdf(
                    Bedrijfsnaam,
                    Machine,
                    InterneNotities,
                    ExterneNotities,
                    Username,
                    _totalTime
                );

                // Open the PDF file
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = pdfPath,
                    UseShellExecute = true
                });

                PdfDownloaded = true;
                await Task.Delay(1500);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating PDF: {ex.Message}");
                StatusMessage = $"Fout bij PDF generatie: {ex.Message}";
                StatusColor = "Red";
                return;
            }

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