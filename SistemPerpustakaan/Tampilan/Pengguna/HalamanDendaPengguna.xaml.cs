using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using SistemPerpustakaan.Pembantu;
using SistemPerpustakaan.Repositori;

namespace SistemPerpustakaan.Tampilan.Pengguna
{
    public partial class HalamanDendaPengguna : Page
    {
        private readonly DendaRepositori _repo = new DendaRepositori();

        public HalamanDendaPengguna()
        {
            InitializeComponent();
            _ = MuatDataAsync();
        }

        private async Task MuatDataAsync()
        {
            var pengguna = SesiPengguna.PenggunaSaatIni;
            if (pengguna == null) return;

            // Panel blokir
            PanelBlokir.Visibility = pengguna.AdalahDiblokir
                ? Visibility.Visible : Visibility.Collapsed;

            var daftarDenda = await _repo.AmbilByPenggunaAsync(pengguna.IdPengguna);

            if (daftarDenda.Count == 0)
            {
                DaftarDenda.Visibility  = Visibility.Collapsed;
                PanelKosong.Visibility  = Visibility.Visible;
                TeksBelumLunas.Text     = "Rp 0";
                TeksSudahLunas.Text     = "Rp 0";
                TeksJumlahTagihan.Text  = "0 tagihan";
                return;
            }

            // Muat detail per denda
            foreach (var d in daftarDenda)
                d.DetailBuku = await _repo.AmbilDetailAsync(d.IdDenda);

            // Ringkasan
            decimal totalBL = 0, totalSL = 0;
            foreach (var d in daftarDenda)
            {
                if (!d.SudahLunas) totalBL += d.TotalDenda;
                else               totalSL += d.TotalDenda;
            }

            TeksBelumLunas.Text    = "Rp " + totalBL.ToString("N0");
            TeksSudahLunas.Text    = "Rp " + totalSL.ToString("N0");
            TeksJumlahTagihan.Text = daftarDenda.Count + " tagihan";

            DaftarDenda.ItemsSource = daftarDenda;
            DaftarDenda.Visibility  = Visibility.Visible;
            PanelKosong.Visibility  = Visibility.Collapsed;
        }
    }
}
