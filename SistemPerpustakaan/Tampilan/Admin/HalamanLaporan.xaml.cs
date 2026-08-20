using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using SistemPerpustakaan.Pembantu;
using SistemPerpustakaan.Repositori;
using System.Collections.Generic;

// MigraDoc / PdfSharp
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using MigOrientation = MigraDoc.DocumentObjectModel.Orientation;

namespace SistemPerpustakaan.Tampilan.Admin
{
    public partial class HalamanLaporan : Page
    {
        private readonly PeminjamanRepositori _pinjamRepo = new PeminjamanRepositori();
        private readonly DendaRepositori _dendaRepo = new DendaRepositori();
        private readonly BukuRepositori _bukuRepo = new BukuRepositori();

        public HalamanLaporan() { InitializeComponent(); }

        private Document BuildPdfDocument(string reportTitle, string reportSubtitle, double[] colWidthsCm, Action<Table> fillBody, Color headerShade, int[] rightAlignColumns = null)
        {
            var doc = new Document();
            doc.Info.Title = reportTitle;

            // Pengaturan Font Standar Modern
            var normal = doc.Styles[StyleNames.Normal];
            normal.Font.Name = KonstantaAplikasi.CorporateFontName ?? "Segoe UI";
            normal.Font.Size = 9;
            normal.Font.Color = Colors.DarkSlateGray;

            var heading = doc.Styles[StyleNames.Heading1];
            heading.Font.Name = normal.Font.Name;
            heading.Font.Size = 16;
            heading.Font.Bold = true;
            heading.Font.Color = Colors.Black;

            var sec = doc.AddSection();
            sec.PageSetup.PageFormat = PageFormat.A4;

            // 1. Tentukan Orientasi berdasarkan kebutuhan lebar tabel
            double totalRequested = colWidthsCm?.Sum() ?? 0;
            // Jika total ukuran kolom yang direquest > 17 cm, otomatis ubah ke Landscape
            sec.PageSetup.Orientation = totalRequested > 17.0 ? MigOrientation.Landscape : MigOrientation.Portrait;

            // 2. Perbaiki Margin agar Header tidak menabrak konten (Overlap)
            sec.PageSetup.LeftMargin = Unit.FromCentimeter(1.5);
            sec.PageSetup.RightMargin = Unit.FromCentimeter(1.5);
            sec.PageSetup.TopMargin = Unit.FromCentimeter(4.5); // Ruang lapang untuk header
            sec.PageSetup.BottomMargin = Unit.FromCentimeter(2.5);
            sec.PageSetup.HeaderDistance = Unit.FromCentimeter(1.0);
            sec.PageSetup.FooterDistance = Unit.FromCentimeter(1.0);

            // Hitung lebar area bersih yang bisa dipakai
            double pageWidth = sec.PageSetup.Orientation == MigOrientation.Landscape ? 29.7 : 21.0;
            double availableCm = pageWidth - 3.0; // 3.0 = margin kiri (1.5) + margin kanan (1.5)

            // Skalakan kolom secara proporsional mengisi lebar halaman
            double[] finalColWidths = colWidthsCm ?? new double[0];
            if (totalRequested > 0)
            {
                double factor = availableCm / totalRequested;
                finalColWidths = colWidthsCm.Select(w => w * factor).ToArray();
            }

            // === BAGIAN HEADER DOKUMEN ===
            double logoCol = 2.5;
            double rightCol = 6.0;
            double centerCol = availableCm - logoCol - rightCol;

            var headerTable = sec.Headers.Primary.AddTable();
            headerTable.AddColumn(Unit.FromCentimeter(logoCol));
            headerTable.AddColumn(Unit.FromCentimeter(centerCol));
            headerTable.AddColumn(Unit.FromCentimeter(rightCol));

            var hr = headerTable.AddRow();
            hr.VerticalAlignment = MigraDoc.DocumentObjectModel.Tables.VerticalAlignment.Center;

            // Menggunakan logo.jpg dari folder eksekusi aplikasi (.exe)
            string logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logo.png");

            if (File.Exists(logoPath))
            {
                var img = hr.Cells[0].AddImage(logoPath);
                img.LockAspectRatio = true;
                img.Width = Unit.FromCentimeter(2.2); // Ukuran proporsional untuk logo SMK
            }
            else
            {
                var initials = string.Join("", KonstantaAplikasi.OrganisasiNama.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s[0])).ToUpper();
                var pInit = hr.Cells[0].AddParagraph(initials);
                pInit.Format.Font.Size = 22;
                pInit.Format.Font.Bold = true;
                pInit.Format.Font.Color = headerShade;
                pInit.Format.Alignment = ParagraphAlignment.Left;
            }

            // Info Organisasi
            var orgPar = hr.Cells[1].AddParagraph();
            orgPar.Format.Alignment = ParagraphAlignment.Left;
            orgPar.AddFormattedText(KonstantaAplikasi.OrganisasiNama, TextFormat.Bold);
            orgPar.Format.Font.Size = 10;
            orgPar.AddLineBreak();
            orgPar.AddText(KonstantaAplikasi.OrganisasiAlamat + " — Telp: " + KonstantaAplikasi.OrganisasiTelepon);
            orgPar.AddLineBreak();
            orgPar.AddText(KonstantaAplikasi.OrganisasiEmail);
            orgPar.Format.Font.Size = 9;
            orgPar.Format.Font.Color = Colors.DimGray;

            // Info Periode (Kanan)
            var rightPar = hr.Cells[2].AddParagraph();
            rightPar.Format.Alignment = ParagraphAlignment.Right;
            rightPar.Format.Font.Size = 9;
            rightPar.Format.Font.Color = Colors.DimGray;
            if (!string.IsNullOrEmpty(reportSubtitle))
            {
                rightPar.AddText(reportSubtitle);
                rightPar.AddLineBreak();
            }
            rightPar.AddText("Dicetak: " + DateTime.Now.ToString("dd MMM yyyy HH:mm"));

            // Garis pembatas header
            var headerBorder = headerTable.AddRow();
            headerBorder.Borders.Bottom.Width = 1.5;
            headerBorder.Borders.Bottom.Color = headerShade;
            headerBorder.Height = Unit.FromCentimeter(0.3);

            // === BAGIAN JUDUL & TABEL UTAMA ===
            var title = sec.AddParagraph(reportTitle, StyleNames.Heading1);
            title.Format.Alignment = ParagraphAlignment.Center;
            title.Format.SpaceBefore = Unit.FromCentimeter(0.5);
            title.Format.SpaceAfter = Unit.FromCentimeter(0.5);

            var table = sec.AddTable();
            // Desain tabel modern: Padding lega dan border halus
            table.Borders.Width = 0.5;
            table.Borders.Color = Color.FromRgb(220, 220, 220);
            table.TopPadding = Unit.FromCentimeter(0.15);
            table.BottomPadding = Unit.FromCentimeter(0.15);
            table.LeftPadding = Unit.FromCentimeter(0.2);
            table.RightPadding = Unit.FromCentimeter(0.2);

            foreach (var w in finalColWidths) table.AddColumn(Unit.FromCentimeter(w));

            // Setup Baris Header Tabel
            var header = table.AddRow();
            header.HeadingFormat = true;
            header.Shading.Color = headerShade;
            header.Format.Font.Bold = true;
            header.Format.Font.Color = Colors.White; // Teks header putih agar kontras
            header.Format.Alignment = ParagraphAlignment.Center;
            header.VerticalAlignment = MigraDoc.DocumentObjectModel.Tables.VerticalAlignment.Center;

            // Panggil fungsi untuk mengisi tabel
            fillBody(table);

            // Terapkan Alternating Row Color (Zebra pattern) dan Perataan Kolom
            for (int r = 1; r < table.Rows.Count; r++)
            {
                var row = table.Rows[r];
                row.VerticalAlignment = MigraDoc.DocumentObjectModel.Tables.VerticalAlignment.Center;

                // Warna selang-seling yang enak dilihat mata
                if (r % 2 == 0)
                {
                    row.Shading.Color = Color.FromRgb(249, 250, 252);
                }

                // Default alignment ke kiri
                foreach (Cell cell in row.Cells)
                {
                    cell.Format.Alignment = ParagraphAlignment.Left;
                }

                // Terapkan rata kanan untuk kolom angka (jika ada)
                if (rightAlignColumns != null)
                {
                    foreach (var ci in rightAlignColumns)
                    {
                        if (ci >= 0 && ci < table.Columns.Count)
                            row.Cells[ci].Format.Alignment = ParagraphAlignment.Right;
                    }
                }
            }

            // === BAGIAN FOOTER ===
            var footerTable = sec.Footers.Primary.AddTable();
            footerTable.AddColumn(Unit.FromCentimeter(availableCm / 2));
            footerTable.AddColumn(Unit.FromCentimeter(availableCm / 2));
            var fr = footerTable.AddRow();

            var fLeft = fr.Cells[0].AddParagraph($"Sistem Informasi Perpustakaan © {DateTime.Now.Year}");
            fLeft.Format.Font.Size = 8;
            fLeft.Format.Font.Color = Colors.Gray;

            var fRight = fr.Cells[1].AddParagraph();
            fRight.Format.Alignment = ParagraphAlignment.Right;
            fRight.Format.Font.Size = 8;
            fRight.Format.Font.Color = Colors.Gray;
            fRight.AddText("Halaman ");
            fRight.AddPageField();
            fRight.AddText(" dari ");
            fRight.AddNumPagesField();

            return doc;
        }

        private void RenderAndSave(Document doc, string path)
        {
            var pdf = new PdfDocumentRenderer(unicode: true) { Document = doc };
            pdf.RenderDocument();
            pdf.Save(path);
        }

        // ==========================================
        // EVENT HANDLERS
        // ==========================================

        private async void EksporPeminjamanPdf_Click(object sender, RoutedEventArgs e)
        {
            TeksStatus.Text = "Membuat PDF laporan peminjaman...";
            try
            {
                var data = await _pinjamRepo.AmbilSemuaAsync();
                DateTime? start = TanggalMulai?.SelectedDate;
                DateTime? end = TanggalSampai?.SelectedDate;

                // Validasi rentang tanggal jika keduanya dipilih
                if (start.HasValue && end.HasValue && start.Value.Date > end.Value.Date)
                {
                    MessageBox.Show("Periode tidak valid: Tanggal mulai lebih besar dari tanggal sampai.", "Periode Tidak Valid", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TeksStatus.Text = "Periode tidak valid.";
                    return;
                }

                string subtitle = (start.HasValue || end.HasValue)
                    ? $"Periode: {(start.HasValue ? start.Value.ToString("dd MMM yyyy") : "-")} s.d {(end.HasValue ? end.Value.ToString("dd MMM yyyy") : "-") }"
                    : "";

                // Filter data berdasarkan periode jika ada
                var filtered = data;
                if (start.HasValue || end.HasValue)
                {
                    filtered = data.Where(p =>
                        (!start.HasValue || p.TanggalPinjam.Date >= start.Value.Date) &&
                        (!end.HasValue || p.TanggalPinjam.Date <= end.Value.Date)
                    ).ToList();
                }

                // Format Penamaan File Modern (Contoh: Laporan Peminjaman - 2026-08-12 14.26.30.pdf)
                string namaFile = $"Laporan Peminjaman - {DateTime.Now:yyyy-MM-dd HH.mm.ss}.pdf";
                string path = Path.Combine(KonstantaAplikasi.FolderLaporan, namaFile);

                await Task.Run(() =>
                {
                    double[] colWidths = { 2.2, 6.5, 3.2, 3.2, 3, 3.5 };

                    // Menggunakan warna Biru Modern (#2980b9)
                    var themeColor = Color.FromRgb(41, 128, 185);

                    Document doc = BuildPdfDocument("Laporan Peminjaman", subtitle, colWidths, table =>
                    {
                        table.Rows[0].Cells[0].AddParagraph("ID");
                        table.Rows[0].Cells[1].AddParagraph("Nama Peminjam");
                        table.Rows[0].Cells[2].AddParagraph("Tgl Pinjam");
                        table.Rows[0].Cells[3].AddParagraph("Batas Kembali");
                        table.Rows[0].Cells[4].AddParagraph("Jumlah Buku");
                        table.Rows[0].Cells[5].AddParagraph("Status");

                        foreach (var p in filtered)
                        {
                            var r = table.AddRow();
                            r.Cells[0].AddParagraph(p.IdPeminjaman.ToString());
                            r.Cells[1].AddParagraph(p.NamaPeminjam ?? "-");
                            r.Cells[2].AddParagraph(p.TanggalPinjam.ToString("dd/MM/yyyy"));
                            r.Cells[3].AddParagraph(p.BatasKembali.ToString("dd/MM/yyyy"));
                            r.Cells[4].AddParagraph((p.DetailBuku != null ? p.DetailBuku.Count : 0).ToString());

                            var pStatus = r.Cells[5].AddParagraph(p.TampilStatus);
                            pStatus.Format.Alignment = ParagraphAlignment.Center;
                        }

                    }, themeColor, new int[] { 0, 4 });

                    RenderAndSave(doc, path);
                });

                TeksStatus.Text = "PDF disimpan di: " + path;
                if (MessageBox.Show("PDF berhasil dibuat!\n\nBuka folder laporan?",
                    "Berhasil", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                {
                    System.Diagnostics.Process.Start("explorer.exe", KonstantaAplikasi.FolderLaporan);
                }
            }
            catch (Exception ex)
            {
                TeksStatus.Text = "Gagal membuat PDF: " + ex.Message;
            }
        }

        private async void EksporDendaPdf_Click(object sender, RoutedEventArgs e)
        {
            TeksStatus.Text = "Membuat PDF laporan denda...";
            try
            {
                var data = await _dendaRepo.AmbilSemuaAsync();
                DateTime? start = TanggalMulai?.SelectedDate;
                DateTime? end = TanggalSampai?.SelectedDate;

                // Validasi rentang tanggal jika keduanya dipilih
                if (start.HasValue && end.HasValue && start.Value.Date > end.Value.Date)
                {
                    MessageBox.Show("Periode tidak valid: Tanggal mulai lebih besar dari tanggal sampai.", "Periode Tidak Valid", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TeksStatus.Text = "Periode tidak valid.";
                    return;
                }

                string subtitle = (start.HasValue || end.HasValue)
                    ? $"Periode: {(start.HasValue ? start.Value.ToString("dd MMM yyyy") : "-")} s.d {(end.HasValue ? end.Value.ToString("dd MMM yyyy") : "-") }"
                    : "";

                // Filter data berdasarkan periode pada tanggal kembali
                var filtered = data;
                if (start.HasValue || end.HasValue)
                {
                    filtered = data.Where(d =>
                        (!start.HasValue || d.TanggalKembali.Date >= start.Value.Date) &&
                        (!end.HasValue || d.TanggalKembali.Date <= end.Value.Date)
                    ).ToList();
                }

                // Format Penamaan File Modern
                string namaFile = $"Laporan Denda - {DateTime.Now:yyyy-MM-dd HH.mm.ss}.pdf";
                string path = Path.Combine(KonstantaAplikasi.FolderLaporan, namaFile);

                await Task.Run(() =>
                {
                    double[] colWidths = { 6.5, 3.2, 4.5, 3, 3.5 };

                    // Menggunakan warna Merah Coral Modern (#e74c3c)
                    var themeColor = Color.FromRgb(231, 76, 60);

                    Document doc = BuildPdfDocument("Laporan Denda", subtitle, colWidths, table =>
                    {
                        table.Rows[0].Cells[0].AddParagraph("Nama Peminjam");
                        table.Rows[0].Cells[1].AddParagraph("Tgl Kembali");
                        table.Rows[0].Cells[2].AddParagraph("Total Denda (Rp)");
                        table.Rows[0].Cells[3].AddParagraph("Status");
                        table.Rows[0].Cells[4].AddParagraph("Tgl Bayar");

                        foreach (var d in filtered)
                        {
                            var r = table.AddRow();
                            r.Cells[0].AddParagraph(d.NamaPeminjam ?? "-");
                            r.Cells[1].AddParagraph(d.TanggalKembali.ToString("dd/MM/yyyy"));
                            r.Cells[2].AddParagraph(d.TotalDenda.ToString("N0"));

                            var pStatus = r.Cells[3].AddParagraph(d.TampilStatus);
                            pStatus.Format.Alignment = ParagraphAlignment.Center;

                            r.Cells[4].AddParagraph(d.TanggalBayar.HasValue ? d.TanggalBayar.Value.ToString("dd/MM/yyyy") : "-");
                        }

                    }, themeColor, new int[] { 2 }); // Kolom uang rata kanan

                    RenderAndSave(doc, path);
                });

                TeksStatus.Text = "PDF disimpan di: " + path;
                if (MessageBox.Show("PDF berhasil dibuat!\n\nBuka folder laporan?",
                    "Berhasil", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                {
                    System.Diagnostics.Process.Start("explorer.exe", KonstantaAplikasi.FolderLaporan);
                }
            }
            catch (Exception ex)
            {
                TeksStatus.Text = "Gagal membuat PDF: " + ex.Message;
            }
        }

        private async void EksporBukuPdf_Click(object sender, RoutedEventArgs e)
        {
            TeksStatus.Text = "Membuat PDF laporan koleksi buku...";
            try
            {
                var data = await _bukuRepo.AmbilSemuaAsync();
                DateTime? start = TanggalMulai?.SelectedDate;
                DateTime? end = TanggalSampai?.SelectedDate;

                // Validasi rentang tanggal jika keduanya dipilih
                if (start.HasValue && end.HasValue && start.Value.Date > end.Value.Date)
                {
                    MessageBox.Show("Periode tidak valid: Tanggal mulai lebih besar dari tanggal sampai.", "Periode Tidak Valid", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TeksStatus.Text = "Periode tidak valid.";
                    return;
                }

                string subtitle = (start.HasValue || end.HasValue)
                    ? $"Periode: {(start.HasValue ? start.Value.ToString("dd MMM yyyy") : "-")} s.d {(end.HasValue ? end.Value.ToString("dd MMM yyyy") : "-") }"
                    : "";

                // Filter buku berdasarkan tanggal dibuat (jika ingin filter koleksi baru)
                var filtered = data;
                if (start.HasValue || end.HasValue)
                {
                    filtered = data.Where(b =>
                        (!start.HasValue || b.DibuatPada.Date >= start.Value.Date) &&
                        (!end.HasValue || b.DibuatPada.Date <= end.Value.Date)
                    ).ToList();
                }

                // Format Penamaan File Modern
                string namaFile = $"Laporan Koleksi Buku - {DateTime.Now:yyyy-MM-dd HH.mm.ss}.pdf";
                string path = Path.Combine(KonstantaAplikasi.FolderLaporan, namaFile);

                await Task.Run(() =>
                {
                    double[] colWidths = { 6.5, 4.5, 3.5, 3.5, 2, 2, 2 };

                    // Menggunakan warna Hijau Emerald Modern (#27ae60)
                    var themeColor = Color.FromRgb(39, 174, 96);

                    Document doc = BuildPdfDocument("Laporan Koleksi Buku", subtitle, colWidths, table =>
                    {
                        table.Rows[0].Cells[0].AddParagraph("Judul");
                        table.Rows[0].Cells[1].AddParagraph("Penulis");
                        table.Rows[0].Cells[2].AddParagraph("Penerbit");
                        table.Rows[0].Cells[3].AddParagraph("Kategori");
                        table.Rows[0].Cells[4].AddParagraph("Stok");
                        table.Rows[0].Cells[5].AddParagraph("Tersedia");
                        table.Rows[0].Cells[6].AddParagraph("Dipinjam");

                        foreach (var b in filtered)
                        {
                            var r = table.AddRow();
                            r.Cells[0].AddParagraph(b.Judul ?? "-");
                            r.Cells[1].AddParagraph(b.Penulis ?? "-");
                            r.Cells[2].AddParagraph(b.Penerbit ?? "-");
                            r.Cells[3].AddParagraph(b.Kategori ?? "-");
                            r.Cells[4].AddParagraph((b.JumlahStok).ToString());
                            r.Cells[5].AddParagraph((b.StokTersedia).ToString());
                            r.Cells[6].AddParagraph((b.JumlahDipinjam).ToString());
                        }

                    }, themeColor, new int[] { 4, 5, 6 }); // Kolom angka rata kanan

                    RenderAndSave(doc, path);
                });

                TeksStatus.Text = "PDF disimpan di: " + path;
                if (MessageBox.Show("PDF berhasil dibuat!\n\nBuka folder laporan?",
                    "Berhasil", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                {
                    System.Diagnostics.Process.Start("explorer.exe", KonstantaAplikasi.FolderLaporan);
                }
            }
            catch (Exception ex)
            {
                TeksStatus.Text = "Gagal membuat PDF: " + ex.Message;
            }
        }

        private void ResetTanggal_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (TanggalMulai != null) TanggalMulai.SelectedDate = null;
                if (TanggalSampai != null) TanggalSampai.SelectedDate = null;
                TeksStatus.Text = string.Empty;
            }
            catch { /* ignore UI reset errors */ }
        }

        // PREVIEW HANDLERS WITH EXPORT CALLBACKS

        private async void PratinjauPeminjaman_Click(object sender, RoutedEventArgs e)
        {
            TeksStatus.Text = "Menyiapkan pratinjau laporan peminjaman...";
            try
            {
                var data = await _pinjamRepo.AmbilSemuaAsync();
                DateTime? start = TanggalMulai?.SelectedDate;
                DateTime? end = TanggalSampai?.SelectedDate;

                if (start.HasValue && end.HasValue && start.Value.Date > end.Value.Date)
                {
                    MessageBox.Show("Periode tidak valid: Tanggal mulai lebih besar dari tanggal sampai.", "Periode Tidak Valid", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TeksStatus.Text = "Periode tidak valid.";
                    return;
                }

                var filtered = data;
                if (start.HasValue || end.HasValue)
                {
                    filtered = data.Where(p =>
                        (!start.HasValue || p.TanggalPinjam.Date >= start.Value.Date) &&
                        (!end.HasValue || p.TanggalPinjam.Date <= end.Value.Date)
                    ).ToList();
                }

                var list = filtered.Select(p => new
                {
                    ID = p.IdPeminjaman,
                    NamaPeminjam = p.NamaPeminjam ?? "-",
                    TglPinjam = p.TanggalPinjam.ToString("dd/MM/yyyy"),
                    BatasKembali = p.BatasKembali.ToString("dd/MM/yyyy"),
                    JumlahBuku = (p.DetailBuku != null ? p.DetailBuku.Count : 0),
                    Status = p.TampilStatus
                }).ToList<object>();

                string subtitle = (start.HasValue || end.HasValue)
                    ? $"Periode: {(start.HasValue ? start.Value.ToString("dd MMM yyyy") : "-")} s.d {(end.HasValue ? end.Value.ToString("dd MMM yyyy") : "-") }"
                    : "Semua data";

                var wnd = new HalamanPreviewLaporan("Pratinjau Laporan Peminjaman", subtitle, list);
                wnd.Owner = Window.GetWindow(this);
                wnd.ExportCallback = items => {
                    // Export hanya items yang saat ini tampil di preview (page tertentu)
                    ExportObjectsAsPeminjamanPdf(items, start, end);
                };
                wnd.ShowDialog();

                TeksStatus.Text = string.Empty;
            }
            catch (Exception ex)
            {
                TeksStatus.Text = "Gagal membuat pratinjau: " + ex.Message;
            }
        }

        private async void PratinjauDenda_Click(object sender, RoutedEventArgs e)
        {
            TeksStatus.Text = "Menyiapkan pratinjau laporan denda...";
            try
            {
                var data = await _dendaRepo.AmbilSemuaAsync();
                DateTime? start = TanggalMulai?.SelectedDate;
                DateTime? end = TanggalSampai?.SelectedDate;

                if (start.HasValue && end.HasValue && start.Value.Date > end.Value.Date)
                {
                    MessageBox.Show("Periode tidak valid: Tanggal mulai lebih besar dari tanggal sampai.", "Periode Tidak Valid", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TeksStatus.Text = "Periode tidak valid.";
                    return;
                }

                var filtered = data;
                if (start.HasValue || end.HasValue)
                {
                    filtered = data.Where(d =>
                        (!start.HasValue || d.TanggalKembali.Date >= start.Value.Date) &&
                        (!end.HasValue || d.TanggalKembali.Date <= end.Value.Date)
                    ).ToList();
                }

                var list = filtered.Select(d => new
                {
                    NamaPeminjam = d.NamaPeminjam ?? "-",
                    TglKembali = d.TanggalKembali.ToString("dd/MM/yyyy"),
                    TotalDenda = d.TotalDenda.ToString("N0"),
                    Status = d.TampilStatus,
                    TglBayar = d.TanggalBayar.HasValue ? d.TanggalBayar.Value.ToString("dd/MM/yyyy") : "-"
                }).ToList<object>();

                string subtitle = (start.HasValue || end.HasValue)
                    ? $"Periode: {(start.HasValue ? start.Value.ToString("dd MMM yyyy") : "-")} s.d {(end.HasValue ? end.Value.ToString("dd MMM yyyy") : "-") }"
                    : "Semua data";

                var wnd = new HalamanPreviewLaporan("Pratinjau Laporan Denda", subtitle, list);
                wnd.Owner = Window.GetWindow(this);
                wnd.ExportCallback = items => {
                    ExportObjectsAsDendaPdf(items, start, end);
                };
                wnd.ShowDialog();

                TeksStatus.Text = string.Empty;
            }
            catch (Exception ex)
            {
                TeksStatus.Text = "Gagal membuat pratinjau: " + ex.Message;
            }
        }

        private async void PratinjauBuku_Click(object sender, RoutedEventArgs e)
        {
            TeksStatus.Text = "Menyiapkan pratinjau laporan koleksi buku...";
            try
            {
                var data = await _bukuRepo.AmbilSemuaAsync();
                DateTime? start = TanggalMulai?.SelectedDate;
                DateTime? end = TanggalSampai?.SelectedDate;

                if (start.HasValue && end.HasValue && start.Value.Date > end.Value.Date)
                {
                    MessageBox.Show("Periode tidak valid: Tanggal mulai lebih besar dari tanggal sampai.", "Periode Tidak Valid", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TeksStatus.Text = "Periode tidak valid.";
                    return;
                }

                var filtered = data;
                if (start.HasValue || end.HasValue)
                {
                    filtered = data.Where(b =>
                        (!start.HasValue || b.DibuatPada.Date >= start.Value.Date) &&
                        (!end.HasValue || b.DibuatPada.Date <= end.Value.Date)
                    ).ToList();
                }

                var list = filtered.Select(b => new
                {
                    Judul = b.Judul ?? "-",
                    Penulis = b.Penulis ?? "-",
                    Penerbit = b.Penerbit ?? "-",
                    Kategori = b.Kategori ?? "-",
                    Stok = b.JumlahStok,
                    Tersedia = b.StokTersedia,
                    Dipinjam = b.JumlahDipinjam
                }).ToList<object>();

                string subtitle = (start.HasValue || end.HasValue)
                    ? $"Periode: {(start.HasValue ? start.Value.ToString("dd MMM yyyy") : "-")} s.d {(end.HasValue ? end.Value.ToString("dd MMM yyyy") : "-") }"
                    : "Semua data";

                var wnd = new HalamanPreviewLaporan("Pratinjau Laporan Koleksi Buku", subtitle, list);
                wnd.Owner = Window.GetWindow(this);
                wnd.ExportCallback = items => {
                    ExportObjectsAsBukuPdf(items, start, end);
                };
                wnd.ShowDialog();

                TeksStatus.Text = string.Empty;
            }
            catch (Exception ex)
            {
                TeksStatus.Text = "Gagal membuat pratinjau: " + ex.Message;
            }
        }

        // Export helpers that accept IEnumerable<object> of anonymous objects created above.
        // They will reconstruct the necessary data and call existing PDF generator logic.

        private void ExportObjectsAsPeminjamanPdf(IEnumerable<object> objs, DateTime? start, DateTime? end)
        {
            try
            {
                // Convert back to dynamic-like access via reflection to build PDF rows similar to EksporPeminjamanPdf_Click
                var list = objs.ToList();
                string subtitle = (start.HasValue || end.HasValue)
                    ? $"Periode: {(start.HasValue ? start.Value.ToString("dd MMM yyyy") : "-")} s.d {(end.HasValue ? end.Value.ToString("dd MMM yyyy") : "-") }"
                    : "";

                string namaFile = $"Laporan Peminjaman - {DateTime.Now:yyyy-MM-dd HH.mm.ss}.pdf";
                string path = Path.Combine(KonstantaAplikasi.FolderLaporan, namaFile);

                var themeColor = Color.FromRgb(41, 128, 185);
                double[] colWidths = { 2.2, 6.5, 3.2, 3.2, 3, 3.5 };

                Document doc = BuildPdfDocument("Laporan Peminjaman", subtitle, colWidths, table =>
                {
                    table.Rows[0].Cells[0].AddParagraph("ID");
                    table.Rows[0].Cells[1].AddParagraph("Nama Peminjam");
                    table.Rows[0].Cells[2].AddParagraph("Tgl Pinjam");
                    table.Rows[0].Cells[3].AddParagraph("Batas Kembali");
                    table.Rows[0].Cells[4].AddParagraph("Jumlah Buku");
                    table.Rows[0].Cells[5].AddParagraph("Status");

                    foreach (var o in list)
                    {
                        var type = o.GetType();
                        var id = type.GetProperty("ID")?.GetValue(o)?.ToString() ?? "-";
                        var nama = type.GetProperty("NamaPeminjam")?.GetValue(o)?.ToString() ?? "-";
                        var tgl = type.GetProperty("TglPinjam")?.GetValue(o)?.ToString() ?? "-";
                        var batas = type.GetProperty("BatasKembali")?.GetValue(o)?.ToString() ?? "-";
                        var jumlah = type.GetProperty("JumlahBuku")?.GetValue(o)?.ToString() ?? "0";
                        var status = type.GetProperty("Status")?.GetValue(o)?.ToString() ?? "-";

                        var r = table.AddRow();
                        r.Cells[0].AddParagraph(id);
                        r.Cells[1].AddParagraph(nama);
                        r.Cells[2].AddParagraph(tgl);
                        r.Cells[3].AddParagraph(batas);
                        r.Cells[4].AddParagraph(jumlah);
                        r.Cells[5].AddParagraph(status);
                    }

                }, themeColor, new int[] { 0, 4 });

                RenderAndSave(doc, path);

                MessageBox.Show("PDF berhasil dibuat dari pratinjau!\n\nBuka folder laporan?", "Berhasil", MessageBoxButton.YesNo, MessageBoxImage.Information);
                System.Diagnostics.Process.Start("explorer.exe", KonstantaAplikasi.FolderLaporan);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal mengekspor: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportObjectsAsDendaPdf(IEnumerable<object> objs, DateTime? start, DateTime? end)
        {
            try
            {
                var list = objs.ToList();
                string subtitle = (start.HasValue || end.HasValue)
                    ? $"Periode: {(start.HasValue ? start.Value.ToString("dd MMM yyyy") : "-")} s.d {(end.HasValue ? end.Value.ToString("dd MMM yyyy") : "-") }"
                    : "";

                string namaFile = $"Laporan Denda - {DateTime.Now:yyyy-MM-dd HH.mm.ss}.pdf";
                string path = Path.Combine(KonstantaAplikasi.FolderLaporan, namaFile);

                var themeColor = Color.FromRgb(231, 76, 60);
                double[] colWidths = { 6.5, 3.2, 4.5, 3, 3.5 };

                Document doc = BuildPdfDocument("Laporan Denda", subtitle, colWidths, table =>
                {
                    table.Rows[0].Cells[0].AddParagraph("Nama Peminjam");
                    table.Rows[0].Cells[1].AddParagraph("Tgl Kembali");
                    table.Rows[0].Cells[2].AddParagraph("Total Denda (Rp)");
                    table.Rows[0].Cells[3].AddParagraph("Status");
                    table.Rows[0].Cells[4].AddParagraph("Tgl Bayar");

                    foreach (var o in list)
                    {
                        var type = o.GetType();
                        var nama = type.GetProperty("NamaPeminjam")?.GetValue(o)?.ToString() ?? "-";
                        var tgl = type.GetProperty("TglKembali")?.GetValue(o)?.ToString() ?? "-";
                        var total = type.GetProperty("TotalDenda")?.GetValue(o)?.ToString() ?? "0";
                        var status = type.GetProperty("Status")?.GetValue(o)?.ToString() ?? "-";
                        var tglbayar = type.GetProperty("TglBayar")?.GetValue(o)?.ToString() ?? "-";

                        var r = table.AddRow();
                        r.Cells[0].AddParagraph(nama);
                        r.Cells[1].AddParagraph(tgl);
                        r.Cells[2].AddParagraph(total);
                        r.Cells[3].AddParagraph(status);
                        r.Cells[4].AddParagraph(tglbayar);
                    }

                }, themeColor, new int[] { 2 });

                RenderAndSave(doc, path);
                MessageBox.Show("PDF berhasil dibuat dari pratinjau!\n\nBuka folder laporan?", "Berhasil", MessageBoxButton.YesNo, MessageBoxImage.Information);
                System.Diagnostics.Process.Start("explorer.exe", KonstantaAplikasi.FolderLaporan);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal mengekspor: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportObjectsAsBukuPdf(IEnumerable<object> objs, DateTime? start, DateTime? end)
        {
            try
            {
                var list = objs.ToList();
                string subtitle = (start.HasValue || end.HasValue)
                    ? $"Periode: {(start.HasValue ? start.Value.ToString("dd MMM yyyy") : "-")} s.d {(end.HasValue ? end.Value.ToString("dd MMM yyyy") : "-") }"
                    : "";

                string namaFile = $"Laporan Koleksi Buku - {DateTime.Now:yyyy-MM-dd HH.mm.ss}.pdf";
                string path = Path.Combine(KonstantaAplikasi.FolderLaporan, namaFile);

                var themeColor = Color.FromRgb(39, 174, 96);
                double[] colWidths = { 6.5, 4.5, 3.5, 3.5, 2, 2, 2 };

                Document doc = BuildPdfDocument("Laporan Koleksi Buku", subtitle, colWidths, table =>
                {
                    table.Rows[0].Cells[0].AddParagraph("Judul");
                    table.Rows[0].Cells[1].AddParagraph("Penulis");
                    table.Rows[0].Cells[2].AddParagraph("Penerbit");
                    table.Rows[0].Cells[3].AddParagraph("Kategori");
                    table.Rows[0].Cells[4].AddParagraph("Stok");
                    table.Rows[0].Cells[5].AddParagraph("Tersedia");
                    table.Rows[0].Cells[6].AddParagraph("Dipinjam");

                    foreach (var o in list)
                    {
                        var type = o.GetType();
                        var judul = type.GetProperty("Judul")?.GetValue(o)?.ToString() ?? "-";
                        var penulis = type.GetProperty("Penulis")?.GetValue(o)?.ToString() ?? "-";
                        var penerbit = type.GetProperty("Penerbit")?.GetValue(o)?.ToString() ?? "-";
                        var kategori = type.GetProperty("Kategori")?.GetValue(o)?.ToString() ?? "-";
                        var stok = type.GetProperty("Stok")?.GetValue(o)?.ToString() ?? "0";
                        var tersedia = type.GetProperty("Tersedia")?.GetValue(o)?.ToString() ?? "0";
                        var dipinjam = type.GetProperty("Dipinjam")?.GetValue(o)?.ToString() ?? "0";

                        var r = table.AddRow();
                        r.Cells[0].AddParagraph(judul);
                        r.Cells[1].AddParagraph(penulis);
                        r.Cells[2].AddParagraph(penerbit);
                        r.Cells[3].AddParagraph(kategori);
                        r.Cells[4].AddParagraph(stok);
                        r.Cells[5].AddParagraph(tersedia);
                        r.Cells[6].AddParagraph(dipinjam);
                    }

                }, themeColor, new int[] { 4, 5, 6 });

                RenderAndSave(doc, path);
                MessageBox.Show("PDF berhasil dibuat dari pratinjau!\n\nBuka folder laporan?", "Berhasil", MessageBoxButton.YesNo, MessageBoxImage.Information);
                System.Diagnostics.Process.Start("explorer.exe", KonstantaAplikasi.FolderLaporan);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal mengekspor: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}