using System.Windows;
using System.Windows.Controls;

namespace SistemPerpustakaan.Tampilan.Komponen
{
    public partial class PanelMuat : UserControl
    {
        public PanelMuat() { InitializeComponent(); }

        public void Tampilkan(string pesan = "Memuat...")
        {
            TeksMuat.Text = pesan;
            Visibility    = Visibility.Visible;
        }
        public void Sembunyikan() => Visibility = Visibility.Collapsed;
    }
}
