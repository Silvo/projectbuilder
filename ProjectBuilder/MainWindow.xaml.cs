﻿using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace ProjectBuilder
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        [DllImport("kernel32.dll")]
        static extern bool CreateSymbolicLink(
            string lpSymlinkFileName, string lpTargetFileName, SymbolicLink dwFlags);
        [DllImport("kernel32.dll")]
        public static extern uint GetLastError();

        enum SymbolicLink
        {
            File = 0,
            Directory = 1
        }

        Dictionary<string, string> _fields = new Dictionary<string, string>();
        List<Tuple<string, string, string>> _replacements = new List<Tuple<string, string, string>>();
        List<Tuple<string, string>> _renames = new List<Tuple<string, string>>();
        List<Tuple<string, string>> _symLinks = new List<Tuple<string, string>>();
        List<Tuple<string, string>> _extApps = new List<Tuple<string, string>>();

        bool _isResizing;
        Point _oldMousePos;
        UIElement _mouseHolder;

        public MainWindow()
        {
            InitializeComponent();
            
            XmlDataProvider dp = Resources["ProjectData"] as XmlDataProvider;
            FileInfo f = new FileInfo("projects.xml");
            dp.Document = new XmlDocument();
            dp.Source = new Uri(f.FullName);

            this.CommandBindings.Add(new CommandBinding(SystemCommands.CloseWindowCommand, OnCloseWindow));

            this.MouseDown += MainWindow_MouseDown;
            this.PreviewMouseUp += MainWindow_PreviewMouseUp;
            this.MouseMove += MainWindow_MouseMove;
        }

        void MainWindow_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isResizing)
            {
                if (e.LeftButton == MouseButtonState.Released)
                {
                    _isResizing = false;
                    this.Cursor = Cursors.Arrow;
                    _mouseHolder.ReleaseMouseCapture();
                }
                else
                {
                    Point newMousePos = e.GetPosition((Window)sender);
                    var delta = newMousePos - _oldMousePos;

                    if (this.Width + delta.X > this.MinWidth)
                    {
                        this.Width += delta.X;
                    }

                    if (this.Height + delta.Y > this.MinHeight)
                    {
                        this.Height += delta.Y;
                    }
                    

                    _oldMousePos = newMousePos;
                }
            }
            else
            {
                Grid g = e.OriginalSource as Grid;

                if (g != null && g.Name == "ResizeGrip")
                {
                    this.Cursor = Cursors.SizeNWSE;
                }
                else
                {
                    this.Cursor = Cursors.Arrow;
                }
            }
        }

        void MainWindow_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isResizing)
            {
                _mouseHolder.ReleaseMouseCapture();
                this.Cursor = Cursors.Arrow;
                _isResizing = false;
            }
            e.Handled = false;
        }

        void MainWindow_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                Grid g = e.OriginalSource as Grid;

                if (g != null && g.Name == "ResizeGrip")
                {
                    _oldMousePos = e.GetPosition((Window)sender);
                    _isResizing = true;
                    _mouseHolder = g;
                    this.Cursor = Cursors.SizeNWSE;
                    g.CaptureMouse();
                }
                else
                {
                    this.DragMove();
                }
            }
        }

        private void OnCloseWindow(object target, ExecutedRoutedEventArgs e)
        {
            SystemCommands.CloseWindow(this);
        }

        private bool getFields()
        {
            XmlElement projectType = this.cbProjectType.SelectedItem as XmlElement;
            if (projectType != null)
            {
                _fields.Add("Project type", projectType.GetAttribute("name"));

                for (int i = 0; i < icItems.Items.Count; i++)
                {
                    UIElement elem = (UIElement)icItems.ItemContainerGenerator.ContainerFromIndex(i);
                    DockPanel dock = (DockPanel)VisualTreeHelper.GetChild(elem, 0);

                    Label l = VisualTreeHelper.GetChild(dock, 0) as Label;
                    if (l != null)
                    {
                        XmlAttribute xmlContent = l.Content as XmlAttribute;
                        if (xmlContent != null)
                        {
                            XmlElement host = xmlContent.OwnerElement;
                            if (host != null)
                            {
                                string hostName = host.ParentNode.Name;
                                string name = xmlContent.Value;
                                string content;

                                if (hostName == "text_field" || hostName == "number_field")
                                {
                                    content = ((TextBox)VisualTreeHelper.GetChild(dock, 1)).Text;

                                }
                                else if (hostName == "folder_field" || hostName == "file_field")
                                {
                                    content = ((TextBox)VisualTreeHelper.GetChild(dock, 2)).Text;
                                }
                                else
                                {
                                    throw new XmlException("Unknown XML element type: \"" + hostName + "\"");
                                }
                                _fields.Add(name, content);
                            }
                        }
                        else
                        {
                            string elementType = l.Content as String;

                            if (elementType == "Replacement")
                            {
                                // Store ext, from & to
                                string ext = ((TextBox)VisualTreeHelper.GetChild(dock, 2)).Text;
                                string from = ((TextBox)VisualTreeHelper.GetChild(dock, 4)).Text;
                                string to = ((TextBox)VisualTreeHelper.GetChild(dock, 6)).Text;
                                _replacements.Add(new Tuple<string, string, string>(ext, from, to));
                            }
                            else if (elementType == "Symbolic link")
                            {
                                // Store name & target
                                string name = ((TextBox)VisualTreeHelper.GetChild(dock, 5)).Text;
                                string target = ((TextBox)VisualTreeHelper.GetChild(dock, 2)).Text;
                                _symLinks.Add(new Tuple<string, string>(name, target));
                            }
                            else if (elementType == "Execute program")
                            {
                                // Store path & args
                                string path = ((TextBox)VisualTreeHelper.GetChild(dock, 1)).Text;
                                string args = ((TextBox)VisualTreeHelper.GetChild(dock, 4)).Text;
                                _extApps.Add(new Tuple<string, string>(path, args));
                            }
                            else if (elementType == "File rename")
                            {
                                // Store path & args
                                string from = ((TextBox)VisualTreeHelper.GetChild(dock, 2)).Text;
                                string to = ((TextBox)VisualTreeHelper.GetChild(dock, 4)).Text;
                                _renames.Add(new Tuple<string, string>(from, to));
                            }
                            else
                            {
                                throw new NotSupportedException("Unknown element type: \"" + elementType + "\"");
                            }
                        }
                    }
                }
                return true;
            }
            return false;
        }

        private void bOk_Click(object sender, RoutedEventArgs e)
        {
            bool hasFields = getFields();

            if (hasFields)
            {
                bool valid = false;
                string warnings, errors;

                validateOperations(out warnings, out errors);
                if (!String.IsNullOrEmpty(errors))
                {
                    MessageBox.Show(errors, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else if (!String.IsNullOrEmpty(warnings))
                {
                    // pop up a dialog with list of warnings and abort & continue buttons
                    MessageBoxResult result = MessageBox.Show(warnings, "Warning", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                    if (result == MessageBoxResult.OK)
                    {
                        valid = true;
                    }
                }
                else
                {
                    valid = true;
                }

                if (valid)
                {
                    executeOperations();
                }
                
                // Empty the containers
                _fields = new Dictionary<string, string>();
                _replacements = new List<Tuple<string, string, string>>();
                _renames = new List<Tuple<string, string>>();
                _symLinks = new List<Tuple<string, string>>();
                _extApps = new List<Tuple<string, string>>();
            }
        }

        private void validateOperations(out string warnings, out string errors)
        {
            warnings = "";
            errors = "";

            string projectPath;
            try
            {
                projectPath = _fields["Project path"];
            }
            catch (KeyNotFoundException)
            {
                errors += "No field with name \"Project path\"!\n";
                return;
            }

            // Validate project path (not empty, legal and doesn't exist)
            if (String.IsNullOrWhiteSpace(projectPath))
            {
                errors += "Project path is empty!\n";
            }
            else
            {
                try
                {
                    var pathInfo = new FileInfo(projectPath);
                    if (Directory.Exists(projectPath))
                    {
                        warnings += "The project folder already exists!\n";
                    }
                }
                catch (Exception)
                {
                    errors += "Project path is not valid!\n";
                }
            }
            
            // Validate template path
            string templatePath;
            try
            {
                templatePath = _fields["Template path"];
            }
            catch (KeyNotFoundException)
            {
                errors += "No field with name \"Template path\"!\n";
                return;
            }

            if (String.IsNullOrWhiteSpace(templatePath))
            {
                errors += "Template path is empty!\n";
            }
            else
            {
                try
                {
                    var pathInfo = new FileInfo(templatePath);
                    Directory.Exists(templatePath);
                }
                catch (Exception)
                {
                    errors += "Template path is not valid!\n";
                }
            }

            // Validate text replacements (no empty fields)
            foreach (var r in _replacements)
            {
                if (String.IsNullOrWhiteSpace(r.Item1) ||
                    String.IsNullOrWhiteSpace(r.Item2) ||
                    String.IsNullOrWhiteSpace(r.Item3))
                {
                    warnings += "Empty fields in text replacements!\n";
                }
            }

            // Validate file renaming (no empty fields, no illegal characters)
            foreach (var r in _renames)
            {
                if (String.IsNullOrWhiteSpace(r.Item1) ||
                    String.IsNullOrWhiteSpace(r.Item2))
                {
                    warnings += "Empty fields in file renames!\n";
                }

                if (!String.IsNullOrWhiteSpace(r.Item2))
                {
                    string invalidChars = new string( System.IO.Path.GetInvalidFileNameChars());
                    Regex regexp = new Regex("["+Regex.Escape(invalidChars)+"]");

                    if (regexp.IsMatch(r.Item2))
                    {
                        errors += "Illegal characters in new filename!\n";
                    }
                }
            }

            // Validate symlinks (no empty fields and target exists)
            foreach (var s in _symLinks)
            {
                if (String.IsNullOrWhiteSpace(s.Item1) ||
                    String.IsNullOrWhiteSpace(s.Item2))
                {
                    warnings += "Empty fields in symbolic links!\n";
                }
                else if ((!File.Exists(s.Item2)) && (!Directory.Exists(s.Item2)))
                {
                    errors += "Symbolic link target doesn't exist!\n";
                }
            }

            // Validate external program invocations (path/name is not empty)
            foreach (var a in _extApps)
            {
                if (String.IsNullOrWhiteSpace(a.Item1))
                {
                    warnings += "Empty program name field in external program invocations!\n";
                }
            }
        }

        private void executeOperations()
        {
            copyAndModifyFiles();
            createSymLinks();
            executeExternalApps();
        }

        private void executeExternalApps()
        {
            foreach (var app in _extApps)
            {
                if (!String.IsNullOrWhiteSpace(app.Item1))
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo(app.Item1);
                    startInfo.Arguments = app.Item2;
                    startInfo.UseShellExecute = false;
                    startInfo.WorkingDirectory = _fields["Project path"];

                    /* NOTE: Doesn't support batch files? */

                    try
                    {
                        Process.Start(startInfo);
                    }
                    catch (Exception e)
                    {
                        String errorMsg = String.Format("Error while executing program \"{0} {1}\":\n\"{2}\"", app.Item1, app.Item2, e.Message);
                        MessageBox.Show(errorMsg, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void createSymLinks()
        {
            foreach (var s in _symLinks)
            {
                if (!String.IsNullOrWhiteSpace(s.Item1) && !String.IsNullOrWhiteSpace(s.Item2))
                {
                    String linkName = System.IO.Path.Combine(_fields["Project path"], s.Item1);
                    bool ret;

                    if (File.Exists(s.Item2))
                    {
                        ret = CreateSymbolicLink(linkName, s.Item2, SymbolicLink.File);
                    }
                    else
                    {
                        ret = CreateSymbolicLink(linkName, s.Item2, SymbolicLink.Directory);
                    }


                    uint error = GetLastError();
                }
            }
        }

        private void copyAndModifyFiles()
        {
            // get all template files
            // filter all files that need string replacements and do those using copyAndReplace
            // just copy the rest of the files to the destination folder

            // ...or just copy all and replace later
            copyFolder(_fields["Template path"], _fields["Project path"]);
            foreach (var r in _replacements)
            {
                if (!String.IsNullOrWhiteSpace(r.Item1) && !String.IsNullOrWhiteSpace(r.Item2))
                {
                    string[] files = Directory.GetFiles(_fields["Project path"], "*." + r.Item1, SearchOption.AllDirectories);
                    foreach (var file in files)
                    {
                        replaceText(file, r.Item2, r.Item3);
                    }
                }
            }

            foreach (var r in _renames)
            {
                if (!String.IsNullOrWhiteSpace(r.Item1))
                {
                    renameFiles(r.Item1, r.Item2);
                }
            }
        }

        private void renameFiles(string source, string dest)
        {
            string[] files = Directory.GetFiles(_fields["Project path"], "*"+source+"*.*", SearchOption.AllDirectories);
            foreach (var oldName in files)
            {
                string newName = oldName.Replace(source, dest);
                File.Move(oldName, newName);
            }
        }

        private void copyFolder(string source, string dest)
        {
            DirectoryInfo dir = new DirectoryInfo(source);
            DirectoryInfo[] dirs = dir.GetDirectories();

            if (!Directory.Exists(dest))
            {
                Directory.CreateDirectory(dest);
            }

            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = System.IO.Path.Combine(dest, file.Name);
                file.CopyTo(temppath, false);
            }

            
            foreach (DirectoryInfo subdir in dirs)
            {
                string newDest = System.IO.Path.Combine(dest, subdir.Name);
                copyFolder(subdir.FullName, newDest);
            }
        }

        private void replaceText(string filePath, string source, string dest)
        {
            var contents = File.ReadAllText(filePath);
            contents = contents.Replace(source, dest);
            /*
            for (int i = 0; i < oldString.Length; i++)
            {
                contents = contents.Replace(oldString[i], newString[i]);
            }*/
            File.WriteAllText(filePath, contents);
        }

        private void bCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void bSelectFolder_Click(object sender, RoutedEventArgs e)
        {
            DependencyObject parentObject = VisualTreeHelper.GetParent((DependencyObject)sender);
            DockPanel dock = parentObject as DockPanel;

            if (dock != null)
            {
                var tb = dock.Children.OfType<TextBox>().First();
                
                var dialog = new System.Windows.Forms.FolderBrowserDialog();
                dialog.SelectedPath = tb.Text;
                var res = dialog.ShowDialog();
                
                if (res == System.Windows.Forms.DialogResult.OK)
                {
                    tb.Text = dialog.SelectedPath;
                }
            }
        }

        private void bSelectFile_Click(object sender, RoutedEventArgs e)
        {
            DependencyObject parentObject = VisualTreeHelper.GetParent((DependencyObject)sender);

            DockPanel dock = parentObject as DockPanel;

            if (dock != null)
            {
                var tb = dock.Children.OfType<TextBox>().First();

                var dialog = new OpenFileDialog();
                try
                {
                    dialog.InitialDirectory = System.IO.Path.GetDirectoryName(tb.Text);
                }
                catch (Exception)
                {
                    
                }
                
                var res = dialog.ShowDialog();

                if (res == true)
                {
                    tb.Text = dialog.FileName;
                }
            }
        }
    }
}
