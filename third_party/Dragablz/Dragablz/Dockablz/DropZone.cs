using System.Windows;
using System.Windows.Controls;

namespace Dragablz.Dockablz
{
    public class DropZone : Control
    {
        static DropZone()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DropZone), new FrameworkPropertyMetadata(typeof(DropZone)));            
        }

        public static readonly DependencyProperty LocationProperty = DependencyProperty.Register(
            "Location", typeof (DropZoneLocation), typeof (DropZone), new PropertyMetadata(default(DropZoneLocation)));

        public DropZoneLocation Location
        {
            get { return (DropZoneLocation) GetValue(LocationProperty); }
            set { SetValue(LocationProperty, value); }
        }

        private static readonly DependencyPropertyKey IsOfferedPropertyKey =
            DependencyProperty.RegisterReadOnly(
                "IsOffered", typeof (bool), typeof (DropZone),
                new PropertyMetadata(default(bool)));

        public static readonly DependencyProperty IsOfferedProperty =
            IsOfferedPropertyKey.DependencyProperty;

        public bool IsOffered
        {
            get { return (bool) GetValue(IsOfferedProperty); }
            internal set { SetValue(IsOfferedPropertyKey, value); }
        }

    }
}