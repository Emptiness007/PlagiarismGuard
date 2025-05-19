using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
            ErrorMessage.Visibility = Visibility.Collapsed;
            ErrorMessage.Text = "";

            if (string.IsNullOrWhiteSpace(ServerTextBox.Text) ||
                string.IsNullOrWhiteSpace(DatabaseTextBox.Text) ||
                string.IsNullOrWhiteSpace(PortTextBox.Text) ||
                string.IsNullOrWhiteSpace(UsernameTextBox.Text))
            {
                ErrorMessage.Text = "Заполните все обязательные поля";
                ErrorMessage.Visibility = Visibility.Visible;
                return;
            }

            if (!Regex.IsMatch(ServerTextBox.Text, @"^[\w\.\-]+$"))
            {
                ErrorMessage.Text = "Недопустимый формат сервера. Используйте IP, localhost или доменное имя.";
                ErrorMessage.Visibility = Visibility.Visible;
                return;
            }

            if (!Regex.IsMatch(DatabaseTextBox.Text, @"^[\w]+$"))
            {
                ErrorMessage.Text = "Имя базы данных может содержать только буквы, цифры и подчеркивания.";
                ErrorMessage.Visibility = Visibility.Visible;
                return;
            }

            if (!Regex.IsMatch(PortTextBox.Text, @"^([1-9][0-9]{0,3}|[1-5][0-9]{4}|6[0-4][0-9]{3}|65[0-4][0-9]{2}|655[0-2][0-9]|6553[0-5])$"))
            {
                ErrorMessage.Text = "Порт должен быть числом от 1 до 65535.";
                ErrorMessage.Visibility = Visibility.Visible;
                return;
            }

            if (!Regex.IsMatch(UsernameTextBox.Text, @"^[\w]+$"))
            {
                ErrorMessage.Text = "Имя пользователя может содержать только буквы, цифры и подчеркивания.";
                ErrorMessage.Visibility = Visibility.Visible;
                return;
            }

            string connectionString = $"Server={ServerTextBox.Text};Database={DatabaseTextBox.Text};Port={PortTextBox.Text};User={UsernameTextBox.Text};Password={PasswordBox.Password};";

            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                }
            }
            catch (MySqlException ex)
            {
                ErrorMessage.Text = $"Не удалось подключиться к базе данных: {ex.Message}";
                ErrorMessage.Visibility = Visibility.Visible;
                return;
            }
            catch (Exception ex)
            {
                ErrorMessage.Text = $"Произошла ошибка: {ex.Message}";
                ErrorMessage.Visibility = Visibility.Visible;
                return;
            }

            ConnectionString = connectionString;
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
