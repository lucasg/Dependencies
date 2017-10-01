using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Threading;
using Dragablz.Core;

namespace Dragablz
{
    /// <summary>
    /// Items control which typically uses a canvas and 
    /// </summary>
    public class DragablzItemsControl : ItemsControl
    {        
        private object[] _previousSortQueryResult;

        static DragablzItemsControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DragablzItemsControl), new FrameworkPropertyMetadata(typeof(DragablzItemsControl)));            
        }        

        public DragablzItemsControl()
        {            
            ItemContainerGenerator.StatusChanged += ItemContainerGeneratorOnStatusChanged;
            ItemContainerGenerator.ItemsChanged += ItemContainerGeneratorOnItemsChanged;
            AddHandler(DragablzItem.XChangedEvent, new RoutedPropertyChangedEventHandler<double>(ItemXChanged));
            AddHandler(DragablzItem.YChangedEvent, new RoutedPropertyChangedEventHandler<double>(ItemYChanged));
            AddHandler(DragablzItem.DragDelta, new DragablzDragDeltaEventHandler(ItemDragDelta));
            AddHandler(DragablzItem.DragCompleted, new DragablzDragCompletedEventHandler(ItemDragCompleted));
            AddHandler(DragablzItem.DragStarted, new DragablzDragStartedEventHandler(ItemDragStarted));
            AddHandler(DragablzItem.MouseDownWithinEvent, new DragablzItemEventHandler(ItemMouseDownWithinHandlerTarget));                        
        }

        public static readonly DependencyProperty FixedItemCountProperty = DependencyProperty.Register(
            "FixedItemCount", typeof (int), typeof (DragablzItemsControl), new PropertyMetadata(default(int)));

        public int FixedItemCount
        {
            get { return (int) GetValue(FixedItemCountProperty); }
            set { SetValue(FixedItemCountProperty, value); }
        }

        private void ItemContainerGeneratorOnItemsChanged(object sender, ItemsChangedEventArgs itemsChangedEventArgs)
        {
            //throw new NotImplementedException();
        }

        protected override void ClearContainerForItemOverride(DependencyObject element, object item)
        {
            if (ContainerCustomisations != null && ContainerCustomisations.ClearingContainerForItemOverride != null)
                ContainerCustomisations.ClearingContainerForItemOverride(element, item);            

            base.ClearContainerForItemOverride(element, item);

            ((DragablzItem)element).SizeChanged -= ItemSizeChangedEventHandler;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                var dragablzItems = DragablzItems().ToList();
                if (ItemsOrganiser == null) return;
                ItemsOrganiser.Organise(this, new Size(ItemsPresenterWidth, ItemsPresenterHeight), dragablzItems);
                var measure = ItemsOrganiser.Measure(this, new Size(ActualWidth, ActualHeight), dragablzItems);
                ItemsPresenterWidth = measure.Width;
                ItemsPresenterHeight = measure.Height;
            }), DispatcherPriority.Input);            
        }        

        public static readonly DependencyProperty ItemsOrganiserProperty = DependencyProperty.Register(
            "ItemsOrganiser", typeof (IItemsOrganiser), typeof (DragablzItemsControl), new PropertyMetadata(default(IItemsOrganiser)));

        public IItemsOrganiser ItemsOrganiser
        {
            get { return (IItemsOrganiser) GetValue(ItemsOrganiserProperty); }
            set { SetValue(ItemsOrganiserProperty, value); }
        }

        public static readonly DependencyProperty PositionMonitorProperty = DependencyProperty.Register(
            "PositionMonitor", typeof (PositionMonitor), typeof (DragablzItemsControl), new PropertyMetadata(default(PositionMonitor)));

        public PositionMonitor PositionMonitor
        {
            get { return (PositionMonitor) GetValue(PositionMonitorProperty); }
            set { SetValue(PositionMonitorProperty, value); }
        }

        private static readonly DependencyPropertyKey ItemsPresenterWidthPropertyKey =
            DependencyProperty.RegisterReadOnly(
                "ItemsPresenterWidth", typeof(double), typeof (DragablzItemsControl),
                new PropertyMetadata(default(double)));

        public static readonly DependencyProperty ItemsPresenterWidthProperty =
            ItemsPresenterWidthPropertyKey.DependencyProperty;

        public double ItemsPresenterWidth
        {
            get { return (double) GetValue(ItemsPresenterWidthProperty); }
            private set { SetValue(ItemsPresenterWidthPropertyKey, value); }
        }

        private static readonly DependencyPropertyKey ItemsPresenterHeightPropertyKey =
            DependencyProperty.RegisterReadOnly(
                "ItemsPresenterHeight", typeof (double), typeof (DragablzItemsControl),
                new PropertyMetadata(default(double)));

        public static readonly DependencyProperty ItemsPresenterHeightProperty =
            ItemsPresenterHeightPropertyKey.DependencyProperty;

        public double ItemsPresenterHeight
        {
            get { return (double) GetValue(ItemsPresenterHeightProperty); }
            private set { SetValue(ItemsPresenterHeightPropertyKey, value); }
        }

        /// <summary>
        /// Adds an item to the underlying source, displaying in a specific position in rendered control.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="addLocationHint"></param>
        public void AddToSource(object item, AddLocationHint addLocationHint)
        {
            AddToSource(item, null, addLocationHint);
        }

        /// <summary>
        /// Adds an item to the underlying source, displaying in a specific position in rendered control.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="nearItem"></param>
        /// <param name="addLocationHint"></param>
        public void AddToSource(object item, object nearItem, AddLocationHint addLocationHint)
        {
            CollectionTeaser collectionTeaser;
            if (CollectionTeaser.TryCreate(ItemsSource, out collectionTeaser))
                collectionTeaser.Add(item);
            else
                Items.Add(item);
            MoveItem(new MoveItemRequest(item, nearItem, addLocationHint));
        }

        internal ContainerCustomisations ContainerCustomisations { get; set; }

        private void ItemContainerGeneratorOnStatusChanged(object sender, EventArgs eventArgs)
        {            
            if (ItemContainerGenerator.Status != GeneratorStatus.ContainersGenerated) return;

            InvalidateMeasure();
            //extra kick
            Dispatcher.BeginInvoke(new Action(InvalidateMeasure), DispatcherPriority.Loaded);
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {            
            var dragablzItem = item as DragablzItem;
            if (dragablzItem == null) return false;
            
            return true;
        }

        protected override DependencyObject GetContainerForItemOverride()
        {
            var result = ContainerCustomisations != null && ContainerCustomisations.GetContainerForItemOverride != null
                ? ContainerCustomisations.GetContainerForItemOverride()
                : new DragablzItem();

            result.SizeChanged += ItemSizeChangedEventHandler;

            return result;
        }

        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            if (ContainerCustomisations != null && ContainerCustomisations.PrepareContainerForItemOverride != null)
                ContainerCustomisations.PrepareContainerForItemOverride(element, item);

            base.PrepareContainerForItemOverride(element, item);
        }

        protected override Size MeasureOverride(Size constraint)        
        {
            if (ItemsOrganiser == null) return base.MeasureOverride(constraint);

            if (LockedMeasure.HasValue)
            {
                ItemsPresenterWidth = LockedMeasure.Value.Width;
                ItemsPresenterHeight = LockedMeasure.Value.Height;
                return LockedMeasure.Value;
            }

            var dragablzItems = DragablzItems().ToList();            
            var maxConstraint = new Size(double.PositiveInfinity, double.PositiveInfinity);

            ItemsOrganiser.Organise(this, maxConstraint, dragablzItems);
            var measure = ItemsOrganiser.Measure(this, new Size(ActualWidth, ActualHeight), dragablzItems);

            ItemsPresenterWidth = measure.Width;
            ItemsPresenterHeight = measure.Height;                          

            var width = double.IsInfinity(constraint.Width) ? measure.Width : constraint.Width;
            var height = double.IsInfinity(constraint.Height) ? measure.Height : constraint.Height;

            return new Size(width, height);
        }

        internal void InstigateDrag(object item, Action<DragablzItem> continuation)
        {   
            var dragablzItem = (DragablzItem)ItemContainerGenerator.ContainerFromItem(item);            
            dragablzItem.InstigateDrag(continuation);            
        }

        /// <summary>
        /// Move an item in the rendered layout.
        /// </summary>
        /// <param name="moveItemRequest"></param>
        public void MoveItem(MoveItemRequest moveItemRequest)
        {
            if (moveItemRequest == null) throw new ArgumentNullException("moveItemRequest");

            if (ItemsOrganiser == null) return;

            var dragablzItem = moveItemRequest.Item as DragablzItem ??
                               ItemContainerGenerator.ContainerFromItem(
                                   moveItemRequest.Item) as DragablzItem;
            var contextDragablzItem = moveItemRequest.Context as DragablzItem ??
                               ItemContainerGenerator.ContainerFromItem(
                                   moveItemRequest.Context) as DragablzItem;

            if (dragablzItem == null) return;

            var sortedItems = DragablzItems().OrderBy(di => di.LogicalIndex).ToList();
            sortedItems.Remove(dragablzItem);
                
            switch (moveItemRequest.AddLocationHint)
            {
                case AddLocationHint.First:
                    sortedItems.Insert(0, dragablzItem);                        
                    break;
                case AddLocationHint.Last:
                    sortedItems.Add(dragablzItem);
                    break;
                case AddLocationHint.Prior:
                case AddLocationHint.After:
                    if (contextDragablzItem == null)
                        return;

                    var contextIndex = sortedItems.IndexOf(contextDragablzItem);
                    sortedItems.Insert(moveItemRequest.AddLocationHint == AddLocationHint.Prior ? contextIndex : contextIndex + 1, dragablzItem);

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }            

            //TODO might not be too great for perf on larger lists
            var orderedEnumerable = sortedItems.OrderBy(di => sortedItems.IndexOf(di));

            ItemsOrganiser.Organise(this, new Size(ItemsPresenterWidth, ItemsPresenterHeight), orderedEnumerable);
        }        

        internal IEnumerable<DragablzItem> DragablzItems()
        {
            return this.Containers<DragablzItem>().ToList();            
        }

        internal Size? LockedMeasure { get; set; }

        private void ItemDragStarted(object sender, DragablzDragStartedEventArgs eventArgs)
        {            
            if (ItemsOrganiser != null)
            {
                var bounds = new Size(ActualWidth, ActualHeight);
                ItemsOrganiser.OrganiseOnDragStarted(this, bounds,
                    DragablzItems().Except(new[] { eventArgs.DragablzItem }).ToList(),
                    eventArgs.DragablzItem);
            }

            eventArgs.Handled = true;

            Dispatcher.BeginInvoke(new Action(InvalidateMeasure), DispatcherPriority.Loaded);
        }

        private void ItemDragCompleted(object sender, DragablzDragCompletedEventArgs eventArgs)
        {
            var dragablzItems = DragablzItems()
                .Select(i =>
                {
                    i.IsDragging = false;
                    i.IsSiblingDragging = false;
                    return i;
                })
                .ToList();

            if (ItemsOrganiser != null)
            {
                var bounds = new Size(ActualWidth, ActualHeight);
                ItemsOrganiser.OrganiseOnDragCompleted(this, bounds,
                    dragablzItems.Except(eventArgs.DragablzItem),
                    eventArgs.DragablzItem);
            }            

            eventArgs.Handled = true;

            //wowsers
            Dispatcher.BeginInvoke(new Action(InvalidateMeasure));
            Dispatcher.BeginInvoke(new Action(InvalidateMeasure), DispatcherPriority.Loaded);
        }

        private void ItemDragDelta(object sender, DragablzDragDeltaEventArgs eventArgs)
        {
            var bounds = new Size(ItemsPresenterWidth, ItemsPresenterHeight);
            var desiredLocation = new Point(
                eventArgs.DragablzItem.X + eventArgs.DragDeltaEventArgs.HorizontalChange,
                eventArgs.DragablzItem.Y + eventArgs.DragDeltaEventArgs.VerticalChange
                );
            if (ItemsOrganiser != null)
            {                
                if (FixedItemCount > 0 &&
                    ItemsOrganiser.Sort(DragablzItems()).Take(FixedItemCount).Contains(eventArgs.DragablzItem))
                {
                    eventArgs.Handled = true;
                    return;
                }                
            
                desiredLocation = ItemsOrganiser.ConstrainLocation(this, bounds,
                    new Point(eventArgs.DragablzItem.X, eventArgs.DragablzItem.Y),
                    new Size(eventArgs.DragablzItem.ActualWidth, eventArgs.DragablzItem.ActualHeight),
                    desiredLocation, eventArgs.DragablzItem.DesiredSize);
            }

            foreach (var dragableItem in DragablzItems()
                .Except(new[] { eventArgs.DragablzItem })) // how about Linq.Where() ?
            {
                dragableItem.IsSiblingDragging = true;
            }
            eventArgs.DragablzItem.IsDragging = true;

            eventArgs.DragablzItem.X = desiredLocation.X;
            eventArgs.DragablzItem.Y = desiredLocation.Y;

            if (ItemsOrganiser != null)
                ItemsOrganiser.OrganiseOnDrag(
                    this,
                    bounds,
                    DragablzItems().Except(new[] {eventArgs.DragablzItem}), eventArgs.DragablzItem);
            
            eventArgs.DragablzItem.BringIntoView();

            eventArgs.Handled = true;
        }

        private void ItemXChanged(object sender, RoutedPropertyChangedEventArgs<double> routedPropertyChangedEventArgs)
        {
            UpdateMonitor(routedPropertyChangedEventArgs);
        }

        private void ItemYChanged(object sender, RoutedPropertyChangedEventArgs<double> routedPropertyChangedEventArgs)
        {
            UpdateMonitor(routedPropertyChangedEventArgs);
        }        

        private void UpdateMonitor(RoutedEventArgs routedPropertyChangedEventArgs)
        {
            if (PositionMonitor == null) return;

            var dragablzItem = (DragablzItem) routedPropertyChangedEventArgs.OriginalSource;

            if (!Equals(ItemsControlFromItemContainer(dragablzItem), this)) return;

            PositionMonitor.OnLocationChanged(new LocationChangedEventArgs(dragablzItem.Content, new Point(dragablzItem.X, dragablzItem.Y)));

            var linearPositionMonitor = PositionMonitor as StackPositionMonitor;
            if (linearPositionMonitor == null) return;

            var sortedItems = linearPositionMonitor.Sort(this.Containers<DragablzItem>()).Select(di => di.Content).ToArray();
            if (_previousSortQueryResult == null || !_previousSortQueryResult.SequenceEqual(sortedItems))
                linearPositionMonitor.OnOrderChanged(new OrderChangedEventArgs(_previousSortQueryResult, sortedItems));

            _previousSortQueryResult = sortedItems;
        }

        private void ItemMouseDownWithinHandlerTarget(object sender, DragablzItemEventArgs e)
        {            
            if (ItemsOrganiser == null) return;

            var bounds = new Size(ActualWidth, ActualHeight);
            ItemsOrganiser.OrganiseOnMouseDownWithin(this, bounds,
                DragablzItems().Except(e.DragablzItem).ToList(),
                e.DragablzItem);
        }

        private void ItemSizeChangedEventHandler(object sender, SizeChangedEventArgs e)
        {
            InvalidateMeasure();
            //extra kick
            Dispatcher.BeginInvoke(new Action(InvalidateMeasure), DispatcherPriority.Loaded);
        }
    }
}
