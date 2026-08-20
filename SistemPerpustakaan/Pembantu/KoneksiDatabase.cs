using System;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace SistemPerpustakaan.Pembantu
{
    /// <summary>
    /// Pusat pengelolaan koneksi MySQL.
    /// Semua repositori menggunakan kelas ini.
    /// Kompatibel dengan C# 7.3 / .NET Framework 4.8
    /// </summary>
    public static class KoneksiDatabase
    {
        public static string BuatStringKoneksi(bool tanpaDatabase = false)
        {
            var b = new MySqlConnectionStringBuilder
            {
                Server                = KonstantaAplikasi.ServerDatabase,
                Port                  = (uint)KonstantaAplikasi.PortDatabase,
                UserID                = KonstantaAplikasi.PenggunaDB,
                Password              = KonstantaAplikasi.KataSandiDB,
                CharacterSet          = "utf8mb4",
                ConnectionTimeout     = 10,
                DefaultCommandTimeout = 30,
                AllowUserVariables    = true,
                SslMode               = MySqlSslMode.Disabled,
            };
            if (!tanpaDatabase)
                b.Database = KonstantaAplikasi.NamaDatabase;
            return b.ConnectionString;
        }

        // ── Buka koneksi sinkron ──────────────────────────────────────────
        public static MySqlConnection BukaKoneksi()
        {
            var k = new MySqlConnection(BuatStringKoneksi());
            k.Open();
            return k;
        }

        // ── Buka koneksi tanpa nama database (untuk tes koneksi) ──────────
        public static MySqlConnection BukaKoneksiTanpaDb()
        {
            var k = new MySqlConnection(BuatStringKoneksi(tanpaDatabase: true));
            k.Open();
            return k;
        }

        // ── Buka koneksi async ────────────────────────────────────────────
        public static async Task<MySqlConnection> BukaKoneksiAsync()
        {
            var k = new MySqlConnection(BuatStringKoneksi());
            await k.OpenAsync();
            return k;
        }

        // ── Uji koneksi ───────────────────────────────────────────────────
        public static string UjiKoneksi()
        {
            try
            {
                using (var k = BukaKoneksi())
                using (var c = new MySqlCommand("SELECT 1", k))
                {
                    c.ExecuteScalar();
                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        // ── NonQuery (INSERT / UPDATE / DELETE) ───────────────────────────
        public static int EksekusiNonQuery(string sql,
            Action<MySqlParameterCollection> param = null)
        {
            using (var k = BukaKoneksi())
            using (var c = new MySqlCommand(sql, k))
            {
                if (param != null) param(c.Parameters);
                return c.ExecuteNonQuery();
            }
        }

        public static async Task<int> EksekusiNonQueryAsync(string sql,
            Action<MySqlParameterCollection> param = null)
        {
            using (var k = await BukaKoneksiAsync())
            using (var c = new MySqlCommand(sql, k))
            {
                if (param != null) param(c.Parameters);
                return await c.ExecuteNonQueryAsync();
            }
        }

        // ── Skalar ────────────────────────────────────────────────────────
        public static T EksekusiSkalar<T>(string sql,
            Action<MySqlParameterCollection> param = null,
            T nilaiDefault = default(T))
        {
            using (var k = BukaKoneksi())
            using (var c = new MySqlCommand(sql, k))
            {
                if (param != null) param(c.Parameters);
                var h = c.ExecuteScalar();
                if (h == null || h == DBNull.Value) return nilaiDefault;
                return (T)Convert.ChangeType(h, typeof(T));
            }
        }

        public static async Task<T> EksekusiSkalarAsync<T>(string sql,
            Action<MySqlParameterCollection> param = null,
            T nilaiDefault = default(T),
            bool tanpaDatabase = false)
        {
            var connStr = tanpaDatabase
                ? BuatStringKoneksi(tanpaDatabase: true)
                : BuatStringKoneksi();

            using (var k = new MySqlConnection(connStr))
            {
                await k.OpenAsync();
                using (var c = new MySqlCommand(sql, k))
                {
                    if (param != null) param(c.Parameters);
                    var h = await c.ExecuteScalarAsync();
                    if (h == null || h == DBNull.Value) return nilaiDefault;
                    return (T)Convert.ChangeType(h, typeof(T));
                }
            }
        }

        // ── Transaksi database ────────────────────────────────────────────
        public static void EksekusiDenganTransaksi(
            Action<MySqlConnection, MySqlTransaction> aksi)
        {
            using (var k = BukaKoneksi())
            {
                var t = k.BeginTransaction();
                try
                {
                    aksi(k, t);
                    t.Commit();
                }
                catch
                {
                    t.Rollback();
                    throw;
                }
            }
        }

        public static async Task EksekusiDenganTransaksiAsync(
            Func<MySqlConnection, MySqlTransaction, Task> aksi)
        {
            using (var k = await BukaKoneksiAsync())
            {
                var t = k.BeginTransaction();
                try
                {
                    await aksi(k, t);
                    t.Commit();
                }
                catch
                {
                    t.Rollback();
                    throw;
                }
            }
        }
    }
}
