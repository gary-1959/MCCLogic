using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace SharpCircuit
{
    public static class Scripting
    {
        private static bool pausedForAction { get; set; }
        public static ScriptTime lastEvent { get; set; }
        public delegate void ScriptsChanged(object sender, EventArgs e);
        public static event ScriptsChanged OnScriptsChanged;

        // faults are first defined and listed and combined into script items
        // script items are compiled into scripts
        public static bool scriptRunning { get; set; }
        public static Script currentScript { get; set; }
        public static Dictionary<string, Script> Scripts = new Dictionary<string, Script>();
        public class Script
        {
            public int id { get; set; }
            public string description { get; set; }
            public string filename { get; set; }
            public string path { get; set; }
            public string extension { get; set; }
            public double version { get; set; }
            public Dictionary<int, ScriptItem> script { get; set; } = new Dictionary<int, ScriptItem>();
            
            public Script(string d)
            {
                version = 1.0;
                id = Scripts.Count;
                description = d;
            }
        }
        public class ScriptTime
        {
            public int hours { get; set; }
            public int minutes { get; set; }
            public int seconds { get; set; }

            public ScriptTime(int h, int m, int s)
            {
                hours = h;
                minutes = m;
                seconds = s;
            }
        }
        public class ScriptItem
        {
            public int id { get; set; }
            public Program.FaultItem fault { get; set; }
            public ScriptTime time { get; set; }
            public enum ScriptStatus { WAITING, FAULTED, FIXED }
            public ScriptStatus status { get; set; }
            public string description { get; set; }
            public char faultAction { get; set; }
            public string imageFile { get; set; }
            public BitmapImage image { get; set; }
            public ScriptItem(Program.FaultItem f, ScriptTime t, char fa, string d, string img)
            {
                status = ScriptStatus.WAITING;
                fault = f;
                time = t;
                description = d;
                faultAction = fa;
                imageFile = img;
                image = new BitmapImage(new Uri(@"pack://application:,,,/MCCLogic;component/Resources/scripticons/" + imageFile));
            }
        }
        public static void runScript(bool start)
        {
            if ((start) && (currentScript != null))
            {
                scriptRunning = true;
                Program.mainWindow.vn.ScriptNotRunning = false;
                Program.mainWindow.timerStart();
                Program.mainWindow.vn.TimerIsRunning = true;
                if (!pausedForAction) lastEvent = new SharpCircuit.Scripting.ScriptTime(0, 0, 0);
                pausedForAction = false;
            }
            else if ((start) && (currentScript == null))
            {
                Program.mainWindow.timerStart();
                Program.mainWindow.vn.TimerIsRunning = true;
                pausedForAction = false;
            }
            else
            {
                Program.mainWindow.timerStop();
                scriptRunning = false;
                Program.mainWindow.vn.ScriptNotRunning = true;
                Program.mainWindow.vn.TimerIsRunning = false;
                pausedForAction = false;
            }
            

        }
        public static void openScript()
        {
            OpenFileDialog dlg = new OpenFileDialog();
            if (currentScript != null)
            {
                dlg.InitialDirectory = currentScript.path + "\\";
                dlg.Multiselect = false;
                dlg.FileName = currentScript.filename + currentScript.extension;
                dlg.RestoreDirectory = true;
                dlg.Filter = "Script files|*.scr|All files|*.*";
            }
            else
            {
                dlg.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\";
                dlg.Multiselect = false;
                dlg.FileName = "NEW_SCRIPT.SRC";
                dlg.RestoreDirectory = true;
                dlg.Filter = "Script files|*.scr|All files|*.*";
            }
            dlg.FileName = "LOGIC"; // Default file name
            dlg.DefaultExt = ".SCR"; // Default file extension
            dlg.Filter = "Script SCR (.scr)|*.scr"; // Filter files by extension

            // Show open file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                try
                {
                    if (loadScriptFromFile(dlg.FileName)) {
                        MessageBox.Show("Script loaded!", "Load Script", MessageBoxButton.OK, MessageBoxImage.Information);
                    }

                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Open Script", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        public static bool loadScriptFromFile(string filename)
        {

            currentScript = new Script(filename);
            string[] lines = File.ReadAllLines(filename, Encoding.UTF8);
            // remove comments
            int idx = 0;
            if (lines.GetUpperBound(0) == 0) return (false);
            foreach (string l in lines)
            {
                string c = l.Trim();
                if (c != "")
                {
                    c = l;
                    c = Regex.Replace(c, "/\\*.*", "");
                    c = Regex.Replace(c, "/\\\\.*", "");

                    if (l.Substring(0, 1) == "#")
                    {
                        try
                        {
                            currentScript.version = Convert.ToDouble(l.Substring(1));
                        }
                        catch {
                            
                        }
                    }

                    try
                    {
                        Regex RegexObj = new Regex("(\\d{1,2}):(\\d{1,2}):(\\d{1,2})\\s(\\d{1,4})\\s(\\w)\\s(.+?)\\s?(\\w*\\.png)?$");
                        Match MatchResults = RegexObj.Match(c);
                        if (MatchResults.Success)
                        {
                            int h = Convert.ToInt32(MatchResults.Groups[1].Value);
                            int m = Convert.ToInt32(MatchResults.Groups[2].Value);
                            int s = Convert.ToInt32(MatchResults.Groups[3].Value);
                            int f = Convert.ToInt32(MatchResults.Groups[4].Value);
                            char b = MatchResults.Groups[5].Value[0];
                            string t = Program.DecodeScriptText(MatchResults.Groups[6].Value);
                            string img = MatchResults.Groups[7].Value;

                            Program.FaultItem fi;
                            Program.FaultItems.TryGetValue(f, out fi);
                            ScriptTime st = new ScriptTime(h, m, s);
                            ScriptItem si = new ScriptItem(fi, st, b, t, img);
                            currentScript.script.Add(idx, si);
                            idx++;
                        }
                    }
                    catch
                    {
                        throw (new Exception("Syntax error on line " + (idx + 1).ToString() + " of " + filename));
                    }
                }
            }
            currentScript.path = System.IO.Path.GetDirectoryName(filename);
            currentScript.filename = System.IO.Path.GetFileNameWithoutExtension(filename);
            currentScript.extension = System.IO.Path.GetExtension(filename);
            Script sc;
            Scripts.TryGetValue(filename, out sc);
            if (sc != null) Scripts.Remove(filename);
            Scripts.Add(filename, currentScript);
            EventArgs se = new EventArgs();
            OnScriptsChanged(null, new EventArgs());
            // MessageBox.Show("Script loaded!", "Load Script", MessageBoxButton.OK, MessageBoxImage.Information);

            return (true);
        }
        public static void resetScript()
        {
            if (currentScript != null)
            {
                foreach (KeyValuePair<int, ScriptItem> entry in currentScript.script)
                {
                    ScriptItem si = entry.Value;
                    si.status = ScriptItem.ScriptStatus.WAITING;
                }
            }
            lastEvent = new SharpCircuit.Scripting.ScriptTime(0, 0, 0);
            pausedForAction = false;
        }
 
        public static void processScript(int h, int m, int s)
        {
            if (!scriptRunning) return;
            if (currentScript != null)
            {
                bool eventProcessed = false;
                bool eventsRemain = false;
                bool faultsRemain = false;
                foreach (KeyValuePair<int, ScriptItem> entry in currentScript.script)
                {                   
                    ScriptItem si = entry.Value;
                    if (!eventProcessed)
                    {
                        if (si.status == ScriptItem.ScriptStatus.WAITING) eventProcessed = true;
                        if ((si.status == ScriptItem.ScriptStatus.WAITING) && passed(lastEvent, new ScriptTime(h, m, s), si.time))
                        {
                            lastEvent = new ScriptTime(h, m, s);

                                switch (si.faultAction)
                                {
                                    case 'I':
                                        if (si.description != "")
                                        {

                                            runScript(false);
                                            pausedForAction = true;

                                            ScriptMessageWindow w = new ScriptMessageWindow();
                                            w.Title = "Instruction";
                                            w.sImage = si.image;
                                            w.sText = si.description + "<p>Click &#9654; (START /RESUME) to continue when complete.</p>";
                                            w.Owner = Program.mainWindow;
                                            w.Show();
                                        }
                                        si.status = ScriptItem.ScriptStatus.FIXED;
                                        break;
                                    case 'M':
                                        if (si.description != "")
                                        {
                                            ScriptMessageWindow w = new ScriptMessageWindow();
                                            w.Title = "Message";
                                            w.sImage = si.image;
                                            w.sText = si.description;
                                            w.Owner = Program.mainWindow;
                                            w.Show();
                                        }
                                        si.status = ScriptItem.ScriptStatus.FIXED;
                                        break;
                                    case 'F':
                                        if (si.fault != null)
                                        {
                                            si.fault.setFault();
                                            si.fault.faulted = true;
                                            si.fault.selected = true;
                                            si.status = ScriptItem.ScriptStatus.FAULTED;
                                            faultsRemain = true;
                                        }
                                        else
                                        {
                                            si.status = ScriptItem.ScriptStatus.FIXED;
                                        }
                                        if (si.description != "")
                                        {
                                            ScriptMessageWindow w = new ScriptMessageWindow();
                                            w.Title = "Fault Description";
                                            w.sImage = si.image;
                                            w.sText = si.description;
                                            w.Owner = Program.mainWindow;
                                            w.Show();
                                        }
                                        break;
                                    case 'R':
                                        if (si.fault != null)
                                        {
                                            si.fault.fixFault();
                                            si.fault.faulted = false;
                                            si.status = ScriptItem.ScriptStatus.FIXED;
                                        }
                                        break;
                                    default:
                                        MessageBox.Show("Unrecognised action code in script (" + si.faultAction + ")", "Script Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                        break;

                                }
                            }
                        
                        else // look for unfixed faults
                        {
                            if (si.status == ScriptItem.ScriptStatus.WAITING) eventsRemain = true;
                            switch (si.faultAction)
                            {

                                case 'F':
                                    if (si.fault != null)
                                    {
                                        if (si.fault.faulted) faultsRemain = true;
                                    }
                                    break;
                            }
                        }
                    }
                    else // event has occurred - look for future faults
                    {
                        eventsRemain = true;
                        switch (si.faultAction)
                        {
                            
                            case 'F':
                                if (si.fault != null)
                                {
                                    faultsRemain = true;
                                }
                                break;
                        }
                    }

                }
                Debug.Log("Faults Remain: " + faultsRemain.ToString() + "; Events Remain: " + eventsRemain.ToString());
                if ((!faultsRemain) && (!eventsRemain))
                {
                    MessageBox.Show("Script complete!", "Script", MessageBoxButton.OK, MessageBoxImage.Information);
                    runScript(false);
                    resetScript();
                }
            }
        }
        private static bool passed(ScriptTime last, ScriptTime s1, ScriptTime s2)
        {
            // check s1 > s2

            long l1 = ((((s1.hours - last.hours) * 60) + (s1.minutes - last.minutes)) * 60) + (s1.seconds - last.seconds);
            long l2 = (((s2.hours * 60) + s2.minutes) * 60) + s2.seconds;
            return (l1 > l2);
        }

        public static void openWriter()
        {
            ScriptWriter w = new ScriptWriter();
            w.Owner = Program.mainWindow;
            w.Show();
        }
    }
}
