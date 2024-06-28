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
    /// Interaction logic for FaultItemControl.xaml
    /// </summary>
    public partial class FaultItemControl : UserControl
    {
        public enum FaultItemControlMode { NORMAL, FIX, GRID };
        private FaultItemControlMode _mode { get; set; }
        public FaultItemControl()
        {
            InitializeComponent();
        }

        public void configureFaults(FaultItemControlMode mode)
        {
            _mode = mode;

            if (mode == FaultItemControlMode.GRID)
            {
                ItemStack.Visibility = Visibility.Collapsed;
                SelectedItemName.Visibility = Visibility.Visible;
                SelectedItemName.Text = "No Fault";
            }
            else
            {
                SelectedItemName.Visibility = Visibility.Collapsed;
            }

            string lastGroup = "";
            string lastSection = "";
            Expander sectionExpander = null;
            StackPanel sectionStackPanel = null;
            Expander thisExpander = null;
            StackPanel thisStackPanel = null;

            foreach (KeyValuePair<int, Program.FaultItem> entry in Program.FaultItems)
            {
                Program.FaultItem f = entry.Value;
                if (f.section != lastSection)
                {
                    sectionExpander = new Expander();
                    sectionExpander.Header = f.section;
                    sectionStackPanel = new StackPanel();
                    sectionStackPanel.Margin = new Thickness(22, 2, 10, 10);
                    sectionExpander.Content = sectionStackPanel;
                    expanders.Children.Add(sectionExpander);
                    lastGroup = "";
                    lastSection = f.section;
                    
                }
                if (f.group != lastGroup)
                {
                    thisExpander = new Expander();
                    thisExpander.Header = f.group;
                    thisStackPanel = new StackPanel();
                    thisStackPanel.Margin = new Thickness(22, 2, 10, 10);
                    thisExpander.Content = thisStackPanel;
                    sectionStackPanel.Children.Add(thisExpander);
                    lastGroup = f.group;
                }

                if (thisStackPanel != null)
                {
                    bool enabled = true;
                    CheckBox cb = new CheckBox();
                    cb.IsEnabled = enabled;
                    cb.Margin = new Thickness(2, 2, 2, 2);
                    cb.Content = "[" + f.id.ToString("D4") + "] " + f.name;
                    cb.Tag = f;
                    cb.Checked += CheckBox_Checked;
                    cb.Unchecked += CheckBox_Unchecked;
                    if (mode == FaultItemControlMode.FIX) {
                        cb.IsChecked = false;
                    }
                    else
                    {
                        cb.IsChecked = f.selected; 
                    }
                        
                    thisStackPanel.Children.Add(cb);
                }
            }
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            CheckBox cb = sender as CheckBox;
            Program.FaultItem f = cb.Tag as Program.FaultItem;
            SelectedItemName.Text = "No Fault";
            //f.selected = true;
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox cb = sender as CheckBox;
            if (_mode == FaultItemControlMode.GRID)
            {
                ClearSelections(expanders, cb);
            }
 
            Program.FaultItem f = cb.Tag as Program.FaultItem;
            SelectedItemName.Text = f.section + ">" + f.group + " [" + f.id.ToString("D4") + "] " + f.name;
            //f.selected = false;
        }

        public void UserControl_GotFocus(object sender, RoutedEventArgs e)
        {
            if (_mode == FaultItemControlMode.GRID)
            {
                Debug.Log("Focus");
                ItemStack.Visibility = Visibility.Visible;
                //SelectedItemName.Visibility = Visibility.Collapsed;
            }

        }

        public void UserControl_LostFocus(object sender, RoutedEventArgs e)
        {
            if(_mode == FaultItemControlMode.GRID) {
                Debug.Log("Lost Focus");
                CheckBox x = GetSelectedItem(expanders);
                if (x == null)
                {
                    SelectedItemName.Text = "No Fault";
                    if (DataContext.GetType() == typeof(ScriptWriter.ScriptLine))
                    {
                        ScriptWriter.ScriptLine l = DataContext as ScriptWriter.ScriptLine;
                        l.fault = "0000";
                    }
                } 
                else
                {
                    Program.FaultItem f = x.Tag as Program.FaultItem;
                    SelectedItemName.Text = f.section + ">" + f.group + " [" + f.id.ToString("D4") + "] " + f.name;

                    if (DataContext.GetType() == typeof(ScriptWriter.ScriptLine))
                    {
                        ScriptWriter.ScriptLine l = DataContext as ScriptWriter.ScriptLine;
                        l.fault = f.id.ToString("D4");
                    }
                }

                ItemStack.Visibility = Visibility.Collapsed;
                //SelectedItemName.Visibility = Visibility.Visible;
            }
        }

        private CheckBox GetSelectedItem(StackPanel parent)
        {
            foreach (object c in parent.Children)
            {
                if (c.GetType() == typeof(Expander))
                {
                    Expander e = c as Expander;
                    CheckBox r = GetSelectedItem(e.Content as StackPanel);
                    if (r != null)
                    {
                        return (r);
                    }
                }
                else if (c.GetType() == typeof(StackPanel)) 
                {
                    CheckBox r = GetSelectedItem((StackPanel)c);
                    if (r!= null)
                    {
                        return (r);
                    }
                }
                else if (c.GetType() == typeof(CheckBox))
                {
                    CheckBox x = c as CheckBox;
                    if (x.IsChecked == true)
                    {
                        return (x);
                    }
                }
            }
            return (null);
        }

        private void ClearSelections(StackPanel parent, CheckBox except)
        {
            foreach (object c in parent.Children)
            {
                if (c.GetType() == typeof(Expander))
                {
                    Expander e = c as Expander;
                    ClearSelections(e.Content as StackPanel, except);
                }
                else if (c.GetType() == typeof(StackPanel))
                {
                    ClearSelections((StackPanel)c, except);
                }
                else if (c.GetType() == typeof(CheckBox))
                {
                    CheckBox x = c as CheckBox;
                    if (x != except)
                    {
                        if (x.IsChecked == true)
                        {
                            Program.FaultItem f = x.Tag as Program.FaultItem;
                            x.IsChecked = false;
                        }
                    }

                }
            }
        }

        public void SelectItem(string fault)
        {
            selectItem(expanders, Convert.ToInt32(fault));
        }

        private void selectItem(StackPanel parent, int select)
        {
            foreach (object c in parent.Children)
            {
                if (c.GetType() == typeof(Expander))
                {
                    Expander e = c as Expander;
                    selectItem(e.Content as StackPanel, select);
                }
                else if (c.GetType() == typeof(StackPanel))
                {
                    selectItem((StackPanel)c, select);
                }
                else if (c.GetType() == typeof(CheckBox))
                {
                    CheckBox x = c as CheckBox;
                    Program.FaultItem f = x.Tag as Program.FaultItem;
                    if (f.id == select) { 
                        x.IsChecked = true;
                        SelectedItemName.Text = f.section + ">" + f.group + " [" + f.id.ToString("D4") + "] " + f.name;
                    }
                }
            }
        }
    }
}
