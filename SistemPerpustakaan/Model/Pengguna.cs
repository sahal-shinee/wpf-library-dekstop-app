using System;
namespace SistemPerpustakaan.Model
{
    public class Pengguna
    {
        public int      IdPengguna       { get; set; }
        public string   NamaLengkap     { get; set; }
        public string   NamaPengguna    { get; set; }
        public string   KataSandi       { get; set; }
        public string   Surel           { get; set; }
        public string   Telepon         { get; set; }
        public string   Peran           { get; set; }
        public string   Status          { get; set; }
        public DateTime DibuatPada      { get; set; }
        public DateTime DiperbaruitPada { get; set; }

        public bool AdalahAdmin    { get { return Peran == "admin"; } }
        public bool AdalahAktif    { get { return Status == "aktif"; } }
        public bool AdalahDiblokir { get { return Status == "diblokir"; } }
        public string TampilPeran  { get { return AdalahAdmin ? "Administrator" : "Anggota"; } }

        public string TampilStatus
        {
            get
            {
                switch (Status)
                {
                    case "aktif":    return "Aktif";
                    case "diblokir": return "Diblokir";
                    case "nonaktif": return "Tidak Aktif";
                    default:         return Status;
                }
            }
        }
    }
}
