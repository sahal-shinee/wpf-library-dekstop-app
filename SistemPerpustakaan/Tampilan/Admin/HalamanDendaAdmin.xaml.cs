using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using SistemPerpustakaan.Model;
using SistemPerpustakaan.Repositori;

namespace SistemPerpustakaan.Tampilan.Admin
{
    public partial class HalamanDendaAdmin : Page
    {
        private readonly DendaRepositori _repo = new DendaRepositori();
        private List<Denda> _semua = new List<Denda>();

        public HalamanDendaAdmin()
        {
            InitializeComponent();

            // ✅ FIX: pakai Loaded, bukan langsung async di constructor
            this.Loaded += HalamanDendaAdmin_Loaded;
        }

        // ✅ Event Loaded
        private async void HalamanDendaAdmin_Loaded(object sender, RoutedEventArgs e)
        {
            await MuatDataAsync();
        }

        private async Task MuatDataAsync()
        {
            _semua = await _repo.AmbilSemuaAsync();

            var stat = await _repo.StatistikAsync();

            TeksBelumLunas.Text = "Rp " + stat.Item2.ToString("N0");
            TeksSudahLunas.Text = "Rp " + stat.Item3.ToString("N0");
            TeksTotalTagihan.Text = stat.Item1 + " tagihan";

            Filter();
        }

        private void Filter()
        {
            // ✅ Anti null crash
            if (TabelDenda == null) return;

            string kata = KotakCari.Text?.Trim().ToLower() ?? "";
            string status = (FilterStatus.SelectedItem as ComboBoxItem)?.Content?.ToString();

            var hasil = _semua.Where(d =>
            {
                bool cocok = string.IsNullOrEmpty(kata)
                    || (d.NamaPeminjam != null && d.NamaPeminjam.ToLower().Contains(kata))
                    || (d.NamaPengguna != null && d.NamaPengguna.ToLower().Contains(kata));

                bool cocokStatus = string.IsNullOrEmpty(status)
                    || status == "Semua"
                    || d.StatusPembayaran == status;

                return cocok && cocokStatus;
            }).ToList();

            TabelDenda.ItemsSource = hasil;

            if (PanelKosong != null)
                PanelKosong.Visibility = hasil.Count == 0
                    ? Visibility.Visible : Visibility.Collapsed;
        }

        private void KotakCari_Changed(object s, TextChangedEventArgs e) => Filter();
        private void FilterStatus_Changed(object s, SelectionChangedEventArgs e) => Filter();

        private async void MuatUlang_Click(object s, RoutedEventArgs e)
        {
            await MuatDataAsync();
        }

        private void TabelDenda_SelectionChanged(object s, SelectionChangedEventArgs e) { }

        private async void TombolLihatDetail_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int idDenda)
            {
                var detail = await _repo.AmbilDetailAsync(idDenda);
                var denda = _semua.FirstOrDefault(d => d.IdDenda == idDenda);

                if (denda == null || detail.Count == 0) return;

                string rincian = "Detail Denda - " + denda.NamaPeminjam + "\n\n";

                foreach (var d in detail)
                {
                    rincian += "  • " + d.JudulBuku + "\n";
                    rincian += "    " + d.TampilRincian + "\n\n";
                }

                rincian += "TOTAL: " + denda.TampilTotal;

                MessageBox.Show(rincian, "Detail Denda",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async void TombolLunas_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int idDenda)
            {
                var denda = _semua.FirstOrDefault(d => d.IdDenda == idDenda);
                if (denda == null) return;

                if (MessageBox.Show(
                    "Tandai denda " + denda.TampilTotal + " milik " +
                    denda.NamaPeminjam + " sebagai LUNAS?",
                    "Konfirmasi",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question) != MessageBoxResult.Yes)
                    return;

                bool ok = await _repo.TandaiLunasAsync(idDenda, denda.IdPengguna);

                if (ok)
                {
                    MessageBox.Show(
                        "Denda berhasil ditandai lunas.\n" +
                        "Akun pengguna dipulihkan jika tidak ada denda lain.",
                        "Berhasil",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    await MuatDataAsync();
                }
            }
        }
    }
}