using System.Windows;
using System.Windows.Media;
using SistemPerpustakaan.Model;

namespace SistemPerpustakaan.Tampilan.Bersama
{
    public partial class JendelaDetailBuku : Window
    {
        private Buku _buku;
        public bool  BukuDipilih { get; private set; }

        public JendelaDetailBuku(Buku buku, bool modeAdmin = false)
        {
            InitializeComponent();
            _buku = buku;
            IsiBuku(modeAdmin);
        }

        private void IsiBuku(bool modeAdmin)
        {
            GambarSampul.Source   = _buku.GambarSampul;
            TeksJudul.Text        = _buku.Judul;
            TeksPenulis.Text      = "oleh " + _buku.Penulis;
            TeksKategori.Text     = _buku.Kategori ?? "Umum";
            TeksPenerbit.Text     = _buku.Penerbit ?? "-";
            TeksTahun.Text        = _buku.TahunTerbit.HasValue
                                    ? _buku.TahunTerbit.Value.ToString() : "-";
            TeksIsbn.Text         = _buku.Isbn ?? "-";
            TeksDeskripsi.Text    = string.IsNullOrEmpty(_buku.Deskripsi)
                                    ? "Tidak ada deskripsi." : _buku.Deskripsi;
            TeksKetersediaan.Text = _buku.TampilKetersediaan;

            if (!_buku.Tersedia)
            {
                PanelTersedia.Background = new SolidColorBrush(
                    Color.FromRgb(0xFE, 0xF2, 0xF2));
                TeksKetersediaan.Foreground = new SolidColorBrush(
                    Color.FromRgb(0xDC, 0x26, 0x26));
                TombolPinjam.IsEnabled = false;
            }

            // Sembunyikan tombol pinjam jika dibuka admin
            TombolPinjam.Visibility = modeAdmin
                ? Visibility.Collapsed : Visibility.Visible;
        }

        private void TombolPinjam_Click(object sender, RoutedEventArgs e)
        {
            BukuDipilih       = true;
            this.DialogResult = true;
            this.Close();
        }

        private void TombolTutup_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
