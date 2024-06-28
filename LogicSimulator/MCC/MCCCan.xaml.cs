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

namespace SharpCircuit
{
    /// <summary>
    /// Interaction logic for MCCCan.xaml
    /// </summary>
    public partial class MCCCan : UserControl
    {
        public string path { get; set; }
        public string canName { get; set; }
        public SimCircuit simCan { get; set; }
        public System.Windows.Threading.DispatcherTimer dispatcherTimer;

        private BitmapImage imageCBClosed { get; set; }
        private BitmapImage imageCBOpen { get; set; }
        private BitmapImage imageCBTripped { get; set; }

        public MCCCan()
        {
            InitializeComponent();

            imageCBClosed = new BitmapImage(new Uri(@"pack://application:,,,/MCCLogic;component/Resources/MCC-BREAKER-CLOSED.png"));
            imageCBOpen = new BitmapImage(new Uri(@"pack://application:,,,/MCCLogic;component/Resources/MCC-BREAKER-OPEN.png"));
            imageCBTripped = new BitmapImage(new Uri(@"pack://application:,,,/MCCLogic;component/Resources/MCC-BREAKER-TRIPPED.png"));

        }

        public void configureCan(string f, string c, string label1, string label2)
        {
            path = f;

            simCan = new SimCircuit("MCC_" + c);
            simCan.ReadCircuit(path);

            #region MCCB
            CBContacts = new List<Program.SwitchContact>();
            CBContacts.Add(new Program.SwitchContact(simCan, "CAN", "SCB-1", false));
            CBContacts.Add(new Program.SwitchContact(simCan, "CAN", "SCB-3", false));
            CBContacts.Add(new Program.SwitchContact(simCan, "CAN", "SCB-5", false));

            simCan.setNode(cb1, "CAN", "SCB-1", "1", "MCCB", "1");
            cb1.IsACVoltage = true;
            simCan.setNode(cb2, "CAN", "SCB-1", "2", "MCCB", "2");
            cb2.IsACVoltage = true;
            simCan.setNode(cb3, "CAN", "SCB-3", "3", "MCCB", "3");
            cb3.IsACVoltage = true;
            simCan.setNode(cb4, "CAN", "SCB-3", "4", "MCCB", "4");
            cb4.IsACVoltage = true;
            simCan.setNode(cb5, "CAN", "SCB-5", "5", "MCCB", "5");
            cb5.IsACVoltage = true;
            simCan.setNode(cb6, "CAN", "SCB-5", "6", "MCCB", "6");
            cb6.IsACVoltage = true;

            // open CB

            CBLastLeverPosition = -1;
            CBLeverPosition = -1;
            CBLever.Source = imageCBOpen;
            setCBSwitchContacts();
            CBLever.PreviewMouseWheel += CBControl;
            #endregion

            #region FUSES
            simCan.setNode(pfs1_1A, "CAN", "RPFS1", "1", "PFS1", "1");
            pfs1_1A.IsACVoltage = true;
            simCan.setNode(pfs1_1B, "CAN", "RPFS1", "1", "PFS1", "1");
            pfs1_1B.IsACVoltage = true;
            simCan.setNode(pfs1_2A, "CAN", "RPFS1", "2", "PFS1", "2");
            pfs1_2A.IsACVoltage = true;
            simCan.setNode(pfs1_2B, "CAN", "RPFS1", "2", "PFS1", "2");
            pfs1_2B.IsACVoltage = true;

            simCan.setNode(pfs2_1A, "CAN", "RPFS2", "1", "PFS2", "1");
            pfs2_1A.IsACVoltage = true;
            simCan.setNode(pfs2_1B, "CAN", "RPFS2", "1", "PFS2", "1");
            pfs2_1B.IsACVoltage = true;
            simCan.setNode(pfs2_2A, "CAN", "RPFS2", "2", "PFS2", "2");
            pfs2_2A.IsACVoltage = true;
            simCan.setNode(pfs2_2B, "CAN", "RPFS2", "2", "PFS2", "2");
            pfs2_2B.IsACVoltage = true;
            #endregion

            #region TRANSFORMER
            simCan.setNode(T1_1A, "CAN", "XT1-1", "1", "T1", "1A");
            T1_1A.IsACVoltage = true;
            simCan.setNode(T1_1C, "CAN", "XT1-2", "2", "T1", "1C");
            T1_1C.IsACVoltage = true;
            simCan.setNode(T1_2A, "CAN", "XT1-3", "3", "T1", "2A");
            T1_2A.IsACVoltage = true;
            simCan.setNode(T1_2C, "CAN", "XT1-4", "4", "T1", "2C");
            T1_2C.IsACVoltage = true;

            Program.RelayHandler rlt1 = new Program.RelayHandler(simCan, "CAN", "RT1A", 480);
            rlt1.auxContacts.Clear(); // not KUP type
            rlt1.auxContacts.Add(new Program.SwitchContact(simCan, "CAN", "ST1A", false));

            Program.RelayHandler rlt2 = new Program.RelayHandler(simCan, "CAN", "RT1B", 480);
            rlt2.auxContacts.Clear(); // not KUP type
            rlt2.auxContacts.Add(new Program.SwitchContact(simCan, "CAN", "ST1B", false));
            #endregion

            #region TB1
            int[] tb1 = {1, 2, 3, 4, 5, 11, 12, 13, 14, 15, 16, 17, 18 };

            foreach (int t in tb1)
            {
                CircuitPoint cpA = FindName("TB1_" + t.ToString() + "A") as CircuitPoint;
                simCan.setNode(cpA, "CAN", "XTB1-" + t.ToString(), t.ToString(), "TB1", t.ToString());
                CircuitPoint cpB = FindName("TB1_" + t.ToString() + "B") as CircuitPoint;
                simCan.setNode(cpB, "CAN", "XTB1-" + t.ToString(), t.ToString(), "TB1", t.ToString());

                if (t == 1 || t ==2 || t==3 || t==4 || t == 5)
                {
                    cpA.IsACVoltage = cpB.IsACVoltage = true;
                }
            }
            #endregion

            #region TB2
            int[] tb2 = { 1, 2, 3 };

            foreach (int t in tb2)
            {
                CircuitPoint cpA = FindName("TB2_" + t.ToString() + "A") as CircuitPoint;
                simCan.setNode(cpA, "CAN", "XTB2-" + t.ToString(), t.ToString(), "TB2", t.ToString());
                cpA.IsACVoltage = true;
                CircuitPoint cpB = FindName("TB2_" + t.ToString() + "B") as CircuitPoint;
                simCan.setNode(cpB, "CAN", "XTB2-" + t.ToString(), t.ToString(), "TB2", t.ToString());
                cpB.IsACVoltage = true;
            }
            #endregion

            #region CONTACTOR
            canContactor.configureMCCContactor(simCan, "CAN");
            #endregion

            #region CONTROLS

            Program.PushbuttonHandler pbStart = new Program.PushbuttonHandler(simCan, PBStart, "CAN", "PB2", "/MCCLogic;component/Resources/PB-GREEN-UP.png", "/MCCLogic;component/Resources/PB-GREEN-DOWN.png");
            pbStart.switchContacts.Add(new Program.SwitchContact(simCan, "CAN", "SPB2", false));

            Program.PushbuttonHandler pbStop = new Program.PushbuttonHandler(simCan, PBStop, "CAN", "PB1", "/MCCLogic;component/Resources/PB-RED-UP.png", "/MCCLogic;component/Resources/PB-RED-DOWN.png");
            pbStop.switchContacts.Add(new Program.SwitchContact(simCan, "CAN", "SPB1", true));

            Program.SwitchContacts hoaContacts = new Program.SwitchContacts();
            hoaContacts.leftContacts.Add(new Program.SwitchContact(simCan, "CAN", "S1-1", false));

            hoaContacts.rightContacts.Add(new Program.SwitchContact(simCan, "CAN", "S1-2", false));

            Program.SwitchThreePositionHandler hoa = new Program.SwitchThreePositionHandler(simCan, HOASwitch, "CAN", "S1", hoaContacts);
            hoa.setPosition(0);

            #endregion

            #region LABEL
            Line1.Content = label1;
            Line2.Content = label2;

            #endregion

            // try
            // {
            string text = "Beginning analysis.... ";
            //Debug.Log(text);
            Program.splash.MessageText += text;

            // first analysis
            simCan.sim.analyze();
            simCan.sim.doTick(simCan.name);

            text = "Complete!";
            //Debug.Log(text);
            Program.splash.MessageText += text + Environment.NewLine;

            text = "Starting background tasks...";
            //Debug.Log(text);
            Program.splash.MessageText += text;

        }

        // CB Control

        public int CBLeverPosition { get; set; }
        private int CBLastLeverPosition { get; set; }
        private List<Program.SwitchContact> CBContacts {get; set;}
        private bool cbTripped { get; set; } = false;
        public bool CBTripped
        {
            get
            {
                return (cbTripped);
            }
            set
            {
                if (value)
                {
                    cbTripped = true;
                    CBLeverPosition = 0;
                }
                else
                {
                    cbTripped = false;
                }
                setCBLeverPosition();
            }
        }
        public void CBControl(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                {
                if (CBLeverPosition == 0)
                {
                    if (e.Delta < 0)
                    {
                        CBLeverPosition = -1;   // open
                    }
                }
                else
                {
                    if (e.Delta > 0)
                    {
                        // 0 = tripped

                        CBLeverPosition = 1;
                    }
                    if (e.Delta < 0)
                    {
                        CBLeverPosition = -1;
                    }
                }

                setCBLeverPosition();

                e.Handled = true;
            }
        }

        public void setCBLeverPosition()
        {
            if (CBLeverPosition != CBLastLeverPosition)
                {
                CBLastLeverPosition = CBLeverPosition;

                switch (CBLeverPosition)
                {
                    case -1:
                        CBLever.Source = imageCBOpen;

                        // Added 29/09/17 to fix fault if applied
                        cbTripped = false;
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
                        break;
                    case 0:
                        CBLever.Source = imageCBTripped;
                        break;
                    case 1:
                        CBLever.Source = imageCBClosed;
                        break;
                }

                setCBSwitchContacts();
                Program.contactorClick();
            }
        }
        public void setCBSwitchContacts()
        {
            if (CBLeverPosition == 1)
            {
                foreach (Program.SwitchContact s in CBContacts)
                {
                    simCan.contactPos(s, true);
                }
            }
            else
            {
                foreach (Program.SwitchContact s in CBContacts)
                {
                    simCan.contactPos(s, false);
                }
            }
        }
    }
}
