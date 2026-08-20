using SistemPerpustakaan.Model;
namespace SistemPerpustakaan.Pembantu
{
    public static class SesiPengguna
    {
        public static Pengguna PenggunaSaatIni { get; private set; }
        public static bool     SedangLogin     => PenggunaSaatIni != null;
        public static bool     AdalahAdmin     => PenggunaSaatIni?.Peran == "admin";
        public static void     MulaiSesi(Pengguna p) => PenggunaSaatIni = p;
        public static void     AkhiriSesi()          => PenggunaSaatIni = null;
    }
}
