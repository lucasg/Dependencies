using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Dragablz.Dockablz
{
    internal class Tiler
    {
        public static void Tile(IEnumerable<DragablzItem> dragablzItems, Size bounds)
        {
            if (dragablzItems == null) throw new ArgumentNullException("dragablzItems");            

            var items = new Queue<DragablzItem>(dragablzItems.OrderBy(Panel.GetZIndex));

            var cellCountPerColumn = TilerCalculator.GetCellCountPerColumn(items.Count());
            var x = 0d;
            var cellWidth = bounds.Width / cellCountPerColumn.Length;
            foreach (var cellCount in cellCountPerColumn)
            {
                var y = 0d;
                var cellHeight = bounds.Height / cellCount;
                for (var cell = 0; cell < cellCount; cell++)
                {
                    var item = items.Dequeue();
                    Layout.SetFloatingItemState(item, WindowState.Normal);
                    item.SetCurrentValue(DragablzItem.XProperty, x);
                    item.SetCurrentValue(DragablzItem.YProperty, y);
                    item.SetCurrentValue(FrameworkElement.WidthProperty, cellWidth);
                    item.SetCurrentValue(FrameworkElement.HeightProperty, cellHeight);

                    y += cellHeight;
                }

                x += cellWidth;
            }
        }

        public static void TileHorizontally(IEnumerable<DragablzItem> dragablzItems, Size bounds)
        {
            if (dragablzItems == null) throw new ArgumentNullException("dragablzItems");

            var items = dragablzItems.ToList();

            var x = 0.0;
            var width = bounds.Width/items.Count;
            foreach (var dragablzItem in items)
            {
                Layout.SetFloatingItemState(dragablzItem, WindowState.Normal);
                dragablzItem.SetCurrentValue(DragablzItem.XProperty, x);
                dragablzItem.SetCurrentValue(DragablzItem.YProperty, 0d);
                x += width;
                dragablzItem.SetCurrentValue(FrameworkElement.WidthProperty, width);
                dragablzItem.SetCurrentValue(FrameworkElement.HeightProperty, bounds.Height);
            }
        }

        public static void TileVertically(IEnumerable<DragablzItem> dragablzItems, Size bounds)
        {
            if (dragablzItems == null) throw new ArgumentNullException("dragablzItems");

            var items = dragablzItems.ToList();

            var y = 0.0;
            var height = bounds.Height / items.Count;
            foreach (var dragablzItem in items)
            {
                Layout.SetFloatingItemState(dragablzItem, WindowState.Normal);
                dragablzItem.SetCurrentValue(DragablzItem.YProperty, y);
                dragablzItem.SetCurrentValue(DragablzItem.XProperty, 0d);
                y += height;
                dragablzItem.SetCurrentValue(FrameworkElement.HeightProperty, height);
                dragablzItem.SetCurrentValue(FrameworkElement.WidthProperty, bounds.Width);
            }
        }

    }
}