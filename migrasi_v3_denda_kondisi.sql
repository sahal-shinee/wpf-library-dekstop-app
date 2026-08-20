-- ================================================================
--  MIGRASI v3: Tambahan Denda Kondisi Buku
--  Jalankan di phpMyAdmin → tab SQL → klik Go
--  Target database: db_perpustakaan
-- ================================================================

USE `db_perpustakaan`;

-- ----------------------------------------------------------------
-- 1. Tambah kolom harga ke tabel buku
--    Digunakan untuk menghitung denda buku hilang
-- ----------------------------------------------------------------
ALTER TABLE `buku`
  ADD COLUMN `harga` DECIMAL(12,2) NOT NULL DEFAULT 0.00
  COMMENT 'Harga buku (digunakan untuk menghitung denda hilang)'
  AFTER `deskripsi`;

-- ----------------------------------------------------------------
-- 2. Tambah kolom kondisi_buku dan denda_kondisi ke detail_denda
--    kondisi_buku  : kondisi fisik buku saat dikembalikan
--    denda_kondisi : nominal denda akibat kerusakan / kehilangan buku
--      rusak_ringan = Rp 25.000
--      rusak_berat  = Rp 75.000
--      hilang       = harga buku dari tabel buku (atau Rp 150.000 jika harga belum diisi)
-- ----------------------------------------------------------------
ALTER TABLE `detail_denda`
  ADD COLUMN `kondisi_buku`
    ENUM('baik','rusak_ringan','rusak_berat','hilang')
    NOT NULL DEFAULT 'baik'
    COMMENT 'Kondisi buku saat dikembalikan'
    AFTER `id_buku`,
  ADD COLUMN `denda_kondisi` DECIMAL(12,2) NOT NULL DEFAULT 0.00
    COMMENT 'Denda kondisi buku: rusak_ringan=25.000, rusak_berat=75.000, hilang=harga buku'
    AFTER `subtotal_denda`;

-- ================================================================
-- SELESAI — jalankan sekali pada database yang sudah ada
-- ================================================================
