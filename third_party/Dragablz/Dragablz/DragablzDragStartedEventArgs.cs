using System;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace Dragablz
{
    public delegate void DragablzDragStartedEventHandler(object sender, DragablzDragStartedEventArgs e);

    public class DragablzDragStartedEventArgs : DragablzItemEventArgs
    {
        private readonly DragStartedEventArgs _dragStartedEventArgs;

        public DragablzDragStartedEventArgs(DragablzItem dragablzItem, DragStartedEventArgs dragStartedEventArgs)
            : base(dragablzItem)
        {
            if (dragStartedEventArgs == null) throw new ArgumentNullException("dragStartedEventArgs");

            _dragStartedEventArgs = dragStartedEventArgs;
        }

        public DragablzDragStartedEventArgs(RoutedEvent routedEvent, DragablzItem dragablzItem, DragStartedEventArgs dragStartedEventArgs)
            : base(routedEvent, dragablzItem)
        {
            _dragStartedEventArgs = dragStartedEventArgs;
        }

        public DragablzDragStartedEventArgs(RoutedEvent routedEvent, object source, DragablzItem dragablzItem, DragStartedEventArgs dragStartedEventArgs)
            : base(routedEvent, source, dragablzItem)
        {
            _dragStartedEventArgs = dragStartedEventArgs;
        }

        public DragStartedEventArgs DragStartedEventArgs
        {
            get { return _dragStartedEventArgs; }
        }        
    }
}