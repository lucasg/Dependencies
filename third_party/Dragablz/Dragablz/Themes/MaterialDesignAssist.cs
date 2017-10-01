using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace Dragablz.Themes
{
    /// <summary>
    /// Helper propries for configuring the material design style.
    /// </summary>
    public static class MaterialDesignAssist
    {
        /// <summary>
        /// Framework use only.
        /// </summary>
        public static readonly DependencyProperty IndicatorBrushProperty = DependencyProperty.RegisterAttached(
            "IndicatorBrush", typeof (Brush), typeof (MaterialDesignAssist), new PropertyMetadata(default(Brush)));

        /// <summary>
        /// The indicator (underline) brush.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="value"></param>
        public static void SetIndicatorBrush(DependencyObject element, Brush value)
        {
            element.SetValue(IndicatorBrushProperty, value);
        }

        /// <summary>
        /// The indicator (underline) brush.
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        public static Brush GetIndicatorBrush(DependencyObject element)
        {
            return (Brush) element.GetValue(IndicatorBrushProperty);
        }
    }
}
