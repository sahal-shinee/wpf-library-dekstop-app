using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using SistemPerpustakaan.Repositori;

namespace SistemPerpustakaan.Tampilan.Admin
{
    public partial class HalamanDasbordAdmin : Page
    {
        private readonly BukuRepositori       _bukuRepo   = new BukuRepositori();
        private readonly PenggunaRepositori   _pengRepo   = new PenggunaRepositori();
        private readonly PeminjamanRepositori _pinjamRepo = new PeminjamanRepositori();
        private readonly DendaRepositori      _dendaRepo  = new DendaRepositori();

        public HalamanDasbordAdmin()
        {
            InitializeComponent();
            _ = MuatDataAsync();
        }

        private async Task MuatDataAsync()
        {
            await _pinjamRepo.TandaiTerlambatAsync();

            var statBuku     = await _bukuRepo.StatistikAsync();
            var statPinjam   = await _pinjamRepo.StatistikAsync();
            var statPengguna = await _pengRepo.StatistikAsync();
            var statDenda    = await _dendaRepo.StatistikAsync();

            // Counter animations untuk stat cards
            AnimasiAngka(TotalBuku,     statBuku.Item1);
            AnimasiAngka(TotalDipinjam, statBuku.Item3);
            AnimasiAngka(TotalAnggota,  statPengguna.Item1);

            BukuTersedia.Text        = statBuku.Item2 + " tersedia";
            DipinjamTerlambat.Text   = statPinjam.Item2 + " terlambat";
            TeksBadgeTerlambat.Text  = statPinjam.Item2.ToString();
            AnggotaAktif.Text        = statPengguna.Item2 + " aktif";
            TotalDendaBelumLunas.Text = "Rp " + statDenda.Item2.ToString("N0");
            JumlahTagihanDenda.Text   = statDenda.Item1 + " tagihan";

            // Tabel peminjaman terlambat
            var semuaAktif    = await _pinjamRepo.AmbilAktifAsync();
            var terlambatList = semuaAktif.FindAll(p => p.Terlambat);
            foreach (var p in terlambatList)
                p.DetailBuku = await _pinjamRepo.AmbilDetailAsync(p.IdPeminjaman);

            TabelTerlambat.ItemsSource = terlambatList;
            PanelKosongTerlambat.Visibility =
                terlambatList.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            // Tabel buku terbaru
            var semuaBuku = await _bukuRepo.AmbilSemuaAsync();
            semuaBuku.Sort((a, b2) => b2.DibuatPada.CompareTo(a.DibuatPada));
            TabelBukuTerbaru.ItemsSource =
                semuaBuku.Count > 10 ? semuaBuku.GetRange(0, 10) : semuaBuku;
        }

        private void AnimasiAngka(TextBlock teks, int targetNilai)
        {
            int langkah = Math.Max(1, targetNilai / 25);
            int nilai   = 0;
            var timer   = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(30) };
            timer.Tick += (s, e) =>
            {
                nilai = Math.Min(nilai + langkah, targetNilai);
                teks.Text = nilai.ToString("N0");
                if (nilai >= targetNilai) ((DispatcherTimer)s).Stop();
            };
            timer.Start();
        }
    }
}
