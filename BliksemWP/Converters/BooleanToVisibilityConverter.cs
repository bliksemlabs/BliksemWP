using System;
using System.Windows;
using System.Windows.Data;

namespace BliksemWP.Converters
{
    public class BooleanToVisibilityConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            bool inverse = false;
            if (parameter != null)
            {
                bool.TryParse(parameter.ToString(), out inverse);
            }

            if (targetType != typeof(Visibility))
                throw new InvalidOperationException("The target must be a visibility");

            if (value == null)
            {
                return Visibility.Collapsed;
            }

            if (inverse)
            {
                return (bool)value ? Visibility.Collapsed : Visibility.Visible;
            }
            else
            {
                return (bool)value ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        #endregion
    }
}