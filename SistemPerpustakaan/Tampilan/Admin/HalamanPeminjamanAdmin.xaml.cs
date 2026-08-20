using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using SistemPerpustakaan.Model;
using SistemPerpustakaan.Repositori;

namespace SistemPerpustakaan.Tampilan.Admin
{
    public partial class HalamanPeminjamanAdmin : Page
    {
        private readonly PeminjamanRepositori _repo = new PeminjamanRepositori();
        private List<Peminjaman> _semua = new List<Peminjaman>();

        // ✅ Flag untuk memastikan UI sudah siap
        private bool _isLoaded = false;

        // Paging
        private const int PageSize = 10;
        private int _currentPage = 1;
        private List<Peminjaman> _currentView = new List<Peminjaman>();

        public HalamanPeminjamanAdmin()
        {
            InitializeComponent();

            // ✅ Pindahkan load ke event Loaded
            this.Loaded += HalamanPeminjamanAdmin_Loaded;
        }

        private async void HalamanPeminjamanAdmin_Loaded(object sender, RoutedEventArgs e)
        {
            _isLoaded = true;
            await MuatDataAsync();
        }

        private async Task MuatDataAsync()
        {
            // ✅ Cegah null saat awal
            if (TeksInfo != null)
                TeksInfo.Text = "Memuat data peminjaman...";

            await _repo.TandaiTerlambatAsync();
            _semua = await _repo.AmbilSemuaAsync();

            // Ambil detail buku
            foreach (var p in _semua)
                p.DetailBuku = await _repo.AmbilDetailAsync(p.IdPeminjaman);

            _currentPage = 1;
            RefreshPagingView();
        }

        private void RefreshPagingView()
        {
            if (!_isLoaded || TabelPeminjaman == null || TeksInfo == null) return;

            var hasilFiltered = ApplyFilterToList(_semua);

            int total = hasilFiltered.Count;
            int totalPages = (total + PageSize - 1) / PageSize;
            if (_currentPage < 1) _currentPage = 1;
            if (_currentPage > totalPages) _currentPage = totalPages == 0 ? 1 : totalPages;

            _currentView = hasilFiltered.Skip((_currentPage - 1) * PageSize).Take(PageSize).ToList();

            TabelPeminjaman.ItemsSource = _currentView;
            TeksInfo.Text = $"Menampilkan {_currentView.Count} dari {hasilFiltered.Count} (Halaman {_currentPage}/{totalPages})";

            if (PanelKosong != null)
                PanelKosong.Visibility = hasilFiltered.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private List<Peminjaman> ApplyFilterToList(List<Peminjaman> source)
        {
            string kata = KotakCari.Text?.Trim().ToLower() ?? "";
            string status = (FilterStatus.SelectedItem as ComboBoxItem)?.Content?.ToString();

            var hasil = source.Where(p =>
            {
                bool cocok = string.IsNullOrEmpty(kata)
                    || (p.NamaPeminjam?.ToLower().Contains(kata) ?? false)
                    || (p.NamaPengguna?.ToLower().Contains(kata) ?? false)
                    || p.IdPeminjaman.ToString().Contains(kata);

                bool cocokStatus = string.IsNullOrEmpty(status)
                    || status == "Semua Status"
                    || p.Status == status;

                return cocok && cocokStatus;
            }).ToList();

            return hasil;
        }

        private void Filter()
        {
            if (!_isLoaded) return;
            _currentPage = 1;
            RefreshPagingView();
        }

        private void TabelPeminjaman_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Tidak perlu isi, RowDetails sudah handle otomatis
        }

        private void KotakCari_Changed(object s, TextChangedEventArgs e)
        {
            if (!_isLoaded) return;
            Filter();
        }

        private void FilterStatus_Changed(object s, SelectionChangedEventArgs e)
        {
            if (!_isLoaded) return;
            Filter();
        }

        private async void MuatUlang_Click(object s, RoutedEventArgs e)
        {
            await MuatDataAsync();
        }

        // Paging controls (to be wired from XAML if added)
        private void TombolPrev_Click(object s, RoutedEventArgs e)
        {
            _currentPage = Math.Max(1, _currentPage - 1);
            RefreshPagingView();
        }

        private void TombolNext_Click(object s, RoutedEventArgs e)
        {
            var filtered = ApplyFilterToList(_semua);
            int totalPages = (filtered.Count + PageSize - 1) / PageSize;
            _currentPage = Math.Min(totalPages == 0 ? 1 : totalPages, _currentPage + 1);
            RefreshPagingView();
        }

        // Hapus (hard delete) peminjaman jika status = 'selesai'
        private async void TombolHapusPeminjaman_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                var confirm = MessageBox.Show("Hapus peminjaman ini? (Hanya dapat dihapus jika status 'selesai')",
                    "Konfirmasi Hapus", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (confirm != MessageBoxResult.Yes) return;

                var hasil = await _repo.HapusAsync(id);
                if (hasil.Item1)
                {
                    MessageBox.Show(hasil.Item2, "Berhasil", MessageBoxButton.OK, MessageBoxImage.Information);
                    await MuatDataAsync();
                }
                else
                {
                    MessageBox.Show(hasil.Item2, "Gagal", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }
    }
}