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
    public partial class SplashWindow : Window
    {
        
        public string NetFile { get; set; }

        public SplashWindow()
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

            if (Program.mainWindow != null)
            {
                Program.mainWindow.Close();
            }

            string vs = "N/A";
            try
            {
                System.Version v = ApplicationDeployment.CurrentDeployment.CurrentVersion;
                vs = v.ToString();
            }
            catch { }

            MessageText = "CONTRELEC MCCLogic Logic Simulator" + Environment.NewLine;
            MessageText += "----------------------------------" + Environment.NewLine;
            MessageText += "Version: " + vs + "; " + Assembly.GetExecutingAssembly().GetLinkerTime() + Environment.NewLine; ;
            MessageText += "License: " + "FREE" + Environment.NewLine;
            MessageText += "Loading circuit netlist" + Environment.NewLine;
            Program.InitialiseSim(NetFile);
            Close();
        }

        public void textTargetUpdated(object sender, DataTransferEventArgs e)
        {
            TextBox t = sender as TextBox;
            t.ScrollToEnd();
            t.Dispatcher.Invoke((() => { }), System.Windows.Threading.DispatcherPriority.Render);
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (Program.mainWindow != null) Program.mainWindow.SetTitle();
        }
    }
}

