using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using SistemPerpustakaan.Model;

namespace SistemPerpustakaan.Tampilan.Komponen
{
    public partial class KartuBuku : UserControl
    {
        public event EventHandler<Buku> PadaKlik;

        private Buku _buku;
        private bool _dipilih;

        // Palet warna per kategori
        private static readonly string[,] PaletKategori =
        {
            // Kategori, Warna1, Warna2
            { "Fiksi",             "#8B5CF6", "#6D28D9" },
            { "Non-Fiksi",         "#0891B2", "#0E7490" },
            { "Sejarah",           "#D97706", "#B45309" },
            { "Teknologi",         "#2563EB", "#1D4ED8" },
            { "Pengembangan Diri", "#059669", "#047857" },
            { "Sains",             "#0284C7", "#0369A1" },
            { "Biografi",          "#DB2777", "#BE185D" },
            { "Pendidikan",        "#7C3AED", "#6D28D9" },
            { "Agama",             "#B45309", "#92400E" },
        };

        private readonly TranslateTransform _slideTransform = new TranslateTransform(0, 0);

        public KartuBuku()
        {
            InitializeComponent();
            KartuBorder.RenderTransform = _slideTransform;
        }

        public void IsiBuku(Buku buku)
        {
            _buku             = buku;
            TeksJudul.Text    = buku.Judul;
            TeksPenulis.Text  = buku.Penulis;
            TeksKategori.Text = buku.Kategori ?? "Umum";

            // Set warna gradient berdasarkan kategori
            AturdWarnaBerdasarkanKategori(buku.Kategori);

            // Set gambar sampul jika ada
            var gambar = buku.GambarSampul;
            if (gambar != null && gambar.UriSource != null)
            {
                GambarSampul.Source      = gambar;
                BgPlaceholder.Opacity    = 0.6; // redup sedikit jika ada gambar
            }
            else
            {
                GambarSampul.Source   = null;
                BgPlaceholder.Opacity = 1;
            }

            // Set ikon placeholder berdasarkan kategori
            string ikon = "📖";
            if (buku.Kategori != null)
            {
                string kat = buku.Kategori.ToLower();
                if (kat.Contains("fiksi"))            ikon = "✨";
                else if (kat.Contains("teknologi"))   ikon = "💻";
                else if (kat.Contains("sejarah"))     ikon = "🏛";
                else if (kat.Contains("pengembangan")) ikon = "🚀";
                else if (kat.Contains("sains"))       ikon = "🔬";
                else if (kat.Contains("biografi"))    ikon = "👤";
                else if (kat.Contains("pendidikan"))  ikon = "🎓";
                else if (kat.Contains("agama"))       ikon = "☯";
            }
            IkonPlaceholder.Text = ikon;

            // Badge ketersediaan
            if (buku.Tersedia)
            {
                BadgeStatus.Background =
                    new SolidColorBrush(Color.FromRgb(0x16, 0xA3, 0x4A));
                TeksBadge.Text = buku.StokTersedia + " tersedia";
            }
            else
            {
                BadgeStatus.Background =
                    new SolidColorBrush(Color.FromRgb(0xDC, 0x26, 0x26));
                TeksBadge.Text = "Tidak tersedia";
            }
        }

        private void AturdWarnaBerdasarkanKategori(string kategori)
        {
            // Default: biru
            string w1 = "#3B82F6";
            string w2 = "#1D4ED8";

            if (!string.IsNullOrEmpty(kategori))
            {
                for (int i = 0; i < PaletKategori.GetLength(0); i++)
                {
                    if (kategori.ToLower().Contains(
                        PaletKategori[i, 0].ToLower()))
                    {
                        w1 = PaletKategori[i, 1];
                        w2 = PaletKategori[i, 2];
                        break;
                    }
                }
            }

            GradWarna1.Color = (Color)ColorConverter.ConvertFromString(w1);
            GradWarna2.Color = (Color)ColorConverter.ConvertFromString(w2);
        }

        public void TandaiDipilih(bool dipilih)
        {
            _dipilih = dipilih;
            BadgeKeranjang.Visibility = dipilih
                ? Visibility.Visible : Visibility.Collapsed;

            KartuBorder.BorderBrush = dipilih
                ? new SolidColorBrush(Color.FromRgb(0x1D, 0x4E, 0xD8))
                : new SolidColorBrush(Color.FromRgb(0xE5, 0xE7, 0xEB));

            KartuBorder.BorderThickness = dipilih
                ? new Thickness(2.5) : new Thickness(1.5);
        }

        public Buku   AmbilBuku() { return _buku; }
        public bool   Dipilih     { get { return _dipilih; } }

        private void Kartu_MouseEnter(object sender,
            System.Windows.Input.MouseEventArgs e)
        {
            AnimasiHover(-3, 22, 0.14);
        }

        private void Kartu_MouseLeave(object sender,
            System.Windows.Input.MouseEventArgs e)
        {
            AnimasiHover(0, 10, 0.07);
        }

        private void AnimasiHover(double targetY, double targetBlur, double targetOpacity)
        {
            var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
            var dur  = new Duration(TimeSpan.FromMilliseconds(150));

            var slideAnim = new DoubleAnimation(targetY, dur) { EasingFunction = ease };
            _slideTransform.BeginAnimation(TranslateTransform.YProperty, slideAnim);

            if (KartuBorder.Effect is DropShadowEffect eff)
            {
                eff.BeginAnimation(DropShadowEffect.BlurRadiusProperty,
                    new DoubleAnimation(targetBlur, dur) { EasingFunction = ease });
                eff.BeginAnimation(DropShadowEffect.OpacityProperty,
                    new DoubleAnimation(targetOpacity, dur) { EasingFunction = ease });
            }
        }

        private void Kartu_Klik(object sender,
            System.Windows.Input.MouseButtonEventArgs e)
        {
            if (PadaKlik != null) PadaKlik(this, _buku);
        }
    }
}
