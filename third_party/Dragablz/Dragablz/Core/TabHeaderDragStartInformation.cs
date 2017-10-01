using System;

namespace Dragablz.Core
{
    internal class TabHeaderDragStartInformation
    {
        private readonly DragablzItem _dragItem;
        private readonly double _dragablzItemsControlHorizontalOffset;
        private readonly double _dragablzItemControlVerticalOffset; 
        private readonly double _dragablzItemHorizontalOffset;
        private readonly double _dragablzItemVerticalOffset;

        public TabHeaderDragStartInformation(
            DragablzItem dragItem,
            double dragablzItemsControlHorizontalOffset, double dragablzItemControlVerticalOffset, double dragablzItemHorizontalOffset, double dragablzItemVerticalOffset)
        {
            if (dragItem == null) throw new ArgumentNullException("dragItem");

            _dragItem = dragItem;
            _dragablzItemsControlHorizontalOffset = dragablzItemsControlHorizontalOffset;
            _dragablzItemControlVerticalOffset = dragablzItemControlVerticalOffset;
            _dragablzItemHorizontalOffset = dragablzItemHorizontalOffset;
            _dragablzItemVerticalOffset = dragablzItemVerticalOffset;
        }

        public double DragablzItemsControlHorizontalOffset
        {
            get { return _dragablzItemsControlHorizontalOffset; }
        }

        public double DragablzItemControlVerticalOffset
        {
            get { return _dragablzItemControlVerticalOffset; }
        }

        public double DragablzItemHorizontalOffset
        {
            get { return _dragablzItemHorizontalOffset; }
        }

        public double DragablzItemVerticalOffset
        {
            get { return _dragablzItemVerticalOffset; }
        }

        public DragablzItem DragItem
        {
            get { return _dragItem; }
        }
    }
}