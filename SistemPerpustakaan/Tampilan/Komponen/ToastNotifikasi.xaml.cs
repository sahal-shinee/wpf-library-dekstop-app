using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace SistemPerpustakaan.Tampilan.Komponen
{
    public enum TipeToast { Sukses, Gagal, Info, Peringatan }

    public partial class ToastNotifikasi : System.Windows.Controls.UserControl
    {
        private DispatcherTimer _timer;

        public ToastNotifikasi() { InitializeComponent(); }

        public void Tampilkan(string pesan, TipeToast tipe = TipeToast.Sukses)
        {
            // Hentikan timer sebelumnya jika sedang berjalan
            _timer?.Stop();

            TeksPesan.Text = pesan;

            switch (tipe)
            {
                case TipeToast.Sukses:
                    ToastBorder.Background = new SolidColorBrush(Color.FromRgb(0x16, 0xA3, 0x4A));
                    TeksIkon.Text = "✓";
                    break;
                case TipeToast.Gagal:
                    ToastBorder.Background = new SolidColorBrush(Color.FromRgb(0xDC, 0x26, 0x26));
                    TeksIkon.Text = "✕";
                    break;
                case TipeToast.Peringatan:
                    ToastBorder.Background = new SolidColorBrush(Color.FromRgb(0xD9, 0x77, 0x06));
                    TeksIkon.Text = "⚠";
                    break;
                default:
                    ToastBorder.Background = new SolidColorBrush(Color.FromRgb(0x25, 0x63, 0xEB));
                    TeksIkon.Text = "ℹ";
                    break;
            }

            Visibility = Visibility.Visible;
            IsHitTestVisible = false;
            Opacity = 0;

            // Slide up + fade in
            var slideIn = new DoubleAnimation(80, 0,
                new Duration(TimeSpan.FromMilliseconds(320)))
            { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };

            var fadeIn = new DoubleAnimation(0, 1,
                new Duration(TimeSpan.FromMilliseconds(250)));

            SlideTransform.BeginAnimation(TranslateTransform.YProperty, slideIn);
            this.BeginAnimation(OpacityProperty, fadeIn);

            // Auto-sembunyikan setelah 3 detik
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
            _timer.Tick += (s, e) => { _timer.Stop(); Sembunyikan(); };
            _timer.Start();
        }

        private void Sembunyikan()
        {
            var slideOut = new DoubleAnimation(0, 60,
                new Duration(TimeSpan.FromMilliseconds(280)))
            { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn } };

            var fadeOut = new DoubleAnimation(1, 0,
                new Duration(TimeSpan.FromMilliseconds(250)));

            fadeOut.Completed += (s, e) => Visibility = Visibility.Collapsed;

            SlideTransform.BeginAnimation(TranslateTransform.YProperty, slideOut);
            this.BeginAnimation(OpacityProperty, fadeOut);
        }
    }
}
