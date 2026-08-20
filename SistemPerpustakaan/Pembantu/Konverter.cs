using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SistemPerpustakaan.Pembantu
{
    /// <summary>
    /// Mengambil huruf pertama string sebagai inisial.
    /// Digunakan untuk avatar pengguna di tabel.
    /// </summary>
    public class StringToInisialConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            var str = value as string;
            return string.IsNullOrEmpty(str) ? "?" : str[0].ToString().ToUpper();
        }
        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    /// <summary>
    /// Mengubah bool Tersedia → warna latar badge ketersediaan.
    /// true = hijau muda, false = merah muda
    /// </summary>
    public class BoolToWarnaConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            bool tersedia = value is bool && (bool)value;
            return tersedia
                ? new SolidColorBrush(Color.FromRgb(0xF0, 0xFD, 0xF4))
                : new SolidColorBrush(Color.FromRgb(0xFE, 0xF2, 0xF2));
        }
        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Mengubah bool Tersedia → warna teks badge ketersediaan.
    /// true = hijau, false = merah
    /// </summary>
    public class BoolToWarnaTeksConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            bool tersedia = value is bool && (bool)value;
            return tersedia
                ? new SolidColorBrush(Color.FromRgb(0x16, 0xA3, 0x4A))
                : new SolidColorBrush(Color.FromRgb(0xDC, 0x26, 0x26));
        }
        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Membalik nilai bool.
    /// Digunakan untuk IsEnabled binding (tombol Tandai Lunas).
    /// </summary>
    public class BoolInverterConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            return value is bool && !(bool)value;
        }
        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            return value is bool && !(bool)value;
        }
    }
}
