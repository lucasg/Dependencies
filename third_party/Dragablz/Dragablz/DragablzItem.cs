using System;
using System.Dynamic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using Dragablz.Core;
using Dragablz.Referenceless;

namespace Dragablz
{
    public enum SizeGrip
    {
        NotApplicable,
        Left,
        TopLeft,
        Top,
        TopRight,
        Right,
        BottomRight,
        Bottom,
        BottomLeft
    }

    [TemplatePart(Name = ThumbPartName, Type = typeof(Thumb))]
    public class DragablzItem : ContentControl
    {
        public const string ThumbPartName = "PART_Thumb";

        private readonly SerialDisposable _templateSubscriptions = new SerialDisposable();
        private readonly SerialDisposable _rightMouseUpCleanUpDisposable = new SerialDisposable();

        private Thumb _customThumb;
        private Thumb _thumb;
        private bool _seizeDragWithTemplate;
        private Action<DragablzItem> _dragSeizedContinuation;

        static DragablzItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DragablzItem), new FrameworkPropertyMetadata(typeof(DragablzItem)));            
        }

        public DragablzItem()
        {
            AddHandler(MouseDownEvent, new RoutedEventHandler(MouseDownHandler), true);            
        }

        public static readonly DependencyProperty XProperty = DependencyProperty.Register(
            "X", typeof (double), typeof (DragablzItem), new PropertyMetadata(default(double), OnXChanged));

        public double X
        {
            get { return (double) GetValue(XProperty); }
            set { SetValue(XProperty, value); }
        }

        public static readonly RoutedEvent XChangedEvent =
            EventManager.RegisterRoutedEvent(
                "XChanged",
                RoutingStrategy.Bubble,
                typeof(RoutedPropertyChangedEventHandler<double>),
                typeof(DragablzItem));

        public event RoutedPropertyChangedEventHandler<double> XChanged
        {
            add { AddHandler(XChangedEvent, value); }
            remove { RemoveHandler(IsDraggingChangedEvent, value); }
        }

        private static void OnXChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {   
            var instance = (DragablzItem)d;
            var args = new RoutedPropertyChangedEventArgs<double>(
                (double)e.OldValue,
                (double)e.NewValue)
            {
                RoutedEvent = XChangedEvent
            };
            instance.RaiseEvent(args);            
        } 

        public static readonly DependencyProperty YProperty = DependencyProperty.Register(
            "Y", typeof (double), typeof (DragablzItem), new PropertyMetadata(default(double), OnYChanged));

        public double Y
        {
            get { return (double) GetValue(YProperty); }
            set { SetValue(YProperty, value); }
        }

        public static readonly RoutedEvent YChangedEvent =
            EventManager.RegisterRoutedEvent(
                "YChanged",
                RoutingStrategy.Bubble,
                typeof(RoutedPropertyChangedEventHandler<double>),
                typeof(DragablzItem));

        public event RoutedPropertyChangedEventHandler<double> YChanged
        {
            add { AddHandler(YChangedEvent, value); }
            remove { RemoveHandler(IsDraggingChangedEvent, value); }
        }

        private static void OnYChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var instance = (DragablzItem)d;
            var args = new RoutedPropertyChangedEventArgs<double>(
                (double)e.OldValue,
                (double)e.NewValue)
            {
                RoutedEvent = YChangedEvent
            };
            instance.RaiseEvent(args);
        }

        private static readonly DependencyPropertyKey LogicalIndexPropertyKey =
            DependencyProperty.RegisterReadOnly(
                "LogicalIndex", typeof (int), typeof (DragablzItem),
                new PropertyMetadata(default(int), OnLogicalIndexChanged));

        public static readonly DependencyProperty LogicalIndexProperty =
            LogicalIndexPropertyKey.DependencyProperty;

        public int LogicalIndex
        {
            get { return (int) GetValue(LogicalIndexProperty); }
            internal set { SetValue(LogicalIndexPropertyKey, value); }
        }

        public static readonly RoutedEvent LogicalIndexChangedEvent =
            EventManager.RegisterRoutedEvent(
                "LogicalIndexChanged",
                RoutingStrategy.Bubble,
                typeof (RoutedPropertyChangedEventHandler<int>),
                typeof (DragablzItem));

        public event RoutedPropertyChangedEventHandler<int> LogicalIndexChanged
        {
            add { AddHandler(LogicalIndexChangedEvent, value); }
            remove { RemoveHandler(LogicalIndexChangedEvent, value); }
        }

        private static void OnLogicalIndexChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var instance = (DragablzItem) d;
            var args = new RoutedPropertyChangedEventArgs<int>(
                (int) e.OldValue,
                (int) e.NewValue)
            {
                RoutedEvent = DragablzItem.LogicalIndexChangedEvent
            };
            instance.RaiseEvent(args);
        } 

        public static readonly DependencyProperty SizeGripProperty = DependencyProperty.RegisterAttached(
            "SizeGrip", typeof (SizeGrip), typeof (DragablzItem), new PropertyMetadata(default(SizeGrip), SizeGripPropertyChangedCallback));

        private static void SizeGripPropertyChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            var thumb = (dependencyObject as Thumb);
            if (thumb == null) return;
            thumb.DragDelta += SizeThumbOnDragDelta;
        }

        private static void SizeThumbOnDragDelta(object sender, DragDeltaEventArgs dragDeltaEventArgs)
        {
            var thumb = ((Thumb) sender);
            var dragablzItem = thumb.VisualTreeAncestory().OfType<DragablzItem>().FirstOrDefault();
            if (dragablzItem == null) return;

            var sizeGrip = (SizeGrip) thumb.GetValue(SizeGripProperty);
            var width = dragablzItem.ActualWidth;
            var height = dragablzItem.ActualHeight;
            var x = dragablzItem.X;
            var y = dragablzItem.Y;
            switch (sizeGrip)
            {                                   
                case SizeGrip.NotApplicable:
                    break;
                case SizeGrip.Left:
                    width += -dragDeltaEventArgs.HorizontalChange;
                    x += dragDeltaEventArgs.HorizontalChange;
                    break;
                case SizeGrip.TopLeft:
                    width += -dragDeltaEventArgs.HorizontalChange;
                    height += -dragDeltaEventArgs.VerticalChange;
                    x += dragDeltaEventArgs.HorizontalChange;
                    y += dragDeltaEventArgs.VerticalChange;
                    break;
                case SizeGrip.Top:
                    height += -dragDeltaEventArgs.VerticalChange;                    
                    y += dragDeltaEventArgs.VerticalChange;
                    break;
                case SizeGrip.TopRight:
                    height += -dragDeltaEventArgs.VerticalChange;
                    width += dragDeltaEventArgs.HorizontalChange;
                    y += dragDeltaEventArgs.VerticalChange;
                    break;
                case SizeGrip.Right:
                    width += dragDeltaEventArgs.HorizontalChange;
                    break;
                case SizeGrip.BottomRight:
                    width += dragDeltaEventArgs.HorizontalChange;
                    height += dragDeltaEventArgs.VerticalChange;
                    break;
                case SizeGrip.Bottom:
                    height += dragDeltaEventArgs.VerticalChange;
                    break;
                case SizeGrip.BottomLeft:
                    height += dragDeltaEventArgs.VerticalChange;
                    width += -dragDeltaEventArgs.HorizontalChange;
                    x += dragDeltaEventArgs.HorizontalChange;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            dragablzItem.SetCurrentValue(XProperty, x);
            dragablzItem.SetCurrentValue(YProperty, y);
            dragablzItem.SetCurrentValue(WidthProperty, Math.Max(width, thumb.DesiredSize.Width));
            dragablzItem.SetCurrentValue(HeightProperty, Math.Max(height, thumb.DesiredSize.Height));
        }

        public static void SetSizeGrip(DependencyObject element, SizeGrip value)
        {
            element.SetValue(SizeGripProperty, value);
        }

        public static SizeGrip GetSizeGrip(DependencyObject element)
        {
            return (SizeGrip) element.GetValue(SizeGripProperty);
        }

        /// <summary>
        /// Allows item content to be rotated (in suppported templates), typically for use in a vertical/side tab.
        /// </summary>
        public static readonly DependencyProperty ContentRotateTransformAngleProperty = DependencyProperty.RegisterAttached(
            "ContentRotateTransformAngle", typeof (double), typeof (DragablzItem), new FrameworkPropertyMetadata(default(double), FrameworkPropertyMetadataOptions.Inherits));

        /// <summary>
        /// Allows item content to be rotated (in suppported templates), typically for use in a vertical/side tab.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="value"></param>
        public static void SetContentRotateTransformAngle(DependencyObject element, double value)
        {
            element.SetValue(ContentRotateTransformAngleProperty, value);
        }

        /// <summary>
        /// Allows item content to be rotated (in suppported templates), typically for use in a vertical/side tab.
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        public static double GetContentRotateTransformAngle(DependencyObject element)
        {
            return (double) element.GetValue(ContentRotateTransformAngleProperty);
        }

        public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register(
            "IsSelected", typeof(bool), typeof(DragablzItem), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.AffectsParentMeasure));

        public bool IsSelected
        {
            get { return (bool) GetValue(IsSelectedProperty); }
            set { SetValue(IsSelectedProperty, value); }
        }

        private static readonly DependencyPropertyKey IsDraggingPropertyKey =
            DependencyProperty.RegisterReadOnly(
                "IsDragging", typeof (bool), typeof (DragablzItem),
                new PropertyMetadata(default(bool), OnIsDraggingChanged));

        public static readonly DependencyProperty IsDraggingProperty =
            IsDraggingPropertyKey.DependencyProperty;

        public bool IsDragging
        {
            get { return (bool) GetValue(IsDraggingProperty); }
            internal set { SetValue(IsDraggingPropertyKey, value); }
        }

        public static readonly RoutedEvent IsDraggingChangedEvent =
            EventManager.RegisterRoutedEvent(
                "IsDraggingChanged",
                RoutingStrategy.Bubble,
                typeof (RoutedPropertyChangedEventHandler<bool>),
                typeof (DragablzItem));

        public event RoutedPropertyChangedEventHandler<bool> IsDraggingChanged
        {
            add { AddHandler(IsDraggingChangedEvent, value); }
            remove { RemoveHandler(IsDraggingChangedEvent, value); }
        }

        internal object UnderlyingContent { get; set; }

        private static void OnIsDraggingChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var instance = (DragablzItem)d;
            var args = new RoutedPropertyChangedEventArgs<bool>(
                (bool)e.OldValue,
                (bool)e.NewValue) { RoutedEvent = IsDraggingChangedEvent };
            instance.RaiseEvent(args);
        }

        public static readonly RoutedEvent MouseDownWithinEvent =
            EventManager.RegisterRoutedEvent(
                "MouseDownWithin",
                RoutingStrategy.Bubble,
                typeof(DragablzItemEventHandler),
                typeof (DragablzItem));

        private static void OnMouseDownWithin(DependencyObject d)
        {
            var instance = (DragablzItem)d;
            instance.RaiseEvent(new DragablzItemEventArgs(MouseDownWithinEvent, instance));
        }

        private static readonly DependencyPropertyKey IsSiblingDraggingPropertyKey =
            DependencyProperty.RegisterReadOnly(
                "IsSiblingDragging", typeof (bool), typeof (DragablzItem),
                new PropertyMetadata(default(bool), OnIsSiblingDraggingChanged));

        public static readonly DependencyProperty IsSiblingDraggingProperty =
            IsSiblingDraggingPropertyKey.DependencyProperty;

        public bool IsSiblingDragging
        {
            get { return (bool) GetValue(IsSiblingDraggingProperty); }
            internal set { SetValue(IsSiblingDraggingPropertyKey, value); }
        }

        public static readonly RoutedEvent IsSiblingDraggingChangedEvent =
            EventManager.RegisterRoutedEvent(
                "IsSiblingDraggingChanged",
                RoutingStrategy.Bubble,
                typeof (RoutedPropertyChangedEventHandler<bool>),
                typeof (DragablzItem));

        public event RoutedPropertyChangedEventHandler<bool> IsSiblingDraggingChanged
        {
            add { AddHandler(IsSiblingDraggingChangedEvent, value); }
            remove { RemoveHandler(IsSiblingDraggingChangedEvent, value); }
        }

        private static void OnIsSiblingDraggingChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var instance = (DragablzItem) d;
            var args = new RoutedPropertyChangedEventArgs<bool>(
                (bool) e.OldValue,
                (bool) e.NewValue)
            {
                RoutedEvent = IsSiblingDraggingChangedEvent
            };
            instance.RaiseEvent(args);
        }         

        public static readonly RoutedEvent DragStarted =
            EventManager.RegisterRoutedEvent(
                "DragStarted",
                RoutingStrategy.Bubble,
                typeof(DragablzDragStartedEventHandler),
                typeof(DragablzItem));

        protected void OnDragStarted(DragablzDragStartedEventArgs e)
        {            
            RaiseEvent(e);
        }

        public static readonly RoutedEvent DragDelta =
            EventManager.RegisterRoutedEvent(
                "DragDelta",
                RoutingStrategy.Bubble,
                typeof (DragablzDragDeltaEventHandler),
                typeof (DragablzItem));

        protected void OnDragDelta(DragablzDragDeltaEventArgs e)
        {            
            RaiseEvent(e);            
        }

        public static readonly RoutedEvent PreviewDragDelta =
            EventManager.RegisterRoutedEvent(
                "PreviewDragDelta",
                RoutingStrategy.Tunnel,
                typeof(DragablzDragDeltaEventHandler),
                typeof(DragablzItem));

        protected void OnPreviewDragDelta(DragablzDragDeltaEventArgs e)
        {            
            RaiseEvent(e);
        }

        public static readonly RoutedEvent DragCompleted =
            EventManager.RegisterRoutedEvent(
                "DragCompleted",
                RoutingStrategy.Bubble,
                typeof(DragablzDragCompletedEventHandler),
                typeof(DragablzItem));

        protected void OnDragCompleted(DragCompletedEventArgs e)
        {
            var args = new DragablzDragCompletedEventArgs(DragCompleted, this, e);
            RaiseEvent(args);

            //OK, this is a cheeky bit.  A completed drag may have occured after a tab as been pushed
            //intom a new window, which means we may have reverted to the template thumb.  So, let's
            //refresh the thumb in case the user has a custom one
            _customThumb = FindCustomThumb();
            _templateSubscriptions.Disposable = SelectAndSubscribeToThumb().Item2;
        }

        /// <summary>
        /// <see cref="DragablzItem" /> templates contain a thumb, which is used to drag the item around.
        /// For most scenarios this is fine, but by setting this flag to <value>true</value> you can define
        /// a custom thumb in your content, without having to override the template.  This can be useful if you
        /// have extra content; such as a custom button that you want the user to be able to interact with (as usually
        /// the default thumb will handle mouse interaction).
        /// </summary>
        public static readonly DependencyProperty IsCustomThumbProperty = DependencyProperty.RegisterAttached(
            "IsCustomThumb", typeof (bool), typeof (DragablzItem), new PropertyMetadata(default(bool), IsCustomThumbPropertyChangedCallback));

        private static void IsCustomThumbPropertyChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            var thumb = dependencyObject as Thumb;
            if (thumb == null) throw new ApplicationException("IsCustomThumb can only be applied to a thumb");

            if (thumb.IsLoaded)
                ApplyCustomThumbSetting(thumb);
            else
                thumb.Loaded += CustomThumbOnLoaded;
        }        

        /// <summary>
        /// <see cref="DragablzItem" /> templates contain a thumb, which is used to drag the item around.
        /// For most scenarios this is fine, but by setting this flag to <value>true</value> you can define
        /// a custom thumb in your content, without having to override the template.  This can be useful if you
        /// have extra content; such as a custom button that you want the user to be able to interact with (as usually
        /// the default thumb will handle mouse interaction).
        /// </summary>
        public static void SetIsCustomThumb(Thumb element, bool value)
        {
            element.SetValue(IsCustomThumbProperty, value);
        }

        public static bool GetIsCustomThumb(Thumb element)
        {
            return (bool) element.GetValue(IsCustomThumbProperty);
        }

        private bool _isTemplateThumbWithMouseAfterSeize = false;
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();                        
            
            var thumbAndSubscription = SelectAndSubscribeToThumb();
            _templateSubscriptions.Disposable = thumbAndSubscription.Item2;
            
            if (_seizeDragWithTemplate && thumbAndSubscription.Item1 != null)
            {
                _isTemplateThumbWithMouseAfterSeize = true;
                Mouse.AddLostMouseCaptureHandler(this, LostMouseAfterSeizeHandler);
                if (_dragSeizedContinuation != null)
                    _dragSeizedContinuation(this);
                _dragSeizedContinuation = null;

                Dispatcher.BeginInvoke(new Action(() => thumbAndSubscription.Item1.RaiseEvent(new MouseButtonEventArgs(InputManager.Current.PrimaryMouseDevice,
                    0,
                    MouseButton.Left) {RoutedEvent = MouseLeftButtonDownEvent})));
            }
            _seizeDragWithTemplate = false;
        }

        protected override void OnPreviewMouseRightButtonDown(MouseButtonEventArgs e)
        {            
            if (_thumb != null)
            {
                var currentThumbIsHitTestVisible = _thumb.IsHitTestVisible;
                _thumb.SetCurrentValue(IsHitTestVisibleProperty, false);
                _rightMouseUpCleanUpDisposable.Disposable = Disposable.Create(() =>
                {
                    _thumb.SetCurrentValue(IsHitTestVisibleProperty, currentThumbIsHitTestVisible);
                });
            }
            else
            {
                _rightMouseUpCleanUpDisposable.Disposable = Disposable.Empty;
            }            
            
            base.OnPreviewMouseRightButtonDown(e);
        }

        protected override void OnPreviewMouseRightButtonUp(MouseButtonEventArgs e)
        {
            _rightMouseUpCleanUpDisposable.Disposable = Disposable.Empty;
            base.OnPreviewMouseRightButtonUp(e);
        }

        private void LostMouseAfterSeizeHandler(object sender, MouseEventArgs mouseEventArgs)
        {
            _isTemplateThumbWithMouseAfterSeize = false;
            Mouse.RemoveLostMouseCaptureHandler(this, LostMouseAfterSeizeHandler);
        }

        internal void InstigateDrag(Action<DragablzItem> continuation)
        {
            _dragSeizedContinuation = continuation;
            var thumb = GetTemplateChild(ThumbPartName) as Thumb;
            if (thumb != null)
            {
                thumb.CaptureMouse();             
            }
            else
                _seizeDragWithTemplate = true;
        }

        internal Point MouseAtDragStart { get; set; }

        internal string PartitionAtDragStart { get; set; }

        internal bool IsDropTargetFound { get; set; }

        private void ThumbOnDragCompleted(object sender, DragCompletedEventArgs dragCompletedEventArgs)
        {            
            OnDragCompleted(dragCompletedEventArgs);
            MouseAtDragStart = new Point();
        }        

        private void ThumbOnDragDelta(object sender, DragDeltaEventArgs dragDeltaEventArgs)
        {
            var thumb = (Thumb) sender;

            var previewEventArgs = new DragablzDragDeltaEventArgs(PreviewDragDelta, this, dragDeltaEventArgs);
            OnPreviewDragDelta(previewEventArgs);            
            if (previewEventArgs.Cancel)
                thumb.CancelDrag();
            if (!previewEventArgs.Handled)
            {
                var eventArgs = new DragablzDragDeltaEventArgs(DragDelta, this, dragDeltaEventArgs);
                OnDragDelta(eventArgs);
                if (eventArgs.Cancel)
                    thumb.CancelDrag();
            }
        }
        
        private void ThumbOnDragStarted(object sender, DragStartedEventArgs dragStartedEventArgs)
        {
            MouseAtDragStart = Mouse.GetPosition(this);
            OnDragStarted(new DragablzDragStartedEventArgs(DragStarted, this, dragStartedEventArgs));            
        }

        private void MouseDownHandler(object sender, RoutedEventArgs routedEventArgs)
        {
            OnMouseDownWithin(this);
        }

        private static void CustomThumbOnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            var thumb = (Thumb)sender;
            thumb.Loaded -= CustomThumbOnLoaded;
            ApplyCustomThumbSetting(thumb);
        }

        private Thumb FindCustomThumb()
        {
            return this.VisualTreeDepthFirstTraversal().OfType<Thumb>().FirstOrDefault(GetIsCustomThumb);
        }        

        private static void ApplyCustomThumbSetting(Thumb thumb)
        {            
            var dragablzItem = thumb.VisualTreeAncestory().OfType<DragablzItem>().FirstOrDefault();
            if (dragablzItem == null) throw new ApplicationException("Cannot find parent DragablzItem for custom thumb");

            var enableCustomThumb = (bool)thumb.GetValue(IsCustomThumbProperty);
            dragablzItem._customThumb = enableCustomThumb ? thumb : null;
            dragablzItem._templateSubscriptions.Disposable = dragablzItem.SelectAndSubscribeToThumb().Item2;

            if (dragablzItem._customThumb != null && dragablzItem._isTemplateThumbWithMouseAfterSeize)
                dragablzItem.Dispatcher.BeginInvoke(new Action(() => dragablzItem._customThumb.RaiseEvent(new MouseButtonEventArgs(InputManager.Current.PrimaryMouseDevice,
                        0,
                        MouseButton.Left) { RoutedEvent = MouseLeftButtonDownEvent })));
        }

        private Tuple<Thumb, IDisposable> SelectAndSubscribeToThumb()
        {
            var templateThumb = GetTemplateChild(ThumbPartName) as Thumb;
            templateThumb?.SetCurrentValue(IsHitTestVisibleProperty, _customThumb == null);
            
            _thumb = _customThumb ?? templateThumb;
            if (_thumb != null)
            {
                _thumb.DragStarted += ThumbOnDragStarted;
                _thumb.DragDelta += ThumbOnDragDelta;
                _thumb.DragCompleted += ThumbOnDragCompleted;
            }

            var tidyUpThumb = _thumb;
            var disposable = Disposable.Create(() =>
            {
                if (tidyUpThumb == null) return;
                tidyUpThumb.DragStarted -= ThumbOnDragStarted;
                tidyUpThumb.DragDelta -= ThumbOnDragDelta;
                tidyUpThumb.DragCompleted -= ThumbOnDragCompleted;
            });

            return new Tuple<Thumb, IDisposable>(_thumb, disposable);
        }
    }
}