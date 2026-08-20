using System;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using SistemPerpustakaan.Model;
using SistemPerpustakaan.Pembantu;

namespace SistemPerpustakaan.Tampilan.Bersama
{
    public partial class JendelaFormBuku : Window
    {
        private Buku   _buku;
        private string _pathGambarBaru;

        public Buku HasilBuku { get; private set; }

        public JendelaFormBuku(Buku buku = null)
        {
            InitializeComponent();
            _buku = buku;
            if (_buku != null)
                IsiBukuEdit();
            else
                JudulForm.Text = "Tambah Buku Baru";
        }

        private void IsiBukuEdit()
        {
            JudulForm.Text          = "Edit Buku";
            KotakJudul.Text         = _buku.Judul;
            KotakPenulis.Text       = _buku.Penulis;
            KotakPenerbit.Text      = _buku.Penerbit ?? string.Empty;
            KotakKategori.Text      = _buku.Kategori ?? string.Empty;
            KotakIsbn.Text          = _buku.Isbn ?? string.Empty;
            KotakTahun.Text         = _buku.TahunTerbit.HasValue
                                      ? _buku.TahunTerbit.Value.ToString() : string.Empty;
            KotakStok.Text          = _buku.JumlahStok.ToString();
            KotakDeskripsi.Text     = _buku.Deskripsi ?? string.Empty;
            KotakHarga.Text         = _buku.Harga.ToString("F0");
            PreviewSampul.Source    = _buku.GambarSampul;
            NamaFileSampul.Text     = _buku.Sampul ?? "Belum dipilih";
        }

        private void TombolPilihGambar_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title  = "Pilih Sampul Buku",
                Filter = "File Gambar|*.jpg;*.jpeg;*.png;*.bmp"
            };

            if (dialog.ShowDialog() != true) return;

            var cek = PembantuGambar.ValidasiFile(dialog.FileName);
            if (!cek.Item1)
            {
                TampilError(cek.Item2);
                return;
            }

            _pathGambarBaru      = dialog.FileName;
            NamaFileSampul.Text  = Path.GetFileName(dialog.FileName);

            // Preview gambar yang dipilih
            try
            {
                var bmp = new System.Windows.Media.Imaging.BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                bmp.UriSource   = new Uri(dialog.FileName, UriKind.Absolute);
                bmp.EndInit();
                bmp.Freeze();
                PreviewSampul.Source = bmp;
            }
            catch { /* preview gagal, tidak masalah */ }

            PanelError.Visibility = Visibility.Collapsed;
        }

        private void TombolSimpan_Click(object sender, RoutedEventArgs e)
        {
            // Validasi wajib
            if (string.IsNullOrWhiteSpace(KotakJudul.Text))
            { TampilError("Judul buku tidak boleh kosong."); return; }

            if (string.IsNullOrWhiteSpace(KotakPenulis.Text))
            { TampilError("Penulis tidak boleh kosong."); return; }

            int stok;
            if (!int.TryParse(KotakStok.Text, out stok) || stok < 1)
            { TampilError("Jumlah stok harus angka positif."); return; }

            // Gambar wajib untuk buku baru
            if (_buku == null && string.IsNullOrEmpty(_pathGambarBaru))
            { TampilError("Sampul buku wajib dipilih untuk buku baru."); return; }

            int? tahun = null;
            if (!string.IsNullOrWhiteSpace(KotakTahun.Text))
            {
                int t;
                if (!int.TryParse(KotakTahun.Text, out t) || t < 1000 || t > 9999)
                { TampilError("Tahun terbit tidak valid."); return; }
                tahun = t;
            }

            decimal harga = 0m;
            if (!string.IsNullOrWhiteSpace(KotakHarga.Text))
                decimal.TryParse(KotakHarga.Text.Replace(".", "").Replace(",", ""),
                    out harga);

            HasilBuku = new Buku
            {
                IdBuku       = _buku != null ? _buku.IdBuku : 0,
                Judul        = KotakJudul.Text.Trim(),
                Penulis      = KotakPenulis.Text.Trim(),
                Penerbit     = string.IsNullOrWhiteSpace(KotakPenerbit.Text)
                               ? null : KotakPenerbit.Text.Trim(),
                Kategori     = string.IsNullOrWhiteSpace(KotakKategori.Text)
                               ? null : KotakKategori.Text.Trim(),
                Isbn         = string.IsNullOrWhiteSpace(KotakIsbn.Text)
                               ? null : KotakIsbn.Text.Trim(),
                TahunTerbit  = tahun,
                JumlahStok   = stok,
                StokTersedia = _buku != null ? _buku.StokTersedia : stok,
                Deskripsi    = string.IsNullOrWhiteSpace(KotakDeskripsi.Text)
                               ? null : KotakDeskripsi.Text.Trim(),
                Harga        = harga,
                Sampul       = _pathGambarBaru ?? (_buku != null ? _buku.Sampul : null),
            };

            // Simpan path gambar baru di Tag agar bisa dipakai caller
            this.Tag          = _pathGambarBaru;
            this.DialogResult = true;
            this.Close();
        }

        private void TombolBatal_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void TampilError(string pesan)
        {
            PanelError.Visibility = Visibility.Visible;
            TeksError.Text        = pesan;
        }
    }
}
