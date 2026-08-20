using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using SistemPerpustakaan.Layanan;
using SistemPerpustakaan.Pembantu;

// ? FIX konflik namespace
using PenggunaModel = SistemPerpustakaan.Model.Pengguna;

namespace SistemPerpustakaan.Tampilan.Bersama
{
    public partial class JendelaLogin : Window
    {
        private readonly LayananAutentikasi _auth = new LayananAutentikasi();

        public JendelaLogin()
        {
            InitializeComponent();
            KotakNamaPengguna.Focus();
        }

        private void TombolKeRegistrasi_Click(object sender, RoutedEventArgs e)
        {
            PanelLogin.Visibility = Visibility.Collapsed;
            PanelRegistrasi.Visibility = Visibility.Visible;

            JudulForm.Text = "Buat Akun Baru";
            SubjudulForm.Text = "Daftar sebagai anggota perpustakaan";

            SembunyikanPesan();
            KotakNamaLengkap.Focus();
        }

        private void TombolKeLogin_Click(object sender, RoutedEventArgs e)
        {
            PanelRegistrasi.Visibility = Visibility.Collapsed;
            PanelLogin.Visibility = Visibility.Visible;

            JudulForm.Text = "Selamat Datang Kembali";
            SubjudulForm.Text = "Masuk ke akun Anda";

            SembunyikanPesan();
            KotakNamaPengguna.Focus();
        }

        private async void TombolMasuk_Click(object sender, RoutedEventArgs e)
        {
            string nama = KotakNamaPengguna.Text.Trim();
            string sandi = KotakKataSandi.Password;

            if (string.IsNullOrEmpty(nama))
            {
                TampilkanError("Nama pengguna tidak boleh kosong.");
                return;
            }

            if (string.IsNullOrEmpty(sandi))
            {
                TampilkanError("Kata sandi tidak boleh kosong.");
                return;
            }

            TombolMasuk.IsEnabled = false;
            TombolMasuk.Content = "Memeriksa...";
            SembunyikanPesan();

            try
            {
                var hasil = await _auth.MasukAsync(nama, sandi);

                bool ok = hasil.Item1;
                string pesan = hasil.Item2;
                PenggunaModel p = hasil.Item3;

                if (!ok)
                {
                    TampilkanError(pesan);
                    KotakKataSandi.Clear();
                    return;
                }

                // mulai sesi
                SesiPengguna.MulaiSesi(p);

                Window dasbor;
                if (p.AdalahAdmin)
                    dasbor = new JendelaUtamaAdmin();
                else
                    dasbor = new JendelaUtamaPengguna();

                dasbor.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                TampilkanError("Kesalahan: " + ex.Message);
            }
            finally
            {
                TombolMasuk.IsEnabled = true;
                TombolMasuk.Content = "Masuk";
            }
        }

        private async void TombolDaftar_Click(object sender, RoutedEventArgs e)
        {
            string namaLengkap = KotakNamaLengkap.Text.Trim();
            string namaPengguna = KotakNamaPenggunaReg.Text.Trim();
            string surel = KotakSurel.Text.Trim();
            string telepon = KotakTelepon.Text.Trim();
            string sandi = KotakKataSandiReg.Password;
            string konfirmasi = KotakKonfirmasi.Password;

            if (namaLengkap.Length < 3)
            {
                TampilkanError("Nama lengkap minimal 3 karakter.");
                return;
            }

            if (namaPengguna.Length < 4)
            {
                TampilkanError("Nama pengguna minimal 4 karakter.");
                return;
            }

            if (!Regex.IsMatch(namaPengguna, @"^[a-zA-Z0-9_]+$"))
            {
                TampilkanError("Nama pengguna hanya boleh huruf, angka, dan garis bawah.");
                return;
            }

            if (!surel.Contains("@"))
            {
                TampilkanError("Alamat surel tidak valid.");
                return;
            }

            var cekSandi = PembantuHash.ValidasiKekuatan(sandi);
            if (!cekSandi.Item1)
            {
                TampilkanError(cekSandi.Item2);
                return;
            }

            if (sandi != konfirmasi)
            {
                TampilkanError("Konfirmasi kata sandi tidak cocok.");
                return;
            }

            TombolDaftar.IsEnabled = false;
            TombolDaftar.Content = "Mendaftarkan...";
            SembunyikanPesan();

            try
            {
                var penggunaBaru = new PenggunaModel
                {
                    NamaLengkap = namaLengkap,
                    NamaPengguna = namaPengguna,
                    Surel = surel,
                    Telepon = string.IsNullOrEmpty(telepon) ? null : telepon
                };

                var hasil = await _auth.DaftarAsync(penggunaBaru, sandi);

                bool ok = hasil.Item1;
                string pesan = hasil.Item2;

                if (!ok)
                {
                    TampilkanError(pesan);
                    return;
                }

                TombolKeLogin_Click(null, null);
                TampilkanSukses("Akun berhasil dibuat! Silakan masuk.");

                // reset form
                KotakNamaLengkap.Clear();
                KotakNamaPenggunaReg.Clear();
                KotakSurel.Clear();
                KotakTelepon.Clear();
                KotakKataSandiReg.Clear();
                KotakKonfirmasi.Clear();
            }
            catch (Exception ex)
            {
                TampilkanError("Kesalahan: " + ex.Message);
            }
            finally
            {
                TombolDaftar.IsEnabled = true;
                TombolDaftar.Content = "Buat Akun";
            }
        }

        private void KotakTeks_KeyDown(object s, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                KotakKataSandi.Focus();
        }

        private void KotakKataSandi_KeyDown(object s, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                TombolMasuk_Click(s, e);
        }

        private void TampilkanError(string pesan)
        {
            PanelError.Visibility = Visibility.Visible;
            PanelSukses.Visibility = Visibility.Collapsed;
            TeksError.Text = pesan;
        }

        private void TampilkanSukses(string pesan)
        {
            PanelSukses.Visibility = Visibility.Visible;
            PanelError.Visibility = Visibility.Collapsed;
            TeksSukses.Text = pesan;
        }

        private void SembunyikanPesan()
        {
            PanelError.Visibility = Visibility.Collapsed;
            PanelSukses.Visibility = Visibility.Collapsed;
        }
    }
}