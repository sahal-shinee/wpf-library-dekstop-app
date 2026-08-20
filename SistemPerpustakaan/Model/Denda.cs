using System;
using System.Collections.Generic;
namespace SistemPerpustakaan.Model
{
    public class Denda
    {
        public int       IdDenda          { get; set; }
        public int       IdPengembalian   { get; set; }
        public int       IdPengguna       { get; set; }
        public decimal   TotalDenda       { get; set; }
        public string    StatusPembayaran { get; set; }
        public DateTime? TanggalBayar     { get; set; }
        public DateTime  DibuatPada       { get; set; }
        public string    NamaPeminjam     { get; set; }
        public string    NamaPengguna     { get; set; }
        public DateTime  TanggalKembali   { get; set; }
        public List<DetailDenda> DetailBuku { get; set; }

        public Denda() { DetailBuku = new List<DetailDenda>(); }

        public bool   SudahLunas
        {
            get { return StatusPembayaran == "lunas"; }
        }
        public string TampilStatus
        {
            get { return SudahLunas ? "Lunas" : "Belum Lunas"; }
        }
        public string TampilTotal
        {
            get { return "Rp " + TotalDenda.ToString("N0"); }
        }
    }
}
