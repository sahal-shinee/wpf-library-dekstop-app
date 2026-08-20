using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using SistemPerpustakaan.Model;
using SistemPerpustakaan.Pembantu;
using SistemPerpustakaan.Repositori;
using SistemPerpustakaan.Tampilan.Bersama;
using SistemPerpustakaan.Tampilan.Komponen;

namespace SistemPerpustakaan.Tampilan.Pengguna
{
    public partial class HalamanCariPinjamBuku : Page
    {
        private readonly BukuRepositori       _bukuRepo   = new BukuRepositori();
        private readonly PeminjamanRepositori _pinjamRepo = new PeminjamanRepositori();

        private List<Buku>      _semuaBuku  = new List<Buku>();
        private List<Buku>      _keranjang  = new List<Buku>();
        private List<KartuBuku> _semuaKartu = new List<KartuBuku>();

        public HalamanCariPinjamBuku()
        {
            InitializeComponent();
            _ = MuatDataAsync();
        }

        private async Task MuatDataAsync()
        {
            PanelMuat.Visibility   = Visibility.Visible;
            PanelKosong.Visibility = Visibility.Collapsed;
            GridBuku.Children.Clear();
            _semuaKartu.Clear();

            _semuaBuku = await _bukuRepo.AmbilSemuaAsync();

            var kategoriList = new List<string> { "Semua Kategori" };
            kategoriList.AddRange(await _bukuRepo.AmbilKategoriAsync());
            FilterKategori.ItemsSource   = kategoriList;
            FilterKategori.SelectedIndex = 0;

            TampilkanBuku(_semuaBuku);
            PanelMuat.Visibility = Visibility.Collapsed;
        }

        private void TampilkanBuku(List<Buku> daftar)
        {
            GridBuku.Children.Clear();
            _semuaKartu.Clear();

            if (daftar.Count == 0)
            {
                PanelKosong.Visibility = Visibility.Visible;
                TeksInfo.Text          = "Tidak ada buku yang sesuai.";
                return;
            }

            PanelKosong.Visibility = Visibility.Collapsed;
            TeksInfo.Text = "Menampilkan " + daftar.Count + " buku" +
                (_keranjang.Count > 0 ? "  ·  " + _keranjang.Count + " dipilih" : "");

            foreach (var buku in daftar)
            {
                var kartu = new KartuBuku();
                kartu.IsiBuku(buku);
                kartu.Margin  = new Thickness(8);
                kartu.PadaKlik += Kartu_Diklik;

                if (_keranjang.Any(b => b.IdBuku == buku.IdBuku))
                    kartu.TandaiDipilih(true);

                _semuaKartu.Add(kartu);
                GridBuku.Children.Add(kartu);
            }
        }

        private void Kartu_Diklik(object sender, Buku buku)
        {
            // Jika sudah di keranjang → keluarkan
            if (_keranjang.Any(b => b.IdBuku == buku.IdBuku))
            {
                KelaurkanDariKeranjang(buku);
                return;
            }

            // Buka detail buku
            var detail = new JendelaDetailBuku(buku);
            detail.Owner = Window.GetWindow(this);

            if (detail.ShowDialog() == true && detail.BukuDipilih)
                MasukkanKeKeranjang(buku);
        }

        private void MasukkanKeKeranjang(Buku buku)
        {
            var pengguna = SesiPengguna.PenggunaSaatIni;

            if (pengguna != null && pengguna.AdalahDiblokir)
            {
                MessageBox.Show(
                    "Akun Anda diblokir karena ada denda belum lunas.\n" +
                    "Lunasi denda terlebih dahulu sebelum meminjam.",
                    "Akun Diblokir", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_keranjang.Count >= KonstantaAplikasi.MaksimalBukuPerPinjaman)
            {
                MessageBox.Show(
                    "Maksimal " + KonstantaAplikasi.MaksimalBukuPerPinjaman +
                    " buku per peminjaman.",
                    "Batas Tercapai", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_keranjang.Any(b => b.IdBuku == buku.IdBuku)) return;

            _keranjang.Add(buku);
            PerbaruilKartuDipilih(buku.IdBuku, true);
            PerbaruiPanelKeranjang();
        }

        private void KelaurkanDariKeranjang(Buku buku)
        {
            _keranjang.RemoveAll(b => b.IdBuku == buku.IdBuku);
            PerbaruilKartuDipilih(buku.IdBuku, false);
            PerbaruiPanelKeranjang();
        }

        private void PerbaruilKartuDipilih(int idBuku, bool dipilih)
        {
            foreach (var kartu in _semuaKartu)
            {
                if (kartu.AmbilBuku() != null && kartu.AmbilBuku().IdBuku == idBuku)
                {
                    kartu.TandaiDipilih(dipilih);
                    break;
                }
            }
        }

        private void PerbaruiPanelKeranjang()
        {
            if (_keranjang.Count == 0)
            {
                PanelKeranjang.Visibility = Visibility.Collapsed;
            }
            else
            {
                PanelKeranjang.Visibility = Visibility.Visible;
                var judulList = _keranjang.Select(b =>
                    b.Judul.Length > 20 ? b.Judul.Substring(0, 20) + "…" : b.Judul);
                TeksKeranjang.Text = _keranjang.Count + " buku dipilih: " +
                    string.Join(", ", judulList);
            }

            TeksInfo.Text = "Menampilkan " + _semuaKartu.Count + " buku" +
                (_keranjang.Count > 0 ? "  ·  " + _keranjang.Count + " dipilih" : "");
        }

        private async void TombolPinjam_Click(object sender, RoutedEventArgs e)
        {
            if (_keranjang.Count == 0) return;

            var pengguna = SesiPengguna.PenggunaSaatIni;
            if (pengguna == null) return;

            // Konfirmasi
            string listBuku = string.Join("\n",
                _keranjang.Select((b, i) => "  " + (i + 1) + ". " + b.Judul));
            string batas = DateTime.Today
                .AddDays(KonstantaAplikasi.DurasiPeminjamanHari)
                .ToString("dd MMMM yyyy");

            if (MessageBox.Show(
                "Pinjam " + _keranjang.Count + " buku:\n" + listBuku +
                "\n\nBatas kembali: " + batas,
                "Konfirmasi Peminjaman", MessageBoxButton.YesNo, MessageBoxImage.Question)
                != MessageBoxResult.Yes) return;

            TombolPinjam.IsEnabled = false;
            TombolPinjam.Content   = "Memproses...";

            try
            {
                // Cek batas aktif
                int aktif = await _pinjamRepo.HitungBukuAktifAsync(pengguna.IdPengguna);
                if (aktif + _keranjang.Count > KonstantaAplikasi.MaksimalBukuPerPinjaman)
                {
                    MessageBox.Show(
                        "Anda sudah meminjam " + aktif + " buku.\n" +
                        "Maksimal " + KonstantaAplikasi.MaksimalBukuPerPinjaman + " buku aktif.",
                        "Melebihi Batas", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var idList = _keranjang.Select(b => b.IdBuku).ToList();
                var h = await _pinjamRepo.BuatPeminjamanAsync(pengguna.IdPengguna, idList);

                if (h.Item1)
                {
                    _keranjang.Clear();
                    PanelKeranjang.Visibility = Visibility.Collapsed;
                    foreach (var k in _semuaKartu) k.TandaiDipilih(false);

                    MessageBox.Show(
                        "Peminjaman berhasil!\nID: #" + h.Item3 +
                        "\nBatas kembali: " + batas,
                        "Berhasil", MessageBoxButton.OK, MessageBoxImage.Information);

                    await MuatDataAsync();
                }
                else
                    MessageBox.Show(h.Item2, "Gagal", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                TombolPinjam.IsEnabled = true;
                TombolPinjam.Content   = "Pinjam Sekarang";
            }
        }

        private void TombolBatalPilihan_Click(object sender, RoutedEventArgs e)
        {
            _keranjang.Clear();
            foreach (var k in _semuaKartu) k.TandaiDipilih(false);
            PanelKeranjang.Visibility = Visibility.Collapsed;
            TeksInfo.Text = "Menampilkan " + _semuaKartu.Count + " buku";
        }

        private void Filter()
        {
            string kata      = KotakCari.Text.Trim().ToLower();
            string kategori  = FilterKategori.SelectedItem?.ToString();
            bool   hanyaAda  = FilterTersedia.IsChecked == true;

            var hasil = _semuaBuku.Where(b =>
            {
                bool cocokKata = string.IsNullOrEmpty(kata)
                    || b.Judul.ToLower().Contains(kata)
                    || b.Penulis.ToLower().Contains(kata)
                    || (b.Kategori != null && b.Kategori.ToLower().Contains(kata));

                bool cocokKat = string.IsNullOrEmpty(kategori)
                    || kategori == "Semua Kategori"
                    || b.Kategori == kategori;

                bool cocokAda = !hanyaAda || b.Tersedia;

                return cocokKata && cocokKat && cocokAda;
            }).ToList();

            TampilkanBuku(hasil);
        }

        private void KotakCari_Changed(object s, TextChangedEventArgs e)          => Filter();
        private void FilterKategori_Changed(object s, SelectionChangedEventArgs e) => Filter();
        private void Filter_Changed(object s, RoutedEventArgs e)                   => Filter();
    }
}
