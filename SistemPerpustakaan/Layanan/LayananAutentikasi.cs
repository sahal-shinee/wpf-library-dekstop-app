using System;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using SistemPerpustakaan.Model;
using SistemPerpustakaan.Pembantu;

namespace SistemPerpustakaan.Layanan
{
    public class LayananAutentikasi
    {
        // ── Login ─────────────────────────────────────────
        public async Task<Tuple<bool, string, Pengguna>> MasukAsync(
            string namaPengguna, string kataSandi)
        {
            try
            {
                const string sql = @"
                    SELECT id_pengguna, nama_lengkap, nama_pengguna, kata_sandi,
                           surel, telepon, peran, status, dibuat_pada
                    FROM pengguna
                    WHERE nama_pengguna = @nama
                    LIMIT 1";

                Pengguna p = null;

                using (var k = await KoneksiDatabase.BukaKoneksiAsync())
                using (var c = new MySqlCommand(sql, k))
                {
                    c.Parameters.AddWithValue("@nama", namaPengguna);

                    // 🔥 FIX: pakai MySqlDataReader
                    using (MySqlDataReader r = (MySqlDataReader)await c.ExecuteReaderAsync())
                    {
                        if (!r.HasRows)
                            return Tuple.Create(false,
                                "Nama pengguna tidak ditemukan.", (Pengguna)null);

                        await r.ReadAsync();

                        p = new Pengguna
                        {
                            IdPengguna = Convert.ToInt32(r["id_pengguna"]),
                            NamaLengkap = r["nama_lengkap"].ToString(),
                            NamaPengguna = r["nama_pengguna"].ToString(),
                            KataSandi = r["kata_sandi"].ToString(),
                            Surel = r["surel"].ToString(),
                            Telepon = r["telepon"] == DBNull.Value
                                           ? null : r["telepon"].ToString(),
                            Peran = r["peran"].ToString(),
                            Status = r["status"].ToString(),
                            DibuatPada = Convert.ToDateTime(r["dibuat_pada"]),
                        };
                    }
                }

                if (!PembantuHash.Cocokkan(kataSandi, p.KataSandi))
                    return Tuple.Create(false, "Kata sandi salah.", (Pengguna)null);

                if (p.Status == "diblokir")
                    return Tuple.Create(false,
                        "Akun diblokir. Hubungi petugas.", (Pengguna)null);

                if (p.Status == "nonaktif")
                    return Tuple.Create(false,
                        "Akun tidak aktif.", (Pengguna)null);

                return Tuple.Create(true, "Login berhasil.", p);
            }
            catch (Exception ex)
            {
                return Tuple.Create(false,
                    "Kesalahan: " + ex.Message, (Pengguna)null);
            }
        }

        // ── Registrasi ────────────────────────────────────
        public async Task<Tuple<bool, string>> DaftarAsync(
            Pengguna pengguna, string kataSandiPolos)
        {
            try
            {
                if (await CekAdaAsync("nama_pengguna", pengguna.NamaPengguna))
                    return Tuple.Create(false, "Nama pengguna sudah digunakan.");

                if (await CekAdaAsync("surel", pengguna.Surel))
                    return Tuple.Create(false, "Surel sudah terdaftar.");

                const string sql = @"
                    INSERT INTO pengguna
                    (nama_lengkap, nama_pengguna, kata_sandi,
                     surel, telepon, peran, status)
                    VALUES
                    (@namaLengkap, @namaPengguna, @kataSandi,
                     @surel, @telepon, 'pengguna', 'aktif')";

                int baris = await KoneksiDatabase.EksekusiNonQueryAsync(sql, p =>
                {
                    p.AddWithValue("@namaLengkap", pengguna.NamaLengkap);
                    p.AddWithValue("@namaPengguna", pengguna.NamaPengguna);
                    p.AddWithValue("@kataSandi", PembantuHash.Hash(kataSandiPolos));
                    p.AddWithValue("@surel", pengguna.Surel);

                    p.AddWithValue("@telepon",
                        string.IsNullOrEmpty(pengguna.Telepon)
                        ? (object)DBNull.Value
                        : pengguna.Telepon);
                });

                return baris > 0
                    ? Tuple.Create(true, "Akun berhasil dibuat.")
                    : Tuple.Create(false, "Gagal membuat akun.");
            }
            catch (Exception ex)
            {
                return Tuple.Create(false, "Kesalahan: " + ex.Message);
            }
        }

        // ── Ganti password ────────────────────────────────
        public async Task<Tuple<bool, string>> GantiKataSandiAsync(
            int idPengguna, string lama, string baru)
        {
            // 🔥 FIX: pastikan return tidak null
            string hashLama = await KoneksiDatabase.EksekusiSkalarAsync<string>(
                "SELECT kata_sandi FROM pengguna WHERE id_pengguna = @id",
                p => p.AddWithValue("@id", idPengguna));

            if (string.IsNullOrEmpty(hashLama))
                return Tuple.Create(false, "Pengguna tidak ditemukan.");

            if (!PembantuHash.Cocokkan(lama, hashLama))
                return Tuple.Create(false, "Password lama salah.");

            var cek = PembantuHash.ValidasiKekuatan(baru);
            if (!cek.Item1)
                return Tuple.Create(false, cek.Item2);

            await KoneksiDatabase.EksekusiNonQueryAsync(
                "UPDATE pengguna SET kata_sandi = @hash WHERE id_pengguna = @id",
                p =>
                {
                    p.AddWithValue("@hash", PembantuHash.Hash(baru));
                    p.AddWithValue("@id", idPengguna);
                });

            return Tuple.Create(true, "Password berhasil diubah.");
        }

        // ── Helper ────────────────────────────────────────
        private async Task<bool> CekAdaAsync(string kolom, string nilai)
        {
            int n = await KoneksiDatabase.EksekusiSkalarAsync<int>(
                "SELECT COUNT(*) FROM pengguna WHERE " + kolom + " = @v",
                p => p.AddWithValue("@v", nilai));

            return n > 0;
        }
    }
}

