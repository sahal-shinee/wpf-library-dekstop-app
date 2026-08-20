using System.Windows;

namespace SistemPerpustakaan.Tampilan.Komponen
{
    public partial class KotakDialog : Window
    {
        public bool Dikonfirmasi { get; private set; }

        public KotakDialog(string judul, string pesan,
            string ikon = "❓",
            string labelOk = "Ya, Lanjutkan",
            string labelBatal = "Batal")
        {
            InitializeComponent();
            TeksIkon.Text    = ikon;
            TeksJudul.Text   = judul;
            TeksPesan.Text   = pesan;
            TombolOk.Content = labelOk;
            TombolBatal.Content = labelBatal;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            Dikonfirmasi  = true;
            DialogResult  = true;
            Close();
        }
        private void Batal_Click(object sender, RoutedEventArgs e)
        {
            Dikonfirmasi  = false;
            DialogResult  = false;
            Close();
        }

        // Buka dan kembalikan hasil
        public static bool Tanya(string judul, string pesan,
            string ikon = "❓", string labelOk = "Ya, Lanjutkan")
        {
            var dialog = new KotakDialog(judul, pesan, ikon, labelOk);
            dialog.ShowDialog();
            return dialog.Dikonfirmasi;
        }
    }
}
