# 📚 Panduan Setup — Sistem Manajemen Perpustakaan
**Visual Studio 2022 · .NET Framework 4.8 · WPF · MySQL (XAMPP)**

---

## Prasyarat
| Kebutuhan | Keterangan |
|---|---|
| Windows 10/11 | OS yang didukung |
| Visual Studio 2022 | Community Edition sudah cukup |
| .NET Framework 4.8 | Biasanya sudah ada di Windows 10/11 |
| XAMPP (terbaru) | Instal ke `C:\xampp` (path default) |

---

## Langkah 1 — Instal XAMPP
1. Unduh dari https://www.apachefriends.org/
2. Instal ke `C:\xampp` (path default — jangan ubah)
3. **Tidak perlu menyalakan XAMPP secara manual** — aplikasi akan otomatis menyalakannya

---

## Langkah 2 — Buka Proyek di Visual Studio 2022
1. Ekstrak file zip ini
2. Buka **`SistemPerpustakaan.sln`** — klik dua kali atau buka lewat Visual Studio
3. Visual Studio akan mendeteksi proyek otomatis

---

## Langkah 3 — Install NuGet Packages
Di Visual Studio:
1. Klik **Tools → NuGet Package Manager → Package Manager Console**
2. Jalankan perintah ini satu per satu:
```
Install-Package MySql.Data -Version 8.0.33
Install-Package ClosedXML -Version 0.102.1
```
Atau klik kanan proyek → **Manage NuGet Packages** → cari dan instal manual.

---

## Langkah 4 — Setup Database
**Opsi A (Otomatis):** Jalankan aplikasi — database akan dibuat otomatis.

**Opsi B (Manual):**
1. Buka phpMyAdmin: http://localhost/phpmyadmin
2. Import file: `skema_database_perpustakaan_v2.sql`

---

## Langkah 5 — Jalankan Aplikasi
1. Tekan **F5** atau klik tombol ▶ Start
2. Layar splash akan muncul sambil MySQL dinyalakan otomatis
3. Halaman login terbuka

---

## Akun Bawaan
| Role | Username | Kata Sandi |
|---|---|---|
| Admin | `admin` | `Admin@123` |
| Pengguna | Daftar sendiri lewat halaman registrasi | — |

---

## Konfigurasi (jika XAMPP di path berbeda)
Edit `Pembantu/KonstantaAplikasi.cs`:
```csharp
public static readonly string PathXampp = @"D:\xampp"; // sesuaikan
```

---

## Aturan Bisnis Default
| Parameter | Nilai |
|---|---|
| Maks. buku per pinjaman | 5 buku |
| Durasi peminjaman | 7 hari |
| Denda keterlambatan | Rp 2.000 / hari / buku |
| Blokir otomatis | Jika ada denda belum lunas |

---

## Struktur Folder Proyek
```
SistemPerpustakaan/
├── Model/          ← Kelas data (Pengguna, Buku, dll.)
├── Repositori/     ← Akses langsung ke database
├── Layanan/        ← Logika bisnis
├── Pembantu/       ← Utilitas (koneksi, hash, gambar)
├── Tampilan/
│   ├── Admin/      ← Halaman khusus admin
│   ├── Pengguna/   ← Halaman khusus pengguna
│   ├── Bersama/    ← Login, Registrasi, Jendela utama
│   └── Komponen/   ← UserControl reusable
└── Aset/
    └── SampulBuku/ ← Cover buku + placeholder.png
```
