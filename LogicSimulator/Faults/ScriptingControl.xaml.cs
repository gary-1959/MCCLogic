using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
    /// Interaction logic for ScriptingControl.xaml
    /// </summary>
    public partial class ScriptingControl : UserControl
    {
        public ScriptingControl()
        {
            InitializeComponent();
            //DataContext = Program.mainWindow.vn;
            ScriptsChangedHandler(null, new EventArgs());
            scripts.SelectedIndex = 0;
            
            Scripting.OnScriptsChanged += ScriptsChangedHandler;
        }

        private void Rewind(object sender, RoutedEventArgs e)
        {
            Scripting.resetScript();
            Program.mainWindow.timerReset();
            Program.mainWindow.vn.ScriptIsEditable = true;
        }

        private void Pause(object sender, RoutedEventArgs e)
        {
            Scripting.runScript(false);
            Program.mainWindow.vn.ScriptIsEditable = true;
        }

        private void Stop(object sender, RoutedEventArgs e)
        {
            Scripting.runScript(false);
            Program.mainWindow.vn.ScriptIsEditable = true;
        }

        private void Play(object sender, RoutedEventArgs e)
        {
            Scripting.runScript(true);
            Program.mainWindow.vn.ScriptIsEditable = true;
        }

        public void ScriptsChangedHandler(object sender, EventArgs e)
        {
            int selection = scripts.SelectedIndex;

            scripts.Items.Clear();
            scripts.Items.Add(new ScriptComboClass { DisplayValue = "No Script", Value = null });
            foreach (KeyValuePair<string, Scripting.Script> entry in Scripting.Scripts)
            {
                scripts.Items.Add(new ScriptComboClass { DisplayValue = System.IO.Path.GetFileName(entry.Value.description), Value = entry.Value });
            };

            if (Scripting.currentScript != null)
            {
                string key = Scripting.currentScript.path + "\\" + Scripting.currentScript.filename + Scripting.currentScript.extension;
                foreach (ScriptComboClass c in scripts.Items)
                {
                    if (c.Value != null)
                    {

                        if (key == (c.Value.path + "\\" + c.Value.filename + c.Value.extension))
                        {
                            scripts.SelectedIndex = scripts.Items.IndexOf(c);
                            break;
                        }
                    }
                }
            }
            

            if (Program.mainWindow != null) Program.mainWindow.vn.ScriptIsEditable = true;
        }

        public class ScriptComboClass
        {
            public Scripting.Script Value { get; set; }
            public string DisplayValue { get; set; }

        }

        private void scripts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                ComboBox c = sender as ComboBox;
                ScriptComboClass cc = e.AddedItems[0] as ScriptComboClass;
                Scripting.currentScript = cc.Value;
                if (Program.mainWindow != null) Program.mainWindow.vn.ScriptIsEditable = true;
            }
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (Scripting.currentScript != null)
            {
                ScriptWriter w = new ScriptWriter();
                w.Owner = Program.mainWindow;
                w.EditFile = Scripting.currentScript.path + "\\" + Scripting.currentScript.filename + Scripting.currentScript.extension;
                w.ShowDialog();
                if (w.EditFileSuccess)
                {
                    Scripting.loadScriptFromFile(w.EditFile);
                }
            }
                
        }

        private void Fix_Click(object sender, RoutedEventArgs e)
        {
            if (Scripting.currentScript != null)
            {
                FaultWindow w = new FaultWindow();
                w.Owner = Program.mainWindow;
                w.Mode = FaultItemControl.FaultItemControlMode.FIX;
                w.Show();
            }

        }

        private void Load_Click(object sender, RoutedEventArgs e)
        {
            Scripting.openScript();
        }
    }
}
