using System;
using System.Collections.Generic;
using SistemPerpustakaan.Pembantu;
namespace SistemPerpustakaan.Model
{
    public class Peminjaman
    {
        public int      IdPeminjaman  { get; set; }
        public int      IdPengguna    { get; set; }
        public DateTime TanggalPinjam { get; set; }
        public DateTime BatasKembali  { get; set; }
        public string   Status        { get; set; }
        public string   Catatan       { get; set; }
        public DateTime DibuatPada    { get; set; }

        public string NamaPeminjam { get; set; }
        public string NamaPengguna { get; set; }
        public List<DetailPeminjaman> DetailBuku { get; set; }

        public Peminjaman() { DetailBuku = new List<DetailPeminjaman>(); }

        public int HariTerlambat
        {
            get
            {
                if (Status != "selesai" && DateTime.Today > BatasKembali)
                    return (DateTime.Today - BatasKembali).Days;
                return 0;
            }
        }
        public bool Terlambat { get { return HariTerlambat > 0; } }

        public string TampilStatus
        {
            get
            {
                switch (Status)
                {
                    case "aktif":            return "Aktif";
                    case "sebagian_kembali": return "Sebagian Dikembalikan";
                    case "selesai":          return "Selesai";
                    case "terlambat":        return "Terlambat";
                    default:                 return Status;
                }
            }
        }

        public string TampilBatasKembali
        {
            get { return BatasKembali.ToString(KonstantaAplikasi.FormatTanggal); }
        }
    }
}
