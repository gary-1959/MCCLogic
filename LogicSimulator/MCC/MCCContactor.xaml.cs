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
using WpfAnimatedGif;

namespace SharpCircuit
{
    /// <summary>
    /// Interaction logic for MCCContactor.xaml
    /// </summary>
    public partial class MCCContactor : UserControl
    {
        public SimCircuit simcircuit { get; set; }
        private double contactorVolume { get; set; } = 1;
        private MediaPlayer contactorClickPlayer = new MediaPlayer();
        private BitmapImage imageOpen { get; set; }
        private BitmapImage imageClosed { get; set; }
        private BitmapImage imageTripUp { get; set; }
        private BitmapImage imageTripDn { get; set; }
        private BitmapImage imageResetUp { get; set; }
        private BitmapImage imageResetDn { get; set; }
        private Image blowerImage { get; set; }
        private BitmapImage blowerStopped { get; set; }
        private BitmapImage blowerRunning { get; set; }
        public List<Program.SwitchContact> mainContacts;
        public List<Program.SwitchContact> overloadContacts;
        public Resistor coil { get; set; }
        private double coilVoltage { get; set; }
        private bool _isClosed;
        public bool isClosed
        {
            get
            {
                return (_isClosed);
            }
            set
            {
                if (value != _isClosed)
                {
                    if (value)
                    {
                        Indicator.Source = imageClosed;
                        contactorClick();
                        _isClosed = true;
                        setAuxContacts();
                    }
                    else
                    {
                        if (!isStuck)
                        {
                            Indicator.Source = imageOpen;
                            contactorClick();
                            _isClosed = false;
                            setAuxContacts();
                        }
                    }

                }
            }
        }
        private bool _isFaulty;
        public bool isFaulty
        {
            get
            {
                return _isFaulty;
            }
            set
            {
                _isFaulty = value;
                isClosed = false;
            }
        }
        private bool _isStuck;
        public bool isStuck
        {
            get
            {
                return _isStuck;

            }
            set
            {
                _isStuck = value;
            }
        }
        private bool _isTripped;
        public bool isTripped
        {
            get
            {
                return (_isTripped);
            }
            set
            {
                if (value != _isTripped)
                {
                    if (value)
                    {
                        Trip.Source = imageTripUp;
                        _isTripped = value;
                        isClosed = false;   // sets AUX contacts
                    }
                    else
                    {
                        Trip.Source = imageTripDn;
                        _isTripped = value;
                        setAuxContacts();
                    }
                }
            }
        }
        public string location { get; set; }
        public MCCContactor()
        {
            InitializeComponent();

            mainContacts = new List<Program.SwitchContact>();
            overloadContacts = new List<Program.SwitchContact>();

            imageOpen = new BitmapImage(new Uri(@"pack://application:,,,/MCCLogic;component/Resources/MCC-CONTACTOR-OPEN.png"));
            imageClosed = new BitmapImage(new Uri(@"pack://application:,,,/MCCLogic;component/Resources/MCC-CONTACTOR-CLOSED.png"));

            imageTripUp = new BitmapImage(new Uri(@"pack://application:,,,/MCCLogic;component/Resources/BLOWER-CONTACTOR-TRIP-UP.png"));
            imageTripDn = new BitmapImage(new Uri(@"pack://application:,,,/MCCLogic;component/Resources/BLOWER-CONTACTOR-TRIP-DOWN.png"));

            imageResetUp = new BitmapImage(new Uri(@"pack://application:,,,/MCCLogic;component/Resources/BLOWER-CONTACTOR-RESET-UP.png"));
            imageResetDn = new BitmapImage(new Uri(@"pack://application:,,,/MCCLogic;component/Resources/BLOWER-CONTACTOR-RESET-DOWN.png"));

            contactorClickPlayer.Open(new Uri(@"Resources\metal_door_close.mp3", UriKind.Relative));

            Indicator.Source = imageOpen;
            Trip.Source = imageTripDn;
            Reset.Source = imageResetUp;

            coilVoltage = 115;

            Reset.MouseLeftButtonDown += MouseDownReset;
            Reset.MouseLeftButtonUp += MouseUpReset;

            Program.TickComplete += tickCompleteHandler;
            Program.OnSoundMuteChange += SoundMuteHandler;
        }

        public void contactorClick()
        {
            double v = contactorVolume;
            if (Program.soundMute) v = 0;
            contactorClickPlayer.Volume = v;
            contactorClickPlayer.Position = new TimeSpan(0, 0, 0);
            contactorClickPlayer.Play();
        }

        public void tickCompleteHandler(object sender, EventArgs e)
        {
            if (coil != null)
            {
                if ((Program.isCoilEnergised(coil, coilVoltage)) && (_isClosed == false) && (_isTripped == false) && (isFaulty == false) && (!isStuck || (isStuck && _isClosed == false)))
                {
                    isClosed = true;
                }
                else if (!Program.isCoilEnergised(coil, coilVoltage))
                {
                    if (!isStuck)
                    {
                        isClosed = false;
                    }
                }
                else if (_isTripped && !isStuck)
                {
                    isClosed = false;
                }
            }

        }
        private void MouseUpReset(object sender, MouseButtonEventArgs e)
        {
            Reset.Source = imageResetUp;
            e.Handled = true;
        }

        private void MouseDownReset(object sender, MouseButtonEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                Reset.Source = imageResetDn;
                isTripped = false;

                // Added 29/09/17 to fix fault if applied
                foreach (KeyValuePair<int, Program.FaultItem> entry in Program.FaultItems)
                {
                    Program.FaultItem f = entry.Value;
                    
                    if (f.selected && f.faulted && f.owner == this)
                    {
                        f.fixFault();
                        f.faulted = false;
                        f.selected = false;
                    }
                }
                e.Handled = true;
            }
            else
            {
                e.Handled = false;
            }

        }

        public void setAuxContacts()
        {
            foreach (Program.SwitchContact c in overloadContacts)
            {
                simcircuit.contactPos(c, _isTripped);
            }

            foreach (Program.SwitchContact c in mainContacts)
            {
                simcircuit.contactPos(c, _isClosed);
            }
        }



        public void configureMCCContactor(SimCircuit sc, string scr)
        {
            simcircuit = sc;
            location = scr;

            mainContacts.Add(new Program.SwitchContact(simcircuit, scr, "SC1-1", false));
            mainContacts.Add(new Program.SwitchContact(simcircuit, scr, "SC1-3", false));
            mainContacts.Add(new Program.SwitchContact(simcircuit, scr, "SC1-5", false));
            mainContacts.Add(new Program.SwitchContact(simcircuit, scr, "SC1-COM", false));
            mainContacts.Add(new Program.SwitchContact(simcircuit, scr, "SC1-NC1", true));
            mainContacts.Add(new Program.SwitchContact(simcircuit, scr, "SC1-NO1", false));
            mainContacts.Add(new Program.SwitchContact(simcircuit, scr, "SC1-NC2", true));
            mainContacts.Add(new Program.SwitchContact(simcircuit, scr, "SC1-NO2", false));

            overloadContacts.Add(new Program.SwitchContact(simcircuit, scr, "SOL1", true));
            overloadContacts.Add(new Program.SwitchContact(simcircuit, scr, "SOL2", false));

            foreach (KeyValuePair<string, NetElement> el in simcircuit.elements)
            {
                if (el.Value.location == scr)
                {
                    if (el.Value.nettag == "RC1")
                    {
                        coil = (Resistor)el.Value.simElement;
                        break;
                    }
                }

            }
            #region MCC_CONTACTOR
            simcircuit.setNode(cpC1_A, scr, "RC1", "A", "C1", "A");
            cpC1_A.IsACVoltage = true;
            simcircuit.setNode(cpC1_B, scr, "RC1", "B", "C1", "B");
            cpC1_B.IsACVoltage = true;

            simcircuit.setNode(cpC1_1, scr, "SC1-1", "1", "C1", "1");
            cpC1_1.IsACVoltage = true;
            simcircuit.setNode(cpC1_2, scr, "SC1-1", "2", "C1", "2");
            cpC1_2.IsACVoltage = true;
            simcircuit.setNode(cpC1_3, scr, "SC1-3", "3", "C1", "3");
            cpC1_3.IsACVoltage = true;
            simcircuit.setNode(cpC1_4, scr, "SC1-3", "4", "C1", "4");
            cpC1_4.IsACVoltage = true;
            simcircuit.setNode(cpC1_5, scr, "SC1-5", "5", "C1", "5");
            cpC1_5.IsACVoltage = true;
            simcircuit.setNode(cpC1_6, scr, "SC1-5", "6", "C1", "6");
            cpC1_6.IsACVoltage = true;

            simcircuit.setNode(cpC1_COM, scr, "SC1-COM", "COM", "C1", "COM");
            cpC1_COM.IsACVoltage = true;
            simcircuit.setNode(cpC1_NO, scr, "SC1-COM", "NO", "C1", "NO");
            cpC1_NO.IsACVoltage = true;

            simcircuit.setNode(cpC1_11, scr, "SC1-NO1", "11", "C1", "11");
            simcircuit.setNode(cpC1_12, scr, "SC1-NO1", "12", "C1", "12");
            simcircuit.setNode(cpC1_13, scr, "SC1-NO2", "13", "C1", "13");
            simcircuit.setNode(cpC1_14, scr, "SC1-NO2", "14", "C1", "14");
            simcircuit.setNode(cpC1_15, scr, "SC1-NC1", "15", "C1", "15");
            simcircuit.setNode(cpC1_16, scr, "SC1-NC1", "16", "C1", "16");
            simcircuit.setNode(cpC1_17, scr, "SC1-NC2", "17", "C1", "17");
            simcircuit.setNode(cpC1_18, scr, "SC1-NC2", "18", "C1", "18");

            simcircuit.setNode(cpOL1_2, scr, "ROL1-1", "2", "OL1", "2");
            cpOL1_2.IsACVoltage = true;
            simcircuit.setNode(cpOL1_4, scr, "ROL1-3", "4", "OL1", "4");
            cpOL1_4.IsACVoltage = true;
            simcircuit.setNode(cpOL1_6, scr, "ROL1-5", "6", "OL1", "6");
            cpOL1_6.IsACVoltage = true;
            
            simcircuit.setNode(cpOL1_NC1, scr, "SOL1", "NC1", "OL1", "NC");
            cpOL1_NC1.IsACVoltage = true;
            simcircuit.setNode(cpOL1_NC2, scr, "SOL1", "NC2", "OL1", "NC");
            cpOL1_NC2.IsACVoltage = true;

            simcircuit.setNode(cpOL1_NO1, scr, "SOL2", "NO1", "OL1", "NO");
            cpOL1_NO1.IsACVoltage = true;
            simcircuit.setNode(cpOL1_NO2, scr, "SOL2", "NO2", "OL1", "NO");
            cpOL1_NO2.IsACVoltage = true;

            
            #endregion

            simcircuit.MCCContactors.Add(this);
        }

        private void SoundMuteHandler(object sender, EventArgs e)
        {

            double v = contactorVolume;
            if (Program.soundMute) v = 0;
            contactorClickPlayer.Volume = v;
        }

        private void Media_Ended(object sender, EventArgs e)
        {
            MediaPlayer m = sender as MediaPlayer;
            m.Position = new TimeSpan(0, 0, 0);
            m.Play();
        }
    }
}
