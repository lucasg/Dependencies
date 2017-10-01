using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Dragablz
{
    public class CanvasOrganiser : IItemsOrganiser
    {
        public virtual void Organise(DragablzItemsControl requestor, Size measureBounds, IEnumerable<DragablzItem> items)
        {
            
        }

        public virtual void Organise(DragablzItemsControl requestor, Size measureBounds, IOrderedEnumerable<DragablzItem> items)
        {

        }

        public virtual void OrganiseOnMouseDownWithin(DragablzItemsControl requestor, Size measureBounds, List<DragablzItem> siblingItems, DragablzItem dragablzItem)
        {
            var zIndex = int.MaxValue;
            foreach (var source in siblingItems.OrderByDescending(Panel.GetZIndex))
            {
                Panel.SetZIndex(source, --zIndex);

            }
            Panel.SetZIndex(dragablzItem, int.MaxValue);
        }

        public virtual void OrganiseOnDragStarted(DragablzItemsControl requestor, Size measureBounds, IEnumerable<DragablzItem> siblingItems, DragablzItem dragItem)
        {
            
        }

        public virtual void OrganiseOnDrag(DragablzItemsControl requestor, Size measureBounds, IEnumerable<DragablzItem> siblingItems, DragablzItem dragItem)
        {
            
        }

        public virtual void OrganiseOnDragCompleted(DragablzItemsControl requestor, Size measureBounds, IEnumerable<DragablzItem> siblingItems, DragablzItem dragItem)
        {
            
        }

        public virtual Point ConstrainLocation(DragablzItemsControl requestor, Size measureBounds, Point itemCurrentLocation, Size itemCurrentSize, Point itemDesiredLocation, Size itemDesiredSize)
        {
            //we will stop it pushing beyond the bounds...unless it's already beyond...
            var reduceBoundsWidth = itemCurrentLocation.X + itemCurrentSize.Width > measureBounds.Width
                ? 0
                : itemDesiredSize.Width;
            var reduceBoundsHeight = itemCurrentLocation.Y + itemCurrentSize.Height > measureBounds.Height
                ? 0
                : itemDesiredSize.Height;

            return new Point(
                Math.Min(Math.Max(itemDesiredLocation.X, 0), measureBounds.Width - reduceBoundsWidth),
                Math.Min(Math.Max(itemDesiredLocation.Y, 0), measureBounds.Height - reduceBoundsHeight));
        }

        public virtual Size Measure(DragablzItemsControl requestor, Size availableSize, IEnumerable<DragablzItem> items)
        {
            return availableSize;
        }

        public virtual IEnumerable<DragablzItem> Sort(IEnumerable<DragablzItem> items)
        {
            return items;
        }
    }
}