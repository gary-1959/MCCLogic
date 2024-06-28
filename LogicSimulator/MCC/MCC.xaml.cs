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
    /// Interaction logic for MCC.xaml
    /// </summary>
    public partial class MCC : UserControl
    {
        public SimCircuit simcircuit { get; set; }
        public MCC()
        {
            InitializeComponent();
        }

        public void configureMCC()
        {
            A1.configureCan(@"Resources\CANCON-1.CSV", "A1", "AUXILIARY", "FAN");
            A1.simCan.startTimer();
        }
    }
}
