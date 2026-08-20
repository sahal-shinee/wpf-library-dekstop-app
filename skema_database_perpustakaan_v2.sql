-- ================================================================
--  SISTEM MANAJEMEN PERPUSTAKAAN — SKEMA DATABASE v3
--  Revisi: Peminjaman, Pengembalian, Denda masing-masing dipisah
--          beserta tabel detail masing-masing
--  v3     : Tambah kolom harga & sampul di buku;
--           kondisi_buku & denda_kondisi di detail_denda
--  Framework : .NET 4.8 + WPF
--  Database  : MySQL (XAMPP)
--  Aturan    : Maks 5 buku, 7 hari, denda Rp 2.000/hari/buku
--              + denda kondisi: rusak_ringan Rp 25.000,
--                rusak_berat Rp 75.000, hilang = harga buku
-- ================================================================

CREATE DATABASE IF NOT EXISTS `perpustakaan`
  CHARACTER SET utf8mb4
  COLLATE utf8mb4_unicode_ci;

USE `perpustakaan`;

-- ================================================================
-- URUTAN PEMBUATAN TABEL (mengikuti ketergantungan foreign key):
--   1. pengguna
--   2. buku
--   3. peminjaman
--   4. detail_peminjaman
--   5. pengembalian
--   6. detail_pengembalian
--   7. denda
--   8. detail_denda
-- ================================================================


-- ----------------------------------------------------------------
-- 1. TABEL PENGGUNA
--    Menyimpan seluruh akun: admin (dibuat manual) & pengguna umum
-- ----------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `pengguna` (
  `id_pengguna`     INT           NOT NULL AUTO_INCREMENT,
  `nama_lengkap`    VARCHAR(150)  NOT NULL  COMMENT 'Nama lengkap',
  `nama_pengguna`   VARCHAR(60)   NOT NULL  COMMENT 'Username unik untuk login',
  `kata_sandi`      VARCHAR(255)  NOT NULL  COMMENT 'Password hash SHA-256',
  `surel`           VARCHAR(150)  NOT NULL  COMMENT 'Alamat email',
  `telepon`         VARCHAR(20)       NULL  COMMENT 'Nomor HP (opsional)',
  `peran`           ENUM('admin','pengguna')
                    NOT NULL DEFAULT 'pengguna'
                    COMMENT 'admin = pengelola, pengguna = anggota biasa',
  `status`          ENUM('aktif','diblokir','nonaktif')
                    NOT NULL DEFAULT 'aktif'
                    COMMENT 'diblokir otomatis jika ada denda belum lunas',
  `dibuat_pada`     TIMESTAMP     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `diperbarui_pada` TIMESTAMP     NOT NULL DEFAULT CURRENT_TIMESTAMP
                    ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id_pengguna`),
  UNIQUE KEY `uq_nama_pengguna` (`nama_pengguna`),
  UNIQUE KEY `uq_surel`         (`surel`),
  INDEX `idx_peran`   (`peran`),
  INDEX `idx_status`  (`status`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
  COMMENT='Akun seluruh pengguna sistem perpustakaan';

-- Akun admin bawaan (kata sandi: Admin@123 — SHA-256)
INSERT INTO `pengguna`
  (`nama_lengkap`, `nama_pengguna`, `kata_sandi`, `surel`, `peran`, `status`)
VALUES (
  'Administrator',
  'admin',
  '0a041b9462caa4a31bac3567e0b6e6fd9100787db2ab433d96f6d178cabfce90',
  'admin@perpustakaan.id',
  'admin',
  'aktif'
);


-- ----------------------------------------------------------------
-- 2. TABEL BUKU
--    Koleksi lengkap buku beserta stok real-time
--    Kategori disimpan sebagai kolom teks (simpel, tidak perlu tabel terpisah)
-- ----------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `buku` (
  `id_buku`         INT           NOT NULL AUTO_INCREMENT,
  `judul`           VARCHAR(255)  NOT NULL  COMMENT 'Judul buku',
  `penulis`         VARCHAR(200)  NOT NULL  COMMENT 'Nama penulis / pengarang',
  `penerbit`        VARCHAR(150)      NULL  COMMENT 'Nama penerbit',
  `kategori`        VARCHAR(80)       NULL  COMMENT 'Kategori / genre buku (teks bebas)',
  `isbn`            VARCHAR(20)       NULL  COMMENT 'Nomor ISBN (opsional)',
  `tahun_terbit`    YEAR              NULL  COMMENT 'Tahun penerbitan',
  `jumlah_stok`     INT           NOT NULL DEFAULT 1
                    COMMENT 'Total eksemplar yang dimiliki perpustakaan',
  `stok_tersedia`   INT           NOT NULL DEFAULT 1
                    COMMENT 'Jumlah eksemplar yang masih bisa dipinjam',
  `deskripsi`       TEXT              NULL  COMMENT 'Sinopsis / catatan buku',
  `harga`           DECIMAL(12,2) NOT NULL DEFAULT 0.00
                    COMMENT 'Harga buku (digunakan untuk denda hilang)',
  `sampul`          VARCHAR(255)      NULL  COMMENT 'Nama file gambar sampul (relatif ke folder Sampul/)',
  `dibuat_pada`     TIMESTAMP     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `diperbarui_pada` TIMESTAMP     NOT NULL DEFAULT CURRENT_TIMESTAMP
                    ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id_buku`),
  UNIQUE KEY `uq_isbn`        (`isbn`),
  INDEX `idx_judul`    (`judul`),
  INDEX `idx_penulis`  (`penulis`),
  INDEX `idx_kategori` (`kategori`),
  CONSTRAINT `chk_stok_valid`
    CHECK (`stok_tersedia` >= 0 AND `stok_tersedia` <= `jumlah_stok`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
  COMMENT='Koleksi buku perpustakaan';

-- Data contoh buku
INSERT INTO `buku`
  (`judul`, `penulis`, `penerbit`, `kategori`, `isbn`, `tahun_terbit`, `jumlah_stok`, `stok_tersedia`, `deskripsi`, `harga`)
VALUES
  ('Laskar Pelangi',   'Andrea Hirata',         'Bentang Pustaka', 'Fiksi',             '9789793062792', 2005, 3, 3, 'Novel tentang persahabatan dan semangat anak-anak Belitung.',   85000),
  ('Bumi Manusia',     'Pramoedya Ananta Toer', 'Hasta Mitra',     'Sejarah',           '9789799731234', 1980, 2, 2, 'Tetralogi Buru, kisah Minke di masa kolonial Belanda.',          95000),
  ('Atomic Habits',    'James Clear',           'Gramedia',        'Pengembangan Diri', '9786020633176', 2019, 4, 4, 'Cara membangun kebiasaan kecil yang menghasilkan perubahan besar.', 98000),
  ('Clean Code',       'Robert C. Martin',      'Prentice Hall',   'Teknologi',         '9780132350884', 2008, 2, 2, 'Panduan menulis kode yang bersih dan mudah dipelihara.',         175000),
  ('Sapiens',          'Yuval Noah Harari',     'Gramedia',        'Sejarah',           '9786020319780', 2014, 3, 3, 'Sejarah singkat umat manusia dari zaman purba hingga modern.',   110000);


-- ----------------------------------------------------------------
-- 3. TABEL PEMINJAMAN
--    Header sesi peminjaman — satu baris = satu sesi pinjam
--    Bisa memuat 1 s/d 5 buku (lihat detail_peminjaman)
-- ----------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `peminjaman` (
  `id_peminjaman`   INT           NOT NULL AUTO_INCREMENT,
  `id_pengguna`     INT           NOT NULL  COMMENT 'Pengguna yang meminjam',
  `tanggal_pinjam`  DATE          NOT NULL  COMMENT 'Tanggal sesi peminjaman dimulai',
  `batas_kembali`   DATE          NOT NULL  COMMENT 'Batas pengembalian (tanggal_pinjam + 7 hari)',
  `status`          ENUM(
                      'aktif',          -- sedang dipinjam, belum jatuh tempo
                      'sebagian_kembali',-- sebagian buku sudah dikembalikan
                      'selesai',        -- semua buku sudah dikembalikan
                      'terlambat'       -- melewati batas_kembali, belum selesai
                    ) NOT NULL DEFAULT 'aktif',
  `catatan`         TEXT              NULL  COMMENT 'Catatan admin (opsional)',
  `dibuat_pada`     TIMESTAMP     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `diperbarui_pada` TIMESTAMP     NOT NULL DEFAULT CURRENT_TIMESTAMP
                    ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id_peminjaman`),
  INDEX `idx_pinjam_pengguna`     (`id_pengguna`),
  INDEX `idx_pinjam_status`       (`status`),
  INDEX `idx_pinjam_batas_kembali`(`batas_kembali`),
  CONSTRAINT `fk_pinjam_pengguna`
    FOREIGN KEY (`id_pengguna`) REFERENCES `pengguna`(`id_pengguna`)
    ON UPDATE CASCADE ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
  COMMENT='Header sesi peminjaman buku';


-- ----------------------------------------------------------------
-- 4. TABEL DETAIL PEMINJAMAN
--    Rincian tiap buku dalam satu sesi peminjaman
--    Maks. 5 baris per id_peminjaman (ditegakkan di aplikasi & trigger)
-- ----------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `detail_peminjaman` (
  `id_detail_pinjam`  INT   NOT NULL AUTO_INCREMENT,
  `id_peminjaman`     INT   NOT NULL  COMMENT 'Referensi ke header peminjaman',
  `id_buku`           INT   NOT NULL  COMMENT 'Buku yang dipinjam',
  `status_buku`       ENUM('dipinjam','dikembalikan')
                      NOT NULL DEFAULT 'dipinjam'
                      COMMENT 'Status per buku dalam sesi ini',
  `dibuat_pada`       TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id_detail_pinjam`),
  UNIQUE KEY `uq_pinjam_buku` (`id_peminjaman`, `id_buku`)
    COMMENT 'Satu buku hanya sekali per sesi peminjaman',
  INDEX `idx_dpinjam_pinjam` (`id_peminjaman`),
  INDEX `idx_dpinjam_buku`   (`id_buku`),
  CONSTRAINT `fk_dpinjam_pinjam`
    FOREIGN KEY (`id_peminjaman`) REFERENCES `peminjaman`(`id_peminjaman`)
    ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT `fk_dpinjam_buku`
    FOREIGN KEY (`id_buku`) REFERENCES `buku`(`id_buku`)
    ON UPDATE CASCADE ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
  COMMENT='Detail buku per sesi peminjaman (maks 5 buku)';


-- ----------------------------------------------------------------
-- 5. TABEL PENGEMBALIAN
--    Header satu aksi pengembalian
--    Satu sesi peminjaman bisa menghasilkan BANYAK pengembalian
--    (jika buku dikembalikan satu per satu di waktu berbeda)
-- ----------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `pengembalian` (
  `id_pengembalian`   INT           NOT NULL AUTO_INCREMENT,
  `id_peminjaman`     INT           NOT NULL  COMMENT 'Sesi peminjaman asal',
  `id_pengguna`       INT           NOT NULL  COMMENT 'Pengguna yang mengembalikan',
  `tanggal_kembali`   DATE          NOT NULL  COMMENT 'Tanggal aksi pengembalian ini',
  `jenis`             ENUM('sebagian','semua')
                      NOT NULL DEFAULT 'semua'
                      COMMENT 'sebagian = tidak semua buku dikembalikan sekarang',
  `catatan`           TEXT              NULL  COMMENT 'Catatan petugas (opsional)',
  `dibuat_pada`       TIMESTAMP     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id_pengembalian`),
  INDEX `idx_kembali_pinjam`    (`id_peminjaman`),
  INDEX `idx_kembali_pengguna`  (`id_pengguna`),
  INDEX `idx_kembali_tanggal`   (`tanggal_kembali`),
  CONSTRAINT `fk_kembali_pinjam`
    FOREIGN KEY (`id_peminjaman`) REFERENCES `peminjaman`(`id_peminjaman`)
    ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT `fk_kembali_pengguna`
    FOREIGN KEY (`id_pengguna`) REFERENCES `pengguna`(`id_pengguna`)
    ON UPDATE CASCADE ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
  COMMENT='Header aksi pengembalian buku (bisa sebagian atau semua)';


-- ----------------------------------------------------------------
-- 6. TABEL DETAIL PENGEMBALIAN
--    Rincian buku mana saja yang dikembalikan dalam satu aksi
--    Terhubung ke detail_peminjaman untuk memastikan konsistensi
-- ----------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `detail_pengembalian` (
  `id_detail_kembali`     INT   NOT NULL AUTO_INCREMENT,
  `id_pengembalian`       INT   NOT NULL  COMMENT 'Referensi ke header pengembalian',
  `id_detail_pinjam`      INT   NOT NULL  COMMENT 'Referensi ke baris detail peminjaman',
  `id_buku`               INT   NOT NULL  COMMENT 'Buku yang dikembalikan',
  `tanggal_kembali_buku`  DATE  NOT NULL  COMMENT 'Tanggal buku ini fisik dikembalikan',
  `kondisi_buku`          ENUM('baik','rusak_ringan','rusak_berat','hilang')
                          NOT NULL DEFAULT 'baik'
                          COMMENT 'Kondisi buku saat dikembalikan',
  `keterangan_kondisi`    VARCHAR(255)  NULL  COMMENT 'Catatan kondisi jika tidak baik',
  PRIMARY KEY (`id_detail_kembali`),
  UNIQUE KEY `uq_kembali_detail_pinjam` (`id_pengembalian`, `id_detail_pinjam`)
    COMMENT 'Satu buku hanya dikembalikan sekali per aksi pengembalian',
  INDEX `idx_dkembali_kembali`      (`id_pengembalian`),
  INDEX `idx_dkembali_detail_pinjam`(`id_detail_pinjam`),
  INDEX `idx_dkembali_buku`         (`id_buku`),
  CONSTRAINT `fk_dkembali_kembali`
    FOREIGN KEY (`id_pengembalian`) REFERENCES `pengembalian`(`id_pengembalian`)
    ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT `fk_dkembali_detail_pinjam`
    FOREIGN KEY (`id_detail_pinjam`) REFERENCES `detail_peminjaman`(`id_detail_pinjam`)
    ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT `fk_dkembali_buku`
    FOREIGN KEY (`id_buku`) REFERENCES `buku`(`id_buku`)
    ON UPDATE CASCADE ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
  COMMENT='Detail buku yang dikembalikan dalam satu aksi pengembalian';


-- ----------------------------------------------------------------
-- 7. TABEL DENDA
--    Header denda — satu baris per sesi pengembalian yang terlambat
--    Denda bisa muncul di setiap aksi pengembalian (sebagian/semua)
-- ----------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `denda` (
  `id_denda`          INT           NOT NULL AUTO_INCREMENT,
  `id_pengembalian`   INT           NOT NULL  COMMENT 'Aksi pengembalian yang memicu denda',
  `id_pengguna`       INT           NOT NULL  COMMENT 'Pengguna yang dikenakan denda',
  `total_denda`       DECIMAL(12,2) NOT NULL DEFAULT 0.00
                      COMMENT 'Total denda = jumlah subtotal dari detail_denda',
  `status_pembayaran` ENUM('belum_lunas','lunas')
                      NOT NULL DEFAULT 'belum_lunas',
  `tanggal_bayar`     DATE              NULL  COMMENT 'Tanggal denda dilunasi',
  `dibuat_pada`       TIMESTAMP     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `diperbarui_pada`   TIMESTAMP     NOT NULL DEFAULT CURRENT_TIMESTAMP
                      ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id_denda`),
  UNIQUE KEY `uq_denda_pengembalian` (`id_pengembalian`)
    COMMENT 'Satu aksi pengembalian hanya menghasilkan satu header denda',
  INDEX `idx_denda_pengguna`         (`id_pengguna`),
  INDEX `idx_denda_status`           (`status_pembayaran`),
  CONSTRAINT `fk_denda_pengembalian`
    FOREIGN KEY (`id_pengembalian`) REFERENCES `pengembalian`(`id_pengembalian`)
    ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT `fk_denda_pengguna`
    FOREIGN KEY (`id_pengguna`) REFERENCES `pengguna`(`id_pengguna`)
    ON UPDATE CASCADE ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
  COMMENT='Header denda keterlambatan per aksi pengembalian';


-- ----------------------------------------------------------------
-- 8. TABEL DETAIL DENDA
--    Rincian denda PER BUKU yang terlambat
--    Tarif: Rp 2.000 × jumlah hari terlambat per buku
-- ----------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `detail_denda` (
  `id_detail_denda`       INT           NOT NULL AUTO_INCREMENT,
  `id_denda`              INT           NOT NULL  COMMENT 'Referensi ke header denda',
  `id_detail_kembali`     INT           NOT NULL  COMMENT 'Buku spesifik yang terlambat',
  `id_buku`               INT           NOT NULL  COMMENT 'Buku yang dikenakan denda',
  `kondisi_buku`          ENUM('baik','rusak_ringan','rusak_berat','hilang')
                          NOT NULL DEFAULT 'baik'
                          COMMENT 'Kondisi buku saat dikembalikan',
  `batas_kembali`         DATE          NOT NULL  COMMENT 'Tenggat dari sesi peminjaman asal',
  `tanggal_kembali_aktual`DATE          NOT NULL  COMMENT 'Tanggal buku ini benar-benar dikembalikan',
  `jumlah_hari_terlambat` INT           NOT NULL DEFAULT 0
                          COMMENT 'DATEDIFF(tanggal_kembali_aktual, batas_kembali)',
  `tarif_per_hari`        DECIMAL(12,2) NOT NULL DEFAULT 2000.00
                          COMMENT 'Rp 2.000 per hari (bisa berbeda jika tarif berubah)',
  `subtotal_denda`        DECIMAL(12,2) NOT NULL DEFAULT 0.00
                          COMMENT 'jumlah_hari_terlambat × tarif_per_hari (denda keterlambatan)',
  `denda_kondisi`         DECIMAL(12,2) NOT NULL DEFAULT 0.00
                          COMMENT 'Denda kondisi buku: rusak_ringan=25.000, rusak_berat=75.000, hilang=harga buku',
  PRIMARY KEY (`id_detail_denda`),
  UNIQUE KEY `uq_detail_denda_kembali` (`id_denda`, `id_detail_kembali`)
    COMMENT 'Satu buku hanya satu baris denda per header denda',
  INDEX `idx_ddenda_denda`         (`id_denda`),
  INDEX `idx_ddenda_detail_kembali`(`id_detail_kembali`),
  INDEX `idx_ddenda_buku`          (`id_buku`),
  CONSTRAINT `fk_ddenda_denda`
    FOREIGN KEY (`id_denda`) REFERENCES `denda`(`id_denda`)
    ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT `fk_ddenda_detail_kembali`
    FOREIGN KEY (`id_detail_kembali`) REFERENCES `detail_pengembalian`(`id_detail_kembali`)
    ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT `fk_ddenda_buku`
    FOREIGN KEY (`id_buku`) REFERENCES `buku`(`id_buku`)
    ON UPDATE CASCADE ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
  COMMENT='Detail denda per buku yang terlambat dikembalikan';


-- ================================================================
-- TRIGGER: Batas maks 5 buku per sesi peminjaman
-- ================================================================
DELIMITER $$

CREATE TRIGGER IF NOT EXISTS `trg_batas_5_buku`
BEFORE INSERT ON `detail_peminjaman`
FOR EACH ROW
BEGIN
  DECLARE v_jumlah INT;
  SELECT COUNT(*) INTO v_jumlah
    FROM `detail_peminjaman`
   WHERE `id_peminjaman` = NEW.`id_peminjaman`;
  IF v_jumlah >= 5 THEN
    SIGNAL SQLSTATE '45000'
      SET MESSAGE_TEXT = 'Satu sesi peminjaman tidak boleh melebihi 5 buku';
  END IF;
END$$


-- ================================================================
-- TRIGGER: Kurangi stok_tersedia saat detail peminjaman ditambah
-- ================================================================
CREATE TRIGGER IF NOT EXISTS `trg_kurangi_stok_pinjam`
AFTER INSERT ON `detail_peminjaman`
FOR EACH ROW
BEGIN
  UPDATE `buku`
     SET `stok_tersedia` = `stok_tersedia` - 1
   WHERE `id_buku` = NEW.`id_buku`;
END$$


-- ================================================================
-- TRIGGER: Kembalikan stok_tersedia saat detail pengembalian ditambah
--          Juga update status_buku di detail_peminjaman
-- ================================================================
CREATE TRIGGER IF NOT EXISTS `trg_kembalikan_stok`
AFTER INSERT ON `detail_pengembalian`
FOR EACH ROW
BEGIN
  -- Tambah kembali stok
  UPDATE `buku`
     SET `stok_tersedia` = `stok_tersedia` + 1
   WHERE `id_buku` = NEW.`id_buku`;

  -- Tandai buku sebagai dikembalikan di detail_peminjaman
  UPDATE `detail_peminjaman`
     SET `status_buku` = 'dikembalikan'
   WHERE `id_detail_pinjam` = NEW.`id_detail_pinjam`;
END$$


-- ================================================================
-- TRIGGER: Update status header peminjaman setelah pengembalian
--          aktif → sebagian_kembali → selesai / terlambat
-- ================================================================
CREATE TRIGGER IF NOT EXISTS `trg_update_status_pinjam`
AFTER UPDATE ON `detail_peminjaman`
FOR EACH ROW
BEGIN
  DECLARE v_total     INT;
  DECLARE v_kembali   INT;
  DECLARE v_id_pinjam INT;
  DECLARE v_batas     DATE;

  SET v_id_pinjam = NEW.`id_peminjaman`;

  SELECT COUNT(*)                                    INTO v_total
    FROM `detail_peminjaman` WHERE `id_peminjaman` = v_id_pinjam;

  SELECT COUNT(*)                                    INTO v_kembali
    FROM `detail_peminjaman`
   WHERE `id_peminjaman` = v_id_pinjam
     AND `status_buku`   = 'dikembalikan';

  SELECT `batas_kembali` INTO v_batas
    FROM `peminjaman` WHERE `id_peminjaman` = v_id_pinjam;

  IF v_kembali = v_total THEN
    -- Semua buku sudah kembali
    UPDATE `peminjaman`
       SET `status` = 'selesai'
     WHERE `id_peminjaman` = v_id_pinjam;
  ELSEIF v_kembali > 0 THEN
    -- Sebagian sudah kembali
    IF CURDATE() > v_batas THEN
      UPDATE `peminjaman`
         SET `status` = 'terlambat'
       WHERE `id_peminjaman` = v_id_pinjam;
    ELSE
      UPDATE `peminjaman`
         SET `status` = 'sebagian_kembali'
       WHERE `id_peminjaman` = v_id_pinjam;
    END IF;
  END IF;
END$$


-- ================================================================
-- STORED PROCEDURE: Proses Pengembalian (sebagian atau semua)
--
-- Parameter:
--   p_id_peminjaman  : sesi peminjaman asal
--   p_id_pengguna    : pengguna yang mengembalikan
--   p_ids_detail     : koma-separated id_detail_pinjam yang dikembalikan
--                      contoh: '1,2,3'
--   p_kondisi        : kondisi buku (baik/rusak_ringan/rusak_berat/hilang)
--   p_catatan        : catatan petugas
-- ================================================================
CREATE PROCEDURE IF NOT EXISTS `proses_pengembalian`(
  IN p_id_peminjaman  INT,
  IN p_id_pengguna    INT,
  IN p_catatan        TEXT
)
BEGIN
  -- Header pengembalian dibuat dari sisi aplikasi (C#)
  -- Procedure ini menghitung denda per buku setelah detail_pengembalian diisi
  -- Dipanggil setelah INSERT detail_pengembalian selesai

  DECLARE v_id_pengembalian INT;
  DECLARE v_batas_kembali   DATE;
  DECLARE v_total_denda     DECIMAL(12,2) DEFAULT 0;
  DECLARE v_id_denda        INT;

  -- Ambil pengembalian terakhir untuk sesi ini
  SELECT `id_pengembalian` INTO v_id_pengembalian
    FROM `pengembalian`
   WHERE `id_peminjaman` = p_id_peminjaman
   ORDER BY `dibuat_pada` DESC LIMIT 1;

  -- Ambil batas kembali sesi peminjaman
  SELECT `batas_kembali` INTO v_batas_kembali
    FROM `peminjaman`
   WHERE `id_peminjaman` = p_id_peminjaman;

  -- Hitung denda per buku yang baru dikembalikan dan terlambat
  -- Sisipkan ke detail_denda
  INSERT INTO `detail_denda`
    (`id_denda`,
     `id_detail_kembali`,
     `id_buku`,
     `batas_kembali`,
     `tanggal_kembali_aktual`,
     `jumlah_hari_terlambat`,
     `tarif_per_hari`,
     `subtotal_denda`)
  SELECT
    0,  -- id_denda akan diisi setelah INSERT header denda
    dk.`id_detail_kembali`,
    dk.`id_buku`,
    v_batas_kembali,
    dk.`tanggal_kembali_buku`,
    GREATEST(DATEDIFF(dk.`tanggal_kembali_buku`, v_batas_kembali), 0),
    2000.00,
    GREATEST(DATEDIFF(dk.`tanggal_kembali_buku`, v_batas_kembali), 0) * 2000.00
  FROM `detail_pengembalian` dk
  WHERE dk.`id_pengembalian` = v_id_pengembalian
    AND DATEDIFF(dk.`tanggal_kembali_buku`, v_batas_kembali) > 0;

  -- Hitung total denda aksi ini
  SELECT SUM(GREATEST(DATEDIFF(dk.`tanggal_kembali_buku`, v_batas_kembali), 0) * 2000.00)
    INTO v_total_denda
    FROM `detail_pengembalian` dk
   WHERE dk.`id_pengembalian` = v_id_pengembalian
     AND DATEDIFF(dk.`tanggal_kembali_buku`, v_batas_kembali) > 0;

  -- Jika ada denda, buat header denda dan update id_denda di detail
  IF v_total_denda > 0 THEN
    INSERT INTO `denda`
      (`id_pengembalian`, `id_pengguna`, `total_denda`, `status_pembayaran`)
    VALUES
      (v_id_pengembalian, p_id_pengguna, v_total_denda, 'belum_lunas');

    SET v_id_denda = LAST_INSERT_ID();

    -- Update id_denda yang tadi diisi 0
    UPDATE `detail_denda`
       SET `id_denda` = v_id_denda
     WHERE `id_detail_kembali` IN (
       SELECT `id_detail_kembali`
         FROM `detail_pengembalian`
        WHERE `id_pengembalian` = v_id_pengembalian
     ) AND `id_denda` = 0;
  END IF;
END$$


-- ================================================================
-- STORED PROCEDURE: Perbarui Status Pengguna
--   Blokir jika ada denda belum lunas, aktifkan kembali jika lunas
-- ================================================================
CREATE PROCEDURE IF NOT EXISTS `perbarui_status_pengguna`(
  IN p_id_pengguna INT
)
BEGIN
  DECLARE v_denda_aktif INT DEFAULT 0;

  SELECT COUNT(*) INTO v_denda_aktif
    FROM `denda`
   WHERE `id_pengguna`       = p_id_pengguna
     AND `status_pembayaran` = 'belum_lunas';

  IF v_denda_aktif > 0 THEN
    UPDATE `pengguna`
       SET `status` = 'diblokir'
     WHERE `id_pengguna` = p_id_pengguna
       AND `peran`        = 'pengguna';
  ELSE
    UPDATE `pengguna`
       SET `status` = 'aktif'
     WHERE `id_pengguna` = p_id_pengguna
       AND `peran`        = 'pengguna';
  END IF;
END$$


-- ================================================================
-- STORED PROCEDURE: Tandai Status Peminjaman Terlambat
--   Dijalankan rutin tiap aplikasi dibuka (cek semua yang melewati batas)
-- ================================================================
CREATE PROCEDURE IF NOT EXISTS `tandai_terlambat`()
BEGIN
  UPDATE `peminjaman`
     SET `status` = 'terlambat'
   WHERE `status` IN ('aktif', 'sebagian_kembali')
     AND `batas_kembali` < CURDATE();
END$$

DELIMITER ;


-- ================================================================
-- VIEW: Ringkasan peminjaman aktif (untuk dasbor admin)
-- ================================================================
CREATE OR REPLACE VIEW `v_peminjaman_aktif` AS
SELECT
  p.`id_peminjaman`,
  pg.`nama_lengkap`                          AS nama_peminjam,
  pg.`nama_pengguna`,
  COUNT(dp.`id_detail_pinjam`)               AS jumlah_buku,
  SUM(dp.`status_buku` = 'dikembalikan')     AS sudah_kembali,
  p.`tanggal_pinjam`,
  p.`batas_kembali`,
  GREATEST(DATEDIFF(CURDATE(), p.`batas_kembali`), 0) AS hari_terlambat,
  p.`status`
FROM `peminjaman`        p
JOIN `pengguna`          pg ON p.`id_pengguna`   = pg.`id_pengguna`
JOIN `detail_peminjaman` dp ON p.`id_peminjaman` = dp.`id_peminjaman`
WHERE p.`status` NOT IN ('selesai')
GROUP BY p.`id_peminjaman`;


-- ================================================================
-- VIEW: Detail riwayat peminjaman per pengguna (untuk halaman user)
-- ================================================================
CREATE OR REPLACE VIEW `v_riwayat_peminjaman` AS
SELECT
  dp.`id_detail_pinjam`,
  p.`id_peminjaman`,
  p.`id_pengguna`,
  b.`judul`,
  b.`penulis`,
  b.`kategori`,
  p.`tanggal_pinjam`,
  p.`batas_kembali`,
  dp.`status_buku`,
  dkb.`tanggal_kembali_buku`,
  dkb.`kondisi_buku`,
  GREATEST(DATEDIFF(
    COALESCE(dkb.`tanggal_kembali_buku`, CURDATE()),
    p.`batas_kembali`
  ), 0)                                      AS hari_terlambat,
  GREATEST(DATEDIFF(
    COALESCE(dkb.`tanggal_kembali_buku`, CURDATE()),
    p.`batas_kembali`
  ), 0) * 2000                               AS estimasi_denda
FROM `peminjaman`         p
JOIN `detail_peminjaman`  dp  ON p.`id_peminjaman`    = dp.`id_peminjaman`
JOIN `buku`               b   ON dp.`id_buku`         = b.`id_buku`
LEFT JOIN `detail_pengembalian` dkb ON dp.`id_detail_pinjam` = dkb.`id_detail_pinjam`;


-- ================================================================
-- VIEW: Ringkasan denda per pengguna (untuk laporan & halaman user)
-- ================================================================
CREATE OR REPLACE VIEW `v_denda_pengguna` AS
SELECT
  pg.`id_pengguna`,
  pg.`nama_lengkap`,
  pg.`nama_pengguna`,
  pg.`status`                                           AS status_akun,
  COUNT(d.`id_denda`)                                   AS jumlah_tagihan_denda,
  SUM(CASE WHEN d.`status_pembayaran` = 'belum_lunas'
           THEN d.`total_denda` ELSE 0 END)             AS total_belum_lunas,
  SUM(COALESCE(d.`total_denda`, 0))                     AS total_semua_denda
FROM `pengguna` pg
LEFT JOIN `denda` d ON pg.`id_pengguna` = d.`id_pengguna`
WHERE pg.`peran` = 'pengguna'
GROUP BY pg.`id_pengguna`;


-- ================================================================
-- VIEW: Detail denda per buku (untuk halaman detail denda user/admin)
-- ================================================================
CREATE OR REPLACE VIEW `v_detail_denda_buku` AS
SELECT
  dd.`id_detail_denda`,
  d.`id_denda`,
  d.`id_pengguna`,
  pg.`nama_lengkap`                            AS nama_peminjam,
  b.`judul`                                    AS judul_buku,
  b.`penulis`,
  p.`id_peminjaman`,
  p.`tanggal_pinjam`,
  dd.`kondisi_buku`,
  dd.`batas_kembali`,
  dd.`tanggal_kembali_aktual`,
  dd.`jumlah_hari_terlambat`,
  dd.`tarif_per_hari`,
  dd.`subtotal_denda`,
  dd.`denda_kondisi`,
  (dd.`subtotal_denda` + dd.`denda_kondisi`)   AS total_per_buku,
  d.`status_pembayaran`,
  d.`tanggal_bayar`
FROM `detail_denda`     dd
JOIN `denda`            d   ON dd.`id_denda`          = d.`id_denda`
JOIN `pengguna`         pg  ON d.`id_pengguna`         = pg.`id_pengguna`
JOIN `buku`             b   ON dd.`id_buku`            = b.`id_buku`
JOIN `pengembalian`     k   ON d.`id_pengembalian`     = k.`id_pengembalian`
JOIN `peminjaman`       p   ON k.`id_peminjaman`       = p.`id_peminjaman`;


-- ================================================================
-- SELESAI
-- Cara menjalankan:
--   1. Buka phpMyAdmin → tab SQL → paste seluruh isi file ini → klik Go
--   2. Atau via terminal: mysql -u root -p < skema_database_perpustakaan_v2.sql
--
-- Akun admin bawaan:
--   Username : admin
--   Password : Admin@123
-- ================================================================
