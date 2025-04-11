using PlagiarismGuard.Data;
using PlagiarismGuard.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
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
    /// Логика взаимодействия для Registration.xaml
    /// </summary>
    public partial class Registration : Window
    {
        private readonly PlagiarismContext _context;
        public Registration()
        {
            InitializeComponent();
            _context = new PlagiarismContext();
        }
        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            string username = Username.Text.Trim();
            string email = Email.Text.Trim();
            string password = Password.Password;
            string confirmPassword = ConfirmPassword.Password;
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) ||
                string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirmPassword))
            {
                ErrorMessage.Text = "Заполните все поля";
                ErrorMessage.Visibility = Visibility.Visible;
                return;
            }

            if (password != confirmPassword)
            {
                ErrorMessage.Text = "Пароли не совпадают";
                ErrorMessage.Visibility = Visibility.Visible;
                return;
            }

            if (_context.Users.Any(u => u.Username == username))
            {
                ErrorMessage.Text = "Пользователь с таким логином уже существует";
                ErrorMessage.Visibility = Visibility.Visible;
                return;
            }

            if (_context.Users.Any(u => u.Email == email))
            {
                ErrorMessage.Text = "Email уже зарегистрирован";
                ErrorMessage.Visibility = Visibility.Visible;
                return;
            }

            string passwordHash = HashPassword(password);

            var newUser = new User
            {
                Username = username,
                Email = email,
                PasswordHash = passwordHash,
                Role = "user",
                CreatedAt = DateTime.Now
            };

            _context.Users.Add(newUser);
            _context.SaveChanges();

            MessageBox.Show("Регистрация успешна! Теперь вы можете войти.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

            var authWindow = new Windows.Authorization();
            authWindow.Show();
            Close();
        }

        private void LoginLink_Click(object sender, MouseButtonEventArgs e)
        {
            var authWindow = new Windows.Authorization();
            authWindow.Show();
            Close();
        }

        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}
