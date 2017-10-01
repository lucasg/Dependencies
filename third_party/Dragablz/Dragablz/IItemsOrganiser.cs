using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace Dragablz
{
    public interface IItemsOrganiser
    {
        void Organise(DragablzItemsControl requestor, Size measureBounds, IEnumerable<DragablzItem> items);
        void Organise(DragablzItemsControl requestor, Size measureBounds, IOrderedEnumerable<DragablzItem> items);
        void OrganiseOnMouseDownWithin(DragablzItemsControl requestor, Size measureBounds, List<DragablzItem> siblingItems, DragablzItem dragablzItem);
        void OrganiseOnDragStarted(DragablzItemsControl requestor, Size measureBounds, IEnumerable<DragablzItem> siblingItems, DragablzItem dragItem);
        void OrganiseOnDrag(DragablzItemsControl requestor, Size measureBounds, IEnumerable<DragablzItem> siblingItems, DragablzItem dragItem);
        void OrganiseOnDragCompleted(DragablzItemsControl requestor, Size measureBounds, IEnumerable<DragablzItem> siblingItems, DragablzItem dragItem);        
        Point ConstrainLocation(DragablzItemsControl requestor, Size measureBounds, Point itemCurrentLocation, Size itemCurrentSize, Point itemDesiredLocation, Size itemDesiredSize);
        Size Measure(DragablzItemsControl requestor, Size availableSize, IEnumerable<DragablzItem> items);
        IEnumerable<DragablzItem> Sort(IEnumerable<DragablzItem> items);
    }
}
