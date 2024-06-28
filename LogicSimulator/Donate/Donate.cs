using System;
using System.Collections.Generic;
using System.Deployment.Application;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;

namespace SharpCircuit
{
    static class Donate
    {
        public static string GetLicenseType()
        {
            return ("DONATEWARE");
        }

        public static string GetDonateURL()
        {
            return ("https://www.paypal.com/donate/?hosted_button_id=TUQ2SYUKLFJ2G");
        }

        public static bool CheckDonation()
        {
            string email = Properties.Settings.Default.DonateEmail;
            if (String.IsNullOrEmpty(email))
            {
                Properties.Settings.Default.DonationStatus = false;
            } else {
                if (!Properties.Settings.Default.DonationStatus)
                {
                    // initiate task to check donation status
                    _ = Task.Run(() => GetLicenseStatusAsync(email));
                }
            }
            Debug.Log("Donation is: " + Properties.Settings.Default.DonationStatus);
            return (Properties.Settings.Default.DonationStatus);
        }

        public static async Task GetLicenseStatusAsync(string email)
        {
            string uri = "http://www.contrelec.co.uk/contrelec/donations/check.php" +
                "?email=" + email +
                "&package=mcclogic" +
                "&id=" + Licensing.FingerPrint.Value();
            string r = "FALSE";
            try
            {
                Debug.Log("Getting donation status");
                HttpResponseMessage response = new HttpResponseMessage();
                response = await Program.httpClient.GetAsync(new Uri(uri, UriKind.Absolute));
                r = await response.Content.ReadAsStringAsync();
                if (r == "TRUE")
                {
                    Properties.Settings.Default.DonationStatus = true;
                } else
                {
                    Properties.Settings.Default.DonationStatus = false;
                }

            }
            catch (Exception ex)
            {
                // Console.WriteLine(ex.Message.ToString());
            }


        }
    }
}
