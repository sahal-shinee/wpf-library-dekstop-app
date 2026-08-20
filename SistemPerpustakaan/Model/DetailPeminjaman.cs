using System;
namespace SistemPerpustakaan.Model
{
    public class DetailPeminjaman
    {
        public int      IdDetailPinjam { get; set; }
        public int      IdPeminjaman   { get; set; }
        public int      IdBuku         { get; set; }
        public string   StatusBuku     { get; set; }
        public DateTime DibuatPada     { get; set; }
        public string   JudulBuku      { get; set; }
        public string   PenulisBuku    { get; set; }
        public string   KategoriBuku   { get; set; }
        public string   SampulBuku     { get; set; }

        public bool SudahKembali
        {
            get { return StatusBuku == "dikembalikan"; }
        }
        public string TampilStatusBuku
        {
            get { return SudahKembali ? "Sudah Dikembalikan" : "Belum Dikembalikan"; }
        }
    }
}
