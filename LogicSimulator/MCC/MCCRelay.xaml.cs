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
    /// Interaction logic for MCCRelay.xaml
    /// </summary>
    public partial class MCCRelay : UserControl
    {
        public System.Windows.Threading.DispatcherTimer dispatcherTimer;
        public List<Program.MCCInterface> mccInterfaces;

        private Program.RelayHandler RLMP1B { get; set; }
        private Program.RelayHandler RL3A { get; set; }
        private Program.RelayHandler RL3B { get; set; }
        private Program.RelayHandler RK3 { get; set; }
        private Program.RelayHandler RLMP1AFL { get; set; }
        private Program.RelayHandler RLMP1BFL { get; set; }
        private Program.RelayHandler RLMP1AFLD { get; set; }
        private Program.RelayHandler RLMP1BFLD { get; set; }
        private Program.RelayHandler RLCP1 { get; set; }
        private Program.RelayHandler RLMP1HTMS { get; set; }

        private Program.RelayHandler RLMP2B { get; set; }
        private Program.RelayHandler RL4A { get; set; }
        private Program.RelayHandler RL4B { get; set; }
        private Program.RelayHandler RK4 { get; set; }
        private Program.RelayHandler RLMP2AFL { get; set; }
        private Program.RelayHandler RLMP2BFL { get; set; }
        private Program.RelayHandler RLMP2AFLD { get; set; }
        private Program.RelayHandler RLMP2BFLD { get; set; }
        private Program.RelayHandler RLCP2 { get; set; }
        private Program.RelayHandler RLMP2HTMS { get; set; }

        private Program.RelayHandler RLDWAB { get; set; }
        private Program.RelayHandler RLDWBB { get; set; }
        private Program.RelayHandler RL2A { get; set; }
        private Program.RelayHandler RL2B { get; set; }
        private Program.RelayHandler RK2 { get; set; }
        private Program.RelayHandler RLDWAFL { get; set; }
        private Program.RelayHandler RLDWBFL { get; set; }
        private Program.RelayHandler RLDWAFLD { get; set; }
        private Program.RelayHandler RLDWBFLD { get; set; }
        private Program.RelayHandler RLDWON { get; set; }

        private Program.RelayHandler RLRTB { get; set; }
        private Program.RelayHandler RL1 { get; set; }
        private Program.RelayHandler RK1 { get; set; }
        private Program.RelayHandler RLRTFL { get; set; }
        private Program.RelayHandler RLRTFLD { get; set; }
        private Program.RelayHandler RLRTON { get; set; }

        public MCCRelay()
        {
            InitializeComponent();

            mccInterfaces = new List<Program.MCCInterface>();
            // TODO: for quick debugging hide MCC
           
            MCC mcc = Program.mainWindow.mccTabControl.MCCControl;




            MCCCan can = mcc.A1;
            /*mccInterfaces.Add(new Program.MCCInterface("RT BLOWER", can,
                can.simCan.getElementByLocationAndNettag("CAN", "SPB21"),
                can.simCan.getElementByLocationAndNettag("CAN", "SC1-NO1"),
                can.simCan.getElementByLocationAndNettag("CAN", "SC1-NC1"),
                RLRTB,
                Program.simMain.getElementByLocationAndNettag("MCC", "SMCC-RTB-RUN"),
                Program.simMain.getElementByLocationAndNettag("MCC", "SMCC-RTB-ALARM")));   */



            //Program.simMain.TickComplete += dispatcherTimer_Tick;

            dispatcherTimer = new System.Windows.Threading.DispatcherTimer(System.Windows.Threading.DispatcherPriority.Normal);
            dispatcherTimer.Tick += dispatcherTimer_Tick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 250);
            dispatcherTimer.Start(); 


        }

        bool timerLock = false;
        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            if (timerLock)
            {
                Debug.Log("Exiting re-entry");
                return;
            }
            timerLock = true;

            foreach (Program.MCCInterface m in mccInterfaces)
            {
                foreach (Program.VoltageSourceHook h in m.voltageSourceHooks)
                {
                    h.doHookUp();
                }
            }

            timerLock = false;
        }


    }


}
