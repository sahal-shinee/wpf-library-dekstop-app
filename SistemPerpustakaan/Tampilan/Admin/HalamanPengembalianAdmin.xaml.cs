using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using SistemPerpustakaan.Model;
using SistemPerpustakaan.Pembantu;
using SistemPerpustakaan.Repositori;
using SistemPerpustakaan.Layanan;

namespace SistemPerpustakaan.Tampilan.Admin
{
    // ViewModel untuk daftar buku yang akan dikembalikan
    public class ItemBukuKembali
    {
        public int IdDetailPinjam { get; set; }
        public int IdBuku { get; set; }
        public string JudulBuku { get; set; }
        public string PenulisBuku { get; set; }
        public bool Dipilih { get; set; }
        public string Kondisi { get; set; }

        public ItemBukuKembali() { Dipilih = true; Kondisi = "baik"; }
    }

    public partial class HalamanPengembalianAdmin : Page
    {
        private readonly PeminjamanRepositori _pinjamRepo = new PeminjamanRepositori();
        private readonly PengembalianRepositori _kembaliRepo = new PengembalianRepositori();
        private readonly LayananPengembalian _layananKembali = new LayananPengembalian();

        private Peminjaman _peminjamanDipilih;
        private List<ItemBukuKembali> _daftarItem = new List<ItemBukuKembali>();

        // Paging
        private const int PageSize = 10;
        private int _currentPage = 1;
        private List<Pengembalian> _semua = new List<Pengembalian>();
        private bool _isLoaded = false;

        public HalamanPengembalianAdmin()
        {
            InitializeComponent();
            _ = MuatRiwayatAsync();
        }

        private async Task MuatRiwayatAsync()
        {
            _semua = await _kembaliRepo.AmbilSemuaAsync();
            _currentPage = 1;
            RefreshPagingView();

            if (PanelKosong != null)
                PanelKosong.Visibility = _semua.Count == 0
                    ? Visibility.Visible : Visibility.Collapsed;

            _isLoaded = true;
        }

        private void RefreshPagingView()
        {
            if (TabelKembali == null) return;

            var filtered = _semua; // optionally add filters
            int total = filtered.Count;
            int totalPages = (total + PageSize - 1) / PageSize;
            if (_currentPage < 1) _currentPage = 1;
            if (_currentPage > totalPages) _currentPage = totalPages == 0 ? 1 : totalPages;

            var view = filtered.Skip((_currentPage - 1) * PageSize).Take(PageSize).ToList();
            TabelKembali.ItemsSource = view;

            // update UI teks jika ada
            if (TeksInfo != null)
                TeksInfo.Text = $"Menampilkan {view.Count} dari {filtered.Count} entri (Halaman {_currentPage}/{totalPages})";
        }

        private async void TombolCari_Click(object sender, RoutedEventArgs e)
        {
            int id;
            if (!int.TryParse(KotakIdPeminjaman.Text, out id))
            {
                MessageBox.Show("Masukkan ID peminjaman yang valid.",
                    "Perhatian", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var semuaPinjam = await _pinjamRepo.AmbilSemuaAsync();
            _peminjamanDipilih = semuaPinjam.FirstOrDefault(p => p.IdPeminjaman == id);

            if (_peminjamanDipilih == null)
            {
                MessageBox.Show("Peminjaman dengan ID " + id + " tidak ditemukan.",
                    "Tidak Ditemukan", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_peminjamanDipilih.Status == "selesai")
            {
                MessageBox.Show("Semua buku pada peminjaman ini sudah dikembalikan.",
                    "Selesai", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Tampilkan info peminjaman
            TeksInfoPinjam.Text = "Peminjam: " + _peminjamanDipilih.NamaPeminjam +
                                  "  |  Tanggal Pinjam: " +
                                  _peminjamanDipilih.TanggalPinjam.ToString("dd/MM/yyyy");

            TeksBatasKembali.Text = _peminjamanDipilih.Terlambat
                ? "TERLAMBAT " + _peminjamanDipilih.HariTerlambat + " hari! " +
                  "Batas: " + _peminjamanDipilih.BatasKembali.ToString("dd/MM/yyyy")
                : "Batas kembali: " + _peminjamanDipilih.BatasKembali.ToString("dd/MM/yyyy");

            // Ambil buku yang belum dikembalikan
            var detail = await _pinjamRepo.AmbilDetailAsync(id);
            _daftarItem = detail
                .Where(d => d.StatusBuku == "dipinjam")
                .Select(d => new ItemBukuKembali
                {
                    IdDetailPinjam = d.IdDetailPinjam,
                    IdBuku = d.IdBuku,
                    JudulBuku = d.JudulBuku,
                    PenulisBuku = d.PenulisBuku,
                }).ToList();

            DaftarBukuPinjam.ItemsSource = _daftarItem;
            PanelInfoPinjam.Visibility = Visibility.Visible;
            PanelDaftarBuku.Visibility = Visibility.Visible;
        }

        // ── Inisialisasi & update kondisi buku via event (lebih reliable dari binding) ──
        private void KondisiComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ComboBox cb && cb.DataContext is ItemBukuKembali item)
                cb.SelectedItem = item.Kondisi; // cocokkan string ke string langsung
        }

        private void KondisiComboBox_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox cb
                && cb.SelectedItem is string kondisi
                && cb.DataContext is ItemBukuKembali item)
            {
                item.Kondisi = kondisi; // tulis langsung ke objek, tanpa binding
            }
        }

        private async void TombolProses_Click(object sender, RoutedEventArgs e)
        {
            if (_peminjamanDipilih == null) return;

            var dipilih = _daftarItem.Where(i => i.Dipilih).ToList();
            if (dipilih.Count == 0)
            {
                MessageBox.Show("Pilih minimal 1 buku untuk dikembalikan.",
                    "Perhatian", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Konfirmasi (tampilkan kondisi untuk verifikasi)
            string listBuku = string.Join("\n",
                dipilih.Select((x, i) =>
                    "  " + (i + 1) + ". " + x.JudulBuku + "  [" + x.Kondisi + "]"));
            if (MessageBox.Show("Proses pengembalian " + dipilih.Count + " buku:\n" +
                listBuku, "Konfirmasi", MessageBoxButton.YesNo, MessageBoxImage.Question)
                != MessageBoxResult.Yes) return;

            var daftarBuku = dipilih
                .Select(i => Tuple.Create(i.IdDetailPinjam, i.IdBuku, i.Kondisi, (string)null))
                .ToList();

            // gunakan layanan untuk memproses dan mendapatkan hasil terstruktur
            var hasil = await _layananKembali.ProsesAsync(
                _peminjamanDipilih.IdPeminjaman,
                _peminjamanDipilih.IdPengguna,
                daftarBuku);

            if (hasil.Sukses)
            {
                MessageBox.Show(hasil.Pesan, "Berhasil", MessageBoxButton.OK, MessageBoxImage.Information);

                if (hasil.TotalDenda > 0 && hasil.DetailBuku != null && hasil.DetailBuku.Count > 0)
                {
                    try
                    {
                        var sb = new System.Text.StringBuilder();
                        sb.AppendLine("Rincian denda:");
                        foreach (var d in hasil.DetailBuku)
                        {
                            if (d.TotalDendaBuku <= 0) continue;
                            sb.AppendFormat("- {0}: {1}\n", d.JudulBuku, d.TampilTotalDenda);
                        }
                        sb.AppendLine();
                        sb.AppendFormat("Total denda: Rp {0:N0}", hasil.TotalDenda);

                        MessageBox.Show(sb.ToString(), "Denda", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    catch { /* ignore */ }
                }

                KotakIdPeminjaman.Clear();
                PanelInfoPinjam.Visibility = Visibility.Collapsed;
                PanelDaftarBuku.Visibility = Visibility.Collapsed;
                _peminjamanDipilih = null;
                await MuatRiwayatAsync();
            }
            else
            {
                MessageBox.Show(hasil.Pesan, "Gagal", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void TombolDetail_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button btn)) return;
            int idPengembalian = Convert.ToInt32(btn.Tag);

            var detail = await _kembaliRepo.AmbilDetailAsync(idPengembalian);
            DaftarDetailKembali.ItemsSource = detail;

            decimal total = 0m;
            foreach (var d in detail) total += d.TotalDendaBuku;

            TeksTotalDenda.Text = total > 0 ? "Rp " + total.ToString("N0") : "Tidak ada denda";
            JudulOverlay.Text = "Detail Pengembalian  #" + idPengembalian;
            OverlayDetail.Visibility = Visibility.Visible;
        }

        private void TombolTutupOverlay_Click(object sender, RoutedEventArgs e)
        {
            OverlayDetail.Visibility = Visibility.Collapsed;
        }

        // Paging controls
        private void TombolPrev_Click(object s, RoutedEventArgs e)
        {
            _currentPage = Math.Max(1, _currentPage - 1);
            RefreshPagingView();
        }

        private void TombolNext_Click(object s, RoutedEventArgs e)
        {
            int totalPages = (_semua.Count + PageSize - 1) / PageSize;
            _currentPage = Math.Min(totalPages == 0 ? 1 : totalPages, _currentPage + 1);
            RefreshPagingView();
        }

        // Hapus pengembalian jika tidak ada denda terkait
        private async void TombolHapusPengembalian_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                int id = Convert.ToInt32(btn.Tag);
                var confirm = MessageBox.Show("Apakah Anda yakin ingin menghapus riwayat pengembalian ini? (Tidak dapat dihapus jika ada denda terkait)",
                    "Konfirmasi Hapus", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (confirm != MessageBoxResult.Yes) return;

                var hasil = await _kembaliRepo.HapusAsync(id);
                if (hasil.Item1)
                {
                    MessageBox.Show(hasil.Item2, "Berhasil Dihapus", MessageBoxButton.OK, MessageBoxImage.Information);
                    await MuatRiwayatAsync(); // Refresh Tabel
                }
                else
                {
                    MessageBox.Show(hasil.Item2, "Gagal Menghapus", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }
    }
}