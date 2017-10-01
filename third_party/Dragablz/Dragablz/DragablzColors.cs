using System;
using System.Windows;
using System.Windows.Media;
using Dragablz.Core;

namespace Dragablz
{
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// In supporting .Net 4.0 we don't have access to SystemParameters.WindowGlassBrush, and even then
    /// the opacity is not provided, so this class wraps up a few issues around here.
    /// </remarks>
    public static class DragablzColors
    {
        //TODO listen to changes from the OS to provide updates
        public static Color WindowBaseColor = Color.FromRgb(217, 217, 217);
        public static Brush WindowGlassBrush = GetWindowGlassBrush();
        public static Brush WindowGlassBalancedBrush = GetBalancedWindowGlassBrush();
        public static Brush WindowInactiveBrush = GetWindowInactiveBrush();

        private static Brush GetWindowGlassBrush()
        {
            var colorizationParams = new Native.DWMCOLORIZATIONPARAMS();
            Native.DwmGetColorizationParameters(ref colorizationParams);
            var frameColor = ToColor(colorizationParams.ColorizationColor);

            return new SolidColorBrush(frameColor);
        }


        private static Brush GetBalancedWindowGlassBrush()
        {
            var colorizationParams = new Native.DWMCOLORIZATIONPARAMS();
            Native.DwmGetColorizationParameters(ref colorizationParams);
            var frameColor = ToColor(colorizationParams.ColorizationColor);
            var blendedColor = BlendColor(frameColor, WindowBaseColor, 100f - colorizationParams.ColorizationColorBalance);

            return new SolidColorBrush(blendedColor);
        }

        private static Brush GetWindowInactiveBrush()
        {
            return new SolidColorBrush(SystemColors.MenuBarColor);
        }

        private static Color ToColor(UInt32 value)
        {
            return Color.FromArgb(255,
                (byte)(value >> 16),
                (byte)(value >> 8),
                (byte)value
                );
        }

        private static Color BlendColor(Color color1, Color color2, double percentage)
        {
            percentage = Math.Min(100, Math.Max(0, percentage));

            return Color.FromRgb(
                BlendColorChannel(color1.R, color2.R, percentage),
                BlendColorChannel(color1.G, color2.G, percentage),
                BlendColorChannel(color1.B, color2.B, percentage));
        }

        private static byte BlendColorChannel(double channel1, double channel2, double channel2Percentage)
        {
            var buff = channel1 + (channel2 - channel1) * channel2Percentage / 100D;
            return Math.Min((byte)Math.Round(buff), (byte)255);
        }   
        
    }
}