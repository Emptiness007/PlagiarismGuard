using PlagiarismGuard.Data;
using PlagiarismGuard.Models;
using PlagiarismGuard.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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
    /// Логика взаимодействия для Registration.xaml
    /// </summary>
    public partial class Registration : Window
    {
        private readonly PlagiarismContext _context;
        private string _confirmationCode;
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
            Regex passwordRegex = new Regex(@"(?=.*[0-9])(?=.*[!@#$%^&?*\-_=])(?=.*[a-z])(?=.*[A-Z])[0-9a-zA-Z!@#$%^&?*\-_=]{8,}");
            if (!passwordRegex.IsMatch(password))
            {
                ErrorMessage.Text = "Пароль не соответствует требованиям";
                ErrorMessage.Visibility = Visibility.Visible;
                return;
            }

            if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                ErrorMessage.Text = "Некорректный формат email";
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

            _confirmationCode = new Random().Next(100000, 999999).ToString();

            try
            {
                SendMail.SendMessage($"Ваш код подтверждения: {_confirmationCode}", email);
            }
            catch (System.Net.Mail.SmtpException)
            {
                ErrorMessage.Text = "Email не существует или недоступен";
                ErrorMessage.Visibility = Visibility.Visible;
                return;
            }

            var confirmationWindow = new Confirmation(email, _confirmationCode);
            if (confirmationWindow.ShowDialog() == true)
            {
                string passwordHash = PasswordHelper.HashPassword(password);
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

                CustomMessageBox.Show(Window.GetWindow(this), "Регистрация успешна! Теперь вы можете войти.", "Успех", MessageType.Information);

                var authWindow = new Authorization();
                authWindow.Show();
                Close();
            }
            else
            {
                ErrorMessage.Text = "Подтверждение email не выполнено";
                ErrorMessage.Visibility = Visibility.Visible;
            }
        }

        private void LoginLink_Click(object sender, MouseButtonEventArgs e)
        {
            var authWindow = new Windows.Authorization();
            authWindow.Show();
            Close();
        }
    }
}
