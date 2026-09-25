using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using Microsoft.Win32;

namespace Dependencies
{
    public enum AppTheme
    {
        System,
        Light,
        Dark
    }

    /// <summary>
    /// Handles switching between light and dark application themes.
    /// </summary>
    public static class ThemeManager
    {
        private const string LightThemeUri = "pack://application:,,,/DependenciesGui;component/Themes/LightTheme.xaml";
        private const string DarkThemeUri = "pack://application:,,,/DependenciesGui;component/Themes/DarkTheme.xaml";

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int pvAttribute, int cbAttribute);

        public static bool IsDarkThemeActive { get; private set; }

        public static AppTheme CurrentSetting
        {
            get
            {
                AppTheme theme;
                if (Enum.TryParse(Properties.Settings.Default.Theme, out theme))
                    return theme;
                return AppTheme.System;
            }
        }

        public static void Initialize()
        {
            EventManager.RegisterClassHandler(typeof(Window), FrameworkElement.LoadedEvent, new RoutedEventHandler(OnWindowLoaded));
            SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
            ApplyTheme();
        }

        public static void Shutdown()
        {
            SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
        }

        public static void SetTheme(AppTheme theme)
        {
            Properties.Settings.Default.Theme = theme.ToString();
            ApplyTheme();
        }

        public static void ApplyTheme()
        {
            bool useDark = ShouldUseDarkTheme(CurrentSetting);

            var mergedDictionaries = Application.Current.Resources.MergedDictionaries;
            // Only match this application's own theme dictionaries. Third party dictionaries
            // (e.g. "pack://application:,,,/Dragablz;component/Themes/Generic.xaml") also contain
            // "/Themes/" and must never be replaced, otherwise their styles disappear.
            ResourceDictionary current = mergedDictionaries.FirstOrDefault(d =>
                d.Source != null &&
                (d.Source.OriginalString.IndexOf(LightThemeUri, StringComparison.OrdinalIgnoreCase) >= 0 ||
                 d.Source.OriginalString.IndexOf(DarkThemeUri, StringComparison.OrdinalIgnoreCase) >= 0));

            var newTheme = new ResourceDictionary { Source = new Uri(useDark ? DarkThemeUri : LightThemeUri, UriKind.Absolute) };
            if (current != null)
            {
                mergedDictionaries[mergedDictionaries.IndexOf(current)] = newTheme;
            }
            else
            {
                mergedDictionaries.Add(newTheme);
            }

            IsDarkThemeActive = useDark;

            foreach (Window window in Application.Current.Windows)
            {
                ApplyTitleBarTheme(window);
            }
        }

        private static bool ShouldUseDarkTheme(AppTheme theme)
        {
            switch (theme)
            {
                case AppTheme.Dark:
                    return true;
                case AppTheme.Light:
                    return false;
                default:
                    return IsSystemInDarkMode();
            }
        }

        private static bool IsSystemInDarkMode()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    object value = key?.GetValue("AppsUseLightTheme");
                    if (value is int)
                        return (int)value == 0;
                }
            }
            catch (Exception)
            {
            }

            return false;
        }

        private static void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            ApplyTitleBarTheme(sender as Window);
        }

        private static void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category != UserPreferenceCategory.General || CurrentSetting != AppTheme.System)
                return;

            Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (ShouldUseDarkTheme(AppTheme.System) != IsDarkThemeActive)
                    ApplyTheme();
            }));
        }

        private static void ApplyTitleBarTheme(Window window)
        {
            if (window == null)
                return;

            IntPtr hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero)
                return;

            int useDark = IsDarkThemeActive ? 1 : 0;
            try
            {
                if (DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDark, sizeof(int)) != 0)
                {
                    DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1, ref useDark, sizeof(int));
                }
            }
            catch (DllNotFoundException)
            {
            }
            catch (EntryPointNotFoundException)
            {
            }
        }
    }

    /// <summary>
    /// Drop-in replacement for <see cref="System.Windows.MessageBox"/> which follows the
    /// application theme. Because this type lives in the <c>Dependencies</c> namespace it
    /// takes precedence over the one imported through <c>using System.Windows;</c>, so every
    /// existing <c>MessageBox.Show(...)</c> call site automatically gets a themed dialog.
    /// </summary>
    public static class MessageBox
    {
        public static MessageBoxResult Show(string messageBoxText)
        {
            return Show(null, messageBoxText, string.Empty, MessageBoxButton.OK);
        }

        public static MessageBoxResult Show(string messageBoxText, string caption)
        {
            return Show(null, messageBoxText, caption, MessageBoxButton.OK);
        }

        public static MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button)
        {
            return Show(null, messageBoxText, caption, button);
        }

        public static MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon)
        {
            return Show(null, messageBoxText, caption, button);
        }

        public static MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult)
        {
            return Show(null, messageBoxText, caption, button);
        }

        public static MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult, MessageBoxOptions options)
        {
            return Show(null, messageBoxText, caption, button);
        }

        public static MessageBoxResult Show(Window owner, string messageBoxText)
        {
            return Show(owner, messageBoxText, string.Empty, MessageBoxButton.OK);
        }

        public static MessageBoxResult Show(Window owner, string messageBoxText, string caption)
        {
            return Show(owner, messageBoxText, caption, MessageBoxButton.OK);
        }

        public static MessageBoxResult Show(Window owner, string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon)
        {
            return Show(owner, messageBoxText, caption, button);
        }

        public static MessageBoxResult Show(Window owner, string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult)
        {
            return Show(owner, messageBoxText, caption, button);
        }

        public static MessageBoxResult Show(Window owner, string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult, MessageBoxOptions options)
        {
            return Show(owner, messageBoxText, caption, button);
        }

        public static MessageBoxResult Show(Window owner, string messageBoxText, string caption, MessageBoxButton button)
        {
            MessageBoxResult result = button == MessageBoxButton.OK ? MessageBoxResult.OK : MessageBoxResult.Cancel;

            var message = new TextBlock
            {
                Text = messageBoxText,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 460,
                Margin = new Thickness(16, 16, 16, 8)
            };
            message.SetResourceReference(TextBlock.ForegroundProperty, SystemColors.ControlTextBrushKey);

            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(8, 4, 8, 12)
            };

            var layout = new StackPanel();
            layout.Children.Add(message);
            layout.Children.Add(buttons);

            var window = new Window
            {
                Title = caption ?? string.Empty,
                Content = layout,
                SizeToContent = SizeToContent.WidthAndHeight,
                ResizeMode = ResizeMode.NoResize,
                ShowInTaskbar = false,
                MinWidth = 260,
                WindowStartupLocation = owner != null ? WindowStartupLocation.CenterOwner : WindowStartupLocation.CenterScreen
            };
            window.SetResourceReference(Window.BackgroundProperty, SystemColors.ControlBrushKey);
            window.SetResourceReference(Window.ForegroundProperty, SystemColors.ControlTextBrushKey);

            Window ownerWindow = owner ?? GetActiveWindow();
            if (ownerWindow != null && !ReferenceEquals(ownerWindow, window))
            {
                window.Owner = ownerWindow;
            }

            switch (button)
            {
                case MessageBoxButton.OK:
                    AddButton(buttons, window, "OK", MessageBoxResult.OK, r => result = r, true, true);
                    break;

                case MessageBoxButton.OKCancel:
                    AddButton(buttons, window, "OK", MessageBoxResult.OK, r => result = r, true, false);
                    AddButton(buttons, window, "Cancel", MessageBoxResult.Cancel, r => result = r, false, true);
                    break;

                case MessageBoxButton.YesNo:
                    AddButton(buttons, window, "Yes", MessageBoxResult.Yes, r => result = r, true, false);
                    AddButton(buttons, window, "No", MessageBoxResult.No, r => result = r, false, true);
                    result = MessageBoxResult.No;
                    break;

                case MessageBoxButton.YesNoCancel:
                    AddButton(buttons, window, "Yes", MessageBoxResult.Yes, r => result = r, true, false);
                    AddButton(buttons, window, "No", MessageBoxResult.No, r => result = r, false, false);
                    AddButton(buttons, window, "Cancel", MessageBoxResult.Cancel, r => result = r, false, true);
                    break;
            }

            window.ShowDialog();
            return result;
        }

        private static void AddButton(Panel host, Window window, string caption, MessageBoxResult result,
                                      Action<MessageBoxResult> setResult, bool isDefault, bool isCancel)
        {
            var button = new Button
            {
                Content = caption,
                MinWidth = 75,
                MinHeight = 23,
                Margin = new Thickness(8, 0, 0, 0),
                IsDefault = isDefault,
                IsCancel = isCancel
            };

            button.Click += (sender, e) =>
            {
                setResult(result);
                window.Close();
            };

            host.Children.Add(button);
        }

        private static Window GetActiveWindow()
        {
            if (Application.Current == null)
            {
                return null;
            }

            foreach (Window window in Application.Current.Windows)
            {
                if (window.IsActive)
                {
                    return window;
                }
            }

            return Application.Current.MainWindow;
        }
    }
}
