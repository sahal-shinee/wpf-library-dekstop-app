using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using SistemPerpustakaan.Repositori;

using ModelPengguna = SistemPerpustakaan.Model.Pengguna;

namespace SistemPerpustakaan.Tampilan.Admin
{
    public partial class HalamanKelolaPengguna : Page
    {
        private readonly PenggunaRepositori _repo = new PenggunaRepositori();
        private List<ModelPengguna> _semua = new List<ModelPengguna>();

        // ? FLAG untuk memastikan UI sudah siap
        private bool _isLoaded = false;

        public HalamanKelolaPengguna()
        {
            InitializeComponent();
            this.Loaded += HalamanKelolaPengguna_Loaded;
        }

        private async void HalamanKelolaPengguna_Loaded(object sender, RoutedEventArgs e)
        {
            await MuatDataAsync();

            // ? tandai bahwa UI sudah siap
            _isLoaded = true;
        }

        private async Task MuatDataAsync()
        {
            _semua = await _repo.AmbilSemuaAnggotaAsync();
            Tampilkan(_semua);
        }

        private void Tampilkan(List<ModelPengguna> daftar)
        {
            if (TabelPengguna == null) return;

            TabelPengguna.ItemsSource  = daftar;
            PanelKosong.Visibility = daftar.Count == 0
                ? Visibility.Visible : Visibility.Collapsed;
            TeksInfo.Text = $"Menampilkan {daftar.Count} dari {_semua.Count} anggota";
        }

        private void Filter()
        {
            // ? CEGAH jalan sebelum siap
            if (!_isLoaded || TabelPengguna == null || _semua == null) return;

            string kata = KotakCari.Text?.Trim().ToLower() ?? "";
            string status = (FilterStatus.SelectedItem as ComboBoxItem)?.Content?.ToString();

            var hasil = _semua.Where(p =>
            {
                bool cocokKata =
                    string.IsNullOrEmpty(kata) ||
                    (p.NamaLengkap?.ToLower().Contains(kata) ?? false) ||
                    (p.NamaPengguna?.ToLower().Contains(kata) ?? false) ||
                    (p.Surel?.ToLower().Contains(kata) ?? false);

                bool cocokStatus =
                    string.IsNullOrEmpty(status) ||
                    status == "Semua Status" ||
                    p.Status == status;

                return cocokKata && cocokStatus;
            }).ToList();

            Tampilkan(hasil);
        }

        private void KotakCari_TextChanged(object s, TextChangedEventArgs e)
        {
            if (!_isLoaded) return;
            Filter();
        }

        private void FilterStatus_Changed(object s, SelectionChangedEventArgs e)
        {
            if (!_isLoaded) return;
            Filter();
        }

        private async void TombolMuatUlang_Click(object s, RoutedEventArgs e)
        {
            await MuatDataAsync();
        }

        private async void TombolAktifkan_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                await _repo.UbahStatusAsync(id, "aktif");
                await MuatDataAsync();
            }
        }

        private async void TombolBlokir_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                if (MessageBox.Show("Blokir pengguna ini?", "Konfirmasi",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                    return;

                await _repo.UbahStatusAsync(id, "diblokir");
                await MuatDataAsync();
            }
        }

        private async void TombolHapus_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                if (MessageBox.Show(
                    "Hapus pengguna ini? Tindakan tidak bisa dibatalkan.",
                    "Konfirmasi Hapus",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning) != MessageBoxResult.Yes)
                    return;

                var hasil = await _repo.HapusAsync(id);

                if (hasil.Item1)
                    await MuatDataAsync();
                else
                    MessageBox.Show(hasil.Item2, "Gagal",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}