// ================================================================
//  PengembalianRepositori.cs
//  v3 — Denda keterlambatan + denda kondisi buku (updated)
// ================================================================
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using SistemPerpustakaan.Model;
using SistemPerpustakaan.Pembantu;

namespace SistemPerpustakaan.Repositori
{
    public class PengembalianRepositori
    {
        // ──────────────────────────────────────────────────────────
        //  Helper: hitung denda kondisi per buku
        // ──────────────────────────────────────────────────────────
        private static decimal HitungDendaKondisi(string kondisi, decimal hargaBuku)
        {
            switch (kondisi)
            {
                case "rusak_ringan": return KonstantaAplikasi.DendaRusakRingan;
                case "rusak_berat":  return KonstantaAplikasi.DendaRusakBerat;
                case "hilang":
                    return hargaBuku > 0
                        ? hargaBuku
                        : KonstantaAplikasi.DendaHilangDefault;
                default: return 0m;
            }
        }

        // ──────────────────────────────────────────────────────────
        //  Proses pengembalian (sebagian atau semua buku)
        // ──────────────────────────────────────────────────────────
        public async Task<Tuple<bool, string, int>> ProsesAsync(
            int idPeminjaman,
            int idPengguna,
            List<Tuple<int, int, string, string>> daftarBuku,
            string catatan = null)
        {
            if (daftarBuku.Count == 0)
                return Tuple.Create(false, "Pilih minimal 1 buku.", 0);

            int idPengembalian = 0;
            decimal totalDenda = 0m;

            try
            {
                await KoneksiDatabase.EksekusiDenganTransaksiAsync(async (k, t) =>
                {
                    var tanggal = DateTime.Today;

                    int totalBuku = await HitungTotalBukuAsync(idPeminjaman, k, t);
                    string jenis = daftarBuku.Count < totalBuku ? "sebagian" : "semua";

                    // ── Insert header pengembalian ──────────────────
                    var cmdH = new MySqlCommand(
                        @"INSERT INTO pengembalian
                          (id_peminjaman, id_pengguna, tanggal_kembali, jenis, catatan)
                          VALUES (@ip, @ipen, @tk, @j, @cat);
                          SELECT LAST_INSERT_ID();", k, t);

                    cmdH.Parameters.AddWithValue("@ip",   idPeminjaman);
                    cmdH.Parameters.AddWithValue("@ipen", idPengguna);
                    cmdH.Parameters.AddWithValue("@tk",   tanggal);
                    cmdH.Parameters.AddWithValue("@j",    jenis);
                    cmdH.Parameters.AddWithValue("@cat",  (object)catatan ?? DBNull.Value);

                    idPengembalian = Convert.ToInt32(await cmdH.ExecuteScalarAsync());

                    // ── Insert detail per buku ──────────────────────
                    foreach (var item in daftarBuku)
                    {
                        string kondisiBuku = string.IsNullOrWhiteSpace(item.Item3) ? "baik" : item.Item3;

                        var cmdD = new MySqlCommand(
                            @"INSERT INTO detail_pengembalian
                              (id_pengembalian, id_detail_pinjam, id_buku,
                               tanggal_kembali_buku, kondisi_buku, keterangan_kondisi)
                              VALUES (@ik, @idp, @ib, @tk, @ko, @ket)", k, t);

                        cmdD.Parameters.AddWithValue("@ik",  idPengembalian);
                        cmdD.Parameters.AddWithValue("@idp", item.Item1);
                        cmdD.Parameters.AddWithValue("@ib",  item.Item2);
                        cmdD.Parameters.AddWithValue("@tk",  tanggal);
                        cmdD.Parameters.AddWithValue("@ko",  kondisiBuku);
                        cmdD.Parameters.AddWithValue("@ket", (object)item.Item4 ?? DBNull.Value);

                        await cmdD.ExecuteNonQueryAsync();
                    }

                    // ── Hitung & simpan denda (terlambat + kondisi) ─
                    totalDenda = await HitungDendaAsync(idPeminjaman, idPengembalian, idPengguna, tanggal, k, t);

                    // ── Perbarui status peminjaman ──────────────────
                    int sisa = await HitungSisaDipinjamAsync(idPeminjaman, k, t);
                    string statusBaru = sisa == 0 ? "selesai" : "sebagian_kembali";

                    var cmdS = new MySqlCommand(
                        "UPDATE peminjaman SET status = @s WHERE id_peminjaman = @id", k, t);
                    cmdS.Parameters.AddWithValue("@s",  statusBaru);
                    cmdS.Parameters.AddWithValue("@id", idPeminjaman);
                    await cmdS.ExecuteNonQueryAsync();
                });

                await PerbaruiStatusPenggunaAsync(idPengguna);

                string pesan = "Pengembalian berhasil dicatat.";
                if (totalDenda > 0)
                {
                    pesan += " Total denda: Rp " + totalDenda.ToString("N0") + " (belum_lunas).";
                }

                return Tuple.Create(true, pesan, idPengembalian);
            }
            catch (Exception ex)
            {
                return Tuple.Create(false,
                    "Gagal memproses pengembalian: " + ex.Message, 0);
            }
        }

        // ──────────────────────────────────────────────────────────
        //  Hitung & simpan denda (keterlambatan + kondisi buku)
        //  sekarang mengembalikan total denda yang disimpan
        // ──────────────────────────────────────────────────────────
        private async Task<decimal> HitungDendaAsync(
            int idPeminjaman, int idPengembalian, int idPengguna,
            DateTime tanggalKembali,
            MySqlConnection k, MySqlTransaction t)
        {
            decimal totalDenda = 0m;

            // Ambil batas kembali
            var cmdBatas = new MySqlCommand(
                "SELECT batas_kembali FROM peminjaman WHERE id_peminjaman = @id", k, t);
            cmdBatas.Parameters.AddWithValue("@id", idPeminjaman);
            var batas = (DateTime)await cmdBatas.ExecuteScalarAsync();

            int hariTelat = Math.Max(0, (tanggalKembali.Date - batas.Date).Days);
            decimal tarif = KonstantaAplikasi.TarifDendaPerHari;

            // Ambil buku yang dikembalikan: kondisi + harga
            var cmdBuku = new MySqlCommand(
                @"SELECT dk.id_detail_kembali, dk.id_buku, dk.kondisi_buku, b.harga
                  FROM detail_pengembalian dk
                  JOIN buku b ON dk.id_buku = b.id_buku
                  WHERE dk.id_pengembalian = @id", k, t);
            cmdBuku.Parameters.AddWithValue("@id", idPengembalian);

            // idDetailKembali, idBuku, kondisi, harga
            var bukuList = new List<Tuple<int, int, string, decimal>>();
            using (var r = (MySqlDataReader)await cmdBuku.ExecuteReaderAsync())
            {
                while (await r.ReadAsync())
                    bukuList.Add(Tuple.Create(
                        r.GetInt32(0), r.GetInt32(1),
                        r.GetString(2), Convert.ToDecimal(r[3])));
            }

            if (bukuList.Count == 0) return 0m;

            // Hitung denda per buku
            // (idDetailKembali, idBuku, kondisi, dendaTerlambat, dendaKondisi)
            var detailList = new List<Tuple<int, int, string, decimal, decimal>>();

            foreach (var buku in bukuList)
            {
                decimal dendaTerlambat = hariTelat * tarif;
                decimal dendaKondisi   = HitungDendaKondisi(buku.Item3, buku.Item4);
                decimal subtotal       = dendaTerlambat + dendaKondisi;

                if (subtotal > 0)
                {
                    totalDenda += subtotal;
                    detailList.Add(Tuple.Create(
                        buku.Item1, buku.Item2, buku.Item3, dendaTerlambat, dendaKondisi));
                }
            }

            if (totalDenda <= 0 || detailList.Count == 0) return 0m;

            // Insert header denda
            var cmdDenda = new MySqlCommand(
                @"INSERT INTO denda
                  (id_pengembalian, id_pengguna, total_denda, status_pembayaran)
                  VALUES (@ik, @ip, @td, 'belum_lunas');
                  SELECT LAST_INSERT_ID();", k, t);
            cmdDenda.Parameters.AddWithValue("@ik", idPengembalian);
            cmdDenda.Parameters.AddWithValue("@ip", idPengguna);
            cmdDenda.Parameters.AddWithValue("@td", totalDenda);
            int idDenda = Convert.ToInt32(await cmdDenda.ExecuteScalarAsync());

            // Insert detail denda per buku
            foreach (var d in detailList)
            {
                var cmdDetail = new MySqlCommand(
                    @"INSERT INTO detail_denda
                      (id_denda, id_detail_kembali, id_buku, kondisi_buku, batas_kembali,
                      (id_denda, id_detail_kembali, id_buku, kondisi_buku, batas_kembali,
                       tanggal_kembali_aktual, jumlah_hari_terlambat,
                       tarif_per_hari, subtotal_denda, denda_kondisi)
                      VALUES (@id, @idk, @ib, @ko, @bk, @tk, @ht, @tp, @sub, @dk)", k, t);

                cmdDetail.Parameters.AddWithValue("@id",  idDenda);
                cmdDetail.Parameters.AddWithValue("@idk", d.Item1);          // idDetailKembali
                cmdDetail.Parameters.AddWithValue("@ib",  d.Item2);          // idBuku
                cmdDetail.Parameters.AddWithValue("@ko",  d.Item3);          // kondisi
                cmdDetail.Parameters.AddWithValue("@bk",  batas);
                cmdDetail.Parameters.AddWithValue("@tk",  tanggalKembali);
                cmdDetail.Parameters.AddWithValue("@ht",  hariTelat);
                cmdDetail.Parameters.AddWithValue("@tp",  tarif);
                cmdDetail.Parameters.AddWithValue("@sub", d.Item4);          // denda terlambat
                cmdDetail.Parameters.AddWithValue("@dk",  d.Item5);          // denda kondisi

                await cmdDetail.ExecuteNonQueryAsync();
            }

            return totalDenda;
        }

        // ──────────────────────────────────────────────────────────
        //  Ambil semua riwayat pengembalian (LEFT JOIN denda untuk AdaDenda)
        // ──────────────────────────────────────────────────────────
        public async Task<List<Pengembalian>> AmbilSemuaAsync()
        {
            var list = new List<Pengembalian>();

            const string sql = @"
                SELECT k.id_pengembalian, k.id_peminjaman, k.id_pengguna,
                       k.tanggal_kembali, k.jenis, k.catatan, k.dibuat_pada,
                       pg.nama_lengkap, p.batas_kembali,
                       CASE WHEN d.id_denda IS NOT NULL THEN 1 ELSE 0 END AS ada_denda
                FROM pengembalian k
                JOIN pengguna pg   ON k.id_pengguna   = pg.id_pengguna
                JOIN peminjaman p  ON k.id_peminjaman = p.id_peminjaman
                LEFT JOIN denda d  ON k.id_pengembalian = d.id_pengembalian
                ORDER BY k.dibuat_pada DESC";

            using (var k = await KoneksiDatabase.BukaKoneksiAsync())
            using (var c = new MySqlCommand(sql, k))
            using (var r = (MySqlDataReader)await c.ExecuteReaderAsync())
            {
                while (await r.ReadAsync())
                {
                    list.Add(new Pengembalian
                    {
                        IdPengembalian = r.GetInt32("id_pengembalian"),
                        IdPeminjaman   = r.GetInt32("id_peminjaman"),
                        IdPengguna     = r.GetInt32("id_pengguna"),
                        TanggalKembali = r.GetDateTime("tanggal_kembali"),
                        Jenis          = r.GetString("jenis"),
                        Catatan        = r.IsDBNull(r.GetOrdinal("catatan"))
                                            ? null : r.GetString("catatan"),
                        DibuatPada     = r.GetDateTime("dibuat_pada"),
                        NamaPeminjam   = r.GetString("nama_lengkap"),
                        BatasKembali   = r.GetDateTime("batas_kembali"),
                        AdaDenda       = r.GetInt32("ada_denda") == 1,
                    });
                }
            }

            return list;
        }

        // ──────────────────────────────────────────────────────────
        //  Ambil detail per buku + data denda aktual (LEFT JOIN detail_denda)
        // ──────────────────────────────────────────────────────────
        public async Task<List<DetailPengembalian>> AmbilDetailAsync(int idPengembalian)
        {
            var list = new List<DetailPengembalian>();

            const string sql = @"
                SELECT dk.id_detail_kembali, dk.id_pengembalian,
                       dk.id_detail_pinjam, dk.id_buku,
                       dk.tanggal_kembali_buku, dk.kondisi_buku,
                       dk.keterangan_kondisi,
                       b.judul, b.penulis, b.harga,
                       p.batas_kembali,
                       COALESCE(dd.subtotal_denda, 0)  AS denda_terlambat,
                       COALESCE(dd.denda_kondisi,  0)  AS denda_kondisi
                FROM detail_pengembalian dk
                JOIN buku b             ON dk.id_buku          = b.id_buku
                JOIN detail_peminjaman dp ON dk.id_detail_pinjam = dp.id_detail_pinjam
                JOIN peminjaman p       ON dp.id_peminjaman     = p.id_peminjaman
                LEFT JOIN detail_denda dd ON dk.id_detail_kembali = dd.id_detail_kembali
                WHERE dk.id_pengembalian = @id";

            using (var k = await KoneksiDatabase.BukaKoneksiAsync())
            using (var c = new MySqlCommand(sql, k))
            {
                c.Parameters.AddWithValue("@id", idPengembalian);

                using (var r = (MySqlDataReader)await c.ExecuteReaderAsync())
                {
                    while (await r.ReadAsync())
                    {
                        list.Add(new DetailPengembalian
                        {
                            IdDetailKembali    = r.GetInt32("id_detail_kembali"),
                            IdPengembalian     = r.GetInt32("id_pengembalian"),
                            IdDetailPinjam     = r.GetInt32("id_detail_pinjam"),
                            IdBuku             = r.GetInt32("id_buku"),
                            TanggalKembaliBuku = r.GetDateTime("tanggal_kembali_buku"),
                            KondisiBuku        = r.IsDBNull(r.GetOrdinal("kondisi_buku")) ? null : r.GetString("kondisi_buku"),
                            KeteranganKondisi  = r.IsDBNull(r.GetOrdinal("keterangan_kondisi"))
                                                    ? null : r.GetString("keterangan_kondisi"),
                            JudulBuku          = r.GetString("judul"),
                            PenulisBuku        = r.GetString("penulis"),
                            HargaBuku          = Convert.ToDecimal(r["harga"]),
                            BatasKembali       = r.GetDateTime("batas_kembali"),
                            DendaTerlambat     = Convert.ToDecimal(r["denda_terlambat"]),
                            DendaKondisiNominal = Convert.ToDecimal(r["denda_kondisi"]),
                        });
                    }
                }
            }

            return list;
        }

        // ──────────────────────────────────────────────────────────
        //  Helper methods
        // ──────────────────────────────────────────────────────────
        private async Task<int> HitungTotalBukuAsync(
            int id, MySqlConnection k, MySqlTransaction t)
        {
            var c = new MySqlCommand(
                "SELECT COUNT(*) FROM detail_peminjaman WHERE id_peminjaman = @id", k, t);
            c.Parameters.AddWithValue("@id", id);
            return Convert.ToInt32(await c.ExecuteScalarAsync());
        }

        private async Task<int> HitungSisaDipinjamAsync(
            int id, MySqlConnection k, MySqlTransaction t)
        {
            var c = new MySqlCommand(
                @"SELECT COUNT(*) FROM detail_peminjaman
                  WHERE id_peminjaman = @id AND status_buku = 'dipinjam'", k, t);
            c.Parameters.AddWithValue("@id", id);
            return Convert.ToInt32(await c.ExecuteScalarAsync());
        }

        private async Task PerbaruiStatusPenggunaAsync(int idPengguna)
        {
            int denda = await KoneksiDatabase.EksekusiSkalarAsync<int>(
                @"SELECT COUNT(*) FROM denda
                  WHERE id_pengguna = @id AND status_pembayaran = 'belum_lunas'",
                p => p.AddWithValue("@id", idPengguna));

            string status = denda > 0 ? "diblokir" : "aktif";

            await KoneksiDatabase.EksekusiNonQueryAsync(
                "UPDATE pengguna SET status = @s WHERE id_pengguna = @id",
                p =>
                {
                    p.AddWithValue("@s",  status);
                    p.AddWithValue("@id", idPengguna);
                });
        }

        // HAPUS PENGEMBALIAN (hard delete) hanya jika tidak ada denda terkait
        public async Task<Tuple<bool, string>> HapusAsync(int idPengembalian)
        {
            try
            {
                using (var k = await KoneksiDatabase.BukaKoneksiAsync())
                using (var t = k.BeginTransaction())
                {
                    // cek apakah ada denda untuk pengembalian ini
                    var cek = new MySqlCommand(@"SELECT COUNT(*) FROM denda WHERE id_pengembalian = @id", k, t);
                    cek.Parameters.AddWithValue("@id", idPengembalian);
                    var countObj = await cek.ExecuteScalarAsync();
                    int count = Convert.ToInt32(countObj);
                    if (count > 0)
                    {
                        t.Rollback();
                        return Tuple.Create(false, "Tidak dapat menghapus pengembalian karena ada denda terkait.");
                    }

                    // hapus detail_pengembalian
                    var cmdD = new MySqlCommand("DELETE FROM detail_pengembalian WHERE id_pengembalian = @id", k, t);
                    cmdD.Parameters.AddWithValue("@id", idPengembalian);
                    await cmdD.ExecuteNonQueryAsync();

                    // hapus header pengembalian
                    var cmdH = new MySqlCommand("DELETE FROM pengembalian WHERE id_pengembalian = @id", k, t);
                    cmdH.Parameters.AddWithValue("@id", idPengembalian);
                    await cmdH.ExecuteNonQueryAsync();

                    t.Commit();
                }

                return Tuple.Create(true, "Pengembalian berhasil dihapus.");
            }
            catch (Exception ex)
            {
                return Tuple.Create(false, "Gagal menghapus pengembalian: " + ex.Message);
            }
        }
    }
}
