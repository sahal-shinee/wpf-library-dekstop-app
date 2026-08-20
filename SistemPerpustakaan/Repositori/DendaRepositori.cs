using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using SistemPerpustakaan.Model;
using SistemPerpustakaan.Pembantu;

namespace SistemPerpustakaan.Repositori
{
    public class DendaRepositori
    {
        private static Denda PetakanDenda(MySqlDataReader r)
        {
            return new Denda
            {
                IdDenda = Convert.ToInt32(r["id_denda"]),
                IdPengembalian = Convert.ToInt32(r["id_pengembalian"]),
                IdPengguna = Convert.ToInt32(r["id_pengguna"]),
                TotalDenda = Convert.ToDecimal(r["total_denda"]),
                StatusPembayaran = r["status_pembayaran"].ToString(),
                TanggalBayar = r["tanggal_bayar"] == DBNull.Value
                                   ? (DateTime?)null : Convert.ToDateTime(r["tanggal_bayar"]),
                DibuatPada = Convert.ToDateTime(r["dibuat_pada"]),
                NamaPeminjam = r["nama_lengkap"].ToString(),
                NamaPengguna = r["nama_pengguna"].ToString(),
                TanggalKembali = Convert.ToDateTime(r["tanggal_kembali"]),
            };
        }

        private const string SqlDasar = @"SELECT d.id_denda, d.id_pengembalian,
            d.id_pengguna, d.total_denda, d.status_pembayaran,
            d.tanggal_bayar, d.dibuat_pada,
            pg.nama_lengkap, pg.nama_pengguna, k.tanggal_kembali
            FROM denda d
            JOIN pengguna pg ON d.id_pengguna = pg.id_pengguna
            JOIN pengembalian k ON d.id_pengembalian = k.id_pengembalian";

        public async Task<List<Denda>> AmbilSemuaAsync()
        {
            var list = new List<Denda>();

            using (var k = await KoneksiDatabase.BukaKoneksiAsync())
            using (var c = new MySqlCommand(SqlDasar + " ORDER BY d.dibuat_pada DESC", k))
            using (MySqlDataReader r = (MySqlDataReader)await c.ExecuteReaderAsync())
            {
                while (await r.ReadAsync())
                    list.Add(PetakanDenda(r));
            }

            return list;
        }

        public async Task<List<Denda>> AmbilByPenggunaAsync(int idPengguna)
        {
            var list = new List<Denda>();

            using (var k = await KoneksiDatabase.BukaKoneksiAsync())
            using (var c = new MySqlCommand(
                SqlDasar + " WHERE d.id_pengguna = @id ORDER BY d.dibuat_pada DESC", k))
            {
                c.Parameters.AddWithValue("@id", idPengguna);

                using (MySqlDataReader r = (MySqlDataReader)await c.ExecuteReaderAsync())
                {
                    while (await r.ReadAsync())
                        list.Add(PetakanDenda(r));
                }
            }

            return list;
        }

        public async Task<List<Denda>> AmbilBelumLunasAsync()
        {
            var list = new List<Denda>();

            using (var k = await KoneksiDatabase.BukaKoneksiAsync())
            using (var c = new MySqlCommand(
                SqlDasar + " WHERE d.status_pembayaran = 'belum_lunas' ORDER BY d.dibuat_pada ASC", k))
            using (MySqlDataReader r = (MySqlDataReader)await c.ExecuteReaderAsync())
            {
                while (await r.ReadAsync())
                    list.Add(PetakanDenda(r));
            }

            return list;
        }

        public async Task<List<DetailDenda>> AmbilDetailAsync(int idDenda)
        {
            var list = new List<DetailDenda>();

            const string sql = @"SELECT dd.id_detail_denda, dd.id_denda,
                dd.id_detail_kembali, dd.id_buku, dd.kondisi_buku,
                dd.batas_kembali, dd.tanggal_kembali_aktual,
                dd.jumlah_hari_terlambat, dd.tarif_per_hari,
                dd.subtotal_denda, dd.denda_kondisi,
                b.judul, b.penulis
                FROM detail_denda dd
                JOIN buku b ON dd.id_buku = b.id_buku
                WHERE dd.id_denda = @id";

            using (var k = await KoneksiDatabase.BukaKoneksiAsync())
            using (var c = new MySqlCommand(sql, k))
            {
                c.Parameters.AddWithValue("@id", idDenda);

                using (MySqlDataReader r = (MySqlDataReader)await c.ExecuteReaderAsync())
                {
                    while (await r.ReadAsync())
                    {
                        list.Add(new DetailDenda
                        {
                            IdDetailDenda        = Convert.ToInt32(r["id_detail_denda"]),
                            IdDenda              = Convert.ToInt32(r["id_denda"]),
                            IdDetailKembali      = Convert.ToInt32(r["id_detail_kembali"]),
                            IdBuku               = Convert.ToInt32(r["id_buku"]),
                            KondisiBuku          = r["kondisi_buku"].ToString(),
                            BatasKembali         = Convert.ToDateTime(r["batas_kembali"]),
                            TanggalKembaliAktual = Convert.ToDateTime(r["tanggal_kembali_aktual"]),
                            JumlahHariTerlambat  = Convert.ToInt32(r["jumlah_hari_terlambat"]),
                            TarifPerHari         = Convert.ToDecimal(r["tarif_per_hari"]),
                            SubtotalDenda        = Convert.ToDecimal(r["subtotal_denda"]),
                            DendaKondisi         = Convert.ToDecimal(r["denda_kondisi"]),
                            JudulBuku            = r["judul"].ToString(),
                            PenulisBuku          = r["penulis"].ToString(),
                        });
                    }
                }
            }

            return list;
        }

        public async Task<bool> TandaiLunasAsync(int idDenda, int idPengguna)
        {
            int n = await KoneksiDatabase.EksekusiNonQueryAsync(
                @"UPDATE denda SET status_pembayaran = 'lunas', tanggal_bayar = CURDATE()
                  WHERE id_denda = @id",
                p => p.AddWithValue("@id", idDenda));

            if (n > 0)
            {
                int sisaDenda = await KoneksiDatabase.EksekusiSkalarAsync<int>(
                    @"SELECT COUNT(*) FROM denda
                      WHERE id_pengguna = @id AND status_pembayaran = 'belum_lunas'",
                    p => p.AddWithValue("@id", idPengguna));

                if (sisaDenda == 0)
                {
                    await KoneksiDatabase.EksekusiNonQueryAsync(
                        @"UPDATE pengguna SET status = 'aktif'
                          WHERE id_pengguna = @id AND peran = 'pengguna'",
                        p => p.AddWithValue("@id", idPengguna));
                }
            }

            return n > 0;
        }

        public async Task<Tuple<int, decimal, decimal>> StatistikAsync()
        {
            const string sql = @"SELECT COUNT(*) total,
                SUM(CASE WHEN status_pembayaran='belum_lunas' THEN total_denda ELSE 0 END) belum,
                SUM(CASE WHEN status_pembayaran='lunas' THEN total_denda ELSE 0 END) lunas
                FROM denda";

            using (var k = await KoneksiDatabase.BukaKoneksiAsync())
            using (var c = new MySqlCommand(sql, k))
            using (MySqlDataReader r = (MySqlDataReader)await c.ExecuteReaderAsync())
            {
                if (await r.ReadAsync())
                {
                    return Tuple.Create(
                        Convert.ToInt32(r["total"]),
                        r["belum"] == DBNull.Value ? 0m : Convert.ToDecimal(r["belum"]),
                        r["lunas"] == DBNull.Value ? 0m : Convert.ToDecimal(r["lunas"])
                    );
                }
            }

            return Tuple.Create(0, 0m, 0m);
        }
    }
}