// ================================================================
// Model/Pengembalian.cs
// ================================================================
using System;
using System.Collections.Generic;
namespace SistemPerpustakaan.Model
{
    public class Pengembalian
    {
        public int      IdPengembalian { get; set; }
        public int      IdPeminjaman   { get; set; }
        public int      IdPengguna     { get; set; }
        public DateTime TanggalKembali { get; set; }
        public string   Jenis          { get; set; }
        public string   Catatan        { get; set; }
        public DateTime DibuatPada     { get; set; }
        public string   NamaPeminjam   { get; set; }
        public DateTime BatasKembali   { get; set; }
        public List<DetailPengembalian> DetailBuku { get; set; }

        public Pengembalian() { DetailBuku = new List<DetailPengembalian>(); }

        // Settable — diisi dari DB (LEFT JOIN denda)
        public bool AdaDenda { get; set; }

        public string TampilAdaDenda => AdaDenda ? "Ya" : "Tidak";

        public string TampilJenis
        {
            get { return Jenis == "sebagian" ? "Sebagian Buku" : "Semua Buku"; }
        }
    }
}
