using SistemPerpustakaan.Model;
using SistemPerpustakaan.Pembantu;
using SistemPerpustakaan.Repositori;
using SistemPerpustakaan.Tampilan.Admin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Text;

namespace SistemPerpustakaan.Tampilan.Pengguna
{
    public partial class HalamanRiwayatPinjam : Page
    {
        private readonly PeminjamanRepositori _pinjamRepo = new PeminjamanRepositori();
        private readonly PengembalianRepositori _kembaliRepo = new PengembalianRepositori();

        private List<Peminjaman> _semua = new List<Peminjaman>();
        private Peminjaman _pinjamDipilih;
        private List<ItemBukuKembali> _itemKembali = new List<ItemBukuKembali>();

        // ✅ FLAG
        private bool _isLoaded = false;

        public HalamanRiwayatPinjam()
        {
            InitializeComponent();
            this.Loaded += HalamanRiwayatPinjam_Loaded;
        }

        private async void HalamanRiwayatPinjam_Loaded(object sender, RoutedEventArgs e)
        {
            _isLoaded = true;
            await MuatDataAsync();
        }

        private async Task MuatDataAsync()
        {
            // ✅ Cegah null crash
            if (PanelMuatRiwayat != null)
                PanelMuatRiwayat.Visibility = Visibility.Visible;

            if (PanelKosongRiwayat != null)
                PanelKosongRiwayat.Visibility = Visibility.Collapsed;

            if (DaftarPeminjaman != null)
                DaftarPeminjaman.ItemsSource = null;

            var pengguna = SesiPengguna.PenggunaSaatIni;
            if (pengguna == null) return;

            await _pinjamRepo.TandaiTerlambatAsync();
            _semua = await _pinjamRepo.AmbilByPenggunaAsync(pengguna.IdPengguna);

            foreach (var p in _semua)
                p.DetailBuku = await _pinjamRepo.AmbilDetailAsync(p.IdPeminjaman);

            if (PanelMuatRiwayat != null)
                PanelMuatRiwayat.Visibility = Visibility.Collapsed;

            TampilkanFilter();
        }

        private void TampilkanFilter()
        {
            // ✅ GUARD UTAMA (ANTI ERROR)
            if (!_isLoaded ||
                DaftarPeminjaman == null ||
                PanelKosongRiwayat == null ||
                TeksInfo == null)
                return;

            string filter = (FilterStatus.SelectedItem as ComboBoxItem)?
                .Content?.ToString();

            List<Peminjaman> hasil;

            if (string.IsNullOrEmpty(filter) || filter == "Semua Peminjaman")
            {
                hasil = _semua;
            }
            else
            {
                hasil = _semua.Where(p =>
                {
                    if (filter == "Aktif")
                        return p.Status == "aktif" || p.Status == "sebagian_kembali";
                    if (filter == "Terlambat")
                        return p.Status == "terlambat";
                    if (filter == "Selesai")
                        return p.Status == "selesai";
                    return true;
                }).ToList();
            }

            if (hasil.Count == 0)
            {
                DaftarPeminjaman.ItemsSource = null;
                PanelKosongRiwayat.Visibility = Visibility.Visible;
            }
            else
            {
                DaftarPeminjaman.ItemsSource = hasil;
                PanelKosongRiwayat.Visibility = Visibility.Collapsed;
            }

            TeksInfo.Text = $"{hasil.Count} dari {_semua.Count} peminjaman";
        }

        private async void TombolKembalikanDariKartu_Click(object sender, RoutedEventArgs e)
        {
            if (!_isLoaded) return;

            var btn = sender as Button;
            if (btn == null || !(btn.Tag is int)) return;

            int idPeminjaman = (int)btn.Tag;

            _pinjamDipilih = _semua.FirstOrDefault(p => p.IdPeminjaman == idPeminjaman);
            if (_pinjamDipilih == null) return;

            var detail = await _pinjamRepo.AmbilDetailAsync(idPeminjaman);
            var belum = detail.Where(d => d.StatusBuku == "dipinjam").ToList();

            if (belum.Count == 0)
            {
                MessageBox.Show("Semua buku sudah dikembalikan.",
                    "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            _itemKembali = belum.Select(d => new ItemBukuKembali
            {
                IdDetailPinjam = d.IdDetailPinjam,
                IdBuku = d.IdBuku,
                JudulBuku = d.JudulBuku,
                PenulisBuku = d.PenulisBuku,
            }).ToList();

            TeksInfoKembalikan.Text =
                $"Peminjaman #{_pinjamDipilih.IdPeminjaman}\n" +
                $"Batas kembali: {_pinjamDipilih.BatasKembali:dd MMMM yyyy}" +
                (_pinjamDipilih.Terlambat
                    ? $"\n⚠ TERLAMBAT {_pinjamDipilih.HariTerlambat} hari"
                    : "");

            DaftarBukuKembalikan.ItemsSource = _itemKembali;
            OverlayKembalikan.Visibility = Visibility.Visible;
        }

        private void TombolBatalKembalikan_Click(object sender, RoutedEventArgs e)
        {
            OverlayKembalikan.Visibility = Visibility.Collapsed;
            _pinjamDipilih = null;
        }

        private async void TombolKonfirmasiKembalikan_Click(object sender, RoutedEventArgs e)
        {
            if (_pinjamDipilih == null) return;

            var dipilih = _itemKembali.Where(i => i.Dipilih).ToList();
            if (dipilih.Count == 0)
            {
                MessageBox.Show("Pilih minimal 1 buku.",
                    "Perhatian", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var pengguna = SesiPengguna.PenggunaSaatIni;

            var daftarBuku = dipilih
                .Select(i => Tuple.Create(i.IdDetailPinjam, i.IdBuku, i.Kondisi, (string)null))
                .ToList();

            var h = await _kembaliRepo.ProsesAsync(
                _pinjamDipilih.IdPeminjaman,
                pengguna.IdPengguna,
                daftarBuku);

            OverlayKembalikan.Visibility = Visibility.Collapsed;

            if (h.Item1)
            {
                // Tampilkan pesan dari repositori (mengandung total denda bila ada)
                MessageBox.Show(h.Item2, "Berhasil", MessageBoxButton.OK, MessageBoxImage.Information);

                // Ambil detail pengembalian untuk menampilkan rincian denda per buku bila ada
                if (h.Item3 > 0)
                {
                    try
                    {
                        var detailKembali = await _kembaliRepo.AmbilDetailAsync(h.Item3);
                        decimal total = 0m;
                        foreach (var d in detailKembali) total += d.TotalDendaBuku;

                        if (total > 0)
                        {
                            var sb = new StringBuilder();
                            sb.AppendLine("Rincian denda:");
                            foreach (var d in detailKembali)
                            {
                                if (d.TotalDendaBuku <= 0) continue;
                                sb.AppendFormat("- {0}: {1} \n", d.JudulBuku, d.TampilTotalDenda);
                            }
                            sb.AppendLine();
                            sb.AppendFormat("Total denda: Rp {0:N0}", total);

                            MessageBox.Show(sb.ToString(), "Denda", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                    catch { /* ignore detail fetch errors to avoid breaking UX */ }
                }

                _pinjamDipilih = null;
                await MuatDataAsync();
            }
            else
            {
                MessageBox.Show(h.Item2, "Gagal",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FilterStatus_Changed(object s, SelectionChangedEventArgs e)
        {
            if (!_isLoaded) return;
            TampilkanFilter();
        }

        private async void MuatUlang_Click(object s, RoutedEventArgs e)
        {
            await MuatDataAsync();
        }
    }
}