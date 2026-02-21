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

        private TimeSpan _totalTime;
        private TimeSpan _currentCallTime;
        private DateTime? _callStartTime;

        private Bedrijf? _selectedBedrijf;
        private Machine? _selectedMachine;
        private string _bedrijfsnaam = "";
        private string _machine = "";
        private string _interneNotities = "";
        private string _externeNotities = "";

        private bool _isEditingMode = false;
        public bool IsEditingMode
        {
            get => _isEditingMode;
            set
            {
                _isEditingMode = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsReadOnly));
                OnPropertyChanged(nameof(FieldsEditable));
            }
        }

        private bool _showNieuweCallButton = false;
        public bool ShowNieuweCallButton
        {
            get => _showNieuweCallButton;
            set { _showNieuweCallButton = value; OnPropertyChanged(); }
        }

        public bool FieldsEditable => CurrentlyLoadedCall != null || (!IsEditingMode && !ShowNewCallConfirmation);
        public bool IsReadOnly => !FieldsEditable;

        // Dirty tracking
        private string _savedContactpersoonNaam = "";
        private string _savedContactpersoonEmail = "";
        private string _savedContactpersoonTelefoon = "";
        private string _savedInterneNotities = "";
        private string _savedExterneNotities = "";

        public bool ContactpersoonNaamDirty => CurrentlyLoadedCall != null && ContactpersoonNaam != _savedContactpersoonNaam;
        public bool ContactpersoonEmailDirty => CurrentlyLoadedCall != null && ContactpersoonEmail != _savedContactpersoonEmail;
        public bool ContactpersoonTelefoonDirty => CurrentlyLoadedCall != null && ContactpersoonTelefoon != _savedContactpersoonTelefoon;
        public bool InterneNotitiesDirty => CurrentlyLoadedCall != null && InterneNotities != _savedInterneNotities;
        public bool ExterneNotitiesDirty => CurrentlyLoadedCall != null && ExterneNotities != _savedExterneNotities;

        private bool _showNewCallConfirmation;
        public bool ShowNewCallConfirmation
        {
            get => _showNewCallConfirmation;
            set { _showNewCallConfirmation = value; OnPropertyChanged(); }
        }

        public bool IsPrefilled { get; }

        public List<Bedrijf> BedrijvenSuggestions { get; }
        public List<Machine> MachineSuggestions { get; }

        public Bedrijf? SelectedBedrijf
        {
            get => _selectedBedrijf;
            set
            {
                _selectedBedrijf = value;
                if (value != null) Bedrijfsnaam = value.BedrijfNaam;
                OnPropertyChanged();
                UpdateStatusAfterWarning();
            }
        }

        public Machine? SelectedMachine
        {
            get => _selectedMachine;
            set
            {
                _selectedMachine = value;
                if (value != null) Machine = value.MachineNaam;
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

        private string _statusColor = "Red";
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
            set
            {
                _contactpersoonNaam = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ContactpersoonNaamDirty));
                UpdateStatusAfterWarning();
            }
        }

        private string _contactpersoonEmail = "";
        public string ContactpersoonEmail
        {
            get => _contactpersoonEmail;
            set
            {
                _contactpersoonEmail = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ContactpersoonEmailDirty));
                UpdateStatusAfterWarning();
            }
        }

        private string _contactpersoonTelefoon = "";
        public string ContactpersoonTelefoon
        {
            get => _contactpersoonTelefoon;
            set
            {
                _contactpersoonTelefoon = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ContactpersoonTelefoonDirty));
                UpdateStatusAfterWarning();
            }
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
            set
            {
                _interneNotities = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(InterneNotitiesDirty));
                UpdateStatusAfterWarning();
            }
        }

        public string ExterneNotities
        {
            get => _externeNotities;
            set
            {
                _externeNotities = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ExterneNotitiesDirty));
                UpdateStatusAfterWarning();
            }
        }

        private string _timerDisplay = "00:00:00";
        public string TimerDisplay
        {
            get => _timerDisplay;
            private set { _timerDisplay = value; OnPropertyChanged(); }
        }

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
                OnPropertyChanged(nameof(FieldsEditable));
            }
        }

        private InterventieCall? _pendingCallToLoad;

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

        public ICommand StopAndSaveCommand { get; }
        public ICommand DownloadPdfCommand { get; }
        public ICommand LoadPreviousCallCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand ConfirmNewCallCommand { get; }
        public ICommand DenyNewCallCommand { get; }
        public ICommand NieuweCallStartenCommand { get; }
        public ICommand SaveContactpersoonNaamCommand { get; }
        public ICommand SaveContactpersoonEmailCommand { get; }
        public ICommand SaveContactpersoonTelefoonCommand { get; }
        public ICommand SaveInterneNotitiesCommand { get; }
        public ICommand SaveExterneNotitiesCommand { get; }

        public InterventieFormViewModel(
            AppDbContext db,
            Medewerker currentUser,
            Interventie? interventie = null,
            InterventieCall? callToLoad = null)
        {
            _db = db;
            _currentUser = currentUser;
            _existingInterventie = interventie;

            BedrijvenSuggestions = _db.Bedrijven.ToList();
            MachineSuggestions = _db.Machines.ToList();

            if (interventie != null)
            {
                _selectedBedrijf = _db.Bedrijven.FirstOrDefault(b => b.klantId == interventie.KlantId);

                PreviousCalls = _db.InterventieCalls
                    .Where(c => c.InterventieId == interventie.Id)
                    .OrderByDescending(c => c.StartCall)
                    .ToList();

                if (callToLoad != null)
                {
                    CurrentlyLoadedCall = callToLoad;
                    ContactpersoonNaam = callToLoad.ContactpersoonNaam ?? "";
                    ContactpersoonEmail = callToLoad.ContactpersoonEmail ?? "";
                    ContactpersoonTelefoon = callToLoad.ContactpersoonTelefoonNummer ?? "";
                    InterneNotities = callToLoad.InterneNotities ?? "";
                    ExterneNotities = callToLoad.ExterneNotities ?? "";
                }
                else
                {
                    var mostRecentCall = PreviousCalls.FirstOrDefault();
                    if (mostRecentCall != null)
                    {
                        ContactpersoonNaam = mostRecentCall.ContactpersoonNaam ?? "";
                        ContactpersoonEmail = mostRecentCall.ContactpersoonEmail ?? "";
                        ContactpersoonTelefoon = mostRecentCall.ContactpersoonTelefoonNummer ?? "";
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

            _currentCallTime = TimeSpan.Zero;
            TimerDisplay = "00:00:00";

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (_, _) =>
            {
                _currentCallTime = _currentCallTime.Add(TimeSpan.FromSeconds(1));
                TimerDisplay = _currentCallTime.ToString(@"hh\:mm\:ss");
                TotalTimeDisplay = _totalTime.Add(_currentCallTime).ToString(@"hh\:mm\:ss");
            };

            if (callToLoad == null)
            {
                if (IsPrefilled)
                {
                    ShowNewCallConfirmation = true;
                }
                else
                {
                    _callStartTime = DateTime.Now;
                    _timer.Start();
                }
            }

            StopAndSaveCommand = new RelayCommand(StopAndSave);
            DownloadPdfCommand = new RelayCommand(async () => await DownloadPdfAsync());
            LoadPreviousCallCommand = new RelayCommand<InterventieCall>(LoadPreviousCall);
            CancelCommand = new RelayCommand(Cancel);

            ConfirmNewCallCommand = new RelayCommand(() =>
            {
                ShowNewCallConfirmation = false;
                IsEditingMode = false;
                ShowNieuweCallButton = false;
                CurrentlyLoadedCall = null;
                _callStartTime = DateTime.Now;
                _currentCallTime = TimeSpan.Zero;
                TimerDisplay = "00:00:00";
                _timer.Start();
            });

            DenyNewCallCommand = new RelayCommand(() =>
            {
                ShowNewCallConfirmation = false;
                IsEditingMode = true;
                ShowNieuweCallButton = true;

                var mostRecent = PreviousCalls.FirstOrDefault();
                if (mostRecent != null)
                    LoadCallData(mostRecent);
            });

            NieuweCallStartenCommand = new RelayCommand(() =>
            {
                IsEditingMode = false;
                ShowNieuweCallButton = false;
                CurrentlyLoadedCall = null;
                var mostRecent = PreviousCalls.FirstOrDefault();
                ContactpersoonNaam = mostRecent?.ContactpersoonNaam ?? "";
                ContactpersoonEmail = mostRecent?.ContactpersoonEmail ?? "";
                ContactpersoonTelefoon = mostRecent?.ContactpersoonTelefoonNummer ?? "";
                InterneNotities = "";
                ExterneNotities = "";
                _callStartTime = DateTime.Now;
                _currentCallTime = TimeSpan.Zero;
                TimerDisplay = "00:00:00";
                _timer.Start();
            });

            SaveContactpersoonNaamCommand = new RelayCommand(SaveField);
            SaveContactpersoonEmailCommand = new RelayCommand(SaveField);
            SaveContactpersoonTelefoonCommand = new RelayCommand(SaveField);
            SaveInterneNotitiesCommand = new RelayCommand(SaveField);
            SaveExterneNotitiesCommand = new RelayCommand(SaveField);
        }

        private void SaveField()
        {
            if (CurrentlyLoadedCall == null) return;

            InterventieFormRepository.UpdateCall(
                db: _db,
                callId: CurrentlyLoadedCall.Id,
                contactpersoonNaam: ContactpersoonNaam.Trim(),
                contactpersoonEmail: ContactpersoonEmail.Trim(),
                contactpersoonTelefoon: ContactpersoonTelefoon.Trim(),
                interneNotities: InterneNotities.Trim(),
                externeNotities: ExterneNotities.Trim()
            );

            _savedContactpersoonNaam = ContactpersoonNaam;
            _savedContactpersoonEmail = ContactpersoonEmail;
            _savedContactpersoonTelefoon = ContactpersoonTelefoon;
            _savedInterneNotities = InterneNotities;
            _savedExterneNotities = ExterneNotities;

            OnPropertyChanged(nameof(ContactpersoonNaamDirty));
            OnPropertyChanged(nameof(ContactpersoonEmailDirty));
            OnPropertyChanged(nameof(ContactpersoonTelefoonDirty));
            OnPropertyChanged(nameof(InterneNotitiesDirty));
            OnPropertyChanged(nameof(ExterneNotitiesDirty));
        }

        private void Cancel()
        {
            if (_cancelWarningActive)
            {
                _timer.Stop();
                CloseRequested?.Invoke();
                return;
            }

            if (IsEditingMode && CurrentlyLoadedCall == null)
            {
                _timer.Stop();
                CloseRequested?.Invoke();
                return;
            }

            _cancelWarningActive = true;
            StatusMessage = "Weet u zeker dat u wilt annuleren?";
            StatusColor = "Red";
        }

        private void StopAndSave()
        {
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

            bool allMustFillEmpty = string.IsNullOrWhiteSpace(currentBedrijfsnaam) &&
                                    string.IsNullOrWhiteSpace(currentMachine) &&
                                    string.IsNullOrWhiteSpace(currentExterneNotities) &&
                                    string.IsNullOrWhiteSpace(cpNaam);

            if (allMustFillEmpty)
            {
                _timer.Stop();
                CloseRequested?.Invoke();
                return;
            }

            bool allMustFillComplete = !string.IsNullOrWhiteSpace(currentBedrijfsnaam) &&
                                       !string.IsNullOrWhiteSpace(currentMachine) &&
                                       !string.IsNullOrWhiteSpace(currentExterneNotities) &&
                                       !string.IsNullOrWhiteSpace(cpNaam);

            bool optionalFieldsComplete = !string.IsNullOrWhiteSpace(cpEmail) &&
                                          !string.IsNullOrWhiteSpace(cpTelefoon);

            if (!allMustFillComplete && !_importantWarningActive)
            {
                _importantWarningActive = true;
                StatusMessage = "Niet alle velden zijn ingevuld; als u wilt stoppen zonder opslaan klik dan nog een keer";
                StatusColor = "Red";
                return;
            }

            if (_importantWarningActive)
            {
                if (!allMustFillComplete)
                {
                    _timer.Stop();
                    CloseRequested?.Invoke();
                    return;
                }
                _importantWarningActive = false;
            }

            if (allMustFillComplete && !optionalFieldsComplete && !_nonImportantWarningActive)
            {
                _nonImportantWarningActive = true;
                StatusMessage = "Niet alle contactgegevens zijn ingevuld. Als u wilt opslaan zonder deze gegevens klik dan nog een keer";
                StatusColor = "Yellow";
                return;
            }

            SaveInterventie();

            void SaveInterventie()
            {
                if (_selectedBedrijf == null && _existingInterventie == null)
                {
                    StatusMessage = "Selecteer een bedrijf voordat u opslaat";
                    StatusColor = "Red";
                    return;
                }

                var callEndTime = DateTime.Now;

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

            if (CurrentlyLoadedCall != null || IsEditingMode)
            {
                LoadCallData(call);
                return;
            }

            var currentBedrijfsnaam = (Bedrijfsnaam ?? "").Trim();
            var currentMachine = (Machine ?? "").Trim();
            var currentExterneNotities = (ExterneNotities ?? "").Trim();
            var cpNaam = (ContactpersoonNaam ?? "").Trim();
            var cpEmail = (ContactpersoonEmail ?? "").Trim();
            var cpTelefoon = (ContactpersoonTelefoon ?? "").Trim();

            bool allMustFillEmpty = string.IsNullOrWhiteSpace(currentBedrijfsnaam) &&
                                    string.IsNullOrWhiteSpace(currentMachine) &&
                                    string.IsNullOrWhiteSpace(currentExterneNotities) &&
                                    string.IsNullOrWhiteSpace(cpNaam);

            if (allMustFillEmpty)
            {
                LoadCallData(call);
                return;
            }

            bool allMustFillComplete = !string.IsNullOrWhiteSpace(currentBedrijfsnaam) &&
                                       !string.IsNullOrWhiteSpace(currentMachine) &&
                                       !string.IsNullOrWhiteSpace(currentExterneNotities) &&
                                       !string.IsNullOrWhiteSpace(cpNaam);

            bool optionalFieldsComplete = !string.IsNullOrWhiteSpace(cpEmail) &&
                                          !string.IsNullOrWhiteSpace(cpTelefoon);

            if (!allMustFillComplete && !_importantWarningActive)
            {
                _importantWarningActive = true;
                StatusMessage = "Niet alle velden zijn ingevuld; als u wilt stoppen zonder opslaan klik dan nog een keer";
                StatusColor = "Red";
                _pendingCallToLoad = call;
                return;
            }

            if (_importantWarningActive && _pendingCallToLoad == call)
            {
                if (!allMustFillComplete)
                {
                    LoadCallData(call);
                    return;
                }
                _importantWarningActive = false;
                _pendingCallToLoad = null;
            }

            if (allMustFillComplete && !optionalFieldsComplete && !_nonImportantWarningActive)
            {
                _nonImportantWarningActive = true;
                StatusMessage = "Niet alle contactgegevens zijn ingevuld. Als u wilt opslaan zonder deze gegevens klik dan nog een keer";
                StatusColor = "Yellow";
                _pendingCallToLoad = call;
                return;
            }

            if (_nonImportantWarningActive && _pendingCallToLoad == call)
            {
                SaveCurrentCallThenLoad(call);
                return;
            }

            if (allMustFillComplete && optionalFieldsComplete)
            {
                SaveCurrentCallThenLoad(call);
            }
        }

        private void SaveCurrentCallThenLoad(InterventieCall callToLoad)
        {
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
                callEndTime: DateTime.Now
            );

            _timer.Stop();

            if (_existingInterventie != null)
            {
                PreviousCalls = _db.InterventieCalls
                    .Where(c => c.InterventieId == _existingInterventie.Id)
                    .OrderByDescending(c => c.StartCall)
                    .ToList();
            }

            _importantWarningActive = false;
            _nonImportantWarningActive = false;
            _pendingCallToLoad = null;
            StatusMessage = "";

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

            _savedContactpersoonNaam = ContactpersoonNaam;
            _savedContactpersoonEmail = ContactpersoonEmail;
            _savedContactpersoonTelefoon = ContactpersoonTelefoon;
            _savedInterneNotities = InterneNotities;
            _savedExterneNotities = ExterneNotities;

            _importantWarningActive = false;
            _nonImportantWarningActive = false;
            _cancelWarningActive = false;
            _pendingCallToLoad = null;
            StatusMessage = "";

            _callStartTime = null;
            _currentCallTime = TimeSpan.Zero;
            TimerDisplay = "00:00:00";

            OnPropertyChanged(nameof(ContactpersoonNaamDirty));
            OnPropertyChanged(nameof(ContactpersoonEmailDirty));
            OnPropertyChanged(nameof(ContactpersoonTelefoonDirty));
            OnPropertyChanged(nameof(InterneNotitiesDirty));
            OnPropertyChanged(nameof(ExterneNotitiesDirty));
        }

        private void UpdateStatusAfterWarning()
        {
            if (_cancelWarningActive)
            {
                _cancelWarningActive = false;
                StatusMessage = "";
            }

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
                int interventieId = _existingInterventie?.Id ?? 0;
                if (interventieId == 0)
                    throw new Exception("No intervention ID available for PDF generation");

                var pdfGenerator = new ServiceBonPdf(_db);
                string pdfPath = await Task.Run(() => pdfGenerator.GeneratePdf(interventieId, Username));

                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = pdfPath,
                    UseShellExecute = true
                });

                PdfDownloaded = true;
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

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}