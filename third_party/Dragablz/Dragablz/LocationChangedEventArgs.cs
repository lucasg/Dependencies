using System;
using System.Windows;

namespace Dragablz
{
    public class LocationChangedEventArgs : EventArgs
    {
        private readonly object _item;
        private readonly Point _location;

        public LocationChangedEventArgs(object item, Point location)
        {
            if (item == null) throw new ArgumentNullException("item");
            
            _item = item;
            _location = location;
        }

        public object Item
        {
            get { return _item; }
        }

        public Point Location
        {
            get { return _location; }
        }
    }
}