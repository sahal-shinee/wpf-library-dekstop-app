using System;
using System.Text.RegularExpressions;
using System.Windows;
using SistemPerpustakaan.Layanan;
using SistemPerpustakaan.Pembantu;

// ? FIX alias
using ModelPengguna = SistemPerpustakaan.Model.Pengguna;

namespace SistemPerpustakaan.Tampilan.Bersama
{
    public partial class JendelaRegistrasi : Window
    {
        private readonly LayananAutentikasi _auth = new LayananAutentikasi();

        public JendelaRegistrasi() { InitializeComponent(); }

        private async void TombolDaftar_Click(object sender, RoutedEventArgs e)
        {
            string namaLengkap = KotakNamaLengkap.Text.Trim();
            string namaPengguna = KotakNamaPengguna.Text.Trim();
            string surel = KotakSurel.Text.Trim();
            string telepon = KotakTelepon.Text.Trim();
            string sandi = KotakKataSandi.Password;
            string konfirmasi = KotakKonfirmasi.Password;

            if (namaLengkap.Length < 3)
            { TampilError("Nama lengkap minimal 3 karakter."); return; }

            if (namaPengguna.Length < 4)
            { TampilError("Nama pengguna minimal 4 karakter."); return; }

            if (!Regex.IsMatch(namaPengguna, @"^[a-zA-Z0-9_]+$"))
            { TampilError("Nama pengguna hanya boleh huruf, angka, dan garis bawah."); return; }

            if (!surel.Contains("@"))
            { TampilError("Alamat surel tidak valid."); return; }

            var cekSandi = PembantuHash.ValidasiKekuatan(sandi);
            if (!cekSandi.Item1)
            { TampilError(cekSandi.Item2); return; }

            if (sandi != konfirmasi)
            { TampilError("Konfirmasi kata sandi tidak cocok."); return; }

            TombolDaftar.IsEnabled = false;
            TombolDaftar.Content = "Mendaftarkan...";

            try
            {
                // ? FIX DI SINI
                var p = new ModelPengguna
                {
                    NamaLengkap = namaLengkap,
                    NamaPengguna = namaPengguna,
                    Surel = surel,
                    Telepon = string.IsNullOrEmpty(telepon) ? null : telepon,
                };

                var h = await _auth.DaftarAsync(p, sandi);

                if (!h.Item1)
                {
                    TampilError(h.Item2);
                    return;
                }

                MessageBox.Show("Akun berhasil dibuat! Silakan masuk.",
                    "Registrasi Berhasil",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                new JendelaLogin().Show();
                this.Close();
            }
            catch (Exception ex)
            {
                TampilError("Kesalahan: " + ex.Message);
            }
            finally
            {
                TombolDaftar.IsEnabled = true;
                TombolDaftar.Content = "Buat Akun";
            }
        }

        private void TombolKeLogin_Click(object sender, RoutedEventArgs e)
        {
            new JendelaLogin().Show();
            this.Close();
        }

        private void TampilError(string p)
        {
            PanelError.Visibility = Visibility.Visible;
            TeksError.Text = p;
        }
    }
}