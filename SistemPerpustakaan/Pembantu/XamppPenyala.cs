using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace SistemPerpustakaan.Pembantu
{
    public static class XamppPenyala
    {
        public static event Action<string> LaporanKemajuan;
        public static event Action<bool> SelesaiBerjalan;

        private static void Laporkan(string pesan)
        {
            LaporanKemajuan?.Invoke(pesan);
        }

        // ── ENTRY POINT ─────────────────────────────────────────────
        public static async Task<bool> PastikanMysqlBerjalanAsync()
        {
            try
            {
                Laporkan("Memeriksa status MySQL...");

                // ✔ Cek proses mysqld saja (AMAN)
                if (CekProsesMysqld())
                {
                    Laporkan("Proses MySQL sudah aktif.");
                    return await CekKoneksiDatabaseAsync();
                }

                Laporkan("MySQL belum berjalan. Memulai XAMPP MySQL...");
                bool ok = await JalankanMysqlXamppAsync();

                SelesaiBerjalan?.Invoke(ok);
                return ok;
            }
            catch (Exception ex)
            {
                Laporkan("Kesalahan: " + ex.Message);
                SelesaiBerjalan?.Invoke(false);
                return false;
            }
        }

        // ── CEK PROSES MYSQL ────────────────────────────────────────
        private static bool CekProsesMysqld()
        {
            try
            {
                return Process.GetProcessesByName("mysqld").Length > 0;
            }
            catch
            {
                return false;
            }
        }

        // ── JALANKAN MYSQL XAMPP ────────────────────────────────────
        private static async Task<bool> JalankanMysqlXamppAsync()
        {
            string[] kemungkinan = {
                KonstantaAplikasi.PathXamppMysql,
                @"C:\xampp\mysql\bin\mysqld.exe",
                @"D:\xampp\mysql\bin\mysqld.exe",
                @"E:\xampp\mysql\bin\mysqld.exe"
            };

            string pathMysqld = null;

            foreach (var p in kemungkinan)
            {
                if (!string.IsNullOrEmpty(p) && File.Exists(p))
                {
                    pathMysqld = p;
                    break;
                }
            }

            if (pathMysqld == null)
            {
                Laporkan("mysqld.exe tidak ditemukan. Pastikan XAMPP terinstal.");
                return false;
            }

            try
            {
                Laporkan("Menjalankan MySQL...");
                string folderBin = Path.GetDirectoryName(pathMysqld);
                string pathIni = Path.Combine(folderBin, "my.ini");

                var info = new ProcessStartInfo
                {
                    FileName = pathMysqld,
                    Arguments = $"--defaults-file=\"{pathIni}\" --standalone",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = folderBin
                };

                Process.Start(info);

                return await TungguMysqlSiapAsync();
            }
            catch (Exception ex)
            {
                Laporkan("Gagal menjalankan MySQL: " + ex.Message);
                return false;
            }
        }

        // ── TUNGGU MYSQL SIAP ───────────────────────────────────────
        private static async Task<bool> TungguMysqlSiapAsync()
        {
            Laporkan("Menunggu MySQL siap...");

            for (int i = 1; i <= KonstantaAplikasi.MaksPercobaanKoneksi; i++)
            {
                await Task.Delay(1000);

                Laporkan($"Percobaan koneksi {i}/{KonstantaAplikasi.MaksPercobaanKoneksi}...");

                if (await CekKoneksiDatabaseAsync())
                {
                    Laporkan("MySQL berhasil terhubung!");
                    return true;
                }
            }

            Laporkan("MySQL tidak merespons.");
            return false;
        }

        // ── CEK KONEKSI DATABASE ────────────────────────────────────
        public static async Task<bool> CekKoneksiDatabaseAsync()
        {
            try
            {
                using (var k = new MySqlConnection(
                    KoneksiDatabase.BuatStringKoneksi(tanpaDatabase: true)))
                {
                    await k.OpenAsync();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        // ── INISIALISASI DATABASE ───────────────────────────────────
        public static async Task InisialisasiDatabaseAsync()
        {
            Laporkan("Memeriksa database...");

            int ada = await KoneksiDatabase.EksekusiSkalarAsync<int>(
                "SELECT COUNT(*) FROM INFORMATION_SCHEMA.SCHEMATA WHERE SCHEMA_NAME = @db",
                p => p.AddWithValue("@db", KonstantaAplikasi.NamaDatabase),
                nilaiDefault: 0,
                tanpaDatabase: true);

            if (ada == 0)
            {
                Laporkan("Database belum ada. Membuat...");
                await JalankanSkripSqlAsync();
                Laporkan("Database berhasil dibuat!");
            }
            else
            {
                Laporkan("Database sudah tersedia.");
            }
        }

        private static async Task JalankanSkripSqlAsync()
        {
            string path = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "skema_database_perpustakaan_v2.sql");

            if (!File.Exists(path))
            {
                Laporkan("File SQL tidak ditemukan.");
                return;
            }

            string isi = await Task.Run(() => File.ReadAllText(path));

            using (var k = new MySqlConnection(
                KoneksiDatabase.BuatStringKoneksi(tanpaDatabase: true)))
            {
                await k.OpenAsync();
                var skrip = new MySqlScript(k, isi);
                await Task.Run(() => skrip.Execute());
            }
        }
    }
}