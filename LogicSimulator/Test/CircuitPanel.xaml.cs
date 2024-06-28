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
    /// Interaction logic for CircuitPanel.xaml
    /// </summary>
    public partial class CircuitPanel : UserControl
    {
        public CircuitPanel()
        {
            InitializeComponent();

            circuitPoint1.node = Program.getTerminal("SCR1", "DCM", "134");
            circuitPoint2.node = Program.getTerminal("SCR1", "CB", "A33");
            circuitPoint3.node = Program.getTerminal("SCR1", "CB", "A34");
            circuitPoint4.node = Program.getTerminal("DC", "S5", "11B");
        }

        private void circuitPoint_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            CircuitPoint c = sender as CircuitPoint;
            NetNode n = c.node;
            NetElement el = n.parent;

            CircuitElement x = el.simElement;
            voltage.Text = x.getLeadVoltage(n.index).ToString();
        }

        private void closeCBButton_Click(object sender, RoutedEventArgs e)
        {
            Program.auxSwitch("SCR1", "CB", true);
            Program.sim.analyze();
            //Program.sim.doTick();
        }
    }
}
