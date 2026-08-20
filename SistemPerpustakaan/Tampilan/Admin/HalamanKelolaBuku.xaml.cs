using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using SistemPerpustakaan.Model;
using SistemPerpustakaan.Pembantu;
using SistemPerpustakaan.Repositori;
using SistemPerpustakaan.Tampilan.Bersama;

namespace SistemPerpustakaan.Tampilan.Admin
{
    public partial class HalamanKelolaBuku : Page
    {
        private readonly BukuRepositori _repo = new BukuRepositori();
        private List<Buku> _semuaBuku = new List<Buku>();

        public HalamanKelolaBuku() { InitializeComponent(); _ = MuatDataAsync(); }

        private async Task MuatDataAsync()
        {
            TeksInfo.Text = "Memuat data buku...";
            _semuaBuku    = await _repo.AmbilSemuaAsync();

            var kategoriList = new List<string> { "Semua Kategori" };
            kategoriList.AddRange(await _repo.AmbilKategoriAsync());
            FilterKategori.ItemsSource   = kategoriList;
            FilterKategori.SelectedIndex = 0;

            TampilkanBuku(_semuaBuku);
        }

        private void TampilkanBuku(List<Buku> daftar)
        {
            TabelBuku.ItemsSource = daftar;
            PanelKosong.Visibility = daftar.Count == 0
                ? Visibility.Visible : Visibility.Collapsed;
            TeksInfo.Text = "Menampilkan " + daftar.Count + " dari " + _semuaBuku.Count + " buku";
        }

        private void Filter()
        {
            string kata     = KotakCari.Text.Trim().ToLower();
            string kategori = FilterKategori.SelectedItem?.ToString();

            var hasil = _semuaBuku.Where(b =>
            {
                bool cocokKata = string.IsNullOrEmpty(kata)
                    || b.Judul.ToLower().Contains(kata)
                    || b.Penulis.ToLower().Contains(kata)
                    || (b.Kategori != null && b.Kategori.ToLower().Contains(kata));
                bool cocokKat  = string.IsNullOrEmpty(kategori)
                    || kategori == "Semua Kategori" || b.Kategori == kategori;
                return cocokKata && cocokKat;
            }).ToList();
            TampilkanBuku(hasil);
        }

        private void KotakCari_TextChanged(object s, TextChangedEventArgs e) => Filter();
        private void FilterKategori_Changed(object s, SelectionChangedEventArgs e) => Filter();
        private async void TombolMuatUlang_Click(object s, RoutedEventArgs e) => await MuatDataAsync();

        private void TabelBuku_SelectionChanged(object s, SelectionChangedEventArgs e) { }
        private void TabelBuku_DoubleClick(object s, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (TabelBuku.SelectedItem is Buku b)
                new JendelaDetailBuku(b, modeAdmin: true).ShowDialog();
        }

        private async void TombolTambah_Click(object sender, RoutedEventArgs e)
        {
            var form = new JendelaFormBuku();
            if (form.ShowDialog() != true) return;

            var buku   = form.HasilBuku;
            int idBaru = await _repo.TambahAsync(buku);

            string pathGambar = form.Tag as string;
            if (!string.IsNullOrEmpty(pathGambar) && idBaru > 0)
            {
                string namaFile = PembantuGambar.SimpanSampulBuku(pathGambar, idBaru);
                await _repo.PerbaruiSampulAsync(idBaru, namaFile);
            }

            MessageBox.Show("Buku berhasil ditambahkan!",
                "Berhasil", MessageBoxButton.OK, MessageBoxImage.Information);
            await MuatDataAsync();
        }

        private async void TombolEdit_Click(object sender, RoutedEventArgs e)
        {
            int id = (int)((Button)sender).Tag;
            var buku = await _repo.AmbilByIdAsync(id);
            if (buku == null) return;

            var form = new JendelaFormBuku(buku);
            if (form.ShowDialog() != true) return;

            await _repo.PerbaruiAsync(form.HasilBuku);
            string pathGambar = form.Tag as string;
            if (!string.IsNullOrEmpty(pathGambar))
            {
                string namaFile = PembantuGambar.SimpanSampulBuku(pathGambar, id);
                await _repo.PerbaruiSampulAsync(id, namaFile);
            }

            MessageBox.Show("Buku berhasil diperbarui!",
                "Berhasil", MessageBoxButton.OK, MessageBoxImage.Information);
            await MuatDataAsync();
        }

        private async void TombolHapus_Click(object sender, RoutedEventArgs e)
        {
            int id = (int)((Button)sender).Tag;
            if (MessageBox.Show("Hapus buku ini?", "Konfirmasi",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            var h = await _repo.HapusAsync(id);
            if (h.Item1)
            {
                MessageBox.Show("Buku berhasil dihapus.",
                    "Berhasil", MessageBoxButton.OK, MessageBoxImage.Information);
                await MuatDataAsync();
            }
            else
                MessageBox.Show(h.Item2, "Gagal", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
