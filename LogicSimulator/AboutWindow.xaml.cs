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
    public partial class AboutWindow : Window
    {
        public string LicenseMode { get; set; } = Donate.GetLicenseType();

        public AboutWindow()
        {
            InitializeComponent();
            this.DataContext = this;
        }

        public void Start()
        {

        }

        private string messageText;
        public string MessageText
        {
            get { return messageText; }
            set
            {
                messageText = value;
                textBlock.Text = value;
                textTargetUpdated(textBlock, null);
            }
        }


        bool _shown;

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);

            if (_shown)
                return;

            _shown = true;

            string vs = "N/A";
            try
            {
                System.Version v = ApplicationDeployment.CurrentDeployment.CurrentVersion;
                vs = v.ToString();
            }
            catch { }

            MessageText = "CONTRELEC MCCLogic Logic Simulator" + Environment.NewLine;
            MessageText += "----------------------------------" + Environment.NewLine;
            MessageText += "Version: " + vs + "; " + Assembly.GetExecutingAssembly().GetLinkerTime() + Environment.NewLine;
            MessageText += "License: " + LicenseMode + Environment.NewLine;
            MessageText += "Machine ID: " + Licensing.FingerPrint.Value() + Environment.NewLine;
            MessageText += "Copyright © 2017-2022. All rights reserved. " + Environment.NewLine;
        }

        public void textTargetUpdated(object sender, DataTransferEventArgs e)
        {
            TextBox t = sender as TextBox;
            t.ScrollToEnd();
            t.Dispatcher.Invoke((() => { }), System.Windows.Threading.DispatcherPriority.Render);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }


        private void Window_Closing(object sender, CancelEventArgs e)
        {

        }
    }
}

