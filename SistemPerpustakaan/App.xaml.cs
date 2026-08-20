using System;
using System.Threading.Tasks;
using System.Windows;
using SistemPerpustakaan.Pembantu;
using SistemPerpustakaan.Tampilan.Bersama;
using PdfSharp.Fonts;

namespace SistemPerpustakaan
{
    public partial class App : Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // register font resolver for MigraDoc/PDFsharp
            GlobalFontSettings.FontResolver = new FontResolver();

            KonstantaAplikasi.InisialisasiFolderAplikasi();

            var splash = new JendelaSplash();
            splash.Show();

            bool mysqlSiap = false;

            try
            {
                XamppPenyala.LaporanKemajuan += pesan =>
                    Dispatcher.Invoke(() => splash.PerbaruiPesan(pesan));

                mysqlSiap = await XamppPenyala.PastikanMysqlBerjalanAsync();

                if (mysqlSiap)
                {
                    splash.PerbaruiPesan("Memeriksa database...");
                    await XamppPenyala.InisialisasiDatabaseAsync();

                    splash.PerbaruiPesan("Sistem siap!");
                    await Task.Delay(600);
                }
            }
            catch (Exception ex)
            {
                splash.PerbaruiPesan("Kesalahan: " + ex.Message);
                await Task.Delay(2000);
            }

            // ✅ PENTING: buat login dulu
            var login = new JendelaLogin();

            // ✅ Set sebagai MainWindow (INI KUNCI)
            this.MainWindow = login;

            // baru tutup splash
            splash.Close();

            if (!mysqlSiap)
            {
                MessageBox.Show(
                    "Tidak dapat terhubung ke MySQL.\n\n" +
                    "Pastikan XAMPP sudah terinstal.",
                    "Peringatan",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }

            // ✅ tampilkan login terakhir
            login.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            SesiPengguna.AkhiriSesi();
            base.OnExit(e);
        }
    }
}