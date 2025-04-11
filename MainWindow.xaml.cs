using PlagiarismGuard.Data;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PlagiarismGuard
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly PlagiarismContext _context;

        public MainWindow(PlagiarismContext context)
        {
            InitializeComponent();
            _context = context;
        }

        private void ViewReport_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("отчетик готов!");
        }
    }
}