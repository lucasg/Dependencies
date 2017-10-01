using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace Dragablz
{
    /// <summary>
    /// A linear position monitor simplifies the montoring of the order of items, where they are laid out
    /// horizontally or vertically (typically via a <see cref="StackOrganiser"/>.
    /// </summary>
    public abstract class StackPositionMonitor : PositionMonitor
    {
        private readonly Func<DragablzItem, double> _getLocation;

        protected StackPositionMonitor(Orientation orientation)
        {
            switch (orientation)
            {
                case Orientation.Horizontal:
                    _getLocation = item => item.X;
                    break;
                case Orientation.Vertical:
                    _getLocation = item => item.Y;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("orientation");
            }
        }

        public event EventHandler<OrderChangedEventArgs> OrderChanged;

        internal virtual void OnOrderChanged(OrderChangedEventArgs e)
        {
            var handler = OrderChanged;
            if (handler != null) handler(this, e);
        }

        internal IEnumerable<DragablzItem> Sort(IEnumerable<DragablzItem> items)
        {
            if (items == null) throw new ArgumentNullException("items");

            return items.OrderBy(i => _getLocation(i));
        }
    }
}