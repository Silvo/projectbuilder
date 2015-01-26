﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ProjectBuider
{
    public enum LinkedTextBoxType
    {
        Normal,
        Number
    }

    public class LinkedTextBox : TextBox
    {
        private static List<LinkedTextBox> _links = new List<LinkedTextBox>();
        private string _linkedContent1 = "";
        private string _linkedContent2 = "";

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

        public string TextFormat
        {
            get { return (string)GetValue(TextFormatProperty); }
            set { SetValue(TextFormatProperty, value); }
        }

        public string WriteLink
        {
            get { return (string)GetValue(WriteLinkProperty); }
            set { SetValue(WriteLinkProperty, value); }
        }
        public string ReadLink1
        {
            get { return (string)GetValue(ReadLink1Property); }
            set { SetValue(ReadLink1Property, value); }
        }
        public string ReadLink2
        {
            get { return (string)GetValue(ReadLink2Property); }
            set { SetValue(ReadLink2Property, value); }
        }

        //TODO: Separate the number textbox into its own class.
        //      A common base class with link list and necessary functionality
        //      and two? subclasses that handle the different box types.
        public LinkedTextBoxType BoxType
        {
            get { return (LinkedTextBoxType)GetValue(BoxTypeProperty); }
            set { SetValue(BoxTypeProperty, value); }
        }

        public static readonly DependencyProperty TextStyleProperty =
            DependencyProperty.Register("TextStyle", typeof(string), typeof(LinkedTextBox),
            new PropertyMetadata("none"));

        public static readonly DependencyProperty TextFormatProperty =
            DependencyProperty.Register("TextFormat", typeof(string), typeof(LinkedTextBox),
            new PropertyMetadata("", OnTextFormatChanged));

        public static readonly DependencyProperty WriteLinkProperty =
            DependencyProperty.Register("WriteLink", typeof(string), typeof(LinkedTextBox),
            new PropertyMetadata("", OnWriteLinkChanged));

        public static readonly DependencyProperty ReadLink1Property =
            DependencyProperty.Register("ReadLink1", typeof(string), typeof(LinkedTextBox),
            new PropertyMetadata("", OnReadLinkChanged));

        public static readonly DependencyProperty ReadLink2Property =
            DependencyProperty.Register("ReadLink2", typeof(string), typeof(LinkedTextBox),
            new PropertyMetadata("", OnReadLinkChanged));

        public static readonly DependencyProperty BoxTypeProperty =
            DependencyProperty.Register("BoxType", typeof(LinkedTextBoxType), typeof(LinkedTextBox),
            new PropertyMetadata(LinkedTextBoxType.Normal, OnBoxTypeChanged));

        public LinkedTextBox()
        {
            _links.Add(this);
            base.TextChanged += LinkedTextBox_TextChanged;
        }

        private static void OnWriteLinkChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            LinkedTextBox myLink = d as LinkedTextBox;

            myLink.WriteLink = e.NewValue as string;
            UpdateLinkedBoxes(myLink);
        }

        private void LinkedTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateLinkedBoxes(this);
        }

        private static void UpdateLinkedBoxes(LinkedTextBox myLink)
        {
            if (!String.IsNullOrWhiteSpace(myLink.WriteLink))
            {
                foreach (LinkedTextBox link in _links)
                {
                    if (link.ReadLink1 == myLink.WriteLink)
                    {
                        link._linkedContent1 = myLink.Text;
                        link.updateContents();
                    }
                    if (link.ReadLink2 == myLink.WriteLink)
                    {
                        link._linkedContent2 = myLink.Text;
                        link.updateContents();
                    }
                }
            }
        }

        private static void OnReadLinkChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            LinkedTextBox myLink = d as LinkedTextBox;
            string newValue = e.NewValue as string;
            DependencyProperty dp = e.Property;

            if (dp == ReadLink1Property)
            {
                myLink.ReadLink1 = newValue;
            }
            else
            {
                myLink.ReadLink2 = newValue;
            }

            // A read link ID has been updated so search for a write link with that ID and update contents
            if (!String.IsNullOrWhiteSpace(newValue))
            {
                foreach (LinkedTextBox link in _links)
                {
                    if ((!String.IsNullOrWhiteSpace(link.WriteLink)) && (link.WriteLink == newValue))
                    {
                        if (dp == ReadLink1Property)
                        {
                            myLink._linkedContent1 = link.Text;
                        }
                        else
                        {
                            myLink._linkedContent2 = link.Text;
                        }
                        myLink.updateContents();
                        
                        break;
                    }
                }
            }
        }

        private static void OnTextFormatChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            LinkedTextBox myLink = d as LinkedTextBox;
            myLink.updateContents();
        }

        private static void OnBoxTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            LinkedTextBox myLink = d as LinkedTextBox;
            myLink.updateContents();
        }

        private void updateContents()
        {
            string newText = "";

            if (this.BoxType == LinkedTextBoxType.Normal)
            {
                if (String.IsNullOrWhiteSpace(this.TextFormat))
                {
                    newText = _linkedContent1 + _linkedContent2;
                }
                else
                {
                    //TODO: Not so idiotic conversion
                    if (!String.IsNullOrWhiteSpace(_linkedContent1))
                    {
                        newText = String.Format(this.TextFormat, Convert.ToInt32(_linkedContent1), _linkedContent2);
                    }
                    
                }

                if (this.TextStyle == "caps")
                {
                    newText = newText.Replace(" ", "_");
                    newText = newText.ToUpperInvariant();
                }
                else if (this.TextStyle == "underscore")
                {
                    newText = newText.Replace(" ", "_");
                    newText = newText.ToLowerInvariant();
                }
                else if (this.TextStyle == "pascal")
                {
                    TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
                    newText = textInfo.ToTitleCase(newText);
                    newText = newText.Replace(" ", "");
                }
            }
            else
            {
                newText = "1";
            }
            
            this.SetValue(TextProperty, newText);
            
            RoutedEventArgs newEventArgs = new RoutedEventArgs(LinkedTextBox.LinkedDataChangedEvent);
            this.RaiseEvent(newEventArgs);
        }
    }
}
