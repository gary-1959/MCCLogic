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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SharpCircuit
{
    /// <summary>
    /// Interaction logic for CircuitPoint.xaml
    /// </summary>

    public partial class CircuitPoint : UserControl
    {
        public NetNode node { get; set; }
        public string displayTag { get; set; }
        public string displayTerm { get; set; }
        public bool IsACVoltage { get; set; } = false;


        private double meterACKnobRotation { get; set; } = -10;
        private double meterDCKnobRotation { get; set; } = -66;

        public FrameworkElement HiLite { get; set; }
        public FrameworkElement MainContainer { get; set; }
        bool firstTime { get; set; }
        public CircuitPoint()
        {
            InitializeComponent();

            // search for hHiLite
            FrameworkElement p = this;
            firstTime = true;

        }

        private void UserControl_MouseEnter(object sender, MouseEventArgs e)
        {
            if (node != null)
            {
                MainWindow w = Program.getMainWindow();
                if (w == null) return;
                w.tagValue.Content = displayTag;
                w.termValue.Content = displayTerm;
                //                SolidColorBrush b = new SolidColorBrush(Color.FromArgb(100, 255, 255, 255));
                //                Background = b;

                //Debug.Log("Over " + this.Name);
                if (firstTime) getContext();

                if ((HiLite != null) && (MainContainer != null))
                {
                    Point relativeLocation = this.TranslatePoint(new Point(0, 0), MainContainer);
                    Program.setHilite(HiLite, true, relativeLocation, new Size(this.ActualWidth, this.ActualHeight));
                }

                Cursor = Cursors.Pen;
            }
        }
        private void getContext()
        {
            {
                MainContainer = this;
                while ((MainContainer != null) && (MainContainer.Name != "MainContainer"))
                {
                    MainContainer = MainContainer.Parent as FrameworkElement;
                    if (MainContainer == null) break;
                }

                HiLite = MainContainer.FindName("HiLite") as FrameworkElement;
                firstTime = false;
            }
        }
        private void UserControl_MouseLeave(object sender, MouseEventArgs e)
        {
            MainWindow w = Program.getMainWindow();
            if (w == null) return;
            w.tagValue.Content = "-";
            w.termValue.Content = "-";

            //           SolidColorBrush b = new SolidColorBrush(Color.FromArgb(0, 255, 0, 0));
            //            Background = b;

            //Debug.Log("Leave " + this.Name);
            if (firstTime) getContext();

            if ((HiLite != null) && (MainContainer != null))
            {
                Program.setHilite(HiLite, false, new Point(0d, 0d), new Size(this.ActualWidth, this.ActualHeight));
            }

        }       


        private void UserControl_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                e.Handled = false;
                return;
            }
            double v = -9999;
            if (node != null)
            {
                int index = node.index;
                if (node.parent.type == 'X') index = 0;     // X TBs are special
                try
                {
                    v = node.parent.simElement.getLeadVoltage(index);
                }
                catch (Exception ex)
                {
                    Debug.Log(ex.Message);
                }
            }

            double vabs = Math.Abs(v);
            string format = "";
            if (vabs > 999.9)
            {
                format = "0000";
            }
            else if (vabs > 99.99)
            {
                format = "000.0";
            }
            else if (vabs >= 9.99)
            {
                format = "00.00";
            }
            else
            {
                format = "0.000";
            }
            string sign = (v >= 0 ? "+" : "-");
            double kr = meterDCKnobRotation;

            if (IsACVoltage)
            {
                sign = "~";
                kr = meterACKnobRotation;
            }

            string num = sign + vabs.ToString(format);

            MainWindow w = Program.getMainWindow();
            if (w == null) return;
            w.DVMValue.Text = num;

            RotateTransform rotateTransform = new RotateTransform(kr);
            w.DVMKnob.RenderTransform = rotateTransform;
            Program.meterIn(w.popMeter);
        }
        private void UserControl_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            MainWindow w = Program.getMainWindow();
            if (w == null) return;
            w.DVMValue.Text = " 0.000";
            Program.meterOut(w.popMeter);
        }
    }
}
