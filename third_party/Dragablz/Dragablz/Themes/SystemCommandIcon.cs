using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace Dragablz.Themes
{
    public enum SystemCommandType
    {
        CloseWindow,
        MaximizeWindow,
        MinimzeWindow,
        RestoreWindow
    }

    public class SystemCommandIcon : Control
    {
        static SystemCommandIcon()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SystemCommandIcon), new FrameworkPropertyMetadata(typeof(SystemCommandIcon)));
        }

        public static readonly DependencyProperty SystemCommandTypeProperty = DependencyProperty.Register(
            "SystemCommandType", typeof (SystemCommandType), typeof (SystemCommandIcon), new PropertyMetadata(default(SystemCommandType)));

        public SystemCommandType SystemCommandType
        {
            get { return (SystemCommandType) GetValue(SystemCommandTypeProperty); }
            set { SetValue(SystemCommandTypeProperty, value); }
        }
    }
}
