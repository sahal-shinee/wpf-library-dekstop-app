using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using SistemPerpustakaan.Model;
using SistemPerpustakaan.Pembantu;

namespace SistemPerpustakaan.Repositori
{
    public class BukuRepositori
    {
        private static Buku Petakan(MySqlDataReader r)
        {
            return new Buku
            {
                IdBuku       = Convert.ToInt32(r["id_buku"]),
                Judul        = r["judul"].ToString(),
                Penulis      = r["penulis"].ToString(),
                Penerbit     = r["penerbit"]    == DBNull.Value ? null : r["penerbit"].ToString(),
                Kategori     = r["kategori"]    == DBNull.Value ? null : r["kategori"].ToString(),
                Isbn         = r["isbn"]        == DBNull.Value ? null : r["isbn"].ToString(),
                TahunTerbit  = r["tahun_terbit"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["tahun_terbit"]),
                JumlahStok   = Convert.ToInt32(r["jumlah_stok"]),
                StokTersedia = Convert.ToInt32(r["stok_tersedia"]),
                Deskripsi    = r["deskripsi"]   == DBNull.Value ? null : r["deskripsi"].ToString(),
                Harga        = r["harga"]        == DBNull.Value ? 0m  : Convert.ToDecimal(r["harga"]),
                Sampul       = r["sampul"]       == DBNull.Value ? null : r["sampul"].ToString(),
                DibuatPada   = Convert.ToDateTime(r["dibuat_pada"]),
            };
        }

        private const string SqlPilih = @"SELECT id_buku, judul, penulis, penerbit,
            kategori, isbn, tahun_terbit, jumlah_stok, stok_tersedia,
            deskripsi, harga, sampul, dibuat_pada FROM buku";

        public async Task<List<Buku>> AmbilSemuaAsync()
        {
            var list = new List<Buku>();

            using (var k = await KoneksiDatabase.BukaKoneksiAsync())
            using (var c = new MySqlCommand(SqlPilih + " ORDER BY judul", k))
            using (MySqlDataReader r = (MySqlDataReader)await c.ExecuteReaderAsync())
            {
                while (await r.ReadAsync())
                    list.Add(Petakan(r));
            }

            return list;
        }

        public async Task<List<Buku>> CariAsync(string kata, string kategori = null)
        {
            var list = new List<Buku>();

            string sql = SqlPilih +
                " WHERE (judul LIKE @k OR penulis LIKE @k OR kategori LIKE @k)";

            if (!string.IsNullOrEmpty(kategori))
                sql += " AND kategori = @kat";

            sql += " ORDER BY judul";

            using (var k = await KoneksiDatabase.BukaKoneksiAsync())
            using (var c = new MySqlCommand(sql, k))
            {
                c.Parameters.AddWithValue("@k", "%" + kata + "%");

                if (!string.IsNullOrEmpty(kategori))
                    c.Parameters.AddWithValue("@kat", kategori);

                using (MySqlDataReader r = (MySqlDataReader)await c.ExecuteReaderAsync())
                {
                    while (await r.ReadAsync())
                        list.Add(Petakan(r));
                }
            }

            return list;
        }

        public async Task<Buku> AmbilByIdAsync(int id)
        {
            using (var k = await KoneksiDatabase.BukaKoneksiAsync())
            using (var c = new MySqlCommand(SqlPilih + " WHERE id_buku = @id", k))
            {
                c.Parameters.AddWithValue("@id", id);

                using (MySqlDataReader r = (MySqlDataReader)await c.ExecuteReaderAsync())
                {
                    if (await r.ReadAsync())
                        return Petakan(r);
                }
            }

            return null;
        }

        public async Task<List<string>> AmbilKategoriAsync()
        {
            var list = new List<string>();

            using (var k = await KoneksiDatabase.BukaKoneksiAsync())
            using (var c = new MySqlCommand(
                "SELECT DISTINCT kategori FROM buku WHERE kategori IS NOT NULL ORDER BY kategori", k))
            using (MySqlDataReader r = (MySqlDataReader)await c.ExecuteReaderAsync())
            {
                while (await r.ReadAsync())
                    list.Add(r.GetString(0));
            }

            return list;
        }

        public async Task<int> TambahAsync(Buku b)
        {
            const string sql = @"INSERT INTO buku
                (judul, penulis, penerbit, kategori, isbn, tahun_terbit,
                 jumlah_stok, stok_tersedia, deskripsi, harga, sampul)
                VALUES (@j,@pe,@pn,@ka,@is,@th,@js,@st,@de,@ha,@sa);
                SELECT LAST_INSERT_ID();";

            using (var k = await KoneksiDatabase.BukaKoneksiAsync())
            using (var c = new MySqlCommand(sql, k))
            {
                c.Parameters.AddWithValue("@j",  b.Judul);
                c.Parameters.AddWithValue("@pe", b.Penulis);
                c.Parameters.AddWithValue("@pn", (object)b.Penerbit    ?? DBNull.Value);
                c.Parameters.AddWithValue("@ka", (object)b.Kategori    ?? DBNull.Value);
                c.Parameters.AddWithValue("@is", (object)b.Isbn        ?? DBNull.Value);
                c.Parameters.AddWithValue("@th", (object)b.TahunTerbit ?? DBNull.Value);
                c.Parameters.AddWithValue("@js", b.JumlahStok);
                c.Parameters.AddWithValue("@st", b.JumlahStok);
                c.Parameters.AddWithValue("@de", (object)b.Deskripsi   ?? DBNull.Value);
                c.Parameters.AddWithValue("@ha", b.Harga);
                c.Parameters.AddWithValue("@sa", (object)b.Sampul      ?? DBNull.Value);

                return Convert.ToInt32(await c.ExecuteScalarAsync());
            }
        }

        public async Task<bool> PerbaruiAsync(Buku b)
        {
            int n = await KoneksiDatabase.EksekusiNonQueryAsync(
                @"UPDATE buku SET judul=@j, penulis=@pe, penerbit=@pn,
                  kategori=@ka, isbn=@is, tahun_terbit=@th,
                  jumlah_stok=@js, deskripsi=@de, harga=@ha, sampul=@sa
                  WHERE id_buku=@id",
                p =>
                {
                    p.AddWithValue("@j",  b.Judul);
                    p.AddWithValue("@pe", b.Penulis);
                    p.AddWithValue("@pn", (object)b.Penerbit    ?? DBNull.Value);
                    p.AddWithValue("@ka", (object)b.Kategori    ?? DBNull.Value);
                    p.AddWithValue("@is", (object)b.Isbn        ?? DBNull.Value);
                    p.AddWithValue("@th", (object)b.TahunTerbit ?? DBNull.Value);
                    p.AddWithValue("@js", b.JumlahStok);
                    p.AddWithValue("@de", (object)b.Deskripsi   ?? DBNull.Value);
                    p.AddWithValue("@ha", b.Harga);
                    p.AddWithValue("@sa", (object)b.Sampul      ?? DBNull.Value);
                    p.AddWithValue("@id", b.IdBuku);
                });

            return n > 0;
        }

        public async Task PerbaruiSampulAsync(int idBuku, string namaFile)
        {
            await KoneksiDatabase.EksekusiNonQueryAsync(
                "UPDATE buku SET sampul = @s WHERE id_buku = @id",
                p =>
                {
                    p.AddWithValue("@s", namaFile);
                    p.AddWithValue("@id", idBuku);
                });
        }

        public async Task<Tuple<bool, string>> HapusAsync(int id)
        {
            int sedangDipinjam = await KoneksiDatabase.EksekusiSkalarAsync<int>(
                @"SELECT COUNT(*) FROM detail_peminjaman dp
                  JOIN peminjaman p ON dp.id_peminjaman = p.id_peminjaman
                  WHERE dp.id_buku = @id AND dp.status_buku = 'dipinjam'",
                p => p.AddWithValue("@id", id));

            if (sedangDipinjam > 0)
                return Tuple.Create(false, "Buku sedang dipinjam dan tidak dapat dihapus.");

            PembantuGambar.HapusSampulBuku(id);

            await KoneksiDatabase.EksekusiNonQueryAsync(
                "DELETE FROM buku WHERE id_buku = @id",
                p => p.AddWithValue("@id", id));

            return Tuple.Create(true, "Buku berhasil dihapus.");
        }

        public async Task<Tuple<int, int, int>> StatistikAsync()
        {
            const string sql = @"SELECT COUNT(*) total,
                SUM(stok_tersedia > 0) tersedia,
                SUM(jumlah_stok - stok_tersedia) dipinjam FROM buku";

            using (var k = await KoneksiDatabase.BukaKoneksiAsync())
            using (var c = new MySqlCommand(sql, k))
            using (MySqlDataReader r = (MySqlDataReader)await c.ExecuteReaderAsync())
            {
                if (await r.ReadAsync())
                {
                    return Tuple.Create(
                        Convert.ToInt32(r["total"]),
                        Convert.ToInt32(r["tersedia"]),
                        Convert.ToInt32(r["dipinjam"])
                    );
                }
            }

            return Tuple.Create(0, 0, 0);
        }
    }
}