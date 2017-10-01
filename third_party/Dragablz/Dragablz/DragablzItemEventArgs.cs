using System;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace Dragablz
{
    public delegate void DragablzItemEventHandler(object sender, DragablzItemEventArgs e);

    public class DragablzItemEventArgs : RoutedEventArgs
    {
        private readonly DragablzItem _dragablzItem;

        public DragablzItemEventArgs(DragablzItem dragablzItem)
        {
            if (dragablzItem == null) throw new ArgumentNullException("dragablzItem");            

            _dragablzItem = dragablzItem;
        }

        public DragablzItemEventArgs(RoutedEvent routedEvent, DragablzItem dragablzItem)
            : base(routedEvent)
        {
            _dragablzItem = dragablzItem;
        }

        public DragablzItemEventArgs(RoutedEvent routedEvent, object source, DragablzItem dragablzItem)
            : base(routedEvent, source)
        {
            _dragablzItem = dragablzItem;
        }

        public DragablzItem DragablzItem
        {
            get { return _dragablzItem; }
        }
    }
}