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

        public CustomMessageBox(Window owner, string message, string title, MessageType type)
        {
            InitializeComponent();
            Owner = owner;
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
                _ => "pack://application:,,,/Images/Info.png"
            };
            IconImage.Source = new BitmapImage(new Uri(iconPath));
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        public static void Show(Window owner, string message, string title, MessageType type)
        {
            var messageBox = new CustomMessageBox(owner, message, title, type);
            messageBox.ShowDialog();
        }
    }
}
