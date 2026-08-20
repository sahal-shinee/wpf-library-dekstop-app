# 📌 Catatan Penting — Sebelum Build di Visual Studio 2022

## Langkah Wajib Setelah Buka Proyek

### 1. Install NuGet Package (wajib sebelum build pertama)
Buka **Tools → NuGet Package Manager → Package Manager Console**, lalu jalankan:
```
Install-Package MySql.Data -Version 8.0.33
Install-Package ClosedXML -Version 0.102.1
```

### 2. Jika ada error "Namespace tidak ditemukan"
Klik kanan proyek → **Build** → jika masih error, coba:
- Klik kanan Solution → **Restore NuGet Packages**
- Kemudian Build ulang (Ctrl+Shift+B)

### 3. Koneksi Database
- File: `Pembantu/KonstantaAplikasi.cs`
- Jika XAMPP di drive D: ubah `PathXampp = @"D:\xampp"`
- Password MySQL default XAMPP: kosong (sudah dikonfigurasi)

### 4. Urutan Jalankan Aplikasi
1. Buka proyek di Visual Studio 2022
2. Install NuGet packages
3. Tekan **F5** → XAMPP otomatis menyala via XamppPenyala.cs
4. Database otomatis dibuat jika belum ada
5. Login dengan: username `admin`, password `Admin@123`

---

## Struktur Alur Aplikasi

```
App.xaml.cs (startup)
 └── XamppPenyala.cs     → nyalakan MySQL otomatis
 └── JendelaSplash.xaml  → layar loading
 └── JendelaLogin.xaml   → login / registrasi
      ├── JendelaUtamaAdmin.xaml   (peran=admin)
      │    ├── HalamanDasbordAdmin
      │    ├── HalamanKelolaBuku    (+ JendelaFormBuku, JendelaDetailBuku)
      │    ├── HalamanKelolaPengguna
      │    ├── HalamanPeminjamanAdmin
      │    ├── HalamanPengembalianAdmin
      │    ├── HalamanDendaAdmin
      │    └── HalamanLaporan
      └── JendelaUtamaPengguna.xaml (peran=pengguna)
           ├── HalamanDasbordPengguna
           ├── HalamanCariPinjamBuku  (+ KartuBuku UserControl)
           ├── HalamanRiwayatPinjam
           └── HalamanDendaPengguna
```

---

## Fitur Yang Sudah Berjalan Penuh

| Fitur | Status |
|---|---|
| Auto-start MySQL XAMPP | ✅ |
| Login & Registrasi | ✅ |
| Manajemen Buku (CRUD + upload cover) | ✅ |
| Manajemen Pengguna (CRUD + blokir) | ✅ |
| Peminjaman (maks 5 buku, 7 hari) | ✅ |
| Pengembalian (sebagian/semua) | ✅ |
| Denda otomatis per buku (Rp 2.000/hari) | ✅ |
| Blokir akun otomatis jika denda belum lunas | ✅ |
| Dasbor admin (statistik real-time) | ✅ |
| Cari & Pinjam Buku (sistem keranjang) | ✅ |
| Riwayat pinjam + kembalikan mandiri | ✅ |
| Status denda detail per buku | ✅ |
| Ekspor laporan Excel | ✅ |

