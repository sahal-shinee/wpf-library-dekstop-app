using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using SistemPerpustakaan.Model;
using SistemPerpustakaan.Pembantu;

namespace SistemPerpustakaan.Repositori
{
    public class PeminjamanRepositori
    {
        private static Peminjaman PetakanHeader(MySqlDataReader r)
        {
            return new Peminjaman
            {
                IdPeminjaman = Convert.ToInt32(r["id_peminjaman"]),
                IdPengguna = Convert.ToInt32(r["id_pengguna"]),
                TanggalPinjam = Convert.ToDateTime(r["tanggal_pinjam"]),
                BatasKembali = Convert.ToDateTime(r["batas_kembali"]),
                Status = r["status"].ToString(),
                Catatan = r["catatan"] == DBNull.Value ? null : r["catatan"].ToString(),
                DibuatPada = Convert.ToDateTime(r["dibuat_pada"]),
                NamaPeminjam = r["nama_lengkap"].ToString(),
                NamaPengguna = r["nama_pengguna"].ToString(),
            };
        }

        public async Task<List<Peminjaman>> AmbilSemuaAsync()
        {
            var list = new List<Peminjaman>();

            const string sql = @"SELECT p.id_peminjaman, p.id_pengguna,
                p.tanggal_pinjam, p.batas_kembali, p.status, p.catatan,
                p.dibuat_pada, pg.nama_lengkap, pg.nama_pengguna
                FROM peminjaman p
                JOIN pengguna pg ON p.id_pengguna = pg.id_pengguna
                ORDER BY p.dibuat_pada DESC";

            using (var k = await KoneksiDatabase.BukaKoneksiAsync())
            using (var c = new MySqlCommand(sql, k))
            using (MySqlDataReader r = (MySqlDataReader)await c.ExecuteReaderAsync())
            {
                while (await r.ReadAsync())
                    list.Add(PetakanHeader(r));
            }

            return list;
        }

        public async Task<List<Peminjaman>> AmbilAktifAsync()
        {
            var list = new List<Peminjaman>();

            const string sql = @"SELECT p.id_peminjaman, p.id_pengguna,
                p.tanggal_pinjam, p.batas_kembali, p.status, p.catatan,
                p.dibuat_pada, pg.nama_lengkap, pg.nama_pengguna
                FROM peminjaman p
                JOIN pengguna pg ON p.id_pengguna = pg.id_pengguna
                WHERE p.status NOT IN ('selesai')
                ORDER BY p.batas_kembali ASC";

            using (var k = await KoneksiDatabase.BukaKoneksiAsync())
            using (var c = new MySqlCommand(sql, k))
            using (MySqlDataReader r = (MySqlDataReader)await c.ExecuteReaderAsync())
            {
                while (await r.ReadAsync())
                    list.Add(PetakanHeader(r));
            }

            return list;
        }

        public async Task<List<Peminjaman>> AmbilByPenggunaAsync(int idPengguna)
        {
            var list = new List<Peminjaman>();

            const string sql = @"SELECT p.id_peminjaman, p.id_pengguna,
                p.tanggal_pinjam, p.batas_kembali, p.status, p.catatan,
                p.dibuat_pada, pg.nama_lengkap, pg.nama_pengguna
                FROM peminjaman p
                JOIN pengguna pg ON p.id_pengguna = pg.id_pengguna
                WHERE p.id_pengguna = @id
                ORDER BY p.dibuat_pada DESC";

            using (var k = await KoneksiDatabase.BukaKoneksiAsync())
            using (var c = new MySqlCommand(sql, k))
            {
                c.Parameters.AddWithValue("@id", idPengguna);

                using (MySqlDataReader r = (MySqlDataReader)await c.ExecuteReaderAsync())
                {
                    while (await r.ReadAsync())
                        list.Add(PetakanHeader(r));
                }
            }

            return list;
        }

        public async Task<List<DetailPeminjaman>> AmbilDetailAsync(int idPeminjaman)
        {
            var list = new List<DetailPeminjaman>();

            const string sql = @"SELECT dp.id_detail_pinjam, dp.id_peminjaman,
                dp.id_buku, dp.status_buku, dp.dibuat_pada,
                b.judul, b.penulis, b.kategori, b.sampul
                FROM detail_peminjaman dp
                JOIN buku b ON dp.id_buku = b.id_buku
                WHERE dp.id_peminjaman = @id";

            using (var k = await KoneksiDatabase.BukaKoneksiAsync())
            using (var c = new MySqlCommand(sql, k))
            {
                c.Parameters.AddWithValue("@id", idPeminjaman);

                using (MySqlDataReader r = (MySqlDataReader)await c.ExecuteReaderAsync())
                {
                    while (await r.ReadAsync())
                    {
                        list.Add(new DetailPeminjaman
                        {
                            IdDetailPinjam = Convert.ToInt32(r["id_detail_pinjam"]),
                            IdPeminjaman = Convert.ToInt32(r["id_peminjaman"]),
                            IdBuku = Convert.ToInt32(r["id_buku"]),
                            StatusBuku = r["status_buku"].ToString(),
                            DibuatPada = Convert.ToDateTime(r["dibuat_pada"]),
                            JudulBuku = r["judul"].ToString(),
                            PenulisBuku = r["penulis"].ToString(),
                            KategoriBuku = r["kategori"] == DBNull.Value ? null : r["kategori"].ToString(),
                            SampulBuku = r["sampul"] == DBNull.Value ? null : r["sampul"].ToString(),
                        });
                    }
                }
            }

            return list;
        }

        public async Task<int> HitungBukuAktifAsync(int idPengguna)
        {
            return await KoneksiDatabase.EksekusiSkalarAsync<int>(
                @"SELECT COUNT(*) FROM detail_peminjaman dp
                  JOIN peminjaman p ON dp.id_peminjaman = p.id_peminjaman
                  WHERE p.id_pengguna = @id AND dp.status_buku = 'dipinjam'",
                p => p.AddWithValue("@id", idPengguna));
        }

        public async Task<Tuple<bool, string, int>> BuatPeminjamanAsync(
            int idPengguna, List<int> daftarIdBuku)
        {
            if (daftarIdBuku.Count == 0)
                return Tuple.Create(false, "Pilih minimal 1 buku.", 0);

            if (daftarIdBuku.Count > KonstantaAplikasi.MaksimalBukuPerPinjaman)
                return Tuple.Create(false,
                    "Maksimal " + KonstantaAplikasi.MaksimalBukuPerPinjaman +
                    " buku per peminjaman.", 0);

            int idBaru = 0;

            try
            {
                await KoneksiDatabase.EksekusiDenganTransaksiAsync(async (k, t) =>
                {
                    var tanggal = DateTime.Today;
                    var batas = tanggal.AddDays(KonstantaAplikasi.DurasiPeminjamanHari);

                    var cmdHeader = new MySqlCommand(
                        @"INSERT INTO peminjaman (id_pengguna, tanggal_pinjam,
                          batas_kembali, status)
                          VALUES (@ip, @tp, @bk, 'aktif');
                          SELECT LAST_INSERT_ID();", k, t);

                    cmdHeader.Parameters.AddWithValue("@ip", idPengguna);
                    cmdHeader.Parameters.AddWithValue("@tp", tanggal);
                    cmdHeader.Parameters.AddWithValue("@bk", batas);

                    idBaru = Convert.ToInt32(await cmdHeader.ExecuteScalarAsync());

                    foreach (int idBuku in daftarIdBuku)
                    {
                        var cmdDetail = new MySqlCommand(
                            @"INSERT INTO detail_peminjaman
                              (id_peminjaman, id_buku, status_buku)
                              VALUES (@ip, @ib, 'dipinjam')", k, t);

                        cmdDetail.Parameters.AddWithValue("@ip", idBaru);
                        cmdDetail.Parameters.AddWithValue("@ib", idBuku);

                        await cmdDetail.ExecuteNonQueryAsync();
                    }
                });

                return Tuple.Create(true, "Peminjaman berhasil dicatat.", idBaru);
            }
            catch (Exception ex)
            {
                return Tuple.Create(false,
                    "Gagal membuat peminjaman: " + ex.Message, 0);
            }
        }

        public async Task TandaiTerlambatAsync()
        {
            await KoneksiDatabase.EksekusiNonQueryAsync(
                @"UPDATE peminjaman SET status = 'terlambat'
                  WHERE status IN ('aktif', 'sebagian_kembali')
                  AND batas_kembali < CURDATE()");
        }

        public async Task<Tuple<int, int, int>> StatistikAsync()
        {
            const string sql = @"SELECT
                SUM(status IN ('aktif','sebagian_kembali')) aktif,
                SUM(status = 'terlambat') terlambat,
                SUM(status = 'selesai') selesai
                FROM peminjaman";

            using (var k = await KoneksiDatabase.BukaKoneksiAsync())
            using (var c = new MySqlCommand(sql, k))
            using (MySqlDataReader r = (MySqlDataReader)await c.ExecuteReaderAsync())
            {
                if (await r.ReadAsync())
                {
                    return Tuple.Create(
                        r["aktif"] == DBNull.Value ? 0 : Convert.ToInt32(r["aktif"]),
                        r["terlambat"] == DBNull.Value ? 0 : Convert.ToInt32(r["terlambat"]),
                        r["selesai"] == DBNull.Value ? 0 : Convert.ToInt32(r["selesai"])
                    );
                }
            }

            return Tuple.Create(0, 0, 0);
        }

        // HAPUS PEMINJAMAN (hard delete) hanya jika status = 'selesai'
        public async Task<Tuple<bool, string>> HapusAsync(int idPeminjaman)
        {
            try
            {
                using (var k = await KoneksiDatabase.BukaKoneksiAsync())
                using (var t = k.BeginTransaction())
                {
                    // cek status
                    var cek = new MySqlCommand("SELECT status FROM peminjaman WHERE id_peminjaman = @id", k, t);
                    cek.Parameters.AddWithValue("@id", idPeminjaman);
                    var statusObj = await cek.ExecuteScalarAsync();
                    if (statusObj == null)
                    {
                        t.Rollback();
                        return Tuple.Create(false, "Peminjaman tidak ditemukan.");
                    }

                    string status = statusObj == DBNull.Value ? null : statusObj.ToString();
                    if (status != "selesai")
                    {
                        t.Rollback();
                        return Tuple.Create(false, "Hanya peminjaman dengan status 'selesai' yang boleh dihapus.");
                    }

                    // hapus detail
                    var cmdD = new MySqlCommand("DELETE FROM detail_peminjaman WHERE id_peminjaman = @id", k, t);
                    cmdD.Parameters.AddWithValue("@id", idPeminjaman);
                    await cmdD.ExecuteNonQueryAsync();

                    // hapus header
                    var cmdH = new MySqlCommand("DELETE FROM peminjaman WHERE id_peminjaman = @id", k, t);
                    cmdH.Parameters.AddWithValue("@id", idPeminjaman);
                    await cmdH.ExecuteNonQueryAsync();

                    t.Commit();
                }

                return Tuple.Create(true, "Peminjaman berhasil dihapus.");
            }
            catch (Exception ex)
            {
                return Tuple.Create(false, "Gagal menghapus peminjaman: " + ex.Message);
            }
        }
    }
}