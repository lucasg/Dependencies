using System;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Dragablz.Core;
using Dragablz.Dockablz;
using Dragablz.Referenceless;

namespace Dragablz
{
    /// <summary>
    /// It is not necessary to use a <see cref="DragablzWindow"/> to gain tab dragging features.
    /// What this Window does is allow a quick way to remove the Window border, and support transparency whilst
    /// dragging.  
    /// </summary>
    [TemplatePart(Name = WindowSurfaceGridPartName, Type = typeof(Grid))]
    [TemplatePart(Name = WindowRestoreThumbPartName, Type = typeof(Thumb))]
    [TemplatePart(Name = WindowResizeThumbPartName, Type = typeof(Thumb))]
    public class DragablzWindow : Window
    {
        public const string WindowSurfaceGridPartName = "PART_WindowSurface";
        public const string WindowRestoreThumbPartName = "PART_WindowRestoreThumb";
        public const string WindowResizeThumbPartName = "PART_WindowResizeThumb";
        private readonly SerialDisposable _templateSubscription = new SerialDisposable();

        public static RoutedCommand CloseWindowCommand = new RoutedCommand();
        public static RoutedCommand RestoreWindowCommand = new RoutedCommand();
        public static RoutedCommand MaximizeWindowCommand = new RoutedCommand();
        public static RoutedCommand MinimizeWindowCommand = new RoutedCommand();        

        private const int ResizeMargin = 4;
        private Size _sizeWhenResizeBegan;
        private Point _screenMousePointWhenResizeBegan;
        private Point _windowLocationPointWhenResizeBegan;
        private SizeGrip _resizeType;

        private static SizeGrip[] _leftMode = new[] { SizeGrip.TopLeft, SizeGrip.Left, SizeGrip.BottomLeft };
        private static SizeGrip[] _rightMode = new[] { SizeGrip.TopRight, SizeGrip.Right, SizeGrip.BottomRight };
        private static SizeGrip[] _topMode = new[] { SizeGrip.TopLeft, SizeGrip.Top, SizeGrip.TopRight };
        private static SizeGrip[] _bottomMode = new[] { SizeGrip.BottomLeft, SizeGrip.Bottom, SizeGrip.BottomRight };

        private static double _xScale = 1;
        private static double _yScale = 1;
        private static bool _dpiInitialized = false;

        static DragablzWindow()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DragablzWindow), new FrameworkPropertyMetadata(typeof(DragablzWindow)));            
        }

        public DragablzWindow()
        {
            AddHandler(DragablzItem.DragStarted, new DragablzDragStartedEventHandler(ItemDragStarted), true);
            AddHandler(DragablzItem.DragCompleted, new DragablzDragCompletedEventHandler(ItemDragCompleted), true);
            CommandBindings.Add(new CommandBinding(CloseWindowCommand, CloseWindowExecuted));
            CommandBindings.Add(new CommandBinding(MaximizeWindowCommand, MaximizeWindowExecuted));
            CommandBindings.Add(new CommandBinding(MinimizeWindowCommand, MinimizeWindowExecuted));
            CommandBindings.Add(new CommandBinding(RestoreWindowCommand, RestoreWindowExecuted));                        
        }

        private static readonly DependencyPropertyKey IsWindowBeingDraggedByTabPropertyKey =
            DependencyProperty.RegisterReadOnly(
                "IsBeingDraggedByTab", typeof (bool), typeof (DragablzWindow),
                new PropertyMetadata(default(bool)));

        public static readonly DependencyProperty IsBeingDraggedByTabProperty =
            IsWindowBeingDraggedByTabPropertyKey.DependencyProperty;

        public bool IsBeingDraggedByTab
        {
            get { return (bool) GetValue(IsBeingDraggedByTabProperty); }
            private set { SetValue(IsWindowBeingDraggedByTabPropertyKey, value); }
        }        

        private void ItemDragCompleted(object sender, DragablzDragCompletedEventArgs e)
        {            
            IsBeingDraggedByTab = false;
        }

        private void ItemDragStarted(object sender, DragablzDragStartedEventArgs e)
        {
            var sourceOfDragItemsControl = ItemsControl.ItemsControlFromItemContainer(e.DragablzItem) as DragablzItemsControl;
            if (sourceOfDragItemsControl == null) return;

            var sourceTab = TabablzControl.GetOwnerOfHeaderItems(sourceOfDragItemsControl);
            if (sourceTab == null) return;

            if (sourceOfDragItemsControl.Items.Count != 1
                || (sourceTab.InterTabController != null && !sourceTab.InterTabController.MoveWindowWithSolitaryTabs)
                || Layout.IsContainedWithinBranch(sourceOfDragItemsControl))
                return;

            IsBeingDraggedByTab = true;
        }

        public override void OnApplyTemplate()
        {
            var windowSurfaceGrid = GetTemplateChild(WindowSurfaceGridPartName) as Grid;
            var windowRestoreThumb = GetTemplateChild(WindowRestoreThumbPartName) as Thumb;
            var windowResizeThumb = GetTemplateChild(WindowResizeThumbPartName) as Thumb;

            _templateSubscription.Disposable = Disposable.Create(() =>
            {
                if (windowSurfaceGrid != null)
                {
                    windowSurfaceGrid.MouseLeftButtonDown -= WindowSurfaceGridOnMouseLeftButtonDown;
                }

                if (windowRestoreThumb != null)
                {
                    windowRestoreThumb.DragDelta -= WindowMoveThumbOnDragDelta;
                    windowRestoreThumb.MouseDoubleClick -= WindowRestoreThumbOnMouseDoubleClick;
                }

                if (windowResizeThumb == null) return;

                windowResizeThumb.MouseMove -= WindowResizeThumbOnMouseMove;
                windowResizeThumb.DragStarted -= WindowResizeThumbOnDragStarted;
                windowResizeThumb.DragDelta -= WindowResizeThumbOnDragDelta;
                windowResizeThumb.DragCompleted -= WindowResizeThumbOnDragCompleted;
            });

            base.OnApplyTemplate();

            if (windowSurfaceGrid != null)
            {
                windowSurfaceGrid.MouseLeftButtonDown += WindowSurfaceGridOnMouseLeftButtonDown;
            }

            if (windowRestoreThumb != null)
            {
                windowRestoreThumb.DragDelta += WindowMoveThumbOnDragDelta;
                windowRestoreThumb.MouseDoubleClick += WindowRestoreThumbOnMouseDoubleClick;
            }

            if (windowResizeThumb == null) return;

            windowResizeThumb.MouseMove += WindowResizeThumbOnMouseMove;                
            windowResizeThumb.DragStarted += WindowResizeThumbOnDragStarted;
            windowResizeThumb.DragDelta += WindowResizeThumbOnDragDelta;
            windowResizeThumb.DragCompleted += WindowResizeThumbOnDragCompleted;
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            var resizeThumb = GetTemplateChild(WindowResizeThumbPartName) as Thumb;
            if (resizeThumb != null)
            {
                var outerRectangleGeometry = new RectangleGeometry(new Rect(sizeInfo.NewSize));
                var innerRectangleGeometry =
                    new RectangleGeometry(new Rect(ResizeMargin, ResizeMargin, sizeInfo.NewSize.Width - ResizeMargin * 2, sizeInfo.NewSize.Height - ResizeMargin*2));
                resizeThumb.Clip = new CombinedGeometry(GeometryCombineMode.Exclude, outerRectangleGeometry,
                    innerRectangleGeometry);
            }

            base.OnRenderSizeChanged(sizeInfo);
        }

        protected IntPtr CriticalHandle
        {
            get
            {
                var value = typeof (Window).GetProperty("CriticalHandle", BindingFlags.NonPublic | BindingFlags.Instance)
                    .GetValue(this, new object[0]);
                return (IntPtr) value;
            }
        }

        private void WindowSurfaceGridOnMouseLeftButtonDown(object sender, MouseButtonEventArgs mouseButtonEventArgs)
        {
            if (mouseButtonEventArgs.ChangedButton != MouseButton.Left) return;
            if (mouseButtonEventArgs.ClickCount == 1)
                DragMove();
            if (mouseButtonEventArgs.ClickCount == 2)
                WindowState = WindowState.Maximized;
        }

        private static void WindowResizeThumbOnMouseMove(object sender, MouseEventArgs mouseEventArgs)
        {
            var thumb = (Thumb)sender;
            var mousePositionInThumb = Mouse.GetPosition(thumb);
            thumb.Cursor = SelectCursor(SelectSizingMode(mousePositionInThumb, thumb.RenderSize));
        }

        private void WindowRestoreThumbOnMouseDoubleClick(object sender, MouseButtonEventArgs mouseButtonEventArgs)
        {            
            WindowState = WindowState.Normal;            
        }

        private void WindowResizeThumbOnDragCompleted(object sender, DragCompletedEventArgs dragCompletedEventArgs)
        {
            Cursor = Cursors.Arrow;
        }

        private void WindowResizeThumbOnDragDelta(object sender, DragDeltaEventArgs dragDeltaEventArgs)
        {
            var mousePositionInWindow = Mouse.GetPosition(this);
            var currentScreenMousePoint = PointToScreen(mousePositionInWindow);

            var width = _sizeWhenResizeBegan.Width;
            var height = _sizeWhenResizeBegan.Height;
            var left = _windowLocationPointWhenResizeBegan.X;
            var top = _windowLocationPointWhenResizeBegan.Y;

            if (_leftMode.Contains(_resizeType))
            {
                var diff = currentScreenMousePoint.X - _screenMousePointWhenResizeBegan.X;
                diff /= _xScale;
                var suggestedWidth = width + -diff;
                left += diff;
                width = suggestedWidth;
            }
            if (_rightMode.Contains(_resizeType))
            {
                var diff = currentScreenMousePoint.X - _screenMousePointWhenResizeBegan.X;
                diff /= _xScale;
                width += diff;
            }
            if (_topMode.Contains(_resizeType))
            {
                var diff = currentScreenMousePoint.Y - _screenMousePointWhenResizeBegan.Y;
                diff /= _yScale;
                height += -diff;
                top += diff;
            }
            if (_bottomMode.Contains(_resizeType))
            {
                var diff = currentScreenMousePoint.Y - _screenMousePointWhenResizeBegan.Y;
                diff /= _yScale; 
                height += diff;
            }

            width = Math.Max(MinWidth, width);
            height = Math.Max(MinHeight, height);   
            //TODO must try harder.
            left = Math.Min(left, _windowLocationPointWhenResizeBegan.X + _sizeWhenResizeBegan.Width - ResizeMargin*4);
            //TODO must try harder.
            top = Math.Min(top, _windowLocationPointWhenResizeBegan.Y + _sizeWhenResizeBegan.Height - ResizeMargin * 4);
            SetCurrentValue(WidthProperty, width);
            SetCurrentValue(HeightProperty, height);
            SetCurrentValue(LeftProperty, left);
            SetCurrentValue(TopProperty, top);
        }

        private void GetDPI()
        {
            if (_dpiInitialized)
            {
                return;
            }

            Matrix m = PresentationSource.FromVisual(this).CompositionTarget.TransformToDevice;
            _xScale = m.M11;
            _yScale = m.M22;
            _dpiInitialized = true;
        }

        private void WindowResizeThumbOnDragStarted(object sender, DragStartedEventArgs dragStartedEventArgs)
        {
            _sizeWhenResizeBegan = new Size(ActualWidth, ActualHeight);
            _windowLocationPointWhenResizeBegan = new Point(Left, Top);
            var mousePositionInWindow = Mouse.GetPosition(this);
            _screenMousePointWhenResizeBegan = PointToScreen(mousePositionInWindow);

            var thumb = (Thumb)sender;
            var mousePositionInThumb = Mouse.GetPosition(thumb);
            _resizeType = SelectSizingMode(mousePositionInThumb, thumb.RenderSize);

            GetDPI();
        }

        private static SizeGrip SelectSizingMode(Point mousePositionInThumb, Size thumbSize)
        {
            if (mousePositionInThumb.X <= ResizeMargin)
            {
                if (mousePositionInThumb.Y <= ResizeMargin)
                    return SizeGrip.TopLeft;
                if (mousePositionInThumb.Y >= thumbSize.Height - ResizeMargin)
                    return SizeGrip.BottomLeft;
                return SizeGrip.Left;
            }

            if (mousePositionInThumb.X >= thumbSize.Width - ResizeMargin)
            {
                if (mousePositionInThumb.Y <= ResizeMargin)
                    return SizeGrip.TopRight;
                if (mousePositionInThumb.Y >= thumbSize.Height - ResizeMargin)
                    return SizeGrip.BottomRight;
                return SizeGrip.Right;
            }

            if (mousePositionInThumb.Y <= ResizeMargin)
                return SizeGrip.Top;

            return SizeGrip.Bottom;
        }

        private static Cursor SelectCursor(SizeGrip sizeGrip)
        {
            switch (sizeGrip)
            {                
                case SizeGrip.Left:
                    return Cursors.SizeWE;
                case SizeGrip.TopLeft:
                    return Cursors.SizeNWSE;
                case SizeGrip.Top:
                    return Cursors.SizeNS;
                case SizeGrip.TopRight:
                    return Cursors.SizeNESW;
                case SizeGrip.Right:
                    return Cursors.SizeWE;
                case SizeGrip.BottomRight:
                    return Cursors.SizeNWSE;
                case SizeGrip.Bottom:
                    return Cursors.SizeNS;
                case SizeGrip.BottomLeft:
                    return Cursors.SizeNESW;
                default:
                    return Cursors.Arrow;
            }
        }

        private void WindowMoveThumbOnDragDelta(object sender, DragDeltaEventArgs dragDeltaEventArgs)
        {
            if (WindowState != WindowState.Maximized ||
                (!(Math.Abs(dragDeltaEventArgs.HorizontalChange) > 2) &&
                 !(Math.Abs(dragDeltaEventArgs.VerticalChange) > 2))) return;

            var cursorPos = Native.GetRawCursorPos();
            WindowState = WindowState.Normal;

            Top = cursorPos.Y - 2;
            Left = cursorPos.X - RestoreBounds.Width / 2;
            
            var lParam = (int)(uint)cursorPos.X | (cursorPos.Y << 16);
            Native.SendMessage(CriticalHandle, WindowMessage.WM_LBUTTONUP, (IntPtr)HitTest.HT_CAPTION,
                (IntPtr)lParam);
            Native.SendMessage(CriticalHandle, WindowMessage.WM_SYSCOMMAND, (IntPtr)SystemCommand.SC_MOUSEMOVE,
                IntPtr.Zero);            
        }

        private void RestoreWindowExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            Native.PostMessage(new WindowInteropHelper(this).Handle, WindowMessage.WM_SYSCOMMAND, (IntPtr)SystemCommand.SC_RESTORE, IntPtr.Zero);
        }

        private void MinimizeWindowExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            Native.PostMessage(new WindowInteropHelper(this).Handle, WindowMessage.WM_SYSCOMMAND, (IntPtr)SystemCommand.SC_MINIMIZE, IntPtr.Zero);
        }

        private void MaximizeWindowExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            Native.PostMessage(new WindowInteropHelper(this).Handle, WindowMessage.WM_SYSCOMMAND, (IntPtr)SystemCommand.SC_MAXIMIZE, IntPtr.Zero);
        }

        private void CloseWindowExecuted(object sender, ExecutedRoutedEventArgs executedRoutedEventArgs)
        {
            Native.PostMessage(new WindowInteropHelper(this).Handle, WindowMessage.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
        }             
    }
}
