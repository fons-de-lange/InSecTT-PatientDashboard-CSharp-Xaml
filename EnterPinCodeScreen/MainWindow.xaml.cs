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

namespace EnterPinCodeScreen
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private decimal[] pincode = new decimal[100];
        private decimal pincodeTotal;
        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;
            var caretPosition = PinCodeBox.Text.Length;
            int pinCode = 0;
            if (e.Key >= Key.D0 && e.Key <= Key.D9)
            {
                pinCode+= 10 * (caretPosition + 1) * Int32.Parse(e.Key.ToString().Replace("D", string.Empty));
            }

            if (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9)
            {
                pinCode = 10 * (caretPosition + 1) * Int32.Parse(e.Key.ToString().Replace("NumPad", string.Empty));
            }
                
            if (pinCode > 0)
            { 
                //pincode[PinCodeBox.Text.Length] = ((decimal)e.Key);
                //pincodeTotal = pincodeTotal + ((decimal)e.Key) * 10 ;
                PinCodeBox.Text = PinCodeBox.Text + '*';
                //var caretPosition = PinCodeBox.Text.Length;
                PinCodeBox.SelectionStart = caretPosition;
            }
            else if (e.Key.Equals(Key.Enter))
            {
                (sender as TextBox).Text = string.Empty;

                // Check the pincode
                // If OK go to "Assign Patient Window"
                // Else report error
                
                if (pinCode == 123456)
                    MessageBox.Show("OK", "Message");
                else
                    MessageBox.Show("Wrong pincode", "Error");
            }
            else
                MessageBox.Show("Not a pincode", "Error");
        }
    }
}
