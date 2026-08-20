using System;
using System.Text;
namespace SistemPerpustakaan.Model
{
    public class DetailDenda
    {
        public int      IdDetailDenda        { get; set; }
        public int      IdDenda              { get; set; }
        public int      IdDetailKembali      { get; set; }
        public int      IdBuku               { get; set; }
        public string   KondisiBuku          { get; set; }
        public DateTime BatasKembali         { get; set; }
        public DateTime TanggalKembaliAktual { get; set; }
        public int      JumlahHariTerlambat  { get; set; }
        public decimal  TarifPerHari         { get; set; }
        public decimal  SubtotalDenda        { get; set; }   // denda keterlambatan saja
        public decimal  DendaKondisi         { get; set; }   // denda rusak/hilang
        public string   JudulBuku            { get; set; }
        public string   PenulisBuku          { get; set; }

        // Total per buku = keterlambatan + kondisi
        public decimal TotalPerBuku => SubtotalDenda + DendaKondisi;

        public string TampilSubtotal => "Rp " + TotalPerBuku.ToString("N0");

        public string TampilKondisi
        {
            get
            {
                switch (KondisiBuku)
                {
                    case "rusak_ringan": return "Rusak Ringan";
                    case "rusak_berat":  return "Rusak Berat";
                    case "hilang":       return "Hilang";
                    default:             return "Baik";
                }
            }
        }

        public string TampilRincian
        {
            get
            {
                var sb = new StringBuilder();
                if (JumlahHariTerlambat > 0)
                    sb.Append(JumlahHariTerlambat + " hari × Rp " +
                              TarifPerHari.ToString("N0") + " (terlambat)");
                if (DendaKondisi > 0)
                {
                    if (sb.Length > 0) sb.Append("  +  ");
                    sb.Append("Rp " + DendaKondisi.ToString("N0") +
                              " (" + TampilKondisi + ")");
                }
                if (sb.Length == 0) sb.Append("Tidak ada denda");
                return sb.ToString();
            }
        }

        // Backward compat
        public string TampilDendaTerlambat =>
            SubtotalDenda > 0 ? "Rp " + SubtotalDenda.ToString("N0") : "—";
        public string TampilDendaKondisi =>
            DendaKondisi > 0 ? "Rp " + DendaKondisi.ToString("N0") : "—";
    }
}
