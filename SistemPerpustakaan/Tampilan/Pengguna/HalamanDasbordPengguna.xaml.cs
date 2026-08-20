using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using SistemPerpustakaan.Pembantu;
using SistemPerpustakaan.Repositori;

namespace SistemPerpustakaan.Tampilan.Pengguna
{
    public partial class HalamanDasbordPengguna : Page
    {
        private readonly PeminjamanRepositori _pinjamRepo = new PeminjamanRepositori();
        private readonly DendaRepositori      _dendaRepo  = new DendaRepositori();

        public HalamanDasbordPengguna()
        {
            InitializeComponent();
            IsiSapaan();
            _ = MuatDataAsync();
        }

        private void IsiSapaan()
        {
            var p = SesiPengguna.PenggunaSaatIni;
            if (p == null) return;

            int    jam    = DateTime.Now.Hour;
            string sapaan = jam < 12 ? "Selamat pagi"  :
                            jam < 15 ? "Selamat siang" :
                            jam < 18 ? "Selamat sore"  : "Selamat malam";

            string namaDepan = p.NamaLengkap.Split(' ')[0];
            TeksSapaan.Text  = sapaan + ", " + namaDepan + "!";
            TeksNama.Text    = "@" + p.NamaPengguna + "  ·  Anggota Perpustakaan";

            if (p.AdalahDiblokir)
            {
                PanelStatusAkun.Visibility = Visibility.Visible;
                TeksStatusAkun.Text        =
                    "⚠  Akun Anda diblokir — ada denda belum dilunasi.";
            }
        }

        private async Task MuatDataAsync()
        {
            var pengguna = SesiPengguna.PenggunaSaatIni;
            if (pengguna == null) return;

            // Semua peminjaman aktif pengguna ini
            var semuaPinjam = await _pinjamRepo.AmbilByPenggunaAsync(pengguna.IdPengguna);
            var aktif       = semuaPinjam.Where(p => p.Status != "selesai").ToList();

            // Kumpulkan semua detail buku yang belum dikembalikan
            var semuaDetail = new List<Model.DetailPeminjaman>();
            foreach (var pinjam in aktif)
            {
                var detail = await _pinjamRepo.AmbilDetailAsync(pinjam.IdPeminjaman);
                foreach (var d in detail)
                    if (d.StatusBuku == "dipinjam")
                        semuaDetail.Add(d);
            }

            // Kartu statistik
            AnimasiAngka(JumlahDipinjam, semuaDetail.Count);

            int hampirTempo = aktif.Count(p =>
                p.BatasKembali > DateTime.Today &&
                (p.BatasKembali - DateTime.Today).Days <= 2);
            AnimasiAngka(JumlahHampirTempo, hampirTempo);

            // Tampilkan daftar buku aktif sebagai kartu
            if (semuaDetail.Count > 0)
            {
                DaftarBukuAktif.ItemsSource = semuaDetail;
                PanelKosong.Visibility      = Visibility.Collapsed;
            }
            else
            {
                DaftarBukuAktif.ItemsSource = null;
                PanelKosong.Visibility      = Visibility.Visible;
            }

            // Denda
            var daftarDenda = await _dendaRepo.AmbilByPenggunaAsync(pengguna.IdPengguna);
            var belumLunas  = daftarDenda.Where(d => !d.SudahLunas).ToList();
            decimal total   = 0;
            foreach (var d in belumLunas) total += d.TotalDenda;

            TotalDenda.Text = "Rp " + total.ToString("N0");

            if (belumLunas.Count > 0)
            {
                PanelPeringatanDenda.Visibility = Visibility.Visible;
                TeksDetailDenda.Text =
                    "Total denda: Rp " + total.ToString("N0") +
                    "  (" + belumLunas.Count + " tagihan)";
            }
            else
            {
                PanelPeringatanDenda.Visibility = Visibility.Collapsed;
            }
        }

        private void AnimasiAngka(TextBlock teks, int targetNilai)
        {
            if (targetNilai == 0) { teks.Text = "0"; return; }
            int current = 0;
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(30) };
            timer.Tick += (s, e) =>
            {
                current += Math.Max(1, targetNilai / 25);
                if (current >= targetNilai) { current = targetNilai; timer.Stop(); }
                teks.Text = current.ToString();
            };
            timer.Start();
        }

        private void TombolLihatSemua_Click(object sender, RoutedEventArgs e)
        {
            var jendela = Window.GetWindow(this) as Bersama.JendelaUtamaPengguna;
            if (jendela != null) jendela.NavigasiKeRiwayat();
        }
    }
}
