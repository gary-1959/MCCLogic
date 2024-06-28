using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Xml.Serialization;

namespace SharpCircuit
{
    public class SimCircuit
    {
        public NetElement GND { get; set; }
        public Circuit sim { get; set; }
        public string name { get; set;}
        public string path { get; set; }
        //public SplashWindow splash { get; set; }
        public Dictionary<string, NetElement> elements { get; set; }
        public Dictionary<Program.SwitchContact, Program.ContactChange> contactChange { get; set; }
        public event EventHandler TickComplete;
        public delegate void TickCompleteEventHandler(EventArgs e);
        public void OnTickComplete(EventArgs e)
        {
            EventHandler handler = TickComplete;
            if (handler != null)
            {
                handler(null, e);
            }
        }
        public event EventHandler TimerComplete;
        public void OnTimerComplete(EventArgs e)
        {
            EventHandler handler = TimerComplete;
            if (handler != null)
            {
                handler(null, e);
            }
        }

        public SimCircuit() : this("NONAME") { }
        public SimCircuit(string n)
        {
            sim = new Circuit();
            name = n;
            elements = new Dictionary<string, NetElement>();
            contactChange = new Dictionary<Program.SwitchContact, Program.ContactChange>();

            RelayHandlers = new List<Program.RelayHandler>();
            MCCContactors = new List<MCCContactor>();

            sim.AnalysisComplete += AnalysisCompleteHandler;
        }

        public bool analysisOK { get; set; }
        public void AnalysisCompleteHandler(object sender, EventArgs e)
        {
            analysisOK = sim.converged;
        }


        public void ReadCircuit(string path)
        {
            if (path == null) path = @"Resources\CANCON-1.CSV";

            // TODO: Exclude the following for debugging:
            string[] exclude = { };

            // create the circuit elements
            Ground g = sim.Create<Ground>();
            GND = new NetElement();
            GND.location = "*";
            GND.tag = "GND";
            GND.nettag = "GND";
            GND.name = "*GND";
            GND.type = 'G';
            GND.value = 0;
            GND.simElement = g;
            NetNode ngnd = new SharpCircuit.NetNode(GND, 0, "0", false, "*", "1");
            GND.nodes = new List<NetNode>();
            GND.nodes.Add(ngnd);
            elements.Add("GND", GND);

            string net;
            //string inputFile = @"C:\Users\Gary\Desktop\Logic Simulator\WIRECON.CSV";

            using (StreamReader f = new StreamReader(path))
            {
                while ((net = f.ReadLine()) != null)
                {
                    //Parse the input file
                    if (net[0] == ';') continue;

                    string[] words = net.Split(',');

                    // LOC, TAG, TERM, WIRE, NETTAG, VALUE, TERMDESC (LOCATION IF PREFIXED WITH '*'), SHEET, SHEET No.

                    string loc = words[0].Trim(new char[] { '"' });
                    string tag = words[1].Trim(new char[] { '"' });
                    string term = words[2].Trim(new char[] { '"' });
                    string wire = words[3].Trim(new char[] { '"' });
                    string nettag = words[4].Trim(new char[] { '"' });
                    string value = words[5].Trim(new char[] { '"' });
                    string termdesc = words[6].Trim(new char[] { '"' });
                    string sheet = words[7].Trim(new char[] { '"' });
                    string sheetno = words[8].Trim(new char[] { '"' });

                    string sh = sheet + "-" + sheetno;
                    if (exclude.Any(s => sh.Contains(s))) continue;

                    NetElement element = new NetElement();
                    element.nodes = new List<NetNode>();

                    element.sheet = sheet;
                    element.sheetno = sheetno;

                    element.location = loc;
                    element.tag = tag;
                    element.nettag = nettag;
                    element.name = (loc + "." + nettag).ToUpper();
                    element.type = nettag.ToUpper()[0];
                    value = (value == "" ? "0" : value);
                    element.value = Convert.ToDouble(value);

                    string termLoc = "*" + loc;
                    if (termdesc != "")
                    {
                        if (termdesc[0] == '*')
                        {
                            termLoc = termdesc;
                        }
                    }

                    // look for existing entry
                    NetElement thisElement = null;

                    if (!elements.TryGetValue(element.name, out thisElement))
                    {
                        elements.Add(element.name, element);
                        thisElement = elements.GetValue(element.name, null);

                        // diodes are polarised
                        if ((thisElement.type == 'D') || (thisElement.type == 'Z'))
                        {
                            element.nodes.Add(new NetNode(element, 0, "n.c.", false, "", ""));
                            element.nodes.Add(new NetNode(element, 0, "n.c.", false, "", ""));
                        }
                        else if (thisElement.type == 'P')
                        {   // pots are orientated
                            element.nodes.Add(new NetNode(element, 0, "n.c.", false, "", ""));
                            element.nodes.Add(new NetNode(element, 0, "n.c.", false, "", ""));
                            element.nodes.Add(new NetNode(element, 0, "n.c.", false, "", ""));
                        }
                    }
                    if ((thisElement.type == 'D') || (thisElement.type == 'Z'))
                    {
                        if (term.ToUpper() == "A")
                        {
                            thisElement.nodes[0].index = 0;
                            thisElement.nodes[0].node = wire;
                            thisElement.nodes[0].location = termLoc;
                            thisElement.nodes[0].term = term;
                        }
                        else
                        {
                            thisElement.nodes[1].index = 1;
                            thisElement.nodes[1].node = wire;
                            thisElement.nodes[1].location = termLoc;
                            thisElement.nodes[1].term = term;
                        }
                    }
                    else if (thisElement.type == 'P')
                    {
                        int idx = Convert.ToInt32(term) - 1;
                        thisElement.nodes[idx].index = idx;
                        thisElement.nodes[idx].node = wire;
                        thisElement.nodes[idx].location = termLoc;
                        thisElement.nodes[idx].term = term;
                    }
                    else if (thisElement.type == 'O')
                    {
                        // no op-amps envisaged for this application
                    }
                    else
                    {
                        // standard non-polarised
                        thisElement.nodes.Add(new NetNode(thisElement, thisElement.nodes.Count, wire, false, termLoc, term));
                    }
                }
            }


            foreach (KeyValuePair<string, NetElement> entry in elements)
            {
                NetElement el = entry.Value;
                switch (el.type)
                {

                    case 'P':
                        Potentiometer pot = sim.Create<Potentiometer>();
                        pot.maxResistance = el.value;
                        pot.position = 0.5;
                        el.simElement = pot;
                        break;
                    case 'X':       // terminal
                        Probe p = sim.Create<Probe>();
                        el.simElement = p;
                        break;
                    case 'V':
                        VoltageInput v = sim.Create<VoltageInput>(Voltage.WaveType.DC);
                        v.maxVoltage = el.value;
                        el.simElement = v;
                        break;
                    case 'R':
                        Resistor r = sim.Create<Resistor>();
                        r.resistance = el.value;
                        el.simElement = r;
                        break;
                    case 'D':
                        DiodeElm d = sim.Create<DiodeElm>();
                        el.simElement = d;
                        break;
                    case 'Z':
                        DiodeElm z = sim.Create<DiodeElm>();
                        z.zvoltage = el.value;
                        el.simElement = z;
                        break;
                    case 'S':
                        SwitchSPST s = sim.Create<SwitchSPST>();
                        s.setPosition(Convert.ToInt32(el.value));
                        el.simElement = s;
                        break;
                    case 'L':
                        Resistor l = sim.Create<Resistor>();
                        l.resistance = Convert.ToInt32(el.value);
                        el.simElement = l;
                        break;
                    case 'G':   // nothing to do
                        break;
                    default:
                        throw new Exception("Unrecognised component type in netlist (" + el.tag + " in " + el.location + ")");
                }
            }

            // connect elements

            foreach (KeyValuePair<string, NetElement> entry in elements)
            {
                NetElement el = entry.Value;

                for (int n = 0; n < el.nodes.Count; n++)
                {
                    if (!el.nodes[n].connected)
                    {
                        string thisNode = el.nodes[n].node;
                        foreach (KeyValuePair<string, NetElement> entry2 in elements)
                        {
                            NetElement el2 = entry2.Value;
                            if (thisNode != "n.c.")
                            {

                                for (int n2 = 0; n2 < el2.nodes.Count; n2++)
                                {
                                    if (!el2.nodes[n2].connected)
                                    {

                                        if ((el2.nodes[n2].node == thisNode) && (el2.nodes[n2].node != "n.c.") &&
                                            ((el.nodes[n].location == el2.nodes[n2].location) || (el.nodes[n].location == "*") || (el2.nodes[n2].location == "*")) &&
                                            (!el.nodes[n].Equals(el2.nodes[n2])))
                                        {
                                            Circuit.Lead l1 = Program.getConnection(el, n);
                                            Circuit.Lead l2 = Program.getConnection(el2, n2);
                                            if ((l1 != null) && (l2 != null))
                                            {
                                                string text1 = "Connected " + el.name + "-" + el.nodes[n].term + " to " + el2.name + "-" + el2.nodes[n2].term + " with wire " + thisNode;
                                                //Debug.Log(text1);
                                                //Program.splash.MessageText += text1 + Environment.NewLine;

                                                sim.Connect(l1, l2);
                                                el.nodes[n].connected = true;
                                                el2.nodes[n2].connected = true;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        public void PotFault(string location, string tag, double pos, bool broken)
        {
            foreach (KeyValuePair<string, NetElement> entry in elements)
            {
                NetElement el = entry.Value;
                if ((el.location == location) && (el.nettag == tag))
                {
                    if (el.simElement.GetType() == typeof(Potentiometer))
                    {
                        Potentiometer p = el.simElement as Potentiometer;
                        p.isBroken = broken;
                        p.breakPos = pos;
                    }
                }

            }
        }

        public void setNode(CircuitPoint cp, string location, string tag, string term, string displayTag, string displayTerm)
        {
            cp.node = getTerminal(location, tag, term);
            cp.displayTag = displayTag;
            cp.displayTerm = displayTerm;
        }

        public NetNode getTerminal(string location, string nettag, string term)
        {

            string nLocation = "*" + location;

            // look for ITB first
            foreach (KeyValuePair<string, NetElement> entry in elements)
            {
                NetElement el = entry.Value;

            }

            foreach (KeyValuePair<string, NetElement> entry in elements)
            {
                NetElement el = entry.Value;
                if (nettag == "ITB")
                {
                    if (el.location == "ITB")
                    {
                        foreach (NetNode n in el.nodes)
                        {
                            if (n.location == nLocation)
                            {
                                if (n.term == term) return (n);
                            }
                        }
                    }
                }
                else
                {
                    if (el.nettag == nettag)
                    {
                        foreach (NetNode n in el.nodes)
                        {
                            if ((el.location == location) || (n.location == nLocation))
                            {
                                if (n.term == term) return (n);
                            }
                        }
                    }
                }
            }
            return (null);
        }


        public NetElement getElementByLocationAndNettag(string location, string nettag)
        {
            foreach (KeyValuePair<string, NetElement> entry in elements)
            {
                NetElement el = entry.Value;
                if ((el.location == location) && (el.nettag == nettag))
                {
                    return el;
                }

            }
            return (null);
        }

        // set switch contact position by net tag
        public void contactPos(Program.SwitchContact contact, bool thisPosition)
        {

            bool status;        // false = open
            if (thisPosition)
            {
                status = true;
            }
            else
            {
                status = false;
            }
            if (contact.nc) status = !status;
            if (contact.element != null)
            {
                Program.ContactChange cFound;
                if (contactChange.TryGetValue(contact, out cFound)) contactChange.Remove(contact);

                if (!contact.element.IsOpen != status)
                {
                    contactChange.Add(contact, new Program.ContactChange(contact, status));
                }
            }
        }

        public void Reload()
        {
            elements.Clear();
            if (dispatcherTimer != null) dispatcherTimer.Stop();
            RelayHandlers = new List<Program.RelayHandler>();
            //SplashWindow splash = new SplashWindow();
            //splash.NetFile = path;
            //splash.ShowDialog();
        }

        public List<Program.RelayHandler> RelayHandlers { get; set; }
        public List<MCCContactor> MCCContactors { get; set; }

        bool timerLock = false;
        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            if (timerLock)
            {
                Debug.Log("Exiting re-entry");
                return;
            }
            timerLock = true;

           // Debug.Log("Starting " + name);
            Program.mainWindow.StatusRectangle.Fill = new SolidColorBrush(System.Windows.Media.Colors.Gainsboro);
            Program.mainWindow.StatusRectangle.Refresh();


            // do contact changes
            int limit = 20;
            do
            {
                //Debug.Log("Contact loop " + limit.ToString());

                foreach (KeyValuePair<Program.SwitchContact, Program.ContactChange> c in contactChange)
                {
                    c.Value.contact.element.setPosition(c.Value.newStatus ? 0 : 1);
                }
                contactChange.Clear();

                try
                {
                    sim.analyze();
                    sim.doTick(name);
                }
                catch (Exception ex)
                {
                    Debug.Log("--" + ex.Message);
                }

                OnTickComplete(new EventArgs());
                limit--;
                if (contactChange.Count > 0) Debug.Log(contactChange.Count + " contact changes remain (" + name + ")");
            } while ((contactChange.Count > 0) && (limit > 0));
            OnTimerComplete(new EventArgs());

            if (analysisOK)
            {
                Program.mainWindow.StatusRectangle.Fill = new SolidColorBrush(System.Windows.Media.Colors.Green);
            }
            else
            {
                Program.mainWindow.StatusRectangle.Fill = new SolidColorBrush(System.Windows.Media.Colors.Red);
            }
            Program.mainWindow.StatusRectangle.Refresh();
            //Debug.Log("Ending " + name);

            timerLock = false;
        }
        private static System.Windows.Threading.DispatcherTimer dispatcherTimer { get; set; }

        public void startTimer()
        {
            dispatcherTimer = new System.Windows.Threading.DispatcherTimer(System.Windows.Threading.DispatcherPriority.Normal);
            dispatcherTimer.Tick += dispatcherTimer_Tick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 250);
            dispatcherTimer.Start();
        }


    }
}
