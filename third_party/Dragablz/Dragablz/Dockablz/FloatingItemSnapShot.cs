using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Dragablz.Dockablz
{
    /// <summary>
    /// experimentational.  might have to puish this back to mvvm only
    /// </summary>    
    internal class FloatingItemSnapShot
    {
        private readonly object _content;
        private readonly Rect _location;
        private readonly int _zIndex;
        private readonly WindowState _state;

        public FloatingItemSnapShot(object content, Rect location, int zIndex, WindowState state)
        {
            if (content == null) throw new ArgumentNullException("content");

            _content = content;
            _location = location;
            _zIndex = zIndex;
            _state = state;
        }

        public static FloatingItemSnapShot Take(DragablzItem dragablzItem)
        {
            if (dragablzItem == null) throw new ArgumentNullException("dragablzItem");

            return new FloatingItemSnapShot(
                dragablzItem.Content, 
                new Rect(dragablzItem.X, dragablzItem.Y, dragablzItem.ActualWidth, dragablzItem.ActualHeight), 
                Panel.GetZIndex(dragablzItem),
                Layout.GetFloatingItemState(dragablzItem));
        }

        public void Apply(DragablzItem dragablzItem)
        {
            if (dragablzItem == null) throw new ArgumentNullException("dragablzItem");

            dragablzItem.SetCurrentValue(DragablzItem.XProperty, Location.Left);
            dragablzItem.SetCurrentValue(DragablzItem.YProperty, Location.Top);
            dragablzItem.SetCurrentValue(FrameworkElement.WidthProperty, Location.Width);
            dragablzItem.SetCurrentValue(FrameworkElement.HeightProperty, Location.Height);
            Layout.SetFloatingItemState(dragablzItem, State);
            Panel.SetZIndex(dragablzItem, ZIndex);
        }

        public object Content
        {
            get { return _content; }
        }

        public Rect Location
        {
            get { return _location; }
        }

        public int ZIndex
        {
            get { return _zIndex; }
        }

        public WindowState State
        {
            get { return _state; }
        }
    }
}
