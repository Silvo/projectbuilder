using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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

namespace ProjectBuider
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Dictionary<string, string> fields = new Dictionary<string, string>();
        List<Tuple<string, string, string>> symLinks = new List<Tuple<string, string, string>>();
        List<Tuple<string, string>> extApps = new List<Tuple<string, string>>();

        bool _isResizing;
        Point _oldMousePos;
        UIElement _mouseHolder;

        public MainWindow()
        {
            InitializeComponent();
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

                    this.Width += delta.X;
                    this.Height += delta.Y;

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


        private void getFields()
        {
            XmlElement projectType = this.cbProjectType.SelectedItem as XmlElement;
            fields.Add("Project type", projectType.GetAttribute("name"));

            for (int i = 0; i < icItems.Items.Count; i++)
            {
                UIElement elem = (UIElement)icItems.ItemContainerGenerator.ContainerFromIndex(i);
                DockPanel dock = (DockPanel)VisualTreeHelper.GetChild(elem, 0);

                var childCount = VisualTreeHelper.GetChildrenCount(dock);
                string fieldName = "";
                string fieldContent = "";
                for (int j = 0; j < childCount; j++)
                {
                    var child = VisualTreeHelper.GetChild(dock, j);

                    Label l = child as Label;
                    if (l != null)
                    {
                        XmlAttribute xmlContent = l.Content as XmlAttribute;
                        if (xmlContent != null)
                        {
                            fieldName = xmlContent.Value;
                        }
                        else
                        {
                            fieldName = l.Content.ToString();
                        }
                    }

                    TextBox tb = child as TextBox;
                    if (tb != null)
                    {
                        fieldContent = tb.Text;
                    }
                }

                if (!(String.IsNullOrEmpty(fieldName) || String.IsNullOrEmpty(fieldContent)))
                {
                    fields.Add(fieldName, fieldContent);
                }
            }
        }

        private void getOperationsFromXml(string filename)
        {
            validateXml(filename);

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(filename);

            XmlNode projectTypeNode = null;
            var pTypes = xmlDoc.SelectNodes("/project_types/project_type");
            for (int i = 0; i < pTypes.Count; i++)
            {
                if (pTypes.Item(i).Attributes["name"].Value == fields["Project type"])
                {
                    projectTypeNode = pTypes.Item(i);
                }
            }

            foreach (XmlNode item in projectTypeNode.ChildNodes)
            {
                if (item.Name == "templates")
                {
                    fields.Add("Template folder", item.Attributes["folder"].Value);
                }
            }
        }

        private void copyAndReplace(string oldPath, string newPath, string[] oldString, string[] newString)
        {
            var contents = File.ReadAllText(oldPath);
            for (int i = 0; i < oldString.Length; i++)
            {
                contents = contents.Replace(oldString[i], newString[i]);
            }
            File.WriteAllText(newPath, contents);
        }

        private void validateXml(string filename)
        {
            string schFilename = System.IO.Path.GetFileNameWithoutExtension(filename) + ".xsd";

            XmlSchemaSet schemas = new XmlSchemaSet();
            schemas.Add(null, schFilename);
            XDocument doc = XDocument.Load(filename);

            doc.Validate(schemas, (o, e) =>
            {
                throw new XmlSchemaException(e.Message);
            });
        }

        private void bOk_Click(object sender, RoutedEventArgs e)
        {
            getFields();
            getOperationsFromXml("projects.xml");
            validateOperations();
            executeOperations();
        }

        private void executeOperations()
        {
            copyAndModifyTemplates();
            createSymLinks();
            executeExternalApps();
        }

        private void executeExternalApps()
        {
            throw new NotImplementedException();
        }

        private void createSymLinks()
        {
            throw new NotImplementedException();
        }

        private void copyAndModifyTemplates()
        {
            // get all template files
            // filter all files that need string replacements and do those using copyAndReplace
            // just copy the rest of the files to the destination folder
        }

        private void validateOperations()
        {
            // verify that templates, symlinks and external apps have all the required fields filled
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
