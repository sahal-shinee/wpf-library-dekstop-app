using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using SistemPerpustakaan.Pembantu;
using SistemPerpustakaan.Tampilan.Admin;
using SistemPerpustakaan.Tampilan.Komponen;

namespace SistemPerpustakaan.Tampilan.Bersama
{
    public partial class JendelaUtamaAdmin : Window
    {
        private readonly DispatcherTimer _timer = new DispatcherTimer();
        private Button _navAktif;

        public JendelaUtamaAdmin()
        {
            InitializeComponent();
            IsiInfoPengguna();
            MulaiJam();
            NavigasiKe(new HalamanDasbordAdmin(), BtnDasbord, "Dashboard");
        }

        private void IsiInfoPengguna()
        {
            var p = SesiPengguna.PenggunaSaatIni;
            if (p == null) return;
            string inisial = p.NamaLengkap.Length > 0
                ? p.NamaLengkap[0].ToString().ToUpper() : "A";
            NamaAdmin.Text      = p.NamaLengkap;
            InisialAdmin.Text   = inisial;
            InisialTopbar.Text  = inisial;
        }

        private void MulaiJam()
        {
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick    += (s, e) => PerbaruiJam();
            _timer.Start();
            PerbaruiJam();
        }

        private void PerbaruiJam()
        {
            var now = DateTime.Now;
            var id  = new CultureInfo("id-ID");
            TeksTanggal.Text = now.ToString("dddd, dd MMM yyyy", id);
            TeksJam.Text     = now.ToString("HH:mm:ss");
        }

        public void NavigasiKe(Page halaman, Button tombol, string judul)
        {
            if (_navAktif != null)
                _navAktif.Style = (Style)Resources["TombolNav"];

            tombol.Style      = (Style)Resources["TombolNavAktif"];
            _navAktif         = tombol;
            JudulHalaman.Text = judul;

            // Fade out lalu navigate
            var fadeOut = new DoubleAnimation(1, 0,
                new Duration(TimeSpan.FromMilliseconds(100)));
            fadeOut.Completed += (s, e) =>
            {
                BingkaiKonten.Navigate(halaman);
                var fadeIn = new DoubleAnimation(0, 1,
                    new Duration(TimeSpan.FromMilliseconds(220)));
                BingkaiKonten.BeginAnimation(OpacityProperty, fadeIn);
            };
            BingkaiKonten.BeginAnimation(OpacityProperty, fadeOut);
        }

        // Tampilkan toast dari halaman mana pun
        public void TampilkanToast(string pesan, TipeToast tipe = TipeToast.Sukses)
        {
            ToastAdmin.Tampilkan(pesan, tipe);
        }

        private void BtnDasbord_Click(object s, RoutedEventArgs e)        => NavigasiKe(new HalamanDasbordAdmin(),      BtnDasbord,        "Dashboard");
        private void BtnKelolaBuku_Click(object s, RoutedEventArgs e)     => NavigasiKe(new HalamanKelolaBuku(),        BtnKelolaBuku,     "Kelola Buku");
        private void BtnKelolaPengguna_Click(object s, RoutedEventArgs e) => NavigasiKe(new HalamanKelolaPengguna(),    BtnKelolaPengguna, "Kelola Pengguna");
        private void BtnPeminjaman_Click(object s, RoutedEventArgs e)     => NavigasiKe(new HalamanPeminjamanAdmin(),   BtnPeminjaman,     "Peminjaman");
        private void BtnPengembalian_Click(object s, RoutedEventArgs e)   => NavigasiKe(new HalamanPengembalianAdmin(), BtnPengembalian,   "Pengembalian");
        private void BtnDenda_Click(object s, RoutedEventArgs e)          => NavigasiKe(new HalamanDendaAdmin(),        BtnDenda,          "Manajemen Denda");
        private void BtnLaporan_Click(object s, RoutedEventArgs e)        => NavigasiKe(new HalamanLaporan(),           BtnLaporan,        "Ekspor Laporan");

        private void TombolKeluar_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Apakah Anda yakin ingin keluar?",
                "Konfirmasi Keluar", MessageBoxButton.YesNo, MessageBoxImage.Question)
                != MessageBoxResult.Yes) return;

            _timer.Stop();
            SesiPengguna.AkhiriSesi();
            new JendelaLogin().Show();
            this.Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            _timer.Stop();
            base.OnClosed(e);
        }
    }
}
