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
    /// Interaction logic for MCCPendant.xaml
    /// </summary>
    public partial class MCCPendant : UserControl
    {


        public MCCPendant()
        {
            InitializeComponent();

            

        }

        public void configurePendant(SimCircuit simCan)
        { 

            Program.PushbuttonHandler emStop = new Program.PushbuttonHandler(simCan, EMSTOP, "CAN", "PB11", "/MCCLogic;component/Resources/PENDANT-EM-STOP-UP.png", "/MCCLogic;component/Resources/PENDANT-EM-STOP-DOWN.png");
            emStop.switchContacts.Add(new Program.SwitchContact(simCan, "CAN", "SPB11", true));
            Program.PushbuttonHandler pbStart = new Program.PushbuttonHandler(simCan, START, "CAN", "PB21", "/MCCLogic;component/Resources/PENDANT-PB-UP.png", "/MCCLogic;component/Resources/PENDANT-PB-DOWN.png");
            pbStart.switchContacts.Add(new Program.SwitchContact(simCan, "CAN", "SPB21", false));

            Program.LampHandler runLamp = new Program.LampHandler(simCan, LAMP, "CAN", "L11", 115, "/MCCLogic;component/Resources/PENDANT-LAMP-OFF.png", "/MCCLogic;component/Resources/PENDANT-LAMP-ON.png");
        }
    }
}
