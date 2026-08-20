using System;
using System.IO;

namespace SistemPerpustakaan.Pembantu
{
    public static class KonstantaAplikasi
    {
        // Informasi Aplikasi
        public const string NamaAplikasi     = "Sistem Manajemen Perpustakaan";
        public const string VersiAplikasi    = "1.0.0";
        public const string NamaPerpustakaan = "Perpustakaan Digital";

        // Konfigurasi Database
        public const string ServerDatabase = "localhost";
        public const int    PortDatabase   = 3306;
        public const string NamaDatabase   = "db_perpustakaan";
        public const string PenggunaDB     = "root";
        public const string KataSandiDB    = "";

        // Aturan Bisnis
        public const int     MaksimalBukuPerPinjaman = 5;
        public const int     DurasiPeminjamanHari    = 7;
        public const decimal TarifDendaPerHari       = 2000m;
        public const decimal DendaRusakRingan       = 25000m;   // denda buku rusak ringan
        public const decimal DendaRusakBerat        = 75000m;   // denda buku rusak berat
        public const decimal DendaHilangDefault     = 150000m;  // fallback jika harga buku belum diset

        // XAMPP
        public static readonly string PathXampp =
            Environment.GetEnvironmentVariable("XAMPP_DIR") ?? @"C:\xampp";
        public static string PathXamppMysql =>
            Path.Combine(PathXampp, @"mysql\bin\mysqld.exe");
        public const int TungguXamppMs          = 5000;
        public const int MaksPercobaanKoneksi   = 10;

        // Aset
        public static string FolderSampulBuku =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Perpustakaan", "SampulBuku");

        public static string PathPlaceholder =>
            Path.Combine(FolderSampulBuku, "logo.png");

        // Allowed image extensions and max size
        public static readonly string[] EkstensiGambarDiizinkan = { ".jpg", ".jpeg", ".png", ".bmp" };
        public const long MaksUkuranGambarByte = 5 * 1024 * 1024; // 5 MB

        // Laporan
        public static string FolderLaporan =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "LaporanPerpustakaan");

        // Organization info (used in PDF kop). Can be changed later via UI.
        public static string OrganisasiNama    { get; set; } = "Perpustakaan";
        public static string OrganisasiAlamat  { get; set; } = "Jl. Contoh Alamat No.123 — Kota";
        public static string OrganisasiTelepon { get; set; } = "(021) 555-0123";
        public static string OrganisasiEmail   { get; set; } = "info@perpustakaan.local";

        // Corporate font name used for PDF (must be available on system or mapped in FontResolver)
        public static string CorporateFontName { get; set; } = "Segoe UI";

        // Format
        public const string FormatTanggal       = "dd MMMM yyyy";
        public const string FormatTanggalPendek  = "dd/MM/yyyy";
        public const string FormatWaktu          = "dd MMMM yyyy HH:mm";

        public static void InisialisasiFolderAplikasi()
        {
            if (!Directory.Exists(FolderSampulBuku))
                Directory.CreateDirectory(FolderSampulBuku);
            if (!Directory.Exists(FolderLaporan))
                Directory.CreateDirectory(FolderLaporan);
        }
    }
}
