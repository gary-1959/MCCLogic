using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Deployment.Application;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
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
using System.Windows.Threading;

namespace SharpCircuit
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class DonateWindow : Window
    {

        public string LicenseMode { get; set; } = Donate.GetLicenseType();

        public DonateWindow()
        {
            InitializeComponent();
            this.DataContext = this;

            DonateEmail.Text = Properties.Settings.Default.DonateEmail;
        }


        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void BuyButton_Click(object sender, RoutedEventArgs e)
        {
            string vs = "1.0.0.0";
            try
            {
                System.Version v = ApplicationDeployment.CurrentDeployment.CurrentVersion;
                vs = v.ToString();
            }
            catch { }
            System.Diagnostics.Process.Start(Donate.GetDonateURL());
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            Properties.Settings.Default.DonateEmail = DonateEmail.Text;
            Donate.CheckDonation();
        }

        private void DonateEmail_TextChanged(object sender, TextChangedEventArgs e)
        {
            
        }
    }
}

