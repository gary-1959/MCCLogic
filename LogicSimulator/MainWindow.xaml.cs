using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Deployment.Application;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
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
using Microsoft.Win32;
using mshtml;

namespace SharpCircuit
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public SimCircuit simcircuit {get; set;}
        private int timerHours { get; set; }
        private int timerMinutes{ get; set; }
        private int timerSeconds { get; set; }
        public ViewNotifier vn = new ViewNotifier();
        private System.Windows.Threading.DispatcherTimer dispatcherTimer;
        private bool soundMute { get; set; }
        public MCCRelay mccRelay { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            CheckBox cb = Training_HideMeter_CB as CheckBox;
            Program.AutoHideMeter = (cb.IsChecked == true);
            DataContext = vn;
        }

        public void configureMW(SimCircuit sc)
        {
            simcircuit = sc;
        
            // TODO: remove when debugging complete
            //Properties.Settings.Default.LicenseFile = "";
            //Properties.Settings.Default.Save();

            SetTitle();

            vn.ScriptNotRunning = true;

            dispatcherTimer = new System.Windows.Threading.DispatcherTimer(System.Windows.Threading.DispatcherPriority.Background);
            dispatcherTimer.Tick += dispatcherTimer_Tick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);



            // configure SCRs


            // MCC TAB
            //TODO: for quick debugging hide MCC

            mccTabControl.configureMCC();

            //MCC
            mccRelay = new MCCRelay();

            Program.createMainFaults();
            
            Program.soundMute = soundMute;
        }
        public void SetTitle()
        {
            Title = "MCCLogic by CONTRELEC [" + "DONATEWARE" + "]";
        }
        #region TIMER CONTROL
        public void timerReset()
        {
            dispatcherTimer.Stop();
            timerHours = 0;
            timerMinutes = 0;
            timerSeconds = 0;
            displayTimer();
        }

        public void timerStart()
        {
            dispatcherTimer.Start();
        }

        public void timerStop()
        {
            dispatcherTimer.Stop();
        }
        #endregion
        #region TRAINING
        public class ViewNotifier : INotifyPropertyChanged
        {
            private Boolean _timerIsRunning;
            public Boolean TimerIsRunning
            {
                get
                {
                    return _timerIsRunning;
                }
                set
                {
                    _timerIsRunning = value;
                    NotifyPropertyChanged("TimerIsRunning");
                    NotifyPropertyChanged("TimerNotRunning");
                }
            }

            public Boolean TimerNotRunning
            {
                get
                {
                    return !_timerIsRunning;
                }
                set
                {
                    TimerIsRunning = !value;
                }
            }

            public Boolean ScriptIsRunning
            {
                get
                {
                    return !_scriptNotRunning;
                }
                set
                {
                    ScriptNotRunning = !value;
                }
            }

            private Boolean _scriptNotRunning;
            public Boolean ScriptNotRunning 
            {
                get
                {
                    return _scriptNotRunning;
                }
                set
                {
                    _scriptNotRunning = value; // You miss this line, could be ok to do an equality check here to. :)
                    NotifyPropertyChanged("ScriptNotRunning");
                    NotifyPropertyChanged("ScriptIsRunning");
                }
            }
            private Boolean _scriptIsEditable;
            public Boolean ScriptIsEditable
            {
                get
                {
                    return (_scriptIsEditable);
                }
                set
                {
                    _scriptIsEditable = (_scriptNotRunning && (Scripting.currentScript != null) && value);
                    NotifyPropertyChanged("ScriptIsEditable");
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;

            protected void NotifyPropertyChanged(string name)
            {
                PropertyChangedEventHandler handler = PropertyChanged;
                if (handler != null)
                {
                    handler(this, new PropertyChangedEventArgs(name));
                }
            }
        }

        private void Training_HideMeter(object sender, RoutedEventArgs e)
        {
            CheckBox cb = sender as CheckBox;
            if (cb.IsChecked == true)
            {
                Program.AutoHideMeter = true;
                Program.meterOut(popMeter);
            }
            else
            {
                Program.AutoHideMeter = false;
                Program.meterIn(popMeter);
            }
        }
        private void Training_Faults(object sender, RoutedEventArgs e)
        {
            FaultWindow w = new FaultWindow();
            w.Owner = Program.mainWindow;
            w.Mode = FaultItemControl.FaultItemControlMode.NORMAL;
            w.Show();
        }
        public void Training_PrintFaults(object sender, RoutedEventArgs e)
        {

            string lastGroup = "";
            string lastSection = "";
            string oStr = "";
            string tab = "\t";

            // header

            string vs = "1.0.0.0";
            try
            {
                System.Version v = ApplicationDeployment.CurrentDeployment.CurrentVersion;
                vs = v.ToString();
            }
            catch { }

            oStr = "CONTRELEC MCCLogic Logic Simulator" + Environment.NewLine;
            oStr += "----------------------------------" + Environment.NewLine;
            oStr += "Version: " + vs + "; " + Assembly.GetExecutingAssembly().GetLinkerTime() + Environment.NewLine; ;
            oStr += "License: " + "FREE" + Environment.NewLine;
            oStr += "FAULT LISTING" + Environment.NewLine;
            oStr += "----------------------------------" + Environment.NewLine + Environment.NewLine;

            foreach (KeyValuePair<int, Program.FaultItem> entry in Program.FaultItems)
            {
                Program.FaultItem f = entry.Value;
                if (f.section != lastSection)
                {
                    oStr += f.section + Environment.NewLine;
                    lastGroup = "";
                    lastSection = f.section;

                }
                if (f.group != lastGroup)
                {
                    oStr += tab + f.group + Environment.NewLine;
                    lastGroup = f.group;
                }

                oStr += tab + tab +  "[" + f.id.ToString("D4") + "] " + f.name + Environment.NewLine ;
            }
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = "MCCLogic_Faults.txt";
            dlg.DefaultExt = "txt";

            bool? result = dlg.ShowDialog();
            if (result == true)
            {
                File.WriteAllText(dlg.FileName, oStr);
            }
        }
        private void Training_ApplyFaults(object sender, RoutedEventArgs e)
        {
            Program.settingFaultsStatus = Program.settingFaults.APPLY;
        }
        private void Training_ClearFaults(object sender, RoutedEventArgs e)
        {
            Program.settingFaultsStatus = Program.settingFaults.CLEAR;
        }

        private void Training_ClearAllFaults(object sender, RoutedEventArgs e)
        {
            Program.settingFaultsStatus = Program.settingFaults.CLEARALL;
        }
        private void Training_FixAFault(object sender, RoutedEventArgs e)
        {
            FaultWindow w = new FaultWindow();
            w.Owner = Program.mainWindow;
            w.Mode = FaultItemControl.FaultItemControlMode.FIX;
            w.Show();
        }
        private void Training_LoadScript(object sender, RoutedEventArgs e)
        {
            Scripting.openScript();
        }
        private void Training_ScriptWriter(object sender, RoutedEventArgs e)
        {
            Scripting.openWriter();
        }
        private void Training_SoundMute(object sender, RoutedEventArgs e)
        {
            CheckBox cb = sender as CheckBox;
            Program.soundMute = (cb.IsChecked == true);
        }
        private void Training_ResumeScript(object sender, RoutedEventArgs e)
        {
            Scripting.runScript(true);
        }
        private void Training_TimerStart(object sender, RoutedEventArgs e)
        {
            Scripting.runScript(true);
        }
        private void Training_TimerStop(object sender, RoutedEventArgs e)
        {
            Scripting.runScript(false);
            Scripting.resetScript();
        }
        private void Training_TimerReset(object sender, RoutedEventArgs e)
        {
            Scripting.runScript(false);
            Scripting.resetScript();
            timerReset();
        }
        private void displayTimer()
        {
            tHours.Content = timerHours.ToString("D2");
            tMinutes.Content = timerMinutes.ToString("D2");
            tSeconds.Content = timerSeconds.ToString("D2");

        }
        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            timerSeconds += 1;
            if (timerSeconds > 59)
            {
                timerSeconds = 0;
                timerMinutes += 1;
            }
            if (timerMinutes > 59)
            {
                timerSeconds = 0;
                timerMinutes = 0;
                timerHours += 1;
            }
            if (timerHours > 99)
            {
                timerSeconds = 0;
                timerMinutes = 0;
                timerHours  = 0;
            }
            displayTimer();

            Scripting.processScript(timerHours, timerMinutes, timerSeconds);
        }
        #endregion
        #region FILE
        private void File_Open(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.FileName = "WIRECON"; // Default file name
            dlg.DefaultExt = ".CSV"; // Default file extension
            dlg.Filter = "Netlist CSV (.csv)|*.csv"; // Filter files by extension

            // Show open file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                // Open document
                Program.simMain.path = dlg.FileName;
                Program.Reload();
            }
        }
        private void File_Reload(object sender, RoutedEventArgs e)
        {
            Program.Reload();
        }
        private void File_Exit(object sender, RoutedEventArgs e)
        {
            Close();
        }
        #endregion
        #region HELP
        private void Help_About(object sender, RoutedEventArgs e)
        {
            AboutWindow w = new AboutWindow();
            w.ShowDialog();
        }
        private void Help_Contrelec(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("http://www.contrelec.co.uk");
        }

        private void Help_Schematics(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(@"Resources\MCCLogic Schematic.pdf");
        }
        private void Help_Manual(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(@"Resources\MCCLogic User Manual.pdf");
        }

        #endregion
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            Properties.Settings.Default.Save();
        }


        private void Donate_Click(object sender, RoutedEventArgs e)
        {
            DonateWindow w = new DonateWindow();
            w.ShowDialog();
        }
    }
}
