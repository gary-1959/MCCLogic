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
    public partial class CircuitPoint : UserControl
    {
        public NetNode node { get; set; }
        public CircuitPoint()
        {
            InitializeComponent();
        }
    }
}
