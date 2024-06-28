using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
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
using System.Windows.Threading;

namespace SharpCircuit
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class FaultWindow : Window
    {
        private bool pauseScript { get; set; }

        private FaultItemControl.FaultItemControlMode _mode;
        public FaultItemControl.FaultItemControlMode Mode {
            get
            {
                return (_mode);
            }
            set
            {
                _mode = value;
                FaultItems.configureFaults(_mode);
                if (_mode == FaultItemControl.FaultItemControlMode.FIX)
                {
                    pauseScript = !Program.mainWindow.vn.ScriptNotRunning;
                    if (pauseScript) Scripting.runScript(false);
                    BtnFinished.Content = "Fix";
                    BtnApply.Visibility = Visibility.Hidden;
                    BtnClear.Visibility = Visibility.Hidden;                   
                    BtnFinished.Visibility = Visibility.Visible;
                    BtnSave.Visibility = Visibility.Hidden;
                    BtnToggle.Visibility = Visibility.Hidden;
                }
                else
                {
                    BtnFinished.Content = "Finished";
                    BtnApply.Visibility = Visibility.Visible;
                    BtnClear.Visibility = Visibility.Visible;
                    BtnFinished.Visibility = Visibility.Visible;
                    BtnSave.Visibility = Visibility.Visible;
                    BtnToggle.Visibility = Visibility.Visible;
                }
            }
        }

        public FaultWindow()
        {
            InitializeComponent();
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            foreach (CheckBox cb in Program.FindLogicalChildren<CheckBox>(FaultItems))
            {
                Program.FaultItem f = cb.Tag as Program.FaultItem;
                f.selected = false;
            }
            Program.settingFaultsStatus = Program.settingFaults.SET;
        }

        private void Toggle_Click(object sender, RoutedEventArgs e)
        {
            foreach (CheckBox cb in Program.FindLogicalChildren<CheckBox>(FaultItems))
            {
                Program.FaultItem f = cb.Tag as Program.FaultItem;
                f.selected = !f.selected;
            }
        }

        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            if (_mode == FaultItemControl.FaultItemControlMode.FIX)
            {
                doFixes();
            }
            else
            {
                foreach (CheckBox cb in Program.FindLogicalChildren<CheckBox>(FaultItems))
                {
                    Program.FaultItem f = cb.Tag as Program.FaultItem;
                    f.selected = (cb.IsChecked == true ? true : false);
                }
                Program.settingFaultsStatus = Program.settingFaults.SET;
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            foreach (CheckBox cb in Program.FindLogicalChildren<CheckBox>(FaultItems))
            {
                Program.FaultItem f = cb.Tag as Program.FaultItem;
                f.selected = (cb.IsChecked == true ? true : false); 
            }
        }
    

        private void Finished_Click(object sender, RoutedEventArgs e)
        {
            if (_mode == FaultItemControl.FaultItemControlMode.FIX)
            {
                doFixes();
            }
            Close();
        }

        private void doFixes()
        {
            foreach (CheckBox cb in Program.FindLogicalChildren<CheckBox>(FaultItems))
            {
                Program.FaultItem f = cb.Tag as Program.FaultItem;
                if ((cb.IsChecked == true) && f.selected)
                {

                    MessageBox.Show("Fault correctly identified and fix applied.", "Fix Fault", MessageBoxButton.OK, MessageBoxImage.Information);
                    f.selected = false;
                    f.fixFault();
                    f.faulted = false;
                    break;
                }
                if ((cb.IsChecked == true) && !f.selected)
                {
                    MessageBox.Show("Fault incorrectly identified. Please try again.", "Fix Fault", MessageBoxButton.OK, MessageBoxImage.Error);
                    break;
                }
            }
        }
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (_mode== FaultItemControl.FaultItemControlMode.FIX)
            {
                if (pauseScript) Scripting.runScript(true);
            }
        }
    }
}

