using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SharpCircuit
{
    public partial class CircuitPanel2 : UserControl
    {
        public CircuitPanel2()
        {

            InitializeComponent();
            circuitPoint1.node = Program.getTerminal("SCR1", "CB", "A33");
            circuitPoint2.node = Program.getTerminal("SCR1", "CB", "A34");
            circuitPoint3.node = Program.getTerminal("DC", "S5", "11A");
            circuitPoint4.node = Program.getTerminal("DC", "S5", "11B");

        }

        private void circuitPoint_Click(object sender, EventArgs e)
        {
            CircuitPoint c = sender as CircuitPoint;
            NetNode n = c.node;
            NetElement el = n.parent;

            CircuitElement x = el.simElement;
            voltage.Text = x.getVoltageDelta().ToString("#.##");
        }
    }
}
