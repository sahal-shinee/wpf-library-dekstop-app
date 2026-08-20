using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SistemPerpustakaan.Tampilan.Admin
{
    public partial class HalamanPreviewLaporan : Window
    {
        // Callback untuk meneruskan data yang tampil di layar ke PDF generator di HalamanLaporan
        public Action<IEnumerable<object>> ExportCallback { get; set; }

        private readonly List<object> _dataAsli;
        private List<object> _dataTerfilter;

        private int _halamanSaatIni = 1;
        private int _ukuranHalaman = 15;
        private int _totalHalaman = 1;
        private bool _isDataDimuat = false;

        public HalamanPreviewLaporan(string judul, string subJudul, IEnumerable<object> data)
        {
            InitializeComponent();

            TxtJudul.Text = judul;
            TxtSubJudul.Text = subJudul;

            _dataAsli = data?.ToList() ?? new List<object>();
            _dataTerfilter = new List<object>(_dataAsli);
            _isDataDimuat = true;

            SegarkanTampilanData();
        }

        // ==========================================
        // LOGIKA PAGINASI & FILTERING
        // ==========================================

        private void SegarkanTampilanData()
        {
            if (!_isDataDimuat) return;

            // Hitung total halaman
            _totalHalaman = (int)Math.Ceiling(_dataTerfilter.Count / (double)_ukuranHalaman);
            if (_totalHalaman < 1) _totalHalaman = 1;

            if (_halamanSaatIni > _totalHalaman) _halamanSaatIni = _totalHalaman;
            if (_halamanSaatIni < 1) _halamanSaatIni = 1;

            // Ambil data sesuai halaman
            var dataHalamanIni = _dataTerfilter
                .Skip((_halamanSaatIni - 1) * _ukuranHalaman)
                .Take(_ukuranHalaman)
                .ToList();

            GridData.ItemsSource = dataHalamanIni;

            // Update UI Teks
            TxtPaginasi.Text = $"Hal {_halamanSaatIni} / {_totalHalaman}";
            TxtInfoBaris.Text = $"Menampilkan {dataHalamanIni.Count} baris (Total: {_dataTerfilter.Count} entri data)";

            // Atur status tombol
            BtnPrev.IsEnabled = _halamanSaatIni > 1;
            BtnNext.IsEnabled = _halamanSaatIni < _totalHalaman;
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            string kataKunci = TxtSearch.Text.ToLower().Trim();

            if (string.IsNullOrEmpty(kataKunci))
            {
                _dataTerfilter = new List<object>(_dataAsli);
            }
            else
            {
                // Fitur canggih: Cari teks di SEMUA kolom secara otomatis menggunakan Reflection
                _dataTerfilter = _dataAsli.Where(item =>
                {
                    PropertyInfo[] properti = item.GetType().GetProperties();
                    foreach (var prop in properti)
                    {
                        var nilai = prop.GetValue(item, null);
                        if (nilai != null && nilai.ToString().ToLower().Contains(kataKunci))
                        {
                            return true;
                        }
                    }
                    return false;
                }).ToList();
            }

            _halamanSaatIni = 1; // Reset ke halaman 1 setiap kali mencari
            SegarkanTampilanData();
        }

        private void CboPerHalaman_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isDataDimuat) return;

            var opsiPilihan = (ComboBoxItem)CboPerHalaman.SelectedItem;
            string nilai = opsiPilihan.Content.ToString();

            if (nilai == "Semua")
            {
                _ukuranHalaman = _dataAsli.Count > 0 ? _dataAsli.Count : 15;
            }
            else
            {
                if (int.TryParse(nilai, out int hasil))
                {
                    _ukuranHalaman = hasil;
                }
            }

            _halamanSaatIni = 1;
            SegarkanTampilanData();
        }

        private void BtnPrev_Click(object sender, RoutedEventArgs e)
        {
            if (_halamanSaatIni > 1)
            {
                _halamanSaatIni--;
                SegarkanTampilanData();
            }
        }

        private void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            if (_halamanSaatIni < _totalHalaman)
            {
                _halamanSaatIni++;
                SegarkanTampilanData();
            }
        }

        // ==========================================
        // LOGIKA FORMAT TAMPILAN TABEL
        // ==========================================

        private void GridData_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            // Ubah "NamaPeminjam" jadi "Nama Peminjam" agar enak dibaca
            e.Column.Header = Regex.Replace(e.PropertyName, "([a-z])([A-Z])", "$1 $2");

            // Format Rata Kanan untuk tipe data angka/nominal
            if (e.PropertyType == typeof(int) || e.PropertyType == typeof(decimal) || e.PropertyType == typeof(double))
            {
                var style = new Style(typeof(DataGridCell), (Style)this.FindResource(typeof(DataGridCell)));
                style.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Right));
                e.Column.CellStyle = style;
            }
        }

        // ==========================================
        // KONTROL WINDOW & TOMBOL
        // ==========================================

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Bisa drag and drop window, atau double click untuk maximize
            if (e.ClickCount == 2)
            {
                WindowState = WindowState == WindowState.Normal ? WindowState.Maximized : WindowState.Normal;
            }
            else
            {
                DragMove();
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            if (ExportCallback != null)
            {
                var dataDiLayar = GridData.ItemsSource as IEnumerable<object>;
                if (dataDiLayar != null && dataDiLayar.Any())
                {
                    // Kirim data yang tampil ke HalamanLaporan utama untuk diubah jadi PDF
                    ExportCallback.Invoke(dataDiLayar);
                }
                else
                {
                    MessageBox.Show("Tidak ada data untuk diekspor pada layar ini.", "Informasi", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }
    }
}