using System;
using System.Security.Cryptography;
using System.Text;

namespace SistemPerpustakaan.Pembantu
{
    /// <summary>
    /// Enkripsi dan validasi kata sandi menggunakan SHA-256.
    /// Kompatibel C# 7.3 / .NET Framework 4.8
    /// </summary>
    public static class PembantuHash
    {
        /// <summary>
        /// Mengubah kata sandi teks biasa menjadi hash SHA-256.
        /// </summary>
        public static string Hash(string kataSandi)
        {
            if (string.IsNullOrEmpty(kataSandi))
                throw new ArgumentException("Kata sandi tidak boleh kosong.");

            using (var sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(kataSandi));
                var sb = new StringBuilder();
                foreach (byte b in bytes)
                    sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        /// <summary>
        /// Membandingkan kata sandi teks biasa dengan hash tersimpan.
        /// </summary>
        public static bool Cocokkan(string kataSandiPolos, string hashTersimpan)
        {
            if (string.IsNullOrEmpty(kataSandiPolos) || string.IsNullOrEmpty(hashTersimpan))
                return false;
            string hashBaru = Hash(kataSandiPolos);
            return string.Equals(hashBaru, hashTersimpan, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Validasi kekuatan kata sandi.
        /// Mengembalikan Tuple(valid, pesan_kesalahan).
        /// </summary>
        public static Tuple<bool, string> ValidasiKekuatan(string kataSandi)
        {
            if (string.IsNullOrWhiteSpace(kataSandi))
                return Tuple.Create(false, "Kata sandi tidak boleh kosong.");
            if (kataSandi.Length < 6)
                return Tuple.Create(false, "Kata sandi minimal 6 karakter.");
            return Tuple.Create(true, string.Empty);
        }
    }
}
