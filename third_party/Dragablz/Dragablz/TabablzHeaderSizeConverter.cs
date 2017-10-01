using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace Dragablz
{
    /// <summary>
    /// Provides a little help for sizing the header panel in the tab control
    /// </summary>
    public class TabablzHeaderSizeConverter : IMultiValueConverter
    {        
        public Orientation Orientation { get; set; }

        /// <summary>
        /// The first value should be the total size available size, typically the parent control size.  
        /// The second value should be from <see cref="DragablzItemsControl.ItemsPresenterWidthProperty"/> or (height equivalent)
        /// All additional values should be siblings sizes (width or height) which will affect (reduce) the available size.
        /// </summary>
        /// <param name="values"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null) throw new ArgumentNullException("values");

            if (values.Length < 2) return Binding.DoNothing;

            var val = values
                .Skip(2)
                .OfType<double>()
                .Where(d => !double.IsInfinity(d) && !double.IsNaN(d))
                .Aggregate(values.OfType<double>().First(), (current, diminish) => current - diminish);

            var maxWidth = values.Take(2).OfType<double>().Min();

            return Math.Min(Math.Max(val, 0), maxWidth);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}