using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media.Imaging;
using Newtonsoft.Json;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using PatientHealthStatusScreen;
using RestSharp;

namespace PatientDataRetriever
{
   
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
    public partial class MainWindow : Window
    {
        //TODO: Read all patient profiles, including names and co-morbidities (from local file).
        //TODO:    This is Similar to what is done in Web version
        //TODO: Convert all patient IDs into appropriate IDs for different TBBs
        //TODO: Show on screen all patients, enable selection of a patient and OnPatientSelection show patient details. 
        //TODO:     Then when clicking OnRetrieve (for a patient), get all the patient's info as calculated by all TBBs.
        //TODO:         Show all outputs in Overview Window: enables selection of particular TBB output: expands OnSelection 
        //TODO:            OnSelection show TBB output in bigger window.
        //TODO:               Show Length of Stay Prediction in days in separate big Text (near picture).
        //TODO: Try to do this als for compressed + raw ECG data (Png + Json Time,Value pair array 
        //TODO:     Will show difference in speed of retrieval for compressed versus raw ECG!
        //TODO: Decide on what to provide in dashboard for TuT:  Video?? using Chat Application??
        public PatientDashboardViewModel PatientDashboardViewModel { get;  private set; }
        public VitalSignGraphViewModel VitalSignGraphViewModel { get; private set;}

        public MainWindow()
        {
            InitializeComponent();
        }
        
        private void Button_Click(object sender, RoutedEventArgs e)
        {

            // VTT Vital Sign Data retrieval
            
            RetrieveAndDisplayAsSeriesInWindow("Cam1", "xxx", "OxygenSaturation", 100);
            // Observation Types:
            //  "HeartRate"
            //  "OxygenSaturation"
            //  "RespirationRate"
            //TODO: Get all Info of patient for all sensors and then represent this in different graphs
            //TODO:    But, if different sensors have same observation type, then overlay them (one per sensor) in same graph. 


            // TUD ECG retrieval & display
            // To get the compressed image, please use "extremeiot.ewi.tudelft.nl/ecg-tud/emulation/getCompressed/1"
            // "1" indicates a patient ID
            // and to get raw image, use "extremeiot.ewi.tudelft.nl/ecg-tud/emulation/getRaw/1/?start=0&end=10"
            // start = 0 & end = 10 means get data from 0 to 10 seconds.
            // start = 6 & end = 8 means get data from 6 to 8 seconds.

            //As of now, the server supports only two values(0 - 10 and 6 - 8 seconds) so that you can integrate it on dashboard.
            //Port used: 80
            //You can also replace extremeiot.ewi.tudelft.nl with the ip address 131.180.178.48.
            RetrieveAndDisplayECGs("2");


            // JSI ECG anomaly retrieval
            // "2" indicates Patient Id.
            RetrieveAndDisplayECGAnomalies("2");


            // Length of Stay Prediction (LOFS).
            // "1" indicates Patient Id.
            RetrieveAndDisplayLengthOfStayPrediction("3");
        }






        public void RetrieveAndDisplayAsSeriesInWindow(string sensor, string patientId, string observationType, int nrOfMostRecent)
        {
            Uri baseUrl = new Uri("https://insectt.willab.fi/");

            var client = new RestClient(baseUrl);
            var request = new RestRequest("VitalSign", Method.Get) { };

            request.AddParameter("sensor", sensor);
            request.AddParameter("subject", patientId);
            request.AddParameter("observation", observationType);
            request.AddParameter("mostRecent", nrOfMostRecent);

            var response = client.Execute(request);
            if (!response.IsSuccessStatusCode || !response.IsSuccessful)
            {
                new MainWindow
                {
                    DataContext =
                    PatientDashboardViewModel = new PatientDashboardViewModel { ButtonVisibility = Visibility.Hidden, ErrorMessageVisibility = Visibility.Visible, ErrorMessage = response.StatusDescription + ":  \n" + response.Content, PartnerName = "VTT"},
                    Visibility = Visibility.Visible,
                    Width = 1000
                };
            }
            else
            {
                var observations = (VitalSignObservation[])JsonConvert.DeserializeObject(response.Content, typeof(VitalSignObservation[]));

                VitalSignGraphViewModel = new VitalSignGraphViewModel{WindowDescription = "Contactless Vital Signs Measurement"};
                new VitalSignGraphs { Visibility = Visibility.Visible, Width = 800, Height = 600, DataContext = VitalSignGraphViewModel, Title = "VTT"};

                // Process and display the JSON response
                foreach (var observation in observations)
                {
                    VitalSignGraphViewModel.LineSeries.Points.Add(new DataPoint(DateTimeAxis.ToDouble((object)observation.effectiveDateTime), observation.valueQuantity.value));
                }

                //VitalSignGraphViewModel.ValueAxis.MinimumDataMargin = 200;
                VitalSignGraphViewModel.VitalSignPlotModel.Title = observations[0].code.coding[0].display + "  (" + observations[0].valueQuantity.unit + ")";
                //VitalSignGraphViewModel.ValueAxis.Title = observations[0].valueQuantity.unit;
               // VitalSignGraphViewModel.DateTimeAxis.Title = observations[0].valueQuantity.unit;
            }
        }

        public void RetrieveAndDisplayECGs(string patientId)
        {
            var imageStreamSource = TryRestApiGet(new Uri("http://extremeiot.ewi.tudelft.nl/ecg-tud/emulation/getCompressed/" + patientId)); if (imageStreamSource == null) return;

            // Jpeg image decode and display
            var jpgDecoder = new JpegBitmapDecoder(imageStreamSource, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
            var bitmapSource = jpgDecoder.Frames[0];

            // Display in new window the image received in response: use window size of the image / bitmap returned
            DisplayImageReceived(bitmapSource, "TUD", "Compressed and reconstructed ECG signals");
        }


        public void RetrieveAndDisplayECGAnomalies(string patientId)
        {
            var imageStreamSource = TryRestApiGet(new Uri("http://big.ijs.si:63211/" + patientId)); if (imageStreamSource == null) return;

            // Png image decode and display
            var pngDecoder = new PngBitmapDecoder(imageStreamSource, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
            var bitmapSource = pngDecoder.Frames[0];

            // Display in new window the image received in response: use window size of the image / bitmap returned
            DisplayImageReceived(bitmapSource, "JSI", "Anomaly detection in ECG signals");
        }

        public void RetrieveAndDisplayLengthOfStayPrediction(string patientId)
        {
            var response = TryExecute(new Uri("http://127.0.0.1:5000/lofs/" + patientId));
            var lofsResponse = (LofsResponse)JsonConvert.DeserializeObject(response.Content, typeof(LofsResponse));
            var bytes = Convert.FromBase64String(lofsResponse.img);

            using (MemoryStream ms = new MemoryStream(bytes))   
            {
                var image = System.Drawing.Image.FromStream(ms);
                BitmapSource bitmapSource = BitmapImageFromImage(image);

                DisplayImageReceived(bitmapSource, "NXP", "Length of Stay Prediction: " + lofsResponse.lofs + " Days");
            }
        }

        private void DisplayImageReceived(BitmapSource bitmapSource, string title, string description)
        {
            new MainWindow
            {
                DataContext =
                PatientDashboardViewModel = new PatientDashboardViewModel { ImageReceived = bitmapSource, ButtonVisibility = Visibility.Hidden, ErrorMessageVisibility = Visibility.Hidden, PartnerName = title, WindowDescription = description},
                Width = bitmapSource.PixelWidth,
                Height = bitmapSource.PixelHeight,
                Visibility = Visibility.Visible,
                
            };
        }

        private RestResponse TryExecute(Uri uri)
        {
            var restClient = new RestClient(uri);
            var request = new RestRequest("", Method.Get) { };
            var response = restClient.Execute(request);

            // Display error message in case of unsuccesful response and quit
            if (!response.IsSuccessful || string.IsNullOrWhiteSpace(response.ContentType))
            {
                ShowError(response);
            }
            return response;
        }

        private MemoryStream TryRestApiGet(Uri uri)
        {
            RestResponse response = TryExecute(uri);
            if (!response.IsSuccessful) return null;

            var imageStreamSource = new MemoryStream(response.RawBytes);
            return imageStreamSource;
        }

        private void ShowError(RestResponse response)
        {
            new MainWindow
            {
                DataContext =
                   PatientDashboardViewModel = new PatientDashboardViewModel { ButtonVisibility = Visibility.Hidden, ErrorMessageVisibility = Visibility.Visible, ErrorMessage = response.StatusDescription + ":  \n" + response.Content },
                        Visibility = Visibility.Visible,
                Width = 1000
            };
        }


        // Helper method for conversion of a System.Drawing.Image to a BitmapSource (Needed for processing Lofs response)
        public BitmapImage BitmapImageFromImage(System.Drawing.Image img)
        {
            using (var memory = new MemoryStream())
            {
                img.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
                memory.Position = 0;

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();

                return bitmapImage;
            }
        }


        // JSON response data structures / classes
        // Data structure / class to hold Length of Stay prediction response
        public class LofsResponse
        {
            public string img;
            public int lofs;
            public int patientID;
        }

        // Data structure / classes for representing Vital Sign observations as received from VTT
        public class Subject { public string reference; }
        public class ValueQuantity { public double value; public string unit; }

        public class Coding { public string system; public string code; public string display; }
        public class Code { public Coding[] coding; public string text; }

        public class VitalSignObservation
        {
            public string resourceType;
            public string id;
            //public string meta;
            //public string identifier;
            //public string partOf;
            public string status;
            //public string category;
            public Subject subject;
            public DateTime effectiveDateTime;
            public Code code;
            public ValueQuantity valueQuantity;

        }

    }

    // View Models for display of retrieved information
    public class PatientDashboardViewModel : INotifyPropertyChanged
    {
        public string WindowDescription
        {
            get => _windowDescription;
            set { _windowDescription = value; OnPropertyChanged(); }
        }
        public string PartnerName
        {
            get => _partnerName;
            set { _partnerName = value; OnPropertyChanged(); }
        }
        public string ErrorMessage { get { return _errorMessage; } set { _errorMessage = value; OnPropertyChanged(); } }
        public Visibility ButtonVisibility
        {
            get { return _buttonVisibility; }
            set { _buttonVisibility = value; OnPropertyChanged(); }
        }
        public Visibility ErrorMessageVisibility
        {
            get { return _errorMessageVisibility; }
            set { _errorMessageVisibility = value; OnPropertyChanged(); }
        }
        public BitmapSource ImageReceived
        {
            get { return _imageReceived; }
            set { _imageReceived = value; OnPropertyChanged(); }
        }
        public int ImageWidth
        {
            get { return _imageWidth; }
            set { _imageWidth = value; OnPropertyChanged(); }
        }
        public int ImageHeight
        {
            get { return _imageHeight; }
            set { _imageHeight = value; OnPropertyChanged(); }
        }

        // [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        Visibility _buttonVisibility;
        Visibility _errorMessageVisibility;
        BitmapSource _imageReceived;
        int _imageHeight, _imageWidth;
        string _errorMessage;
        private string _partnerName;
        private string _windowDescription;

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public class VitalSignGraphViewModel: INotifyPropertyChanged
    {
        public string WindowDescription
        {
            get => _windowDescription;
            set { _windowDescription = value; OnPropertyChanged(); }
        }

        

        // Public members that need to be accessed afterwards during measurements insertion.
        public PlotModel VitalSignPlotModel { get; private set; }
        public LineSeries LineSeries { get; private set; }
        
        public DateTimeAxis DateTimeAxis { get; private set; }
        public LinearAxis ValueAxis { get; private set; }

        public VitalSignGraphViewModel()
        {

            VitalSignPlotModel = new PlotModel();

            // Line is transparent between measurement points, actual measurements are displayed as blue dots.
            LineSeries = new LineSeries { MarkerFill = OxyColor.FromRgb(0, 0, 255), MarkerSize = 5, MarkerType = MarkerType.Circle, Color = OxyColor.FromArgb(50, 0, 0, 0) };

            VitalSignPlotModel.Series.Add(LineSeries);
           
            DateTimeAxis = new DateTimeAxis { IsPanEnabled = true, IsZoomEnabled=true, Title= "Date / Time" /* StringFormat = "dd:hh:mm:ss"*/ };
            ValueAxis = new LinearColorAxis {  /*Angle = 90, IsPanEnabled = true, IsZoomEnabled =true, Title = "CCCCCC djfjfkjfk"*/ };

            VitalSignPlotModel.Axes.Add(DateTimeAxis);
            VitalSignPlotModel.Axes.Add(ValueAxis);
        }


        private string _windowDescription;
        

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }

   
}
