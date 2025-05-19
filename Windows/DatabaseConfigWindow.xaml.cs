using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static PlagiarismGuard.Windows.CustomMessageBox;

namespace PlagiarismGuard.Windows
{
    /// <summary>
    /// Логика взаимодействия для DatabaseConfigWindow.xaml
    /// </summary>
    public partial class DatabaseConfigWindow : Window
    {
        public string ConnectionString { get; private set; }

        public DatabaseConfigWindow()
        {
            InitializeComponent();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ServerTextBox.Text) ||
                string.IsNullOrWhiteSpace(DatabaseTextBox.Text) ||
                string.IsNullOrWhiteSpace(PortTextBox.Text) ||
                string.IsNullOrWhiteSpace(UsernameTextBox.Text))
            {
                CustomMessageBox.Show("Заполните все обязательные поля!", "Ошибка", MessageType.Error, Window.GetWindow(this));
                return;
            }

            ConnectionString = $"Server={ServerTextBox.Text};Database={DatabaseTextBox.Text};Port={PortTextBox.Text};User={UsernameTextBox.Text};Password={PasswordBox.Password};";
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
