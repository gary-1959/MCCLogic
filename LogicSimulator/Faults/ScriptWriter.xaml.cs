using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
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
using DataGridDragAndDrop;
using Hardcodet.Wpf.Util;
using LinqToVisualTree;
using Microsoft.Win32;

namespace SharpCircuit
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class ScriptWriter : Window
    {
        public bool AutoLoad { get; set; }
        public bool EditFileSuccess { get; set; }
        private string editFile { get; set; }
        public string EditFile
        {
            get
            {
                return editFile;
            }
            set
            {
                editFile = value;
                if (loadScriptFromFile(editFile))
                {
                    fillScriptForm();
                    EditFileSuccess = true;
                }
                else
                {
                    MessageBox.Show("Unable to open script file", "File Error", MessageBoxButton.OK);
                    EditFileSuccess = false;
                    Close();
                }
            }
        }
        public Script currentScript { get; set; }
        public class Script
        {
            public string filename { get; set; }
            public string path { get; set; }
            public string extension { get; set; }
            public string description { get; set; }
            public double version { get; set; }
            public List<ScriptLine> lines { get; set; }

            public Script()
            {
                version = 1.0;
                lines = new List<ScriptLine>();
            }
        }

        public class ScriptLine
        {
            public int line { get; set; }
            public string hours { get; set; }
            public string minutes { get; set; }
            public string seconds { get; set; }
            public string fault { get; set; }
            public string command { get; set; }
            public string text { get; set; }
            public string image { get; set; }

            public ScriptLine(int l, string h, string m, string s, string f, string c, string txt, string img)
            {
                line = l;
                hours = h;
                minutes = m;
                seconds = s;
                fault = f;
                command = c;
                text = txt;
                image = img;
            }
        }

        public class ScriptCommand
        {
            public string mnemonic { get; set; }
            public string description { get; set; }

            public ScriptCommand( string n, string d)
            {
                mnemonic = n;
                description = d;
            }
        }

        public class ScriptFault
        {
            public string fault { get; set; }
            public string description { get; set; }
            public bool selectable { get; set; }

            public ScriptFault(string f, string d, bool s)
            {
                fault = f;
                description = d;
                selectable = s;
            }
        }

        public class ScriptImage
        {
            public string name { get; set; }
            public string file { get; set; }
            public BitmapImage image { get; set; } 
            public string source { get; set; }

            public ScriptImage(string n, string f)
            {
                name = n;
                file = f;
                source = @"pack://application:,,,/MCCLogic;component/Resources/scripticons/" + file;
                image = new BitmapImage(new Uri(source));

            }
        }

        public List<ScriptCommand> ScriptCommands { get; set; }
        public List<ScriptFault> ScriptFaults { get; set; }
        public List<ScriptImage> ScriptImages { get; set; }

        public ScriptWriter()
        {
            DataContext = this;
            InitializeComponent();
            
            LoadImages();
            CommandList.ItemsSource = LoadCommands();
            //FaultList.ItemsSource = LoadFaults();

            initialiseScript(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
            fillScriptForm();
        }

        private List<ScriptImage> LoadImages()
        {

            ScriptImages = new List<ScriptImage>();
            ScriptImages.Add(new ScriptImage("No Image", "noimage.png"));
            ScriptImages.Add(new ScriptImage("Generator", "generator.png"));
            ScriptImages.Add(new ScriptImage("Mud Pump", "mudpump.png"));
            ScriptImages.Add(new ScriptImage("Tannoy", "tannoy.png"));
            ScriptImages.Add(new ScriptImage("Klaxon", "klaxon.png"));
            ScriptImages.Add(new ScriptImage("Flash", "flash.png"));
            ScriptImages.Add(new ScriptImage("Company Man", "companyman.png"));
            ScriptImages.Add(new ScriptImage("Driller", "driller.png"));
            ScriptImages.Add(new ScriptImage("Roughneck", "roughneck.png"));
            ScriptImages.Add(new ScriptImage("Roustabout", "roustabout.png"));
            ScriptImages.Add(new ScriptImage("Mud Man", "mudman.png"));
            ScriptImages.Add(new ScriptImage("Electrician", "electrician.png"));
            ScriptImages.Add(new ScriptImage("Motor Man", "motorman.png"));
            return (ScriptImages);
        }

        private List<ScriptCommand> LoadCommands()
        {
            ScriptCommands = new List<ScriptCommand>();
            ScriptCommands.Add(new ScriptCommand(null, "Select Command.."));
            ScriptCommands.Add(new ScriptCommand("I", "Instruction (requires resume)"));
            ScriptCommands.Add(new ScriptCommand("M", "Message"));
            ScriptCommands.Add(new ScriptCommand("F", "Add Fault"));
            ScriptCommands.Add(new ScriptCommand("R", "Repair Fault"));
            return (ScriptCommands);
        }

        private List<ScriptFault> LoadFaults()
        {
            string lastSection = "";
            string lastGroup = "";
            ScriptFaults = new List<ScriptFault>();
            ScriptFaults.Add(new ScriptFault("0000", "None", false));

            foreach (KeyValuePair<int, Program.FaultItem> entry in Program.FaultItems)
            {
                Program.FaultItem f = entry.Value;
                if (f.section != lastSection)
                {
                    ScriptFaults.Add(new ScriptFault("0000", f.section, false));
                    lastGroup = "";
                    lastSection = f.section;

                }
                if (f.group != lastGroup)
                {
                    ScriptFaults.Add(new ScriptFault("0000", "    " + f.group, false));
                    lastGroup = f.group;
                }

                ScriptFaults.Add(new ScriptFault(f.id.ToString("D4"), "        [" + f.id.ToString("D4") + "] " + f.name, true));
            }
            return (ScriptFaults);
        }

        #region BUTTONS

        private void save_click(object sender, RoutedEventArgs e)
        {
            currentScript.description = description.Text;

            SaveFileDialog dlg = new SaveFileDialog();
            if (currentScript != null)
            {
                dlg.InitialDirectory = currentScript.path + "\\";
                dlg.FileName = filename.Text + ".SCR";
                dlg.RestoreDirectory = true;
                dlg.Filter = "Script files|*.scr|All files|*.*";
                dlg.DefaultExt = currentScript.extension;
                dlg.OverwritePrompt = true;
                dlg.ValidateNames = true;
            }
            else
            {
                dlg.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\";
                dlg.FileName = filename.Text + ".SCR";
                dlg.RestoreDirectory = true;
                dlg.Filter = "Script files|*.scr|All files|*.*";
                dlg.DefaultExt = ".SCR";
                dlg.OverwritePrompt = true;
                dlg.ValidateNames = true;
            }
            Nullable<bool> result = dlg.ShowDialog();
            if (result == true)
            {
                using (StreamWriter sw = new StreamWriter(dlg.FileName))
                {
                    // description
                    foreach (string l in currentScript.description.GetLines())
                    {
                        sw.WriteLine("/* " + l);
                    }
                    sw.WriteLine("#" + currentScript.version.ToString("0.00"));

                    foreach (ScriptLine s in currentScript.lines)
                    {
                        string txt = s.text.Trim();
                        txt = Program.EncodeScriptText(txt);
                        txt = (String.IsNullOrEmpty(txt) ? "{{empty}}" : txt);
                        sw.WriteLine("{0}:{1}:{2}\t{3}\t{4}\t{5}\t{6}", s.hours, s.minutes, s.seconds, s.fault, s.command, txt, s.image);
                    }
                }
                MessageBox.Show("Script saved!", "Save Script", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void open_click(object sender, RoutedEventArgs e)
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

            // Show open file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                try
                {

                    if (loadScriptFromFile(dlg.FileName))
                    {
                        fillScriptForm();
                    }

                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Open Script", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public bool loadScriptFromFile(string filename)
        {

            currentScript = new Script();

            string[] lines = File.ReadAllLines(filename, Encoding.UTF8);
            // remove comments
            int idx = 0;
            if (lines.GetUpperBound(0) == 0) return (false);
            string d = "";
            bool dflag = true;
            int lnum = 1;

            foreach (string l in lines)
            {
                string c = l.Trim();
                if (c != "")
                {
                    c = l;

                    c = Regex.Replace(c, "/\\*.*", "");
                    c = Regex.Replace(c, "/\\\\.*", "");
                    try
                    {
                        if (l.Substring(0, 1) == "#")
                        {
                            try
                            {
                                currentScript.version = Convert.ToDouble(l.Substring(1));
                            }
                            catch
                            {

                            }
                        }
                        else if (l.Substring(0, 2) == "/*")
                        {
                            if (dflag)
                            {
                                string dl = l.Substring(3);
                                dl = dl.Trim();
                                d += (d != "" ? System.Environment.NewLine : "") + dl;
                            }
                        }
                        else
                        {
                            dflag = false;
                        }

                        if (!dflag)
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

                                if (img == "" || img == null) img = "noimage.png";
                                currentScript.lines.Add(new ScriptLine(lnum++, h.ToString("D2"), m.ToString("D2"), s.ToString("D2"), f.ToString("D4"), b.ToString(), t, img));
                            }
                        }
                    }
                    catch
                    {
                        throw (new Exception("Syntax error on line " + (idx + 1).ToString() + " of " + filename));
                    }

                }

            }
            currentScript.description = d;
            currentScript.path = System.IO.Path.GetDirectoryName(filename);
            currentScript.filename = System.IO.Path.GetFileNameWithoutExtension(filename);
            currentScript.extension = System.IO.Path.GetExtension(filename);
            return (true);
        }

        private void new_click(object sender, RoutedEventArgs e)
        {
            string path;
            if (currentScript != null)
            {
                if (MessageBox.Show("OK to lose current script?", "New Script", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.Cancel)
                {
                    return;
                }
                path = currentScript.path;
            }
            else
            {
                path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }

            initialiseScript(path);
            fillScriptForm();
                

        }

        private void initialiseScript(string path)
        {
            currentScript = new Script();
            currentScript.path = path;
            currentScript.filename = "NEW_SCRIPT";
            currentScript.extension = ".SCR";
            currentScript.description = "MCCLogic Fault Scenario Script" + System.Environment.NewLine +
                                        "DESCRIBE SCRIPT HERE";
            currentScript.lines = new List<ScriptLine>();
        }

        private void fillScriptForm()
        {
            filename.Text = currentScript.filename;
            description.Text = currentScript.description;
            ScriptGrid.ItemsSource = currentScript.lines;
            ScriptGrid.Refresh();
        }

        #endregion

        #region CONTEXT MENU
        private void Row_Delete(object sender, RoutedEventArgs e)
        {
            //Get the clicked MenuItem
            MenuItem menuItem = (MenuItem)sender;

            //Get the ContextMenu to which the menuItem belongs
            ContextMenu contextMenu = (ContextMenu)menuItem.Parent;

            //Find the placementTarget
            DataGrid item = (DataGrid)contextMenu.PlacementTarget;

            //Get the underlying item, that you cast to your object that is bound
            //to the DataGrid (and has subject and state as property)
            ScriptLine toDeleteFromBindedList = (ScriptLine)item.SelectedCells[0].Item;

            //Remove the toDeleteFromBindedList object from your ObservableCollection
            currentScript.lines.Remove(toDeleteFromBindedList);

            reIndexLines();
            ScriptGrid.Items.Refresh();
        }

        private void Row_Insert_Before(object sender, RoutedEventArgs e)
        {
            //Get the clicked MenuItem
            MenuItem menuItem = (MenuItem)sender;

            //Get the ContextMenu to which the menuItem belongs
            ContextMenu contextMenu = (ContextMenu)menuItem.Parent;

            //Find the placementTarget
            DataGrid item = (DataGrid)contextMenu.PlacementTarget;         

            currentScript.lines.Insert(item.SelectedIndex, new ScriptLine(0, "00","00", "10", "0000", "F", "Fault description", "noimage.png"));
            reIndexLines();
            ScriptGrid.Items.Refresh();
        }

        private void Row_Insert_After(object sender, RoutedEventArgs e)
        {
            //Get the clicked MenuItem
            MenuItem menuItem = (MenuItem)sender;

            //Get the ContextMenu to which the menuItem belongs
            ContextMenu contextMenu = (ContextMenu)menuItem.Parent;

            //Find the placementTarget
            DataGrid item = (DataGrid)contextMenu.PlacementTarget;

            if (item.SelectedIndex + 1 == currentScript.lines.Count)
            {
                currentScript.lines.Add(new ScriptLine(0, "00", "00", "10", "0000", "F", "Fault description", "noimage.png"));
            }
            else
            {
                currentScript.lines.Insert(item.SelectedIndex + 1, new ScriptLine(0, "00", "00", "10", "0000", "F", "Fault description", "noimage.png"));
            }
            
            reIndexLines();
            ScriptGrid.Items.Refresh();
        }

        private void Row_Add(object sender, RoutedEventArgs e)
        {
            //Get the clicked MenuItem
            MenuItem menuItem = (MenuItem)sender;

            //Get the ContextMenu to which the menuItem belongs
            ContextMenu contextMenu = (ContextMenu)menuItem.Parent;

            //Find the placementTarget
            DataGrid item = (DataGrid)contextMenu.PlacementTarget;

            currentScript.lines.Add(new ScriptLine(0, "00", "00", "10", "0000", "F", "Fault description", "noimage.png"));
            reIndexLines();
            ScriptGrid.Items.Refresh();
        }

        private void Row_Move_Start(object sender, RoutedEventArgs e)
        {
            //Get the clicked MenuItem
            MenuItem menuItem = (MenuItem)sender;

            //Get the ContextMenu to which the menuItem belongs
            ContextMenu contextMenu = (ContextMenu)menuItem.Parent;

            //Find the placementTarget
            DataGrid item = (DataGrid)contextMenu.PlacementTarget;

            //Get the underlying item, that you cast to your object that is bound
            //to the DataGrid (and has subject and state as property)
            ScriptLine toDeleteFromBindedList = (ScriptLine)item.SelectedCells[0].Item;

            //Remove the toDeleteFromBindedList object from your ObservableCollection
            currentScript.lines.Remove(toDeleteFromBindedList);
            currentScript.lines.Insert(0, toDeleteFromBindedList);

            reIndexLines();
            ScriptGrid.Items.Refresh();
        }

        private void Row_Move_End(object sender, RoutedEventArgs e)
        {
            //Get the clicked MenuItem
            MenuItem menuItem = (MenuItem)sender;

            //Get the ContextMenu to which the menuItem belongs
            ContextMenu contextMenu = (ContextMenu)menuItem.Parent;

            //Find the placementTarget
            DataGrid item = (DataGrid)contextMenu.PlacementTarget;

            //Get the underlying item, that you cast to your object that is bound
            //to the DataGrid (and has subject and state as property)
            ScriptLine toDeleteFromBindedList = (ScriptLine)item.SelectedCells[0].Item;

            //Remove the toDeleteFromBindedList object from your ObservableCollection
            currentScript.lines.Remove(toDeleteFromBindedList);
            currentScript.lines.Add(toDeleteFromBindedList);

            reIndexLines();
            ScriptGrid.Items.Refresh();
        }

        private void Row_Move_Down(object sender, RoutedEventArgs e)
        {
            //Get the clicked MenuItem
            MenuItem menuItem = (MenuItem)sender;

            //Get the ContextMenu to which the menuItem belongs
            ContextMenu contextMenu = (ContextMenu)menuItem.Parent;

            //Find the placementTarget
            DataGrid item = (DataGrid)contextMenu.PlacementTarget;

            //Get the underlying item, that you cast to your object that is bound
            //to the DataGrid (and has subject and state as property)
            ScriptLine toDeleteFromBindedList = (ScriptLine)item.SelectedCells[0].Item;

            if (item.SelectedIndex < currentScript.lines.Count - 1)
            {


                if (item.SelectedIndex + 1 == currentScript.lines.Count)
                {
                    //Remove the toDeleteFromBindedList object from your ObservableCollection
                    currentScript.lines.Remove(toDeleteFromBindedList);
                    currentScript.lines.Add(toDeleteFromBindedList);
                }
                else
                {
                    //Remove the toDeleteFromBindedList object from your ObservableCollection
                    currentScript.lines.Remove(toDeleteFromBindedList);
                    currentScript.lines.Insert(item.SelectedIndex + 1, toDeleteFromBindedList);
                }
            }
            reIndexLines();
            ScriptGrid.Items.Refresh();
        }

        private void Row_Move_Up(object sender, RoutedEventArgs e)
        {
            //Get the clicked MenuItem
            MenuItem menuItem = (MenuItem)sender;

            //Get the ContextMenu to which the menuItem belongs
            ContextMenu contextMenu = (ContextMenu)menuItem.Parent;

            //Find the placementTarget
            DataGrid item = (DataGrid)contextMenu.PlacementTarget;

            if (item.SelectedIndex > 0)
            {
                //Get the underlying item, that you cast to your object that is bound
                //to the DataGrid (and has subject and state as property)
                ScriptLine toDeleteFromBindedList = (ScriptLine)item.SelectedCells[0].Item;

                //Remove the toDeleteFromBindedList object from your ObservableCollection
                currentScript.lines.Remove(toDeleteFromBindedList);
                currentScript.lines.Insert(item.SelectedIndex - 1, toDeleteFromBindedList);
            }
            reIndexLines();
            ScriptGrid.Items.Refresh();

        }
        #endregion

        private void reIndexLines()
        {
            int idx = 1;
            foreach (ScriptLine l in currentScript.lines)
            {
                l.line = idx++;
            }
        }

        private void ScriptGrid_GotFocus(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource.GetType() == typeof(DataGridCell))
            {
                // Starts the Edit on the row;
                DataGrid grd = (DataGrid)sender;
                grd.BeginEdit(e);
            }
        }

        private void ScriptGrid_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            DataGrid g = sender as DataGrid;
            g.CommitEdit();
            g.CommitEdit();
        }

        private void comboSeconds_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox c = sender as ComboBox;
            var x = c;
        }

        public bool IsEditing { get; set; }
        public bool IsDragging { get; set; }

        public ScriptLine DraggedItem { get; set; }

        private void ScriptGrid_CellBeginEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            IsEditing = true;
            if (IsDragging) ResetDragDrop();
        }

        private void ScriptGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            IsEditing = false;
        }

        private void ScriptGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //exit if in edit mode
            if (IsEditing) return;

            //find the clicked row
            var row = UIHelpers.TryFindFromPoint<DataGridRow>((UIElement)sender, e.GetPosition(ScriptGrid));
            if (row == null) return;

            //set flag that indicates we're capturing mouse movements
            IsDragging = true;
            DraggedItem = (ScriptLine)row.Item;
            popText.Text = (DraggedItem.hours + ":" + DraggedItem.minutes + ":" + DraggedItem.text);
            if (popText.Text.Length > 20)
            {
                popText.Text = popText.Text.Substring(0, 20) + "...";
            }
        }

        private void ScriptGrid_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!IsDragging || IsEditing)
            {
                return;
            }
            //get the target item
            ScriptLine targetItem = (ScriptLine)ScriptGrid.SelectedItem;

            if (targetItem == null || !ReferenceEquals(DraggedItem, targetItem))
            {
                
                //remove the source from the list
                currentScript.lines.Remove(DraggedItem);

                //get target index
                var targetIndex = currentScript.lines.IndexOf(targetItem);

                //move source at the target's location
                currentScript.lines.Insert(targetIndex, DraggedItem);

                //select the dropped item
                ScriptGrid.SelectedItem = DraggedItem;
                reIndexLines();
                ScriptGrid.Items.Refresh();
            }

            //reset
            ResetDragDrop();
        }

        /// <summary>
        /// Updates the popup's position in case of a drag/drop operation.
        /// </summary>
        private void ScriptGrid_OnMouseMove(object sender, MouseEventArgs e)
        {
            if (!IsDragging || e.LeftButton != MouseButtonState.Pressed) return;
            //display the popup if it hasn't been opened yet
            if (!dragPopup.IsOpen)
            {
                //switch to read-only mode
                ScriptGrid.IsReadOnly = true;

                //make sure the popup is visible
                dragPopup.IsOpen = true;
            }


            Size popupSize = new Size(dragPopup.ActualWidth, dragPopup.ActualHeight);
            dragPopup.PlacementRectangle = new Rect(e.GetPosition(this), popupSize);

            //make sure the row under the grid is being selected
            Point position = e.GetPosition(ScriptGrid);
            var row = UIHelpers.TryFindFromPoint<DataGridRow>(ScriptGrid, position);
            if (row != null) ScriptGrid.SelectedItem = row.Item;
        }

        private void ResetDragDrop()
        {
            IsDragging = false;
            dragPopup.IsOpen = false;
            ScriptGrid.IsReadOnly = false;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            EditFile = currentScript.path + "\\" + currentScript.filename + currentScript.extension;
        }

        private void combo_DropDownOpened(object sender, EventArgs e)
        {
            IsEditing = true;
            if (IsDragging) ResetDragDrop();
        }

        private void combo_DropDownClosed(object sender, EventArgs e)
        {
            IsEditing = false;
        }

        private void faultControl_Loaded(object sender, RoutedEventArgs e)
        {
            FaultItemControlDataGrid f1 = sender as FaultItemControlDataGrid;
            FaultItemControl f = f1.FaultControl;

            ScriptLine l  = f1.DataContext as ScriptLine;
            f.SelectItem(l.fault);

            var x = e.Source;

        }

        private void ImageComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            Debug.Log("loaded");
            ComboBox c = sender as ComboBox;
            c.ItemsSource = ScriptImages;
        }

    }
}

