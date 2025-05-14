using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PlagiarismGuard.Windows
{
    /// <summary>
    /// Логика взаимодействия для CustomMessageBox.xaml
    /// </summary>
    public partial class CustomMessageBox : Window
    {
        public enum MessageType { Success, Error, Warning, Information }
        public string Message { get; set; }
        public string Title { get; set; }
        public MessageType Type { get; set; }

        public CustomMessageBox(string message, string title, MessageType type, Window owner = null)
        {
            InitializeComponent();
            Owner = owner ?? Application.Current.MainWindow; // Используем активное окно, если owner не указан
            Message = message;
            TitleText.Text = title;
            Type = type;
            DataContext = this;
            SetIcon();
        }

        private void SetIcon()
        {
            string iconPath = Type switch
            {
                MessageType.Success => "pack://application:,,,/Image/Success.png",
                MessageType.Error => "pack://application:,,,/Image/Error.png",
                MessageType.Warning => "pack://application:,,,/Image/Warning.png",
                MessageType.Information => "pack://application:,,,/Image/Info.png",
                _ => "pack://application:,,,/Image/Info.png"
            };
            IconImage.Source = new BitmapImage(new Uri(iconPath));
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        public static void Show(string message, string title, MessageType type, Window owner = null)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    var messageBox = new CustomMessageBox(message, title, type, owner);
                    messageBox.ShowDialog();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка в CustomMessageBox.Show: {ex.Message}");
                }
            });
        }
    }
}
