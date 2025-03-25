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
        public MainWindow()
        {
            InitializeComponent();

            // Создаем коллекцию данных
            var sources = new ObservableCollection<Source>
            {
                new Source
                {
                    SourceNo = 1,
                    SourceName = "Source A",
                    Excerpt = "This is an example excerpt from the source.",
                    Similarity = 20, // Процент сходства
                }
            };

            // Привязываем данные к DataGrid
            SourceDataGrid.ItemsSource = sources;
        }

        // Обработчик кнопки "View Report"
        private void ViewReport_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("View Report clicked!");
        }
    }

    // Класс для представления данных
    public class Source
    {
        public int SourceNo { get; set; }
        public string SourceName { get; set; }
        public string Excerpt { get; set; }
        public double Similarity { get; set; }
        public string Url { get; set; }
    }
}