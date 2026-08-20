using System;
using System.IO;
using System.Windows.Media.Imaging;

namespace SistemPerpustakaan.Pembantu
{
    /// <summary>
    /// Mengelola gambar sampul buku.
    /// Kompatibel C# 7.3 / .NET Framework 4.8
    /// </summary>
    public static class PembantuGambar
    {
        /// <summary>
        /// Menyalin file gambar ke folder SampulBuku.
        /// Mengembalikan nama file yang tersimpan.
        /// </summary>
        public static string SimpanSampulBuku(string pathAsli, int idBuku)
        {
            if (!File.Exists(pathAsli))
                throw new FileNotFoundException("File gambar tidak ditemukan.");

            string eks     = Path.GetExtension(pathAsli).ToLower();
            string nama    = "buku_" + idBuku + eks;
            string tujuan  = Path.Combine(KonstantaAplikasi.FolderSampulBuku, nama);

            // Hapus sampul lama jika ada
            HapusSampulBuku(idBuku);

            File.Copy(pathAsli, tujuan, overwrite: true);
            return nama;
        }

        /// <summary>Menghapus file sampul untuk buku tertentu.</summary>
        public static void HapusSampulBuku(int idBuku)
        {
            foreach (var eks in KonstantaAplikasi.EkstensiGambarDiizinkan)
            {
                string path = Path.Combine(
                    KonstantaAplikasi.FolderSampulBuku, "buku_" + idBuku + eks);
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        /// <summary>
        /// Memuat gambar sampul dari nama file.
        /// Jika tidak ditemukan, kembalikan gambar placeholder.
        /// </summary>
        public static BitmapImage MuatSampul(string namaFile)
        {
            if (!string.IsNullOrEmpty(namaFile))
            {
                string path = Path.Combine(KonstantaAplikasi.FolderSampulBuku, namaFile);
                if (File.Exists(path))
                    return MuatDariPath(path);
            }
            return MuatPlaceholder();
        }

        private static BitmapImage MuatDariPath(string path)
        {
            var img = new BitmapImage();
            img.BeginInit();
            img.CacheOption = BitmapCacheOption.OnLoad;
            img.UriSource   = new Uri(path, UriKind.Absolute);
            img.EndInit();
            img.Freeze();
            return img;
        }

        private static BitmapImage MuatPlaceholder()
        {
            // Coba muat dari file lokal dulu
            if (File.Exists(KonstantaAplikasi.PathPlaceholder))
                return MuatDariPath(KonstantaAplikasi.PathPlaceholder);

            // Fallback ke resource bawaan aplikasi
            try
            {
                var img = new BitmapImage();
                img.BeginInit();
                img.UriSource = new Uri(
                    "pack://application:,,,/Aset/SampulBuku/placeholder.png",
                    UriKind.Absolute);
                img.EndInit();
                img.Freeze();
                return img;
            }
            catch
            {
                return new BitmapImage();
            }
        }

        /// <summary>
        /// Memvalidasi file gambar sebelum disimpan.
        /// Mengembalikan Tuple(valid, pesan_kesalahan).
        /// </summary>
        public static Tuple<bool, string> ValidasiFile(string path)
        {
            if (!File.Exists(path))
                return Tuple.Create(false, "File tidak ditemukan.");

            string eks = Path.GetExtension(path).ToLower();
            bool eksOk = false;
            foreach (var e in KonstantaAplikasi.EkstensiGambarDiizinkan)
            {
                if (e == eks) { eksOk = true; break; }
            }

            if (!eksOk)
                return Tuple.Create(false,
                    "Format tidak didukung. Gunakan JPG, PNG, atau BMP.");

            var info = new FileInfo(path);
            if (info.Length > KonstantaAplikasi.MaksUkuranGambarByte)
                return Tuple.Create(false, "Ukuran file melebihi 5 MB.");

            return Tuple.Create(true, string.Empty);
        }
    }
}
