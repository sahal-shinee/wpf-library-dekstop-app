// ================================================================
//  PenggunaRepositori.cs — FIX FINAL
// ================================================================
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using SistemPerpustakaan.Model;
using SistemPerpustakaan.Pembantu;

namespace SistemPerpustakaan.Repositori
{
    public class PenggunaRepositori
    {
        private static Pengguna Petakan(MySqlDataReader r)
        {
            return new Pengguna
            {
                IdPengguna = r.GetInt32("id_pengguna"),
                NamaLengkap = r.GetString("nama_lengkap"),
                NamaPengguna = r.GetString("nama_pengguna"),
                Surel = r.GetString("surel"),
                Telepon = r.IsDBNull(r.GetOrdinal("telepon"))
                               ? null : r.GetString("telepon"),
                Peran = r.GetString("peran"),
                Status = r.GetString("status"),
                DibuatPada = r.GetDateTime("dibuat_pada"),
            };
        }

        public async Task<List<Pengguna>> AmbilSemuaAnggotaAsync()
        {
            var list = new List<Pengguna>();

            const string sql = @"SELECT id_pengguna, nama_lengkap, nama_pengguna,
                surel, telepon, peran, status, dibuat_pada
                FROM pengguna WHERE peran = 'pengguna' ORDER BY nama_lengkap";

            using (var k = await KoneksiDatabase.BukaKoneksiAsync())
            using (var c = new MySqlCommand(sql, k))
            using (var r = (MySqlDataReader)await c.ExecuteReaderAsync())
            {
                while (await r.ReadAsync())
                    list.Add(Petakan(r));
            }

            return list;
        }

        public async Task<List<Pengguna>> CariAsync(string kata)
        {
            var list = new List<Pengguna>();

            const string sql = @"SELECT id_pengguna, nama_lengkap, nama_pengguna,
                surel, telepon, peran, status, dibuat_pada
                FROM pengguna WHERE peran = 'pengguna'
                AND (nama_lengkap LIKE @k OR nama_pengguna LIKE @k OR surel LIKE @k)
                ORDER BY nama_lengkap";

            using (var k = await KoneksiDatabase.BukaKoneksiAsync())
            using (var c = new MySqlCommand(sql, k))
            {
                c.Parameters.AddWithValue("@k", "%" + kata + "%");

                using (var r = (MySqlDataReader)await c.ExecuteReaderAsync())
                {
                    while (await r.ReadAsync())
                        list.Add(Petakan(r));
                }
            }

            return list;
        }

        public async Task<Pengguna> AmbilByIdAsync(int id)
        {
            const string sql = @"SELECT id_pengguna, nama_lengkap, nama_pengguna,
                surel, telepon, peran, status, dibuat_pada
                FROM pengguna WHERE id_pengguna = @id";

            using (var k = await KoneksiDatabase.BukaKoneksiAsync())
            using (var c = new MySqlCommand(sql, k))
            {
                c.Parameters.AddWithValue("@id", id);

                using (var r = (MySqlDataReader)await c.ExecuteReaderAsync())
                {
                    if (await r.ReadAsync()) return Petakan(r);
                }
            }

            return null;
        }

        public async Task<bool> PerbaruiAsync(Pengguna p)
        {
            int n = await KoneksiDatabase.EksekusiNonQueryAsync(
                @"UPDATE pengguna SET nama_lengkap=@nl, surel=@s,
                  telepon=@t, status=@st WHERE id_pengguna=@id",
                par =>
                {
                    par.AddWithValue("@nl", p.NamaLengkap);
                    par.AddWithValue("@s", p.Surel);
                    par.AddWithValue("@t", string.IsNullOrEmpty(p.Telepon)
                        ? (object)DBNull.Value : p.Telepon);
                    par.AddWithValue("@st", p.Status);
                    par.AddWithValue("@id", p.IdPengguna);
                });

            return n > 0;
        }

        public async Task<bool> UbahStatusAsync(int id, string status)
        {
            int n = await KoneksiDatabase.EksekusiNonQueryAsync(
                "UPDATE pengguna SET status = @s WHERE id_pengguna = @id",
                p =>
                {
                    p.AddWithValue("@s", status);
                    p.AddWithValue("@id", id);
                });

            return n > 0;
        }

        public async Task<Tuple<bool, string>> HapusAsync(int id)
        {
            int aktif = await KoneksiDatabase.EksekusiSkalarAsync<int>(
                @"SELECT COUNT(*) FROM peminjaman
                  WHERE id_pengguna = @id AND status NOT IN ('selesai')",
                p => p.AddWithValue("@id", id));

            if (aktif > 0)
                return Tuple.Create(false, "Pengguna masih memiliki peminjaman aktif.");

            int denda = await KoneksiDatabase.EksekusiSkalarAsync<int>(
                @"SELECT COUNT(*) FROM denda
                  WHERE id_pengguna = @id AND status_pembayaran = 'belum_lunas'",
                p => p.AddWithValue("@id", id));

            if (denda > 0)
                return Tuple.Create(false, "Pengguna masih memiliki denda belum lunas.");

            await KoneksiDatabase.EksekusiNonQueryAsync(
                "DELETE FROM pengguna WHERE id_pengguna = @id AND peran = 'pengguna'",
                p => p.AddWithValue("@id", id));

            return Tuple.Create(true, "Pengguna berhasil dihapus.");
        }

        public async Task<Tuple<int, int, int>> StatistikAsync()
        {
            const string sql = @"SELECT COUNT(*) total,
                SUM(status = 'aktif') aktif,
                SUM(status = 'diblokir') diblokir
                FROM pengguna WHERE peran = 'pengguna'";

            using (var k = await KoneksiDatabase.BukaKoneksiAsync())
            using (var c = new MySqlCommand(sql, k))
            using (var r = (MySqlDataReader)await c.ExecuteReaderAsync())
            {
                if (await r.ReadAsync())
                {
                    return Tuple.Create(
                        r.GetInt32("total"),
                        r.GetInt32("aktif"),
                        r.GetInt32("diblokir"));
                }
            }

            return Tuple.Create(0, 0, 0);
        }
    }
}