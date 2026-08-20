using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using SistemPerpustakaan.Pembantu;
using SistemPerpustakaan.Tampilan.Komponen;
using SistemPerpustakaan.Tampilan.Pengguna;

namespace SistemPerpustakaan.Tampilan.Bersama
{
    public partial class JendelaUtamaPengguna : Window
    {
        private readonly DispatcherTimer _timer = new DispatcherTimer();
        private Button _navAktif;

        public JendelaUtamaPengguna()
        {
            InitializeComponent();
            IsiInfoPengguna();
            MulaiJam();
            NavigasiKe(new HalamanDasbordPengguna(), BtnBerandaUser, "Beranda");
        }

        private void IsiInfoPengguna()
        {
            var p = SesiPengguna.PenggunaSaatIni;
            if (p == null) return;
            NamaUser.Text    = p.NamaLengkap;
            InisialUser.Text = p.NamaLengkap.Length > 0
                ? p.NamaLengkap[0].ToString().ToUpper() : "U";
        }

        private void MulaiJam()
        {
            _timer.Interval = TimeSpan.FromSeconds(60);
            _timer.Tick    += (s, e) => PerbaruiTanggal();
            _timer.Start();
            PerbaruiTanggal();
        }

        private void PerbaruiTanggal()
        {
            TeksTanggal.Text = DateTime.Now.ToString(
                "dddd, dd MMMM yyyy", new CultureInfo("id-ID"));
        }

        public void NavigasiKe(Page halaman, Button tombol, string judul)
        {
            if (_navAktif != null)
                _navAktif.Style = (Style)Resources["TombolNavUser"];

            tombol.Style      = (Style)Resources["TombolNavUserAktif"];
            _navAktif         = tombol;
            JudulHalaman.Text = judul;

            // Fade transition
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

        public void TampilkanToast(string pesan, TipeToast tipe = TipeToast.Sukses)
        {
            ToastUser.Tampilkan(pesan, tipe);
        }

        public void NavigasiKeRiwayat()
        {
            NavigasiKe(new HalamanRiwayatPinjam(), BtnRiwayatPinjam, "Riwayat Peminjaman");
        }

        private void BtnBeranda_Click(object s, RoutedEventArgs e)    => NavigasiKe(new HalamanDasbordPengguna(),    BtnBerandaUser,   "Beranda");
        private void BtnCariPinjam_Click(object s, RoutedEventArgs e) => NavigasiKe(new HalamanCariPinjamBuku(),    BtnCariPinjam,    "Cari & Pinjam Buku");
        private void BtnRiwayat_Click(object s, RoutedEventArgs e)    => NavigasiKe(new HalamanRiwayatPinjam(),     BtnRiwayatPinjam, "Riwayat Peminjaman");
        private void BtnDenda_Click(object s, RoutedEventArgs e)      => NavigasiKe(new HalamanDendaPengguna(),     BtnDendaUser,     "Status Denda");

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
