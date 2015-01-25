using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ProjectBuider
{
    public enum LinkType
    {
        Read,
        Write
    }

    public class LinkedTextBox : TextBox
    {
        private static List<LinkedTextBox> _links = new List<LinkedTextBox>();

        public static readonly RoutedEvent LinkedDataChangedEvent =
            EventManager.RegisterRoutedEvent("LinkedDataChanged", RoutingStrategy.Bubble,
            typeof(RoutedEventHandler), typeof(LinkedTextBox));

        public event RoutedEventHandler LinkedDataChanged
        {
            add
            {
                this.AddHandler(LinkedDataChangedEvent, value);
            }

            remove
            {
                this.RemoveHandler(LinkedDataChangedEvent, value);
            }
        }

        public string TextStyle
        {
            get { return (string)GetValue(TextStyleProperty); }
            set { SetValue(TextStyleProperty, value); }
        }

        public LinkType Link
        {
            get { return (LinkType)GetValue(LinkProperty); }
            set { SetValue(LinkProperty, value); }
        }

        public string IdString
        {
            get { return (string)GetValue(IdStringProperty); }
            set { SetValue(IdStringProperty, value); }
        }

        public static readonly DependencyProperty TextStyleProperty =
            DependencyProperty.Register("TextStyle", typeof(string), typeof(LinkedTextBox),
            new PropertyMetadata("none"));

        public static readonly DependencyProperty LinkProperty =
            DependencyProperty.Register("Link", typeof(LinkType), typeof(LinkedTextBox),
            new PropertyMetadata(LinkType.Read, OnLinkChanged));

        public static readonly DependencyProperty IdStringProperty =
            DependencyProperty.Register("IdString", typeof(string), typeof(LinkedTextBox),
            new PropertyMetadata("", OnIdStringChanged));

        public LinkedTextBox()
        {
            _links.Add(this);
            base.TextChanged += LinkedTextBox_TextChanged;
        }

        private static void OnIdStringChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            LinkedTextBox ltb = d as LinkedTextBox;

            ltb.IdString = e.NewValue as string;

            if (ltb.Link == LinkType.Read)
            {
                CheckForUpdates(ltb);
            }
            else
            {
                UpdateLinkedBoxes(ltb);
            }
        }

        private static void OnLinkChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            LinkedTextBox ltb = d as LinkedTextBox;

            ltb.Link = (LinkType)e.NewValue;

            if (ltb.Link == LinkType.Read)
            {
                CheckForUpdates(ltb);
            }
            else
            {
                UpdateLinkedBoxes(ltb);
            }
        }

        private void LinkedTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (this.Link == LinkType.Write)
            {
                UpdateLinkedBoxes(this);
            }
        }

        private static string getStylizedText(string original, string textStyle)
        {
            string newText;

            if (textStyle == "caps")
            {
                newText = original.Replace(" ", "_");
                newText = newText.ToUpperInvariant();
            }
            else if (textStyle == "underscore")
            {
                newText = original.Replace(" ", "_");
                newText = newText.ToLowerInvariant();
            }
            else if (textStyle == "pascal")
            {
                TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
                newText = textInfo.ToTitleCase(original);
                newText = newText.Replace(" ", "");
            }
            else
            {
                return original;
            }

            return newText;
        }

        private static void UpdateLinkedBoxes(LinkedTextBox myLink)
        {
            foreach (LinkedTextBox link in _links)
            {
                if ((!String.IsNullOrWhiteSpace(link.IdString)) && (link.IdString == myLink.IdString) && (link.Link == LinkType.Read))
                {
                    string newText = getStylizedText(myLink.Text, link.TextStyle);

                    link.SetValue(TextProperty, newText);

                    RoutedEventArgs newEventArgs = new RoutedEventArgs(LinkedTextBox.LinkedDataChangedEvent);
                    link.RaiseEvent(newEventArgs);
                }
            }
        }

        private static void CheckForUpdates(LinkedTextBox myLink)
        {
            foreach (LinkedTextBox link in _links)
            {
                if ((!String.IsNullOrWhiteSpace(link.IdString)) && (link.IdString == myLink.IdString) && (link.Link == LinkType.Write))
                {
                    string newText = getStylizedText(link.Text, myLink.TextStyle);

                    myLink.SetValue(TextProperty, newText);

                    RoutedEventArgs newEventArgs = new RoutedEventArgs(LinkedTextBox.LinkedDataChangedEvent);
                    myLink.RaiseEvent(newEventArgs);
                }
            }
        }
    }
}
