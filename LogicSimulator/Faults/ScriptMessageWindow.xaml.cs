using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Net;
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
using Microsoft.Win32;

namespace SharpCircuit
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class ScriptMessageWindow : Window
    {
        public BitmapImage sImage
        {
            get
            {
                return (null);
            }
            set
            {
                ScriptImage.Source = value;
            }
        }

        public string css { get; set; }
        public string sText
        {
            get
            {
                return ScriptText.Document.ToString();
            }
            set
            {

                WebClient client = new WebClient();
                string fileName = System.IO.Path.GetTempPath() + Guid.NewGuid().ToString() + ".tmp";
                string content = "<html><header> " + css + "</header><body>" + value + "</div></body></html>";

                using (var writeFile = new StreamWriter(fileName))
                {
                    writeFile.Write(content);
                    writeFile.Close();
                    var uri = new Uri(fileName, UriKind.Absolute);
                    ScriptText.Navigate(uri);
                }
            }
        }


        public ScriptMessageWindow()
        {
            InitializeComponent();

            SetBrowserFeatureControl();

            css =   "<style>" +
                    "body { " +
                        "font-family: Arial, Helvetica, sans-serif;" +
                        "font-size: 14px;" +
                    "}" +
                     "h1 { " +
                        "font-size: 22px;" +
                    "}" +
                    "h2 { " +
                        "font-size: 18px;" +
                    "}" +
                    "h3 { " +
                        "font-size: 16px;" +
                    "}" +
                    "</style>";
            
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ScriptText_Loaded(object sender, RoutedEventArgs e)
        {



        }
        private static void SetBrowserFeatureControl()
        {
            // http://msdn.microsoft.com/en-us/library/ee330720(v=vs.85).aspx

            // WebBrowser Feature Control settings are per-process
            var fileName = System.IO.Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName);

            // make the control is not running inside Visual Studio Designer
            if (String.Compare(fileName, "devenv.exe", true) == 0 || String.Compare(fileName, "XDesProc.exe", true) == 0)
                return;

            SetBrowserFeatureControlKey("FEATURE_BROWSER_EMULATION", fileName, GetBrowserEmulationMode());
        }

        private static void SetBrowserFeatureControlKey(string feature, string appName, uint value)
        {
            using (var key = Registry.CurrentUser.CreateSubKey(
                String.Concat(@"Software\Microsoft\Internet Explorer\Main\FeatureControl\", feature),
                RegistryKeyPermissionCheck.ReadWriteSubTree))
            {
                key.SetValue(appName, (UInt32)value, RegistryValueKind.DWord);
            }
        }

        private static UInt32 GetBrowserEmulationMode()
        {
            int browserVersion = 7;
            using (var ieKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Internet Explorer",
                RegistryKeyPermissionCheck.ReadSubTree,
                System.Security.AccessControl.RegistryRights.QueryValues))
            {
                var version = ieKey.GetValue("svcVersion");
                if (null == version)
                {
                    version = ieKey.GetValue("Version");
                    if (null == version)
                        throw new ApplicationException("Microsoft Internet Explorer is required!");
                }
                int.TryParse(version.ToString().Split('.')[0], out browserVersion);
            }

            // Internet Explorer 10. Webpages containing standards-based !DOCTYPE directives are displayed in IE10 Standards mode. Default value for Internet Explorer 10.
            UInt32 mode = 10000;

            switch (browserVersion)
            {
                case 7:
                    // Webpages containing standards-based !DOCTYPE directives are displayed in IE7 Standards mode. Default value for applications hosting the WebBrowser Control.
                    mode = 7000;
                    break;
                case 8:
                    // Webpages containing standards-based !DOCTYPE directives are displayed in IE8 mode. Default value for Internet Explorer 8
                    mode = 8000;
                    break;
                case 9:
                    // Internet Explorer 9. Webpages containing standards-based !DOCTYPE directives are displayed in IE9 mode. Default value for Internet Explorer 9.
                    mode = 9000;
                    break;
                default:
                    // use IE10 mode by default
                    break;
            }

            return mode;
        }
    }
}

