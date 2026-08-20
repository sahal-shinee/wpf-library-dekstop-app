using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SistemPerpustakaan.Model;
using SistemPerpustakaan.Repositori;

// TODO: Akan diisi penuh pada Tahap 3
namespace SistemPerpustakaan.Layanan
{
    public class HasilProsesPengembalian
    {
        public bool Sukses { get; set; }
        public string Pesan { get; set; }
        public int IdPengembalian { get; set; }
        public decimal TotalDenda { get; set; }
        public List<DetailPengembalian> DetailBuku { get; set; }

        public HasilProsesPengembalian()
        {
            DetailBuku = new List<DetailPengembalian>();
        }
    }

    public class LayananPengembalian
    {
        private readonly PengembalianRepositori _repo = new PengembalianRepositori();

        // Proses pengembalian: panggil repositori, lalu ambil rincian untuk menghitung total denda
        public async Task<HasilProsesPengembalian> ProsesAsync(
            int idPeminjaman, int idPengguna, List<Tuple<int, int, string, string>> daftarBuku, string catatan = null)
        {
            var hasil = new HasilProsesPengembalian();

            var r = await _repo.ProsesAsync(idPeminjaman, idPengguna, daftarBuku, catatan);
            hasil.Sukses = r.Item1;
            hasil.Pesan = r.Item2;
            hasil.IdPengembalian = r.Item3;

            if (!hasil.Sukses || hasil.IdPengembalian <= 0)
                return hasil;

            try
            {
                var detail = await _repo.AmbilDetailAsync(hasil.IdPengembalian);
                hasil.DetailBuku = detail ?? new List<DetailPengembalian>();
                decimal total = 0m;
                foreach (var d in hasil.DetailBuku) total += d.TotalDendaBuku;
                hasil.TotalDenda = total;
            }
            catch
            {
                // jangan lempar, cukup kembalikan tanpa detail
            }

            return hasil;
        }
    }
}
