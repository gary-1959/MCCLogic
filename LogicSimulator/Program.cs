using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Media;
using System.Text.RegularExpressions;
using System.Windows.Media.Imaging;
using System.IO;
using System.Security.Cryptography;
using System.Xml;
using System.Security.Cryptography.Xml;
using System.Security;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Threading;
using System.Xml.Serialization;
using System.Windows.Media.Animation;
using System.Net.Http;

namespace SharpCircuit
{
    public static class StringExtensionMethods
    {
        public static IEnumerable<string> GetLines(this string str, bool removeEmptyLines = false)
        {
            using (var sr = new StringReader(str))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    if (removeEmptyLines && String.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }
                    yield return line;
                }
            }
        }
    }
    public static class ExtensionMethods
    {

        private static Action EmptyDelegate = delegate () { };


        public static void Refresh(this UIElement uiElement)
        {
            uiElement.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);
        }
    }
    public class Program
    {
        public static HttpClient httpClient = new HttpClient();
        public static int DWBAssigned { get; set; } = 0;
        public class MCCInterface
        {
            public string tag { get; set; }
            public MCCCan can { get; set; }
            // elements in MCC can
            public NetElement canRemoteStart { get; set; }
            public NetElement canRunIndication { get; set; }
            public NetElement canAlarmIndication { get; set; }
            // elements in other circuit
            public RelayHandler otherRemoteStartRelay { get; set; }
            public NetElement otherRunIndication { get; set; }
            public NetElement otherAlarmIndication { get; set; }
            public List<VoltageSourceHook> voltageSourceHooks { get; set; }

            public MCCInterface(string t, MCCCan c, NetElement cS, NetElement cI, NetElement cA, RelayHandler oS, NetElement oI, NetElement oA)
            {
                tag = t;
                can = c;

                canRemoteStart = cS;
                canRunIndication = cI;
                canAlarmIndication = cA;

                otherRemoteStartRelay = oS;
                otherRunIndication = oI;
                otherAlarmIndication = oA;

                voltageSourceHooks = new List<VoltageSourceHook>();

                // the following gets the schematic contacts operating with the coil across the simulations 
                // add remote start contact in MCC to start relay
                otherRemoteStartRelay.auxContacts.Add(new SwitchContact(can.simCan, canRemoteStart, false));
                // add alarm and run indication contacts to can contactor
                can.canContactor.mainContacts.Add(new SwitchContact(simMain, otherRunIndication, false));
                can.canContactor.mainContacts.Add(new SwitchContact(simMain, otherAlarmIndication, true));

                // V-11 and V-12 map to run contact
                NetElement n11 = can.simCan.getElementByLocationAndNettag("CAN", "V-11");
                VoltageInput v11 = n11.simElement as VoltageInput;
                voltageSourceHooks.Add(new VoltageSourceHook(v11, otherRunIndication.simElement, 0));

                NetElement n12 = can.simCan.getElementByLocationAndNettag("CAN", "V-12");
                VoltageInput v12 = n12.simElement as VoltageInput;
                voltageSourceHooks.Add(new Program.VoltageSourceHook(v12, otherRunIndication.simElement, 1));


                // V-15 and V-16 map to alarm contact
                NetElement n15 = can.simCan.getElementByLocationAndNettag("CAN", "V-15");
                VoltageInput v15 = n15.simElement as VoltageInput;
                voltageSourceHooks.Add(new VoltageSourceHook(v15, otherAlarmIndication.simElement, 0));

                NetElement n16 = can.simCan.getElementByLocationAndNettag("CAN", "V-16");
                VoltageInput v16 = n16.simElement as VoltageInput;
                voltageSourceHooks.Add(new Program.VoltageSourceHook(v16, otherAlarmIndication.simElement, 1));
            }
        }
        public class VoltageSourceHook
        {
            public VoltageInput voltageInput { get; set; }
            public CircuitElement circuitElement { get; set; }
            public int lead { get; set; }

            public VoltageSourceHook(VoltageInput v, CircuitElement c, int l)
            {
                voltageInput = v;
                circuitElement = c;
                lead = l;
            }

            public void doHookUp()
            {
                voltageInput.maxVoltage = circuitElement.getLeadVoltage(lead);
            }
        }
        public static bool AutoHideMeter { get; set; } = true;
        public delegate void SoundMuteEventHandler(object sender, EventArgs e);
        public static event SoundMuteEventHandler OnSoundMuteChange;
        private static bool _soundMute;
        public static bool soundMute
        {
            get
            {
                return _soundMute;
            }
            set
            {
                _soundMute = value;
                if (OnSoundMuteChange != null)
                {
                    OnSoundMuteChange(soundMute, new EventArgs());
                }

            }
        }
        public enum settingFaults { APPLY, CLEAR, CLEARALL, SET, DONE };
        public static settingFaults settingFaultsStatus { get; set; }
        public static Dictionary<int, FaultItem> FaultItems = new Dictionary<int, FaultItem>();
        public class FaultItem
        {
            public int id { get; set; }
            public string section { get; set; }
            public string group { get; set; }
            public string name { get; set; }
            public Action setFault { get; set; }
            public Action fixFault { get; set; }
            public bool selected { get; set; } = false;
            public bool faulted { get; set; } = false;
            // Added 29/09/17 to identify fault owner
            public object owner { get; set; }

            public FaultItem(int i, string s, string g, string n, Action sF, Action fF, object o)
            {
                id = i;
                section = s;
                group = g;
                name = n;
                setFault = sF;
                fixFault = fF;
                selected = false;
                owner = o;
                FaultItems.Add(id, this);
            }
        }
        public static int createMainFaults()
        {
            int idIndex = 1;
            string G;
            string g;
            #region MCC
            G = "MCC";
            // TODO: for quick debugging hide MCC
            foreach (MCCCan can in Program.FindLogicalChildren<MCCCan>(mainWindow.mccTabControl))
            {
                string name = can.Name;
                g = can.Name + " - " + can.Line1.Content + " " + can.Line2.Content;
                // circuit breaker
                new FaultItem(idIndex++, G, g, "MCCB Fault", new Action(() => can.CBTripped = true), new Action(() => can.CBTripped = false), can);
                //new FaultItem(idIndex++, G, g, "Contactor Fault", new Action(() => can.canContactor.isFaulty = true), new Action(() => can.canContactor.isFaulty = false), can.canContactor);
                new FaultItem(idIndex++, G, g, "Contactor Stuck", new Action(() => can.canContactor.isStuck = true), new Action(() => can.canContactor.isStuck = false), can.canContactor);
                new FaultItem(idIndex++, G, g, "Contactor Coil Open Circuit", new Action(() => can.canContactor.coil.isOpenCircuit = true), new Action(() => can.canContactor.coil.isOpenCircuit = false), can.canContactor);
                new FaultItem(idIndex++, G, g, "Contactor Coil Short Circuit", new Action(() => can.canContactor.coil.isShortCircuit = true), new Action(() => can.canContactor.coil.isShortCircuit = false), can.canContactor);
                new FaultItem(idIndex++, G, g, "Contactor Tripped", new Action(() => can.canContactor.isTripped = true), new Action(() => can.canContactor.isTripped = false), can.canContactor);
                foreach (SwitchContact s in can.canContactor.mainContacts)
                {
                    if (s.location == "CAN")
                    {
                        new FaultItem(idIndex++, G, g, "Contactor Main Contact " + s.nettag + " Faulty", new Action(() => s.element.isFaulty = true), new Action(() => s.element.isFaulty = false), s.element);
                    }
                }
                foreach (SwitchContact s in can.canContactor.overloadContacts)
                {
                    if (s.location == "CAN")
                    {
                        new FaultItem(idIndex++, G, g, "Contactor Overload Contact " + s.nettag + " Faulty", new Action(() => s.element.isFaulty = true), new Action(() => s.element.isFaulty = false), s.element);
                    }
                }

                // fuses
                NetElement f1 = can.simCan.getElementByLocationAndNettag("CAN", "RPFS1");
                Resistor rf1 = f1.simElement as Resistor;
                new FaultItem(idIndex++, G, g, "PFS1 Blown", new Action(() => rf1.isOpenCircuit = true), new Action(() => rf1.isOpenCircuit = false), rf1);
                NetElement f2 = can.simCan.getElementByLocationAndNettag("CAN", "RPFS2");
                Resistor rf2 = f2.simElement as Resistor;
                new FaultItem(idIndex++, G, g, "PFS2 Blown", new Action(() => rf2.isOpenCircuit = true), new Action(() => rf2.isOpenCircuit = false), rf2);

                // transformer
                NetElement t1 = can.simCan.getElementByLocationAndNettag("CAN", "RT1A");
                Resistor rt1 = t1.simElement as Resistor;
                new FaultItem(idIndex++, G, g, "T1 Faulty", new Action(() => rt1.isOpenCircuit = true), new Action(() => rt1.isOpenCircuit = false), rt1);

                // local/remote switch

                {
                    NetElement s1 = can.simCan.getElementByLocationAndNettag("CAN", "S1-1");
                    SwitchSPST s = s1.simElement as SwitchSPST;
                    new FaultItem(idIndex++, G, g, "Control Switch Local Contact Faulty", new Action(() => s.isFaulty = true), new Action(() => s.isFaulty = false), s);
                }
                {
                    NetElement s1 = can.simCan.getElementByLocationAndNettag("CAN", "S1-2");
                    SwitchSPST s = s1.simElement as SwitchSPST;
                    new FaultItem(idIndex++, G, g, "Control Switch Remote Contact Faulty", new Action(() => s.isFaulty = true), new Action(() => s.isFaulty = false), s);
                }

                // local start
                {
                    NetElement s1 = can.simCan.getElementByLocationAndNettag("CAN", "SPB2");
                    SwitchSPST s = s1.simElement as SwitchSPST;
                    new FaultItem(idIndex++, G, g, "Local Start PB Faulty", new Action(() => s.isFaulty = true), new Action(() => s.isFaulty = false), s);
                }

                // local stop
                {
                    NetElement s1 = can.simCan.getElementByLocationAndNettag("CAN", "SPB1");
                    SwitchSPST s = s1.simElement as SwitchSPST;
                    new FaultItem(idIndex++, G, g, "Local Stop PB Faulty", new Action(() => s.isFaulty = true), new Action(() => s.isFaulty = false), s);
                }

                // remote start
                {
                    NetElement s1 = can.simCan.getElementByLocationAndNettag("CAN", "SPB21");
                    SwitchSPST s = s1.simElement as SwitchSPST;
                    new FaultItem(idIndex++, G, g, "Remote Start PB Faulty", new Action(() => s.isFaulty = true), new Action(() => s.isFaulty = false), s);
                }

                // remote stop
                {
                    NetElement s1 = can.simCan.getElementByLocationAndNettag("CAN", "SPB11");
                    SwitchSPST s = s1.simElement as SwitchSPST;
                    new FaultItem(idIndex++, G, g, "Remote Stop PB Faulty", new Action(() => s.isFaulty = true), new Action(() => s.isFaulty = false), s);
                }

                // remote lamp
                {
                    NetElement s1 = can.simCan.getElementByLocationAndNettag("CAN", "L11");
                    Resistor s = s1.simElement as Resistor;
                    new FaultItem(idIndex++, G, g, "Remote Lamp Blown", new Action(() => s.isOpenCircuit = true), new Action(() => s.isOpenCircuit = false), s);
                }


                // external voltage sources
                NetElement v2 = can.simCan.getElementByLocationAndNettag("CAN", "VDC-LCR");
                VoltageInput vp2 = v2.simElement as VoltageInput;
                new FaultItem(idIndex++, G, g, "LCR 24VDC Supply Faulty", new Action(() => vp2.isFaulty = true), new Action(() => vp2.isFaulty = false), vp2);

                NetElement v1 = can.simCan.getElementByLocationAndNettag("CAN", "VDC-MCR");
                VoltageInput vp1 = v1.simElement as VoltageInput;
                new FaultItem(idIndex++, G, g, "MCR 24VDC Supply Faulty", new Action(() => vp1.isFaulty = true), new Action(() => vp1.isFaulty = false), vp1);
            }
            #endregion
            return idIndex;
        }
        public static Dictionary<NetNode, Resistor> GroundingResistors = new Dictionary<NetNode, Resistor>();
        public static void addGroundingResistor(SimCircuit sim, NetNode term, bool grounded)
        {
            if (grounded)
            {
                Resistor r;
                if (GroundingResistors.TryGetValue(term, out r))
                {
                    r.resistance = 1;
                }
                else
                {
                    r = sim.sim.Create<Resistor>(1);
                    Circuit.Lead l1A = getConnection(term.parent, term.index);
                    Circuit.Lead l1B = r.leadIn;
                    sim.sim.Connect(l1A, l1B);

                    Circuit.Lead l2A = getConnection(sim.GND, 0);
                    Circuit.Lead l2B = r.leadOut;
                    sim.sim.Connect(l2A, l2B);
                    GroundingResistors.Add(term, r);
                }
            }
            else
            {
                Resistor r;
                if (GroundingResistors.TryGetValue(term, out r))
                {
                    r.resistance = 10E6;
                }
            }
        }
        public static IEnumerable<T> FindLogicalChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                foreach (object rawChild in LogicalTreeHelper.GetChildren(depObj))
                {
                    if (rawChild is DependencyObject)
                    {
                        DependencyObject child = (DependencyObject)rawChild;
                        if (child is T)
                        {
                            yield return (T)child;
                        }

                        foreach (T childOfChild in FindLogicalChildren<T>(child))
                        {
                            yield return childOfChild;
                        }
                    }
                }
            }
        }
        public static MainWindow getMainWindow()
        {
            foreach (Window w in System.Windows.Application.Current.Windows)
            {
                if (w.GetType() == typeof(MainWindow))
                {
                    return ((MainWindow)w);
                }
            }
            return null;
        }
        public class ContactChange
        {
            public SwitchContact contact { get; set; }
            public bool newStatus { get; set; }

            public ContactChange(SwitchContact c, bool n)
            {
                contact = c;
                newStatus = n;
            }

        }
        public static event EventHandler TickComplete;
        public delegate void TickCompleteEventHandler(EventArgs e);
        public static void OnTickComplete(EventArgs e)
        {
            EventHandler handler = TickComplete;
            if (handler != null)
            {
                handler(null, e);
            }
        }
        public static bool analyzeLock { get; set; }
        public static MediaPlayer switchClickPlayer = new MediaPlayer();
        public static MediaPlayer microSwitchClickPlayer = new MediaPlayer();
        public static MediaPlayer contactorClickPlayer = new MediaPlayer();
        public static MediaPlayer breakerClickPlayer = new MediaPlayer();
        public static void switchClick()
        {
            if (!soundMute)
            {
                switchClickPlayer.Position = new TimeSpan(0, 0, 0);
                switchClickPlayer.Play();
            }
        }
        public static void microSwitchClick()
        {
            if (!soundMute)
            {
                microSwitchClickPlayer.Position = new TimeSpan(0, 0, 0);
                microSwitchClickPlayer.Play();
            }
        }
        public static void contactorClick()
        {
            if (!soundMute)
            {
                contactorClickPlayer.Volume = 1;
                contactorClickPlayer.Position = new TimeSpan(0, 0, 0);
                contactorClickPlayer.Play();
            }
        }
        public static void breakerClick()
        {
            if (!soundMute)
            {
                breakerClickPlayer.Volume = 1.0;
                breakerClickPlayer.Position = new TimeSpan(0, 0, 0);
                breakerClickPlayer.Play();
            }
        }
        public class KlaxonHandler
        {
            public MediaPlayer klaxonPlayer1 = new MediaPlayer();

            public Resistor klaxon { get; set; }
            public double onVoltage { get; set; }
            public string location { get; set; }
            public string tag { get; set; }
            private bool lastState { get; set; }
            private bool klaxonON { get; set; } = false;
            private int delayCount = 8;
            private int delayCounter { get; set; }

            private double klaxonVolume = 0.8;

            public KlaxonHandler(string l, string t, double v)
            {
                location = l;
                tag = t;
                onVoltage = v;
                klaxonPlayer1.MediaEnded += Media_Ended;
                klaxonPlayer1.MediaEnded += Media_Ended;
                lastState = false;
                delayCounter = delayCount;

                klaxonPlayer1.Open(new Uri(@"Resources\siren.mp3", UriKind.Relative));

                NetElement thisElement = null;
                if (simMain.elements.TryGetValue(location + "." + tag, out thisElement))
                {
                    klaxon = thisElement.simElement as Resistor;
                }

                Program.OnSoundMuteChange += SoundMuteHandler;
                Program.TickComplete += TickHandler;
            }

            public void klaxonStart()
            {
                double v = klaxonVolume;
                if (Program.soundMute) v = 0;
                klaxonPlayer1.Volume = v;
                klaxonPlayer1.Position = new TimeSpan(0, 0, 0);
                klaxonPlayer1.Play();
            }
            public void klaxonStop()
            {
                klaxonPlayer1.Position = new TimeSpan(0, 0, 0);
                klaxonPlayer1.Stop();
            }

            public void SoundMuteHandler(object sender, EventArgs e)
            {
                double v = klaxonVolume;
                if (Program.soundMute) v = 0;
                klaxonPlayer1.Volume = v;
            }
            private void Media_Ended(object sender, EventArgs e)
            {
                MediaPlayer m = sender as MediaPlayer;
                m.Position = new TimeSpan(0, 0, 0);
                if (klaxonON) m.Play();
            }


            public void TickHandler(object sender, EventArgs e)
            {
                if (klaxon != null)
                {
                    if ((Math.Abs(klaxon.getVoltageDelta()) > (onVoltage * 0.8)) && (!klaxon.isOpenCircuit) && (!klaxon.isOpenCircuit))
                    {
                        klaxonON = true;
                    }
                    else
                    {
                        klaxonON = false;
                    }
                }

                if (klaxonON != lastState)
                {
                    if (klaxonON)
                    {
                        delayCounter--;
                        if (delayCounter == 0)
                        {
                            klaxonStart();
                            lastState = klaxonON;
                        }
                    }
                    else
                    {
                        klaxonStop();
                        delayCounter = delayCount;
                        lastState = klaxonON;
                    }

                }
            }
        }
        public class RelayHandler
        {
            public SimCircuit simcircuit { get; set; }
            public List<Program.SwitchContact> auxContacts { get; set; }
            public Resistor coilElement { get; set; }
            public double coilVoltage { get; set; }

            private bool _isFaulty;
            public bool isFaulty
            {
                get
                {
                    return _isFaulty;
                }
                set
                {
                    if (_isStuck) isStuck = false;
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
                    if (_isFaulty) isFaulty = false;
                    _isStuck = value;
                }
            }
            private bool? _isClosed;
            public bool? isClosed
            {
                get
                {
                    return _isClosed;
                }
                set
                {
                    bool? newState = value;
                    if (_isFaulty)
                    {
                        newState = false;
                    }

                    if (_isStuck && (_isClosed == true))
                    {
                        newState = true;
                    }
                    if ((newState == true) && isFlasher)
                    {
                        if (flashCounter == 0)
                        {
                            flashState = !flashState;
                            flashCounter = flashCount;
                        }
                        flashCounter--;
                        _isClosed = newState;
                        setAuxContacts(flashState);
                    }
                    else
                    {
                        if (_isClosed != newState)
                        {
                            _isClosed = newState;
                            setAuxContacts(_isClosed == true);
                        }
                    }

                }
            }
            public string location { get; set; }
            public string tag { get; set; }
            private bool isFlasher { get; set; }
            private int flashCounter { get; set; } = 0;
            private int flashCount { get; set; } = 2;
            private bool flashState { get; set; }

            public RelayHandler(SimCircuit sc, string l, string t, double v, bool flasher = false)
            {
                simcircuit = sc;
                isFlasher = flasher;
                location = l;
                tag = t;
                coilVoltage = v;
                auxContacts = new List<Program.SwitchContact>();
                _isFaulty = false;
                _isStuck = false;

                auxContacts.Add(new SwitchContact(simcircuit, location, "S" + tag + "-7", false));
                auxContacts.Add(new SwitchContact(simcircuit, location, "S" + tag + "-8", false));
                auxContacts.Add(new SwitchContact(simcircuit, location, "S" + tag + "-9", false));

                auxContacts.Add(new SwitchContact(simcircuit, location, "S" + tag + "-7C", true));
                auxContacts.Add(new SwitchContact(simcircuit, location, "S" + tag + "-8C", true));
                auxContacts.Add(new SwitchContact(simcircuit, location, "S" + tag + "-9C", true));

                string nettag = tag;
                foreach (KeyValuePair<string, NetElement> el in simcircuit.elements)
                {
                    if (el.Value.location == location)
                    {
                        if (el.Value.nettag == nettag)
                        {
                            coilElement = el.Value.simElement as Resistor;
                            break;
                        }
                    }
                }
                simcircuit.RelayHandlers.Add(this);
                simcircuit.TickComplete += tickCompleteHandler;
            }

            public void tickCompleteHandler(object sender, EventArgs e)
            {
                if (isFaulty)
                {
                    isClosed = false;
                }
                else
                {
                    if (coilElement != null)
                    {
                        if (Program.isCoilEnergised(coilElement, coilVoltage))
                        {
                            isClosed = true;
                        }
                        else
                        {
                            isClosed = false;
                        }
                    }
                    else
                    {
                        isClosed = false;
                    }
                }
            }
            public void setAuxContacts(bool closed)
            {
                foreach (Program.SwitchContact c in auxContacts)
                {
                    simcircuit.contactPos(c, closed);
                }
            }

        }

        public class ImageButtonHandler
        {
            private Image buttonImage { get; set; }
            private BitmapImage imageBase { get; set; }
            private BitmapImage imageOver { get; set; }
            private BitmapImage imageSelected { get; set; }
            public List<ImageButtonHandler> buttonGroup { get; set; }
            private UserControl showControl { get; set; }
            private bool _isSelected;
            public bool isSelected
            {
                get
                {
                    return _isSelected;
                }
                set
                {
                    if (value)
                    {
                        if (buttonGroup != null)
                        {
                            foreach (ImageButtonHandler b in buttonGroup)
                            {
                                if (!b.Equals(this))
                                {
                                    b.isSelected = false;
                                }
                            }
                        }
                        buttonImage.Source = imageSelected;
                        showControl.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        buttonImage.Source = imageBase;
                        showControl.Visibility = Visibility.Hidden;
                    }
                    _isSelected = value;
                }
            }

            public ImageButtonHandler(Image img, UserControl c, string b, string o, string s)
            {
                buttonImage = img;
                showControl = c;
                imageBase = new BitmapImage(new Uri(b));
                imageOver = new BitmapImage(new Uri(o));
                imageSelected = new BitmapImage(new Uri(s));
                isSelected = false;

                buttonImage.MouseEnter += MouseEnter;
                buttonImage.MouseLeave += MouseLeave;
                buttonImage.MouseLeftButtonDown += MouseLeftButtonDown;
            }

            private void MouseEnter(object sender, MouseEventArgs e)
            {
                if (!_isSelected)
                {
                    buttonImage.Source = imageOver;
                }
            }

            private void MouseLeave(object sender, MouseEventArgs e)
            {
                if (!_isSelected)
                {
                    buttonImage.Source = imageBase;
                }
            }

            private void MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
            {
                if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                {
                    isSelected = true;
                    e.Handled = true;
                }
                else
                {
                    e.Handled = false;
                }

            }
        }
        public class LampHandler
        {
            private SimCircuit simcircuit { get; set; }
            public Image lampImage { get; set; }
            public Resistor lamp { get; set; }
            BitmapImage onImage { get; set; }
            BitmapImage offImage { get; set; }
            private string onResource { get; set; }
            private string offResource { get; set; }
            private string location { get; set; }
            private string tag { get; set; }
            private bool lastState { get; set; }
            private double onVoltage { get; set; }

            public LampHandler(SimCircuit sc, Image img, string l, string t, double v, string offUri, string onUri)
            {
                simcircuit = sc;
                lampImage = img;
                location = l;
                tag = t;
                onVoltage = v;
                lastState = false;
                onImage = new BitmapImage(new Uri(onUri, UriKind.Relative));
                offImage = new BitmapImage(new Uri(offUri, UriKind.Relative));
                lamp = null;

                NetElement thisElement = null;
                if (simcircuit.elements.TryGetValue(location + "." + tag, out thisElement))
                {
                    lamp = thisElement.simElement as Resistor;
                }

                simcircuit.TickComplete += TickHandler;
            }

            public void TickHandler(object sender, EventArgs e)
            {
                bool newState = false;
                if (lamp != null)
                {
                    if ((Math.Abs(lamp.getVoltageDelta()) > (onVoltage * 0.8)) && (!lamp.isOpenCircuit) && (!lamp.isOpenCircuit))
                    {
                        newState = true;
                    }
                }
                else
                {
                    if (0 > onVoltage)
                    {
                        newState = true;
                    }
                }

                if (newState != lastState)
                {
                    if (newState)
                    {
                        lampImage.Source = onImage;
                    }
                    else
                    {
                        lampImage.Source = offImage;
                    }
                    lastState = newState;
                }
            }
        }
        public class SwitchContact
        {
            SimCircuit simcircuit { get; set; }
            public string location { get; set; }
            public string nettag { get; set; }
            public bool nc { get; set; }
            public SwitchSPST element;

            public SwitchContact(SimCircuit sc, String l, string t, bool c)
            {
                simcircuit = sc;
                location = l;
                nettag = t;
                nc = c;

                foreach (KeyValuePair<string, NetElement> el in simcircuit.elements)
                {
                    if (el.Value.location == location)
                    {
                        if (el.Value.nettag == nettag)
                        {
                            element = (SwitchSPST)el.Value.simElement;
                            break;
                        }
                    }

                }
            }

            public SwitchContact(SimCircuit sc, NetElement el, bool c)
            {
                simcircuit = sc;
                location = el.location;
                nettag = el.nettag;
                nc = c;

                element = (SwitchSPST)el.simElement;
            }
        }
        public class SwitchContacts
        {
            public List<SwitchContact> leftContacts { get; set; }
            public List<SwitchContact> centreContacts { get; set; }
            public List<SwitchContact> rightContacts { get; set; }

            public SwitchContacts()
            {
                leftContacts = new List<SwitchContact>();
                centreContacts = new List<SwitchContact>();
                rightContacts = new List<SwitchContact>();
            }
        }
        public class AssignmentSwitchContacts
        {
            public List<SwitchContact>[] contacts { get; set; }

            public AssignmentSwitchContacts()
            {
                contacts = new List<SwitchContact>[13];

                for (int i = contacts.GetLowerBound(0); i <= contacts.GetUpperBound(0); i++)
                {
                    contacts[i] = new List<SwitchContact>();
                }
            }
        }
        public class PushbuttonHandler
        {
            public SimCircuit simcircuit { get; set; }
            public List<SwitchContact> switchContacts { get; set; }
            public Image pushButton { get; set; }
            public BitmapImage imageUP { get; set; }
            public BitmapImage imageDN { get; set; }
            private string location { get; set; }
            private string tag { get; set; }
            private bool currentState { get; set; } = false;
            private bool nextState { get; set; } = false;
            public PushbuttonHandler(SimCircuit sc, Image img, string l, string t, string up, string down)
            {
                simcircuit = sc;
                img.MouseLeftButtonDown += OnMouseDown;
                pushButton = img;
                imageDN = new BitmapImage(new Uri(down, UriKind.Relative));
                imageUP = new BitmapImage(new Uri(up, UriKind.Relative));
                location = l;
                tag = t;
                switchContacts = new List<SwitchContact>();
                sc.TimerComplete += syncPB;
            }

            void OnMouseDown(object sender, MouseEventArgs e)
            {
                if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                {
                    nextState = !currentState;
                    e.Handled = true;
                }
            }

            public void setSwitchContacts(bool on)
            {
                if (on)
                {
                    foreach (SwitchContact c in switchContacts)
                    {
                        simcircuit.contactPos(c, true);
                    }
                }
                else
                {
                    foreach (SwitchContact c in switchContacts)
                    {
                        simcircuit.contactPos(c, false);
                    }
                }
            }

            public void syncPB(object sender, EventArgs e)
            {
                if (currentState != nextState)
                {
                    if (nextState)
                    {
                        pushButton.Source = imageDN;
                        setSwitchContacts(true);
                    }
                    else
                    {
                        pushButton.Source = imageUP;
                        setSwitchContacts(false);
                    }
                    currentState = nextState;
                }

            }

        }
        public class SwitchAssignmentHandler
        {
            public SimCircuit simcircuit { get; set; }
            public Image img { get; set; }
            private string location { get; set; }
            private string tag { get; set; }
            private double lastAngle { get; set; }
            private int _position;
            private AssignmentSwitchContacts switchContacts { get; set; }
            private int lastEventTime { get; set; } = 0;
            public int position
            {
                get
                {
                    return _position;
                }
                set
                {
                    _position = (value < -4 ? -4 : value);
                    _position = (value > 4 ? 4 : _position);

                }
            }

            public SwitchAssignmentHandler(SimCircuit sc, Image i, string l, string t, AssignmentSwitchContacts c)
            {
                simcircuit = sc;
                i.PreviewMouseWheel += OnPreviewMouseWheel;
                img = i;
                location = l;
                tag = t;
                position = 0;
                switchContacts = c;
                RotateTransform rotateTransform = new RotateTransform(0);
                img.RenderTransform = rotateTransform;
                setSwitchContacts();
            }

            void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
            {
                if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                {
                    if ((e.Timestamp - lastEventTime) > 500)
                    {
                        if (e.Delta > 0)
                        {
                            position += 1;
                        }
                        if (e.Delta < 0)
                        {
                            position -= 1;
                        }
                        double angle = (30 * position);
                        if (angle != lastAngle)
                        {
                            RotateTransform rotateTransform = new RotateTransform(angle);
                            img.RenderTransform = rotateTransform;
                            setSwitchContacts();
                            lastAngle = angle;
                            switchClick();
                        }
                    }
                    lastEventTime = e.Timestamp;
                    e.Handled = true;
                }
            }
            public void setSwitchContacts()
            {
                int[] positions = { 8, 9, 10, 11, 12, 1, 2, 3, 4 };
                int p = positions[position + 4];

                // open all
                foreach (int i in positions)
                {
                    foreach (SwitchContact c in switchContacts.contacts[i])
                    {
                        simcircuit.contactPos(c, false);
                    }
                }

                foreach (SwitchContact c in switchContacts.contacts[p])
                {
                    simcircuit.contactPos(c, true);
                }
            }
        }
        public class SwitchThreePositionHandler
        {
            public SimCircuit simcircuit { get; set; }
            public Image img { get; set; }
            private string location { get; set; }
            private string tag { get; set; }
            private double lastAngle { get; set; }
            private double _position;
            private int lastEventTime { get; set; } = 0;
            private SwitchContacts switchContacts { get; set; }
            public double position
            {
                get
                {
                    return _position;
                }
                set
                {
                    _position = (value < -1 ? -1 : value);
                    _position = (value > 1 ? 1 : _position);

                }
            }

            public SwitchThreePositionHandler(SimCircuit sc, Image i, string l, string t, SwitchContacts c)
            {
                simcircuit = sc;
                i.PreviewMouseWheel += OnPreviewMouseWheel;
                img = i;
                location = l;
                tag = t;
                position = 0;
                switchContacts = c;
                RotateTransform rotateTransform = new RotateTransform(0);
                img.RenderTransform = rotateTransform;
                setSwitchContacts();
            }

            void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
            {
                if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                {
                    if ((e.Timestamp - lastEventTime) > 500)
                    {
                        if (e.Delta > 0)
                        {
                            position += 1;
                        }
                        if (e.Delta < 0)
                        {
                            position -= 1;
                        }
                        double angle = (45 * position);
                        if (angle != lastAngle)
                        {
                            RotateTransform rotateTransform = new RotateTransform(angle);
                            img.RenderTransform = rotateTransform;
                            setSwitchContacts();
                            lastAngle = angle;
                            switchClick();
                        }
                    }
                    lastEventTime = e.Timestamp;
                    e.Handled = true;
                }
            }
            public void setPosition(int pos)
            {
                position = pos;
                double angle = (45 * position);
                RotateTransform rotateTransform = new RotateTransform(angle);
                img.RenderTransform = rotateTransform;
                setSwitchContacts();
                lastAngle = angle;
                switchClick();
            }
            public void setSwitchContacts()
            {
                if (position == -1)
                {
                    foreach (SwitchContact s in switchContacts.rightContacts)
                    {
                        simcircuit.contactPos(s, false);
                    }
                    foreach (SwitchContact s in switchContacts.centreContacts)
                    {
                        simcircuit.contactPos(s, false);
                    }
                    foreach (SwitchContact s in switchContacts.leftContacts)
                    {
                        simcircuit.contactPos(s, true);
                    }
                }
                else if (position == 0)
                {
                    foreach (SwitchContact s in switchContacts.rightContacts)
                    {
                        simcircuit.contactPos(s, false);
                    }
                    foreach (SwitchContact s in switchContacts.leftContacts)
                    {
                        simcircuit.contactPos(s, false);
                    }
                    foreach (SwitchContact s in switchContacts.centreContacts)
                    {
                        simcircuit.contactPos(s, false);
                    }
                }
                else
                {
                    foreach (SwitchContact s in switchContacts.leftContacts)
                    {
                        simcircuit.contactPos(s, false);
                    }
                    foreach (SwitchContact s in switchContacts.centreContacts)
                    {
                        simcircuit.contactPos(s, false);
                    }
                    foreach (SwitchContact s in switchContacts.rightContacts)
                    {
                        simcircuit.contactPos(s, true);
                    }
                }
            }
        }
        public class SwitchTwoPositionHandler
        {
            public SimCircuit simcircuit { get; set; }
            public Image img { get; set; }
            private string location { get; set; }
            private string tag { get; set; }
            private double lastAngle { get; set; }
            private double _position;
            int lastEventTime { get; set; } = 0;
            private SwitchContacts switchContacts { get; set; }
            public double position
            {
                get
                {
                    return _position;
                }
                set
                {
                    _position = (value < -1 ? -1 : value);
                    _position = (value > 1 ? 1 : _position);

                }
            }

            public SwitchTwoPositionHandler(SimCircuit sc, Image i, string l, string t, SwitchContacts c)
            {
                simcircuit = sc;
                i.PreviewMouseWheel += OnPreviewMouseWheel;
                img = i;
                location = l;
                tag = t;
                position = -1;
                switchContacts = c;
                RotateTransform rotateTransform = new RotateTransform(-45);
                img.RenderTransform = rotateTransform;
                setSwitchContacts();
            }

            public void setSwitchContacts()
            {
                if (position == -1)
                {
                    foreach (SwitchContact s in switchContacts.rightContacts)
                    {
                        simcircuit.contactPos(s, false);
                    }
                    foreach (SwitchContact s in switchContacts.leftContacts)
                    {
                        simcircuit.contactPos(s, true);
                    }
                }
                else
                {
                    foreach (SwitchContact s in switchContacts.leftContacts)
                    {
                        simcircuit.contactPos(s, false);
                    }
                    foreach (SwitchContact s in switchContacts.rightContacts)
                    {
                        simcircuit.contactPos(s, true);
                    }
                }
            }

            void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
            {
                if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                {
                    if ((e.Timestamp - lastEventTime) > 500)
                    {
                        if (e.Delta > 0)
                        {
                            position = 1;
                        }
                        if (e.Delta < 0)
                        {
                            position = -1;
                        }
                        double angle = (45 * position);
                        if (angle != lastAngle)
                        {
                            RotateTransform rotateTransform = new RotateTransform(angle);
                            img.RenderTransform = rotateTransform;
                            setSwitchContacts();
                            lastAngle = angle;
                            switchClick();
                        }
                    }
                    lastEventTime = e.Timestamp;
                    e.Handled = true;
                }
            }
        }
        public static void meterIn(Grid grd)
        {
            ThicknessAnimation ta = new ThicknessAnimation();
            ta.From = new Thickness(0, 0, 40, grd.Margin.Bottom);
            ta.To = new Thickness(0, 0, 40, 4);
            ta.Duration = new Duration(TimeSpan.FromSeconds(1));
            grd.BeginAnimation(Grid.MarginProperty, ta);
        }
        public static void meterOut(Grid grd)
        {
            if (Program.AutoHideMeter)
            {
                ThicknessAnimation ta = new ThicknessAnimation();
                ta.From = new Thickness(0, 0, 40, grd.Margin.Bottom);
                ta.To = new Thickness(0, 0, 40, -(grd.ActualHeight + 20));
                ta.Duration = new Duration(TimeSpan.FromSeconds(1));
                grd.BeginAnimation(Grid.MarginProperty, ta);
            }
        }
        public static void setHilite(FrameworkElement r, bool on, Point l, Size s)
        {
            r.Margin = new Thickness(l.X, l.Y, 0, 0);
            r.Height = s.Height;
            r.Width = s.Width;
            r.Visibility = (on ? Visibility.Visible : Visibility.Hidden);
        }
        public class ScrollZoomHandler
        {
            public ScrollViewer scrollViewer { get; set; }
            public Slider slider { get; set; }
            public Grid grid { get; set; }
            public ScaleTransform scaleTransform { get; set; }

            public Point? lastCenterPositionOnTarget { get; set; }
            public Point? lastMousePositionOnTarget { get; set; }
            public Point? lastDragPoint { get; set; }

            public ScrollZoomHandler(ScrollViewer sv, Slider sl, Grid g, ScaleTransform st, double zoom)
            {
                slider = sl;
                slider.Value = zoom;
                scrollViewer = sv;
                grid = g;
                scaleTransform = st;
                scaleTransform.ScaleX = zoom;
                scaleTransform.ScaleY = zoom;

                slider.ValueChanged += OnSliderValueChanged;
                scrollViewer.ScrollChanged += OnScrollViewerScrollChanged;
                scrollViewer.MouseLeftButtonUp += OnMouseLeftButtonUp;
                scrollViewer.PreviewMouseLeftButtonUp += OnMouseLeftButtonUp;
                scrollViewer.PreviewMouseWheel += OnPreviewMouseWheel;

                scrollViewer.PreviewMouseLeftButtonDown += OnMouseLeftButtonDown;
                scrollViewer.MouseMove += OnMouseMove;


            }
            void OnMouseMove(object sender, MouseEventArgs e)
            {

                if (lastDragPoint.HasValue)
                {
                    Point posNow = e.GetPosition(scrollViewer);

                    double dX = posNow.X - lastDragPoint.Value.X;
                    double dY = posNow.Y - lastDragPoint.Value.Y;

                    lastDragPoint = posNow;

                    scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset - dX);
                    scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - dY);
                }
            }
            // left key + shift key
            void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
            {
                if (!Keyboard.IsKeyDown(Key.LeftShift) &&
                    !Keyboard.IsKeyDown(Key.RightShift) &&
                    !Keyboard.IsKeyDown(Key.LeftCtrl) &&
                    !Keyboard.IsKeyDown(Key.RightCtrl))
                {
                    var mousePos = e.GetPosition(scrollViewer);

                    if (mousePos.X <= scrollViewer.ViewportWidth && mousePos.Y < scrollViewer.ViewportHeight) //make sure we still can use the scrollbars
                    {
                        scrollViewer.Cursor = Cursors.SizeAll;
                        lastDragPoint = mousePos;
                        Mouse.Capture(scrollViewer);
                    }
                }
                else
                {
                    e.Handled = false;
                    lastDragPoint = null;
                }
            }
            void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
            {
                if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                {
                    e.Handled = false;
                    return;
                }
                var mousePos = e.GetPosition(scrollViewer);
                lastMousePositionOnTarget = Mouse.GetPosition(grid);

                if (e.Delta > 0)
                {
                    slider.Value += 0.25;
                }
                if (e.Delta < 0)
                {
                    slider.Value -= 0.25;
                }

                e.Handled = true;
            }
            void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
            {
                scrollViewer.Cursor = Cursors.Arrow;
                scrollViewer.ReleaseMouseCapture();
                lastDragPoint = null;
            }
            void OnSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
            {
                scaleTransform.ScaleX = e.NewValue;
                scaleTransform.ScaleY = e.NewValue;

                var centerOfViewport = new Point(scrollViewer.ViewportWidth / 2,
                                                 scrollViewer.ViewportHeight / 2);
                lastCenterPositionOnTarget = scrollViewer.TranslatePoint(centerOfViewport, grid);
            }
            void OnScrollViewerScrollChanged(object sender, ScrollChangedEventArgs e)
            {
                if (e.ExtentHeightChange != 0 || e.ExtentWidthChange != 0)
                {
                    Point? targetBefore = null;
                    Point? targetNow = null;

                    if (!lastMousePositionOnTarget.HasValue)
                    {
                        if (lastCenterPositionOnTarget.HasValue)
                        {
                            var centerOfViewport = new Point(scrollViewer.ViewportWidth / 2,
                                                             scrollViewer.ViewportHeight / 2);
                            Point centerOfTargetNow =
                                  scrollViewer.TranslatePoint(centerOfViewport, grid);

                            targetBefore = lastCenterPositionOnTarget;
                            targetNow = centerOfTargetNow;
                        }
                    }
                    else
                    {
                        targetBefore = lastMousePositionOnTarget;
                        targetNow = Mouse.GetPosition(grid);

                        lastMousePositionOnTarget = null;
                    }

                    if (targetBefore.HasValue)
                    {
                        double dXInTargetPixels = targetNow.Value.X - targetBefore.Value.X;
                        double dYInTargetPixels = targetNow.Value.Y - targetBefore.Value.Y;

                        double multiplicatorX = e.ExtentWidth / grid.Width;
                        double multiplicatorY = e.ExtentHeight / grid.Height;

                        double newOffsetX = scrollViewer.HorizontalOffset -
                                            dXInTargetPixels * multiplicatorX;
                        double newOffsetY = scrollViewer.VerticalOffset -
                                            dYInTargetPixels * multiplicatorY;

                        if (double.IsNaN(newOffsetX) || double.IsNaN(newOffsetY))
                        {
                            return;
                        }

                        scrollViewer.ScrollToHorizontalOffset(newOffsetX);
                        scrollViewer.ScrollToVerticalOffset(newOffsetY);
                    }
                }
            }


        }
        // circuits
        public static SimCircuit simMain { get; set; }
        public static SimCircuit simMP1 { get; set; }
        public static SimCircuit simMP2 { get; set; }
        public static SimCircuit simDW { get; set; }
        public static SimCircuit simRT { get; set; }
        public static double Round(double val, int places)
        {
            if (places < 0) throw new ArgumentException("places");
            return Math.Round(val - (0.5 / Math.Pow(10, places)), places);
        }
        // sets positions of all related pots
        public static void refPos(string location, string tag, double pos)
        {
            Match MatchResults = null;
            Regex RegexObj = new Regex("\\A(POT\\d*)\\z");
            string searchFor = "";
            try
            {
                MatchResults = RegexObj.Match(tag);
                if (MatchResults.Success)
                {
                    searchFor = MatchResults.Groups[0].Value;
                }
                else
                {
                    return;
                }
            }
            catch (ArgumentException ex)
            {
                Debug.Log(ex.Message);
                // Syntax error in the regular expression
            }

            RegexObj = new Regex("\\A" + searchFor + "([A-Z])\\z");
            try
            {
                foreach (KeyValuePair<string, NetElement> entry in simMain.elements)
                {
                    NetElement el = entry.Value;
                    if (el.location == location)
                    {

                        MatchResults = RegexObj.Match(el.tag);
                        if (MatchResults.Success)
                        {
                            var x = el.simElement.GetType();
                            if (el.simElement.GetType() == typeof(Potentiometer))
                            {
                                Potentiometer pot = (Potentiometer)el.simElement;
                                pot.position = pos;
                            }

                        }
                    }
                }
            }
            catch
            {
                return;
            }

        }
        public static Circuit.Lead getConnection(NetElement el, int n)
        {
            switch (el.type)
            {
                case 'P':
                    if (n == 0) return (((Potentiometer)el.simElement).leadOut);
                    else if (n == 1) return (((Potentiometer)el.simElement).leadIn);
                    else if (n == 2) return (((Potentiometer)el.simElement).leadVoltage);
                    break;
                case 'X':
                    if (n == 0) return (((Probe)el.simElement).leadIn);
                    break;
                case 'V':
                    if (n == 0) return (((VoltageInput)el.simElement).leadPos);
                    break;
                case 'R':
                    if (n == 0) return (((Resistor)el.simElement).leadIn);
                    else if (n == 1) return (((Resistor)el.simElement).leadOut);
                    break;
                case 'D':
                case 'Z':
                    if (n == 0) return (((DiodeElm)el.simElement).leadIn);          // anode
                    else if (n == 1) return (((DiodeElm)el.simElement).leadOut);
                    break;
                case 'S':
                    if (n == 0) return (((SwitchSPST)el.simElement).leadA);
                    else if (n == 1) return (((SwitchSPST)el.simElement).leadB);
                    break;
                case 'L':
                    if (n == 0) return (((Resistor)el.simElement).leadIn);
                    else if (n == 1) return (((Resistor)el.simElement).leadOut);
                    break;
                case 'G':
                    return (((Ground)el.simElement).leadIn);
            }
            return (null);
        }

        public static bool isCoilEnergised(Resistor coilElement, double coilVoltage)
        {
            if (coilElement.isOpenCircuit) return false;
            if (coilElement.isShortCircuit) return false;
            if (Math.Abs(coilElement.getVoltageDelta()) > (0.8 * coilVoltage)) return true;
            return (false);
        }
        public static MainWindow mainWindow { get; set; }
        private static System.Windows.Threading.DispatcherTimer dispatcherTimer { get; set; }
        private static App MainApplication { get; set; }
        public static bool InitialiseSim(string path = @"Resources\CANCON-1.CSV")
        {

            Program.soundMute = true;

            //simMain = new SharpCircuit.SimCircuit("MAIN");
            //simMain.ReadCircuit(path);

            // try
            // {

            string text = "Configuring main application...";
            //Debug.Log(text);
            splash.MessageText += text;
            mainWindow = new MainWindow();
            mainWindow.configureMW(Program.simMain);
            text = "Complete!";
            //Debug.Log(text);

            splash.MessageText += text + Environment.NewLine;
            CheckBox cb = mainWindow.Training_SoundMute_CB as CheckBox;
            Program.soundMute = (cb.IsChecked == true);
            soundMute = Program.soundMute;
            mainWindow.Show();


            text = "Starting background tasks...";
            //Debug.Log(text);
            splash.MessageText += text;

            Donate.CheckDonation();
            System.Windows.Threading.DispatcherTimer donateTimer = new System.Windows.Threading.DispatcherTimer(System.Windows.Threading.DispatcherPriority.Background);
            donateTimer.Tick += donateTimer_Tick;
            donateTimer.Interval = new TimeSpan(0, 0, Properties.Settings.Default.DonateSeconds);
            donateTimer.Start();

            dispatcherTimer = new System.Windows.Threading.DispatcherTimer(System.Windows.Threading.DispatcherPriority.Normal);
            dispatcherTimer.Tick += dispatcherTimer_Tick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 260);
            dispatcherTimer.Start();

            /*}
            catch (Exception e)
            {
                string text = "Circuit analysis failed " + e.Message;
                Debug.Log(text);
                splash.MessageText += text + Environment.NewLine;
                throw new Exception(text);
            }   */
            return (true);
        }
        public static SplashWindow splash { get; set; }
        [STAThread]
        static void Main(string[] args)
        {
            switchClickPlayer.Open(new Uri(@"Resources\thunk_switch.mp3", UriKind.Relative));
            microSwitchClickPlayer.Open(new Uri(@"Resources\ping_switch.mp3", UriKind.Relative));
            //contactorClickPlayer.Open(new Uri(@"Resources\heavy_throw_switch.mp3", UriKind.Relative));
            contactorClickPlayer.Open(new Uri(@"Resources\metal_door_close.mp3", UriKind.Relative));
            breakerClickPlayer.Open(new Uri(@"Resources\large_castle_metal_door_slammed_close.mp3", UriKind.Relative));

            MainApplication = new App();


            splash = new SplashWindow();
            MainApplication.Run(splash);
            //SplashWindow splash = new SplashWindow();
            //splash.ShowDialog();
            //MainWindow m = new MainWindow();
            //m.Show();
            //MainApplication = new App();
            //MainApplication.Run();
        }


        public static void Reload()
        {
            FaultItems = new Dictionary<int, FaultItem>();
            simMain.Reload();
        }
        static bool timerLock = false;
        private static void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            if (timerLock)
            {
                Debug.Log("Exiting re-entry");
                return;
            }
            timerLock = true;

            // do fault setting

            switch (settingFaultsStatus)
            {
                case settingFaults.SET:
                    foreach (KeyValuePair<int, Program.FaultItem> entry in Program.FaultItems)
                    {
                        FaultItem f = entry.Value;
                        if (!f.selected)
                        {
                            if (f.faulted) f.fixFault();
                            f.faulted = false;
                        }
                    }
                    foreach (KeyValuePair<int, Program.FaultItem> entry in Program.FaultItems)
                    {
                        FaultItem f = entry.Value;
                        if (f.selected)
                        {
                            if (!f.faulted) f.setFault();
                            f.faulted = true;
                        }
                    }
                    break;

                case settingFaults.APPLY:
                    foreach (KeyValuePair<int, Program.FaultItem> entry in Program.FaultItems)
                    {
                        FaultItem f = entry.Value;
                        if (f.selected)
                        {
                            if (!f.faulted) f.setFault();
                            f.faulted = true;
                        }
                    }
                    break;

                case settingFaults.CLEAR:
                    foreach (KeyValuePair<int, Program.FaultItem> entry in Program.FaultItems)
                    {
                        FaultItem f = entry.Value;
                        if (f.selected)
                        {
                            if (f.faulted) f.fixFault();
                            f.faulted = false;
                        }
                    }
                    break;

                case settingFaults.CLEARALL:
                    foreach (KeyValuePair<int, Program.FaultItem> entry in Program.FaultItems)
                    {
                        FaultItem f = entry.Value;
                        if (f.selected)
                        {
                            if (f.faulted) f.fixFault();
                            f.faulted = false;
                            f.selected = false;
                        }
                    }
                    break;

                default:
                    break;

            }
            settingFaultsStatus = settingFaults.DONE;

            OnTickComplete(new EventArgs());

            timerLock = false;
        }
        private static void demoTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                foreach (Window w in System.Windows.Application.Current.Windows)
                {
                    w.Close();
                    MessageBox.Show("DEMO has timed out!", "DEMO Timeout", MessageBoxButton.OK, MessageBoxImage.Stop);
                    Application.Current.Shutdown();
                }
            }
            catch
            {

            }
        }

        private static void donateTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                WindowCollection wc = Application.Current.Windows;

                foreach (Window wx in wc)
                {
                    //iterate through
                    if (wx.GetType() == typeof(DonateWindow))
                    {
                        return;
                    }
                }
                if (!Donate.CheckDonation())
                {
                    DonateWindow w = new DonateWindow();
                    w.ShowDialog();
                }
            }
            catch
            {

            }
        }

        public static string EncodeScriptText(string s)
        {
            string v = s.Replace(Environment.NewLine, "{{n}}");
            v = v.Replace("\t", "{{t}}");
            return (v);
        }

        public static string DecodeScriptText(string s)
        {
            string v = s.Replace("{{empty}}", "");
            v = v.Replace("{{n}}", Environment.NewLine);
            v = v.Replace("{{t}}","\t");
            return (v);
        }
    }
}
public static class Debug
{
    public static void Log(params object[] objs)
    {
        StringBuilder sb = new StringBuilder();
        foreach (object o in objs)
            sb.Append(o.ToString()).Append(" ");
        Console.WriteLine(sb.ToString());
    }
    public static void LogF(string format, params object[] objs)
    {
        Console.WriteLine(string.Format(format, objs));
    }

}