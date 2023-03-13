using System;
using System.Collections.Generic;
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
using RestSharp;
using System.IO;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using System.Web;
using System.Drawing;
using System.Collections.ObjectModel;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Axes;
using OxyPlot.Legends;


namespace PatientHealthStatusScreen
{
    public class PatientHealthStatusRetrievalProgress : EventArgs
    {
        public string StatusMessage;
        public bool Completed;
    }
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
        public ImageWithDescriptionViewModel ImageWithDescriptionViewModel { get;  private set; }
        //public VitalSignGraphViewModel VitalSignGraphViewModel { get; private set;}

        private PatientHealthStatusDashboardViewModel PatientHealthStatusDashboardViewModel { get; }

        public event EventHandler<PatientHealthStatusRetrievalProgress> PatientHealthStatusRetrieved;


        public MainWindow()
        {
            InitializeComponent();

            WindowState = WindowState.Maximized;

            DataContext = PatientHealthStatusDashboardViewModel = new PatientHealthStatusDashboardViewModel();
        }
        
        public void RetrievePatientData()
        {
            Dispatcher.Invoke(() => { PatientHealthStatusDashboardViewModel.Clear(); });

            // VTT Vital Sign Data retrieval

            RetrieveAndDisplayAsSeriesInWindow("Cam1", "xxx", "RespirationRate", 100);
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
            RetrieveAndDisplayLengthOfStayPrediction("2");

            // Report Completed status to Main Screen (e.g. to stop any progress indicator (e.g. "Analyzing" blinker")
            PatientHealthStatusRetrieved?.Invoke(this,
                new PatientHealthStatusRetrievalProgress() { StatusMessage = "All Data Retrieved", Completed = true });
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
                //var zzz = response.Content.Split(new string[]{"for"}, StringSplitOptions.None);

                //var message = response.Content.Replace("for", "\nfor");

                PatientHealthStatusDashboardViewModel.VttViewModel = new VitalSignGraphViewModel
                {
                    WindowDescription = "Contactless Vital Signs Measurement",
                    ErrorMessageDetail = response.StatusDescription + ":\n" + response.Content,
                    IsErrorMessageVisible = Visibility.Visible,
                    IsPlotVisible = Visibility.Collapsed,
                    IsPlotEnabled = false,
                    VitalSignPlotModel =
                    {
                        //VitalSignGraphViewModel.ValueAxis.MinimumDataMargin = 200;
                        //Title = response.Content,
                    }
                };
            }
            else
            {
                var observations = (VitalSignObservation[])JsonConvert.DeserializeObject(response.Content, typeof(VitalSignObservation[]));


                PatientHealthStatusDashboardViewModel.VttViewModel = new VitalSignGraphViewModel{WindowDescription = "Contactless Vital Signs Measurement"};
                //new VitalSignGraphs { Visibility = Visibility.Visible, Width = 800, Height = 600, DataContext = VitalSignGraphViewModel, Title = "VTT"};

                PatientHealthStatusDashboardViewModel.VttViewModel = new VitalSignGraphViewModel
                {
                    WindowDescription = "Contactless Vital Signs Measurement",
                    IsErrorMessageVisible = Visibility.Collapsed,
                    IsPlotVisible = Visibility.Visible,
                    IsPlotEnabled = true,
                    VitalSignPlotModel =
                    {
                        //VitalSignGraphViewModel.ValueAxis.MinimumDataMargin = 200;
                        Title = observations[0].code.coding[0].display + "  (" + observations[0].valueQuantity.unit + ")",
                    }
                };

                // Process and display the JSON response
                foreach (var observation in observations)
                {
                    PatientHealthStatusDashboardViewModel.VttViewModel.LineSeries.Points.Add(new DataPoint(DateTimeAxis.ToDouble((object)observation.effectiveDateTime), observation.valueQuantity.value));
                }
                


                //VitalSignGraphViewModel.ValueAxis.Title = observations[0].valueQuantity.unit;
               // VitalSignGraphViewModel.DateTimeAxis.Title = observations[0].valueQuantity.unit;
            }
        }

        public void RetrieveAndDisplayECGs(string patientId)
        {
            var imageStreamSource = TryRestApiGet(new Uri("http://extremeiot.ewi.tudelft.nl/ecg-tud/emulation/getCompressed/" + patientId), out var response);
            if (imageStreamSource == null)
            {
                PatientHealthStatusDashboardViewModel.TudEcgCompressedViewModel = new ImageWithDescriptionViewModel
                {
                    ImageDescription = "Compressed and reconstructed ECG signals",
                    IsErrorMessageVisible = Visibility.Visible,
                    ErrorMessageDetail = response.StatusDescription + ":  \n" + response.Content
                };


                return;
            }

            // Jpeg image decode and display
            var jpgDecoder = new JpegBitmapDecoder(imageStreamSource, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
            var bitmapSource = jpgDecoder.Frames[0];

            // Display in new window the image received in response: use window size of the image / bitmap returned
            //DisplayImageReceived(bitmapSource, "TUD", "Compressed and reconstructed ECG signals");
            PatientHealthStatusDashboardViewModel.TudEcgCompressedViewModel = new ImageWithDescriptionViewModel
            {
                ImageReceived = bitmapSource, //ButtonVisibility = Visibility.Hidden,
                IsErrorMessageVisible = Visibility.Hidden,
                PartnerName = "TUD",
                ImageDescription = "Compressed and reconstructed ECG signals"
            };
        }


        public void RetrieveAndDisplayECGAnomalies(string patientId)
        {
            RestResponse response;
            var imageStreamSource = TryRestApiGet(new Uri("http://big.ijs.si:63211/" + patientId), out response);
            if (imageStreamSource == null)
            {
                PatientHealthStatusDashboardViewModel.JsiAnomalyViewModel = new ImageWithDescriptionViewModel
                {
                    ImageDescription = "Anomaly detection in ECG signals",
                    IsErrorMessageVisible = Visibility.Visible,
                    ErrorMessageDetail = response.StatusDescription + ":  \n" + response.Content
                };

                return;
            }

            // Png image decode and display
            var pngDecoder = new PngBitmapDecoder(imageStreamSource, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
            var bitmapSource = pngDecoder.Frames[0];

            // Display in new window the image received in response: use window size of the image / bitmap returned
            PatientHealthStatusDashboardViewModel.JsiAnomalyViewModel = new ImageWithDescriptionViewModel
            {
                ImageReceived = bitmapSource, //ButtonVisibility = Visibility.Hidden,
                IsErrorMessageVisible = Visibility.Hidden,
                PartnerName = "JSI",
                ImageDescription = "Anomaly detection in ECG signals"
            };
        }

        public void RetrieveAndDisplayLengthOfStayPrediction(string patientId)
        {
            var response = TryExecute(new Uri("http://127.0.0.1:5000/lofs/" + patientId));
            if (!response.IsSuccessful)
            {
                PatientHealthStatusDashboardViewModel.NxpLofsViewModel = new ImageWithDescriptionViewModel
                {
                    ErrorMessageDetail = response.Content,
                    IsErrorMessageVisible = Visibility.Visible,
                    ImageDescription = "Length of Stay Prediction"
                };
                return;
            }

            var lofsResponse = (LofsResponse)JsonConvert.DeserializeObject(response.Content, typeof(LofsResponse));
            var bytes = Convert.FromBase64String(lofsResponse.img);

            using (MemoryStream ms = new MemoryStream(bytes))   
            {
                var image = System.Drawing.Image.FromStream(ms);
                BitmapSource bitmapSource = BitmapImageFromImage(image);

                PatientHealthStatusDashboardViewModel.NxpLofsViewModel = new ImageWithDescriptionViewModel
                {
                    ImageReceived = bitmapSource, //ButtonVisibility = Visibility.Hidden,
                    IsErrorMessageVisible = Visibility.Hidden,
                    PartnerName = "NXP",
                    ImageDescription = "Length of Stay Prediction: " + lofsResponse.lofs + " Days"
                };
            }
        }

        private RestResponse TryExecute(Uri uri)
        {
            var restClient = new RestClient(uri);
            var request = new RestRequest("", Method.Get) { };
            var response = restClient.Execute(request);
            return response;
        }

        private MemoryStream TryRestApiGet(Uri uri, out RestResponse Response)
        {
            Response = TryExecute(uri);
            if (!Response.IsSuccessful) return null;

            var imageStreamSource = new MemoryStream(Response.RawBytes);
            return imageStreamSource;
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

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
           RetrievePatientData();
        }

        private void GET_OnClick(object sender, RoutedEventArgs e)
        {
            RetrievePatientData();
        }
    }

    // View Models for display of retrieved information
    public class PatientHealthStatusDashboardViewModel : INotifyPropertyChanged
    {
        //public ObservableCollection<ImageWithDescriptionViewModel> ImageWithDescriptionViewModels;
        //public ObservableCollection<VitalSignGraphViewModel> VitalSignGraphViewModels; // VTT only

        public ImageWithDescriptionViewModel NxpLofsViewModel
        {
            get => _nxpLofsViewModel;
            set { _nxpLofsViewModel = value; OnPropertyChanged(); }
        } 
        public ImageWithDescriptionViewModel TudEcgCompressedViewModel
        {
            get => _tudEcgCompressedViewModel;
            set { _tudEcgCompressedViewModel = value; OnPropertyChanged(); }
        }
        public ImageWithDescriptionViewModel JsiAnomalyViewModel
        {
            get => _jsiAnomalyViewModel;
            set { _jsiAnomalyViewModel = value; OnPropertyChanged(); }
        }
        public VitalSignGraphViewModel VttViewModel
        {
            get => _vttViewModel;
            set { _vttViewModel = value; OnPropertyChanged(); }
        }

        private ImageWithDescriptionViewModel _jsiAnomalyViewModel;
        private ImageWithDescriptionViewModel _nxpLofsViewModel;
        private ImageWithDescriptionViewModel _tudEcgCompressedViewModel;
        private VitalSignGraphViewModel _vttViewModel;

      


        public void Clear()
        {
            //ImageWithDescriptionViewModels.Clear();
            //VitalSignGraphViewModels.Clear();
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) 
                handler(this, new PropertyChangedEventArgs(propertyName));
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }
    public class ImageWithDescriptionViewModel : INotifyPropertyChanged
    {
        public string ImageDescription
        {
            get => _imageDescription;
            set { _imageDescription = value; OnPropertyChanged(); }
        }
        public string PartnerName
        {
            get => _partnerName;
            set { _partnerName = value; OnPropertyChanged(); }
        }


        public string ErrorMessageDetail
        {
            get { return _errorMessageDetail; }
            set { _errorMessageDetail = value; OnPropertyChanged(); }
        }

        public Visibility ButtonVisibility
        {
            get { return _buttonVisibility; }
            set { _buttonVisibility = value; OnPropertyChanged(); }
        }
        public Visibility IsErrorMessageVisible
        {
            get { return _isErrorMessageVisible; }
            set { _isErrorMessageVisible = value; OnPropertyChanged(); }
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
        Visibility _isErrorMessageVisible;
        BitmapSource _imageReceived;
        int _imageHeight, _imageWidth;
        string _errorMessageDetail;
        private string _partnerName;
        private string _imageDescription;

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public class VitalSignGraphViewModel: INotifyPropertyChanged
    {
        public bool IsPlotEnabled
        {
            get => _isPlotEnabled;
            set { _isPlotEnabled = value; OnPropertyChanged(); }
        }
        public string WindowDescription
        {
            get => _windowDescription;
            set { _windowDescription = value; OnPropertyChanged(); }
        }
        public string ErrorMessageDetail
        {
            get => _errorMessageDetail;
            set { _errorMessageDetail = value; OnPropertyChanged(); }
        }

        public Visibility IsErrorMessageVisible
        {
            get { return _isErrorMessageVisible; }
            set { _isErrorMessageVisible = value; OnPropertyChanged(); }
        }

        public Visibility IsPlotVisible
        {
            get { return _isPlotVisible; }
            set { _isPlotVisible = value; OnPropertyChanged(); }
        }


        // Public members that need to be accessed afterwards during measurements insertion.
        public PlotModel VitalSignPlotModel
        {
            get { return _vitalSignPlotModel; }
            set { _vitalSignPlotModel = value; OnPropertyChanged(); }
        }

        private PlotModel _vitalSignPlotModel;

        public LineSeries LineSeries
        {
            get { return _lineSeries; }
            set { _lineSeries = value; OnPropertyChanged(); }

        }

        private LineSeries _lineSeries;

        public DateTimeAxis DateTimeAxis { get; private set; }
        public LinearAxis ValueAxis { get; private set; }

        public VitalSignGraphViewModel()
        {
           
            VitalSignPlotModel = new PlotModel();

            if (!IsPlotEnabled) return;

            // Line is transparent between measurement points, actual measurements are displayed as blue dots.
            LineSeries = new LineSeries { MarkerFill = OxyColor.FromRgb(0, 0, 255), MarkerSize = 5, MarkerType = MarkerType.Circle, Color = OxyColor.FromArgb(50, 0, 0, 0) };

            VitalSignPlotModel.Series.Add(LineSeries);
           
            DateTimeAxis = new DateTimeAxis { IsPanEnabled = true, IsZoomEnabled=true, Title= "Date / Time" /* StringFormat = "dd:hh:mm:ss"*/ };
            ValueAxis = new LinearColorAxis {  /*Angle = 90, IsPanEnabled = true, IsZoomEnabled =true, Title = "CCCCCC djfjfkjfk"*/ };

            VitalSignPlotModel.Axes.Add(DateTimeAxis);
            VitalSignPlotModel.Axes.Add(ValueAxis);
        }


        private string _windowDescription;
        private string _errorMessageDetail;

        private Visibility _isErrorMessageVisible;
        private Visibility _isPlotVisible;
        private bool _isPlotEnabled;



        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }

   
}
