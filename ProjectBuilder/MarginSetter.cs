using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace ProjectBuilder
{
    /* Thanks for Elad Katz for providing this handy helper class!
     * http://blogs.microsoft.co.il/eladkatz/2011/05/29/what-is-the-easiest-way-to-set-spacing-between-items-in-stackpanel/
     */

    public class MarginSetter
    {
        public static Thickness GetMargin(DependencyObject obj)
        {
            return (Thickness)obj.GetValue(MarginProperty);
        }

        public static void SetMargin(DependencyObject obj, Thickness value)
        {
            obj.SetValue(MarginProperty, value);
        }

        public static readonly DependencyProperty MarginProperty =
            DependencyProperty.RegisterAttached("Margin", typeof(Thickness), typeof(MarginSetter),
            new UIPropertyMetadata(new Thickness(), MarginChangedCallback));

        public static void MarginChangedCallback(object sender, DependencyPropertyChangedEventArgs e)
        {
            var panel = sender as Panel;
            if (panel == null) return;

            panel.Loaded += new RoutedEventHandler(panel_Loaded);
        }

        static void panel_Loaded(object sender, RoutedEventArgs e)
        {
            var panel = sender as Panel;

            foreach (var child in panel.Children)
            {
                var fe = child as FrameworkElement;
                if (fe == null) continue;

                fe.Margin = MarginSetter.GetMargin(panel);
            }
        }
    }
}
