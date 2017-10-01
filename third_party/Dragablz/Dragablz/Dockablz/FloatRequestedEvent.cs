using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Dragablz.Dockablz
{
    public delegate void FloatRequestedEventHandler(object sender, FloatRequestedEventArgs e);

    public class FloatRequestedEventArgs : DragablzItemEventArgs
    {
        public FloatRequestedEventArgs(RoutedEvent routedEvent, object source, DragablzItem dragablzItem) 
            : base(routedEvent, source, dragablzItem)
        { }

        public FloatRequestedEventArgs(RoutedEvent routedEvent, DragablzItem dragablzItem) 
            : base(routedEvent, dragablzItem)
        { }
    }
}
