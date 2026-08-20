using System;
using SistemPerpustakaan.Pembantu;
namespace SistemPerpustakaan.Model
{
    public class DetailPengembalian
    {
        public int      IdDetailKembali    { get; set; }
        public int      IdPengembalian     { get; set; }
        public int      IdDetailPinjam     { get; set; }
        public int      IdBuku             { get; set; }
        public DateTime TanggalKembaliBuku { get; set; }
        public string   KondisiBuku        { get; set; }
        public string   KeteranganKondisi  { get; set; }
        public string   JudulBuku          { get; set; }
        public string   PenulisBuku        { get; set; }
        public DateTime BatasKembali       { get; set; }
        public decimal  HargaBuku          { get; set; }

        // Data denda aktual dari detail_denda (diisi via LEFT JOIN)
        public decimal DendaTerlambat    { get; set; }
        public decimal DendaKondisiNominal { get; set; }
        public decimal TotalDendaBuku    => DendaTerlambat + DendaKondisiNominal;

        public int HariTerlambat
        {
            get
            {
                if (TanggalKembaliBuku.Date > BatasKembali.Date)
                    return (TanggalKembaliBuku.Date - BatasKembali.Date).Days;
                return 0;
            }
        }

        public decimal EstimasiDenda
        {
            get { return HariTerlambat * KonstantaAplikasi.TarifDendaPerHari; }
        }

        public string TampilKondisi
        {
            get
            {
                switch (KondisiBuku)
                {
                    case "baik":         return "Baik";
                    case "rusak_ringan": return "Rusak Ringan";
                    case "rusak_berat":  return "Rusak Berat";
                    case "hilang":       return "Hilang";
                    default:             return KondisiBuku ?? "-";
                }
            }
        }

        public string TampilDendaTerlambat =>
            DendaTerlambat > 0 ? "Rp " + DendaTerlambat.ToString("N0") : "—";
        public string TampilDendaKondisi =>
            DendaKondisiNominal > 0 ? "Rp " + DendaKondisiNominal.ToString("N0") : "—";
        public string TampilTotalDenda =>
            TotalDendaBuku > 0 ? "Rp " + TotalDendaBuku.ToString("N0") : "—";
    }
}
