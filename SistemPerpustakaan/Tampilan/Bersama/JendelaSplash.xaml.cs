using System.Windows;
namespace SistemPerpustakaan.Tampilan.Bersama
{
    public partial class JendelaSplash : Window
    {
        public JendelaSplash() { InitializeComponent(); }
        public void PerbaruiPesan(string pesan) { TeksPesan.Text = pesan; }
    }
}
