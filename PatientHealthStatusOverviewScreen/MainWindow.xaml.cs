using System;
using System.Collections.Generic;
using System.IO;
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
using PatientDataRetriever;
using RestSharp;

namespace PatientHealthStatusOverviewScreen
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public PatientDashboardViewModel PatientDashboardViewModel { get; private set; }
        public VitalSignGraphViewModel VitalSignGraphViewModel { get; private set; }

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Analyze(object sender, RoutedEventArgs e)
        {
            var xxx = new PatientDataRetriever.MainWindow();
            // VTT Vital Sign Data retrieval

        //    xxx.RetrieveAndDisplayAsSeriesInWindow("Cam1", "xxx", "OxygenSaturation", 100);
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
           xxx.RetrieveAndDisplayECGs("2");


            // JSI ECG anomaly retrieval
            // "2" indicates Patient Id.
     //       xxx.RetrieveAndDisplayECGAnomalies("2");


            // Length of Stay Prediction (LOFS).
            // "1" indicates Patient Id.
     //       xxx.RetrieveAndDisplayLengthOfStayPrediction("3");
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

        private void DisplayImageReceived(BitmapSource bitmapSource, string title, string description)
        {
            new MainWindow
            {
                DataContext =
                    PatientDashboardViewModel = new PatientDashboardViewModel { ImageReceived = bitmapSource, ButtonVisibility = Visibility.Hidden, ErrorMessageVisibility = Visibility.Hidden, PartnerName = title, WindowDescription = description },
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
    }
}
