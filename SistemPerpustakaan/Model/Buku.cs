using System;
using System.Windows.Media.Imaging;
using SistemPerpustakaan.Pembantu;

namespace SistemPerpustakaan.Model
{
    public class Buku
    {
        public int      IdBuku        { get; set; }
        public string   Judul         { get; set; }
        public string   Penulis       { get; set; }
        public string   Penerbit      { get; set; }
        public string   Kategori      { get; set; }
        public string   Isbn          { get; set; }
        public int?     TahunTerbit   { get; set; }
        public int      JumlahStok    { get; set; }
        public int      StokTersedia  { get; set; }
        public string   Deskripsi     { get; set; }
        public decimal  Harga         { get; set; }
        public string   Sampul        { get; set; }
        public DateTime DibuatPada    { get; set; }

        public bool   Tersedia
        {
            get { return StokTersedia > 0; }
        }
        public int    JumlahDipinjam
        {
            get { return JumlahStok - StokTersedia; }
        }
        public string TampilKetersediaan
        {
            get { return Tersedia ? "Tersedia (" + StokTersedia + " eks.)" : "Tidak Tersedia"; }
        }
        public BitmapImage GambarSampul
        {
            get { return PembantuGambar.MuatSampul(Sampul); }
        }
    }
}
