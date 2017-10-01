using System;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace Dragablz
{
    public delegate void DragablzDragCompletedEventHandler(object sender, DragablzDragCompletedEventArgs e);

    public class DragablzDragCompletedEventArgs : RoutedEventArgs
    {
        private readonly DragablzItem _dragablzItem;
        private readonly bool _isDropTargetFound;
        private readonly DragCompletedEventArgs _dragCompletedEventArgs;

        public DragablzDragCompletedEventArgs(DragablzItem dragablzItem, DragCompletedEventArgs dragCompletedEventArgs)
        {
            if (dragablzItem == null) throw new ArgumentNullException("dragablzItem");
            if (dragCompletedEventArgs == null) throw new ArgumentNullException("dragCompletedEventArgs");
            
            _dragablzItem = dragablzItem;
            _dragCompletedEventArgs = dragCompletedEventArgs;
        }

        public DragablzDragCompletedEventArgs(RoutedEvent routedEvent, DragablzItem dragablzItem, DragCompletedEventArgs dragCompletedEventArgs)
            : base(routedEvent)
        {
            if (dragablzItem == null) throw new ArgumentNullException("dragablzItem");
            if (dragCompletedEventArgs == null) throw new ArgumentNullException("dragCompletedEventArgs");

            _dragablzItem = dragablzItem;            
            _dragCompletedEventArgs = dragCompletedEventArgs;
        }

        public DragablzDragCompletedEventArgs(RoutedEvent routedEvent, object source, DragablzItem dragablzItem, DragCompletedEventArgs dragCompletedEventArgs)
            : base(routedEvent, source)
        {
            if (dragablzItem == null) throw new ArgumentNullException("dragablzItem");
            if (dragCompletedEventArgs == null) throw new ArgumentNullException("dragCompletedEventArgs");

            _dragablzItem = dragablzItem;
            _dragCompletedEventArgs = dragCompletedEventArgs;
        }

        public DragablzItem DragablzItem
        {
            get { return _dragablzItem; }
        }

        public DragCompletedEventArgs DragCompletedEventArgs
        {
            get { return _dragCompletedEventArgs; }
        }        
    }
}