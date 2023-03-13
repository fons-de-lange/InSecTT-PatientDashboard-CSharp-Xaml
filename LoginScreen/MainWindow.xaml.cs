using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using DESFireModuleTester.console;
using Newtonsoft.Json;
using PatientHealthStatusScreen;


namespace LoginScreen
{

    public enum DashboardState
    {
        Initial = 0,
        MedStaffLoggingOn = 1,
        MedStaffLoggedOnPatientSelection = 2,
        PatientSelectionFromDatabase = 3,
        PatientSelectionWithRfIdCard = 4,
        SelectedPatientOverview = 5,
        PatientAnalyzed = 6,
        InvalidRfIdCard = 7,
        UnknownId = 8,
        AccessDenied = 9
    }
    public enum ResourceType { MedStaff, Patient };

    // Classes for holding person data: either patient or Medical Staff
    public class PersonalDetails
    {
        public string Role;
        public string Gender;
        public DateTime DateOfBirth;
        public string Image;
        public string AddressAs;

        public BitmapSource PersonInfoPicture;

        public Name Name;
        public NameValuePair[] Properties;
    }
    public class Name
    {
        public string FirstName;
        public string LastName;
    }

    public class PersonRecord
    {
        public string Id;
        public ResourceType ResourceType;
        public PersonalDetails PersonalDetails;
    }
    
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const string AssetFolderOffset = @"\..\..\Assets\";

        private DispatcherTimer _blinkIntervalTimer = new DispatcherTimer { Interval = new TimeSpan(0, 0, 0, 0, 300) };
        public PatientHealthStatusScreen.MainWindow PatientHealthStatusScreen; // overview of all patient data retrieved from partners
        public PersonRecord ActiveMedStaffMemberRecord;
        public PersonRecord SelectedPatientRecord;
        public readonly PersonRecord[] MedicalStaffMemberObjects;
        public readonly PersonRecord[] PatientObjects;

        public DashboardState CurrentState;
        public DashboardState PreviousState;
        public LoginScreenViewModel LoginScreenViewModel;
        public Thread ReaderThread;

        public MainWindow()
        {
            InitializeComponent();
            WindowState = WindowState.Maximized;
            CurrentState = DashboardState.Initial;

            var medicalStaffStringFromFile = File.ReadAllText(Directory.GetCurrentDirectory() + AssetFolderOffset + "MedicalStaffInfo.json");
            MedicalStaffMemberObjects =
                (PersonRecord[])JsonConvert.DeserializeObject(medicalStaffStringFromFile, typeof(PersonRecord[]));
            var patientsStringFromFile = File.ReadAllText(Directory.GetCurrentDirectory() + AssetFolderOffset + "PatientInfo.json");
            PatientObjects =
                (PersonRecord[])JsonConvert.DeserializeObject(patientsStringFromFile, typeof(PersonRecord[]));

            DataContext = LoginScreenViewModel = new LoginScreenViewModel
            {
                PersonPropertiesNameValuePairs = new ObservableCollection<NameValuePair>(),
                PatientAllergies = new ObservableCollection<NameValuePair>(),
                PatientComorbidities = new ObservableCollection<NameValuePair>(),
                PatientRiskFactors = new ObservableCollection<NameValuePair>()
            };
            SetInitialDashboardState(LoginScreenViewModel);

            ReaderThread = new Thread(StartCardReading);
            ReaderThread.Start();

            _blinkIntervalTimer.Tick += BlinkIntervalTimerTick;

            Closing += MainWindow_Closing;
        }

        public void SetInitialDashboardState(LoginScreenViewModel m)
        {
            PreviousState = CurrentState;
            CurrentState = DashboardState.Initial;

            SetAllDashboardItemsInvisible(m);
            m.IsInSecTTPageTitleVisible = Visibility.Visible;
            m.IsPhilipsLogoVisible = Visibility.Visible;
            m.IsPresentDoctorIdCardMessageVisible = Visibility.Visible;
            m.IsRfIdCardImageVisible = Visibility.Visible;

            ClearActiveStaffMember(m);
        }

        private void ClearActiveStaffMember(LoginScreenViewModel m)
        {
            m.ActiveStaffMemberLastName = string.Empty;
            m.ActiveStaffMemberAddressAs = string.Empty;
            ActiveMedStaffMemberRecord = null;
            MedStaffMemberLoggedOn = false;
            Dispatcher.Invoke(() => { PasswordBox.Password = string.Empty; });
        }

        public void SetInvalidRfIdCardState(LoginScreenViewModel m)
        {
            PreviousState = CurrentState;
            CurrentState = DashboardState.InvalidRfIdCard;

            SetAllDashboardItemsInvisible(m);
            m.IsInSecTTPageTitleVisible = Visibility.Visible;
            m.IsPresentDoctorIdCardMessageVisible = Visibility.Visible;
            m.IsRfIdCardImageVisible = Visibility.Visible;
            m.IsInvalidIdCardMessageVisible = Visibility.Visible;

            if (MedStaffMemberLoggedOn) // Show logged on med staff picture + name 
                m.IsMedStaffMemberActiveVisible = Visibility.Visible;
            else
                ClearActiveStaffMember(m);
        }

        public void SetInvalidPatientIdCardState(LoginScreenViewModel m)
        {
            PreviousState = CurrentState;
            CurrentState = DashboardState.InvalidRfIdCard;

            SetAllDashboardItemsInvisible(m);
            m.IsInSecTTPageTitleVisible = Visibility.Visible;
            m.IsPresentPatientIdCardMessageVisible = Visibility.Visible;
            m.IsRfIdCardImageVisible = Visibility.Visible;
            m.IsInvalidPatientIdCardMessageVisible = Visibility.Visible;

            if (MedStaffMemberLoggedOn) // Show logged on med staff picture + name 
                m.IsMedStaffMemberActiveVisible = Visibility.Visible;
            else
                ClearActiveStaffMember(m);
        }

        public void SetUnknownIdCardState(LoginScreenViewModel m)
        {
            PreviousState = CurrentState;
            CurrentState = DashboardState.UnknownId;
            SetAllDashboardItemsInvisible(m);
            m.IsInSecTTPageTitleVisible = Visibility.Visible;
            m.IsPresentDoctorIdCardMessageVisible = Visibility.Visible;
            m.IsRfIdCardImageVisible = Visibility.Visible;
            m.IsUnknownIdMessageVisible = Visibility.Visible;

            if (MedStaffMemberLoggedOn) // Show logged on med staff picture + name 
                m.IsMedStaffMemberActiveVisible = Visibility.Visible;
            else
                ClearActiveStaffMember(m);
        }

        public void SetAccessDeniedState(LoginScreenViewModel m)
        {
            PreviousState = CurrentState;
            CurrentState = DashboardState.UnknownId;
            SetAllDashboardItemsInvisible(m);
            m.IsInSecTTPageTitleVisible = Visibility.Visible;
            m.IsPresentDoctorIdCardMessageVisible = Visibility.Visible;
            m.IsRfIdCardImageVisible = Visibility.Visible;
            m.IsAccessDeniedMessageVisible = Visibility.Visible;

            ClearActiveStaffMember(m);
        }

        public void SetMedStaffMemberLoggingOnState(LoginScreenViewModel m, PersonRecord p)
        {
            PreviousState = CurrentState;
            CurrentState = DashboardState.MedStaffLoggingOn;

            SetAllDashboardItemsInvisible(m);
            m.IsInSecTTPageTitleVisible = Visibility.Visible;
            m.IsCardReadMessageVisible = Visibility.Visible;
            m.IsPersonInfoVisible = Visibility.Visible;
            m.IsMedStaffMemberOverviewVisible = Visibility.Visible;
            m.IsPasswordEntryVisible = Visibility.Visible;

            m.ActiveStaffMemberLastName = p.PersonalDetails.Name.LastName;
            m.ActiveStaffMemberAddressAs = p.PersonalDetails.AddressAs;
            ActiveMedStaffMemberRecord = p;
            
            PopulatePersonTableOnlySummary(m, p);
        }

        public void SetMedStaffMemberLoggedOnPatientSelectionState(LoginScreenViewModel m, PersonRecord p)
        {
            PreviousState = CurrentState;
            CurrentState = DashboardState.MedStaffLoggedOnPatientSelection;

            SetAllDashboardItemsInvisible(m);
           
            m.IsMedStaffMemberActiveVisible = Visibility.Visible;
            m.IsInSecTTPageTitleVisible = Visibility.Visible;
            m.IsWelcomeMessageVisible = Visibility.Visible;
            m.IsSelectPatientButtonVisible = Visibility.Visible;
            m.IsPersonInfoVisible = Visibility.Visible;
            m.IsMedStaffMemberOverviewVisible = Visibility.Visible;
            PopulateMedStaffTableAllDetails(m, p); //TODO: take care that pictures in info table differ from MedStaff member!
        }

        public void SetPatientOverviewState(LoginScreenViewModel m, PersonRecord p)
        {
            PreviousState = CurrentState;
            CurrentState = DashboardState.SelectedPatientOverview;
            SetAllDashboardItemsInvisible(m);
            m.IsMedStaffMemberActiveVisible = Visibility.Visible; 
            m.IsInSecTTPageTitleVisible = Visibility.Visible;
            m.IsPersonInfoVisible = Visibility.Visible;
            m.IsPatientOverviewVisible = Visibility.Visible;
            m.IsPatientStartAnalysisButtonVisible = Visibility.Visible;
            PopulatePatientTableAllDetails(m, p);
        }
        private void SetPresentPatientIdCardState(LoginScreenViewModel m)
        {
            PreviousState = CurrentState;
            CurrentState = DashboardState.PatientSelectionWithRfIdCard;
            SetAllDashboardItemsInvisible(LoginScreenViewModel);
            m.IsMedStaffMemberActiveVisible = Visibility.Visible;
            m.IsInSecTTPageTitleVisible = Visibility.Visible;
            m.IsPresentPatientIdCardMessageVisible = Visibility.Visible;
            m.IsRfIdCardImageVisible = Visibility.Visible;
            m.IsPhilipsLogoVisible = Visibility.Visible;
        }


        public void SetAllDashboardItemsVisible()
        {

        }

        public void SetAllDashboardItemsInvisible(LoginScreenViewModel m)
        {
            m.IsInvalidPatientIdCardMessageVisible = Visibility.Hidden;
            m.IsMedStaffMemberActiveVisible = Visibility.Hidden;
            m.IsPhilipsLogoVisible = Visibility.Hidden;
            m.IsAccessDeniedMessageVisible = Visibility.Hidden;
            m.IsPresentDoctorIdCardMessageVisible = Visibility.Hidden;
            m.IsPresentPatientIdCardMessageVisible = Visibility.Hidden;
            m.IsCardReadMessageVisible = Visibility.Hidden;
            m.IsInSecTTPageTitleVisible = Visibility.Hidden;
            m.IsPasswordEntryVisible = Visibility.Hidden;
            m.IsRfIdCardImageVisible = Visibility.Hidden;
            m.IsPasswordIncorrectMessageVisible = Visibility.Hidden;
            m.IsWelcomeMessageVisible = Visibility.Hidden;
            m.IsPersonInfoVisible = Visibility.Hidden;
            m.IsUnknownIdMessageVisible = Visibility.Hidden;
            m.IsSelectPatientButtonVisible = Visibility.Hidden;
            m.IsInvalidIdCardMessageVisible = Visibility.Hidden;
            m.IsPatientOverviewVisible = Visibility.Collapsed;
            m.IsMedStaffMemberOverviewVisible = Visibility.Collapsed;
            m.IsPatientStatusAnalyzingVisible = Visibility.Hidden;
            m.IsPatientStartAnalysisButtonVisible = Visibility.Hidden;
        }

        

        public BitmapImage Load(string path)
        {
            var uri = new Uri(path);
            return new BitmapImage(uri);
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            ReaderThread.Abort();

            PatientHealthStatusRetrieverThread?.Abort();
        }

        
        // NFC card stored JSON data format for PersonFhirStyle to be read from NFC card
        // TODO: can be simplified to just an ID.
        //public enum ResourceTypeFhirStyle { PractitionerRole, PatientRole};

        public class Coding { public string code; public string display; }
        public class Code { public string text; public Coding[] coding;  }
        public class PersonInfo { public string id; public string display; }
        public class Organization { public string id; public string display; }
        public class PersonFhirStyle
        {
            public string resourceType;
            public Code code;

            public PersonInfo person;
            public Organization organization;
        }

        public class Person // bare minimum for person info stored on NFC card
        {
            public string Id;
            public ResourceType ResourceType;
        }



        private void DesFireCardAccessor_CardRemovedFromReaderEvent(object sender, EventArgs e)
        {
           
            switch (CurrentState)
            {
                case DashboardState.InvalidRfIdCard:
                case DashboardState.UnknownId:
                    if (MedStaffMemberLoggedOn) // Go back to the logged on state for MedStaff
                    {
                        if (PreviousState == DashboardState.MedStaffLoggedOnPatientSelection)
                            SetMedStaffMemberLoggedOnPatientSelectionState(LoginScreenViewModel,
                                ActiveMedStaffMemberRecord);
                        else if (PreviousState == DashboardState.PatientSelectionWithRfIdCard)
                            SetPresentPatientIdCardState(LoginScreenViewModel);
                    }
                    else
                        SetInitialDashboardState(LoginScreenViewModel);
                    break;
                case DashboardState.AccessDenied:
                case DashboardState.MedStaffLoggingOn:
                    SetInitialDashboardState(LoginScreenViewModel);
                    break;

                case DashboardState.MedStaffLoggedOnPatientSelection:
                    break;

                case DashboardState.PatientSelectionWithRfIdCard:
                    break;
            }

           
        }

        private void DesFireCardAccessor_CardDataRead(object sender, HelloDESFire_main.CardReadEventArgs e)
        {
            

            Person person;
            if (e.StringReadFromCard == null) // Some error occurred while reading RFID Card
                person = null;
            else
                try { person = (Person)JsonConvert.DeserializeObject(e.StringReadFromCard, typeof(Person)); } catch { person = null; }

            
           

            switch (CurrentState)
            {
                case DashboardState.Initial:
                    if (person == null) 
                    {
                        SetInvalidRfIdCardState(LoginScreenViewModel);
                        return;
                    }

                    if (person.ResourceType == ResourceType.Patient)
                    {
                        SetAccessDeniedState(LoginScreenViewModel);
                    }
                    else  //  personTypeFromCard == ResourceType.MedStaff
                    {
                        var personalRecord = MedicalStaffMemberObjects.FirstOrDefault(p => p.Id == person.Id);
                        if (personalRecord == null)
                            SetUnknownIdCardState(LoginScreenViewModel);
                        else  // MedStaff member ID from card found in MedStaff database
                            SetMedStaffMemberLoggingOnState(LoginScreenViewModel, personalRecord);
                    }
                    break;

                case DashboardState.MedStaffLoggingOn:
                    break;

                case DashboardState.MedStaffLoggedOnPatientSelection:
                case DashboardState.PatientSelectionWithRfIdCard:
                    //case DashboardState.SelectedPatientOverview:
                    if (person == null)
                    {
                        // Show message of invalid id card in LoggedOn state
                        SetInvalidRfIdCardState(LoginScreenViewModel);
                        return;
                    } 

                    if (person.ResourceType == ResourceType.Patient)
                    {
                        var personalRecord = PatientObjects.FirstOrDefault(p => p.Id == person.Id);
                        if (personalRecord != null)
                        {
                            SetPatientOverviewState(LoginScreenViewModel, personalRecord);
                            SelectedPatientRecord = personalRecord;
                        }
                        else
                        {
                            SetUnknownIdCardState(LoginScreenViewModel);
                        }
                    }
                    else // MedStaff Member card
                    {
                        // Show message that patient ID card is expected
                        SetInvalidPatientIdCardState(LoginScreenViewModel);
                    }

                    break;

                case DashboardState.UnknownId:
                    
                    break;

                
            }
            

            
            
            // Temporary for check:  Card should contain Person ID + Role at least.
            // And data from card should be parsed , or
            // When only ID is availabe, then use that to retrieve all person info based on this ID, including picture
            //  Then shown in PersonPropertiesNameValuePairs
            Dispatcher.Invoke(new Action(() =>
            {

                
            }));

            
        }

        public bool PopulatePersonTableOnlySummary(LoginScreenViewModel m, PersonRecord p)
        {
            if (p == null)
                return false;

            Dispatcher.Invoke(() =>
            {
                m.CurrentPersonInfoPicture = Load(Directory.GetCurrentDirectory() + AssetFolderOffset + p.PersonalDetails.Image);

                m.PersonPropertiesNameValuePairs.Clear();

                // TODO: Use constant string definitions or resources below, instead of literal strings below
                m.PersonPropertiesNameValuePairs.Add(new NameValuePair { Name = "First Name", Value = p.PersonalDetails.Name.FirstName });
                m.PersonPropertiesNameValuePairs.Add(new NameValuePair { Name = "Last Name", Value = p.PersonalDetails.Name.LastName });
                m.PersonPropertiesNameValuePairs.Add(new NameValuePair { Name = "Identification", Value = p.Id });
            });

            return true;
        }

        public bool PopulateMedStaffTableAllDetails(LoginScreenViewModel m, PersonRecord p)
        {
            var success = PopulatePersonTableOnlySummary(m, p);
            if (!success) return success;

            Dispatcher.Invoke(new Action(() =>
            {
                m.PersonPropertiesNameValuePairs.Add(new NameValuePair { Name = "DoB", Value = p.PersonalDetails.DateOfBirth.ToShortDateString() });
                m.PersonPropertiesNameValuePairs.Add(new NameValuePair { Name = "Role", Value = p.PersonalDetails.Role });

                foreach (var prop in p.PersonalDetails.Properties)
                    m.PersonPropertiesNameValuePairs.Add(new NameValuePair { Name = prop.Name, Value = prop.Value });
            
                m.ActiveMedStaffMemberPicture = Load(Directory.GetCurrentDirectory() + AssetFolderOffset + p.PersonalDetails.Image);
                m.IsMedStaffMemberActiveVisible = MedStaffMemberLoggedOn? Visibility.Visible: Visibility.Hidden;
                m.WelcomeMessage = "Welcome " + p.PersonalDetails.AddressAs + " " + p.PersonalDetails.Name.LastName;
            }));

            return true;
        }

        public bool PopulatePatientTableAllDetails(LoginScreenViewModel m, PersonRecord p)
        {
            var success = PopulatePersonTableOnlySummary(m, p);
            if (!success) return success;

            Dispatcher.Invoke(new Action(() =>
            {
                m.PersonPropertiesNameValuePairs.Add(new NameValuePair { Name = "DoB", Value = p.PersonalDetails.DateOfBirth.ToShortDateString() });
                m.PersonPropertiesNameValuePairs.Add(new NameValuePair { Name = "Category", Value = p.PersonalDetails.Role });
                m.PersonPropertiesNameValuePairs.Add(new NameValuePair { Name = "Admission Date", Value = DateTime.Parse( p.PersonalDetails.Properties.FirstOrDefault(pr => pr.Name == "Admission date")?.Value).ToShortDateString() });
                m.PersonPropertiesNameValuePairs.Add(new NameValuePair { Name = "Pathway", Value = p.PersonalDetails.Properties.FirstOrDefault(pr => pr.Name == "Pathway")?.Value });

                m.PatientComorbidities.Clear();
                var comorbidiies = (p.PersonalDetails.Properties.Where(c => c.Name == "Comorbidity")).ToArray();
                foreach(var c in comorbidiies) m.PatientComorbidities.Add(c);

                m.PatientAllergies.Clear();
                var allergies = (p.PersonalDetails.Properties.Where(c => c.Name == "Allergy")).ToArray();
                foreach (var a in allergies) m.PatientAllergies.Add(a);

                m.PatientRiskFactors.Clear();
                var riskFactors = (p.PersonalDetails.Properties.Where(c => c.Name == "Risk factor")).ToArray();
                foreach (var r in riskFactors) m.PatientRiskFactors.Add(r);

            ;

            //var comorbidiies = (IEnumerable) p.PersonalDetails.Properties.SelectMany(c => c.Name == "Comorbidity");
            //var allergies = p.PersonalDetails.Properties.SelectMany(c => c.Name == "Allergy");
            //var riskFactors = p.PersonalDetails.Properties.SelectMany(c => c.Name == "Risk actor");

           
                //foreach (var prop in p.PersonalDetails.Properties)
                //    m.PersonPropertiesNameValuePairs.Add(new NameValuePair { Name = prop.Name, Value = prop.Value });

                m.IsMedStaffMemberActiveVisible = MedStaffMemberLoggedOn ? Visibility.Visible : Visibility.Hidden;
            }));

            return true;
        }

        private bool MedStaffMemberLoggedOn = false;
        private void PasswordBox_KeyDown(object sender, KeyEventArgs e) // TODO: use PRISM View model
        {
            if (e.Key == Key.Enter)
            {
                var passwordEntered = ((PasswordBox)sender).Password;
                if (passwordEntered == "123456")
                {
                    MedStaffMemberLoggedOn = true;
                    SetMedStaffMemberLoggedOnPatientSelectionState(LoginScreenViewModel, ActiveMedStaffMemberRecord);
                }
                else
                {
                    LoginScreenViewModel.IsPasswordIncorrectMessageVisible = Visibility.Visible;
                    MedStaffMemberLoggedOn = false;
                }
            }
            if (e.Key == Key.Back)
            {
                LoginScreenViewModel.IsPasswordIncorrectMessageVisible = Visibility.Hidden;
            }
        }

        public void StartCardReading()
        {
            var desFireCardAccessor = new HelloDESFire_main();
            desFireCardAccessor.CardDataReadEvent += DesFireCardAccessor_CardDataRead;
            desFireCardAccessor.CardRemovedFromReaderEvent += DesFireCardAccessor_CardRemovedFromReaderEvent;
            desFireCardAccessor.StartCardReading();
        }

        private void PasswordBox_KeyUp(object sender, KeyEventArgs e) //TODO: use PRISM view model
        {
            if (e.Key == Key.Back)
            {
                LoginScreenViewModel.IsPasswordIncorrectMessageVisible = Visibility.Hidden;
            }
        }

        private void ButtonSelectNearbyPatientClick(object sender, RoutedEventArgs e)
        {
            SetPresentPatientIdCardState(LoginScreenViewModel);
        }

      

        private void LogoutButtonClick(object sender, RoutedEventArgs e) // TODO: use PRISM as ViewModel
        {
            SetInitialDashboardState(LoginScreenViewModel);
        }

        //private void xAnalysePatientButtonClick(object sender, RoutedEventArgs e)
        //{

        //    Dispatcher.Invoke(() =>
        //    {
        //        LoginScreenViewModel.IsPatientOverviewVisible = Visibility.Hidden;
        //        LoginScreenViewModel.IsPatientStatusAnalyzingVisible = Visibility.Visible;

        //        PatientHealthStatusScreen = new PatientHealthStatusScreen.MainWindow();//{ Visibility = Visibility.Visible };
        //        PatientHealthStatusScreen.PatientHealthStatusRetrieved += PatientHealthStatusScreen_PatientHealthStatusRetrieved;


        //        //_healthStatusRetrieverThread = new Thread(RunHealthStatusRetrieval);
        //        //_healthStatusRetrieverThread.Start();
        //    });

        //    PatientHealthStatusScreen.RetrievePatientData();


        //}
        private Thread PatientHealthStatusRetrieverThread = null;
        private void AnalyzePatientButtonClick(object sender, RoutedEventArgs e)
        {
            LoginScreenViewModel.IsPatientStartAnalysisButtonVisible = Visibility.Hidden;
            LoginScreenViewModel.IsPatientStatusAnalyzingVisible = Visibility.Visible;
            _blinkIntervalTimer.Start();

            if (PatientHealthStatusRetrieverThread != null)
            { 
                PatientHealthStatusRetrieverThread.Abort();
                PatientHealthStatusRetrieverThread = null;
            }

            PatientHealthStatusRetrieverThread = new Thread(delegate ()
            {
                PatientHealthStatusScreen = new PatientHealthStatusScreen.MainWindow
                {
                    Title = "Patient: " + SelectedPatientRecord.PersonalDetails.AddressAs + " " +
                            SelectedPatientRecord.PersonalDetails.Name.LastName + ", Id: " + SelectedPatientRecord.Id +
                            ", DoB: " + SelectedPatientRecord.PersonalDetails.DateOfBirth.ToShortDateString()
                };
                PatientHealthStatusScreen.PatientHealthStatusRetrieved += PatientHealthStatusScreen_PatientHealthStatusRetrieved;
                PatientHealthStatusScreen.RetrievePatientData();
                PatientHealthStatusScreen.Show();
                Dispatcher.Run();
            });

            PatientHealthStatusRetrieverThread.SetApartmentState(ApartmentState.STA); // needs to be STA or throws exception
            PatientHealthStatusRetrieverThread.Start();
        }


        private void PatientHealthStatusScreen_PatientHealthStatusRetrieved(object sender, PatientHealthStatusRetrievalProgress e)
        {
            _blinkIntervalTimer.Stop();
            Dispatcher.Invoke(() =>
            {
                LoginScreenViewModel.IsPatientStartAnalysisButtonVisible = Visibility.Visible;
                LoginScreenViewModel.IsPatientStatusAnalyzingVisible = Visibility.Hidden;
            });
        }

        private bool _blinkOn = false;
        private void BlinkIntervalTimerTick(object sender, EventArgs e)
        {
            LoginScreenViewModel.IsPatientStatusAnalyzingVisible = _blinkOn ? Visibility.Visible : Visibility.Hidden;
            _blinkOn = !_blinkOn;
        }

        private void AnalyzePatientCancelButtonClick(object sender, RoutedEventArgs e)
        {
            SetMedStaffMemberLoggedOnPatientSelectionState(LoginScreenViewModel, ActiveMedStaffMemberRecord);
        }
    }

    // View Models for display of retrieved informationPa
    public class NameValuePair : INotifyPropertyChanged
    { 
        public string Name
        {
            get { return _name; }
            set { _name = value; OnPropertyChanged(); }
        }
        public string Value
        {
            get { return _value; }
            set { _value = value; OnPropertyChanged(); }
        }

        string _name;
        string _value;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public class LoginScreenViewModel : INotifyPropertyChanged
    {
        public string ActiveStaffMemberLastName
        {
            get { return _lastNameActiveStaffMember; }
            set { _lastNameActiveStaffMember = value; OnPropertyChanged(); }
        }
        public string ActiveStaffMemberAddressAs
        {
            get { return _activeStaffMemberAddressAs; }
            set { _activeStaffMemberAddressAs = value; OnPropertyChanged(); }
        }
        public string ActivePassword
        {
            get { return _activePassword; }
            set { _activePassword = value; OnPropertyChanged(); }
        }

        public Visibility IsMedStaffMemberActiveVisible
        {
            get { return _isMedStaffMemberActiveVisible; }
            set { _isMedStaffMemberActiveVisible = value; OnPropertyChanged(); }
        }

        public BitmapSource ActiveMedStaffMemberPicture
        {
            get { return _activeMedStaffPicture; }
            set { _activeMedStaffPicture = value; OnPropertyChanged(); }
        }

        public BitmapSource CurrentPersonInfoPicture
        {
            get { return _currentPersonInfoPicture; }
            set { _currentPersonInfoPicture = value; OnPropertyChanged(); }
        }

        public BitmapSource SelectedPatientPicture
        {
            get { return _selectedPatientPicture; }
            set { _selectedPatientPicture = value; OnPropertyChanged(); }
        }


        public ObservableCollection<NameValuePair> PersonPropertiesNameValuePairs { get; set; }
        public ObservableCollection<NameValuePair> PatientComorbidities { get; set; }
        public ObservableCollection<NameValuePair> PatientAllergies { get; set; }
        public ObservableCollection<NameValuePair> PatientRiskFactors { get; set; }

        public Visibility IsInvalidPatientIdCardMessageVisible
        {
            get { return _isInvalidPatientIdCardMessageVisible; }
            set { _isInvalidPatientIdCardMessageVisible = value; OnPropertyChanged(); }
        }
        public Visibility IsPhilipsLogoVisible
        {
            get { return _isPhilipsLogoVisible; }
            set { _isPhilipsLogoVisible = value; OnPropertyChanged(); }
        }
        public Visibility IsAccessDeniedMessageVisible
        {
            get { return _isAccessDeniedMessageVisible; }
            set { _isAccessDeniedMessageVisible = value; OnPropertyChanged(); }
        }
        
        public Visibility IsPresentDoctorIdCardMessageVisible
        {
            get { return _isPresentDoctorIdCardMessageVisible; }
            set { _isPresentDoctorIdCardMessageVisible = value; OnPropertyChanged(); }
        }
        public Visibility IsPresentPatientIdCardMessageVisible
        {
            get { return _isPresentPatientIdCardMessageVisible; }
            set { _isPresentPatientIdCardMessageVisible = value; OnPropertyChanged(); }
        }
        public Visibility IsCardReadMessageVisible
        {
            get { return _isCardReadMessageVisible; }
            set { _isCardReadMessageVisible = value; OnPropertyChanged(); }
        }

        public Visibility IsInSecTTPageTitleVisible
        {
            get { return _isInSecTTPageTitleVisible; }
            set { _isInSecTTPageTitleVisible = value; OnPropertyChanged(); }
        }

        public Visibility IsPasswordEntryVisible
        {
            get { return _isPasswordEntryVisible; }
            set { _isPasswordEntryVisible = value; OnPropertyChanged(); }
        }
        public Visibility IsRfIdCardImageVisible
        {
            get { return _sRfIdCardImageVisible; }
            set { _sRfIdCardImageVisible = value; OnPropertyChanged(); }
        }
        public Visibility IsPasswordIncorrectMessageVisible
        {
            get { return _isPasswordIncorrectMessageVisible; }
            set { _isPasswordIncorrectMessageVisible = value; OnPropertyChanged(); }
        }
        public Visibility IsWelcomeMessageVisible
        {
            get { return _isWelcomeMessageVisible; }
            set { _isWelcomeMessageVisible = value; OnPropertyChanged(); }
        }
        public Visibility IsPersonInfoVisible
        {
            get { return _isPersonInfoVisible; }
            set { _isPersonInfoVisible = value; OnPropertyChanged(); }
        }
        public Visibility IsPatientStatusAnalyzingVisible
        {
            get { return _isPatientStatusAnalyzingVisible; }
            set { _isPatientStatusAnalyzingVisible = value; OnPropertyChanged(); }
        }
        public string WelcomeMessage
        {
            get { return _welcomeMessage; }
            set { _welcomeMessage = value; OnPropertyChanged(); }
        }
        public Visibility IsInvalidIdCardMessageVisible
        {
            get { return _isInvalidIdCardMessageVisible; }
            set { _isInvalidIdCardMessageVisible = value; OnPropertyChanged(); }
        }
        public Visibility IsUnknownIdMessageVisible
        {
            get { return _isUnknownIdMessageVisible; }
            set { _isUnknownIdMessageVisible = value; OnPropertyChanged(); }
        }
        public Visibility IsPatientStartAnalysisButtonVisible
        {
            get { return _isPatientStartAnalysisButtonVisible; }
            set { _isPatientStartAnalysisButtonVisible = value; OnPropertyChanged(); }
        }

        public Visibility IsSelectPatientButtonVisible
        {
            get => _isSelectPatientButtonVisible;
            set { _isSelectPatientButtonVisible = value; OnPropertyChanged(); }
        }
        public Visibility IsPatientOverviewVisible
        {
            get => _isPatientOverviewVisible;
            set { _isPatientOverviewVisible = value; OnPropertyChanged(); }
        }
        public Visibility IsMedStaffMemberOverviewVisible
        {
            get => _isMedStaffMemberOverviewVisible;
            set { _isMedStaffMemberOverviewVisible = value; OnPropertyChanged(); }
        }


        string _welcomeMessage;
        string _activePassword;
        string _lastNameActiveStaffMember;
        string _activeStaffMemberAddressAs;
        private Visibility _isInvalidPatientIdCardMessageVisible;
        Visibility _isPhilipsLogoVisible;
        Visibility _isInSecTTPageTitleVisible;
        Visibility _isWelcomeMessageVisible;
        Visibility _isPasswordIncorrectMessageVisible;
        Visibility _isPasswordEntryVisible;
        Visibility _sRfIdCardImageVisible;
        Visibility _isPersonInfoVisible;
        Visibility _isCardReadMessageVisible;
        Visibility _isPresentPatientIdCardMessageVisible;
        Visibility _isPresentDoctorIdCardMessageVisible;
        Visibility _isAccessDeniedMessageVisible;
        Visibility _isUnknownIdMessageVisible;
        Visibility _isSelectPatientButtonVisible;
        Visibility _isInvalidIdCardMessageVisible;
        private Visibility _isPatientOverviewVisible;
        private Visibility _isMedStaffMemberOverviewVisible;
        private Visibility _isPatientStatusAnalyzingVisible;

        private Visibility _isMedStaffMemberActiveVisible;
        private Visibility _isPatientStartAnalysisButtonVisible;

        BitmapSource _activeMedStaffPicture; 
        BitmapSource _currentPersonInfoPicture;
        BitmapSource _selectedPatientPicture;

        PersonRecord _activeMedStaffMember;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }



}
