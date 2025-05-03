using PlagiarismGuard.Models;
using PlagiarismGuard.Services;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace PlagiarismGuard.Windows
{
    public partial class AddUserWindow : Window
    {
        public string Username { get; private set; }
        public string Email { get; private set; }
        public string Role { get; private set; }
        public string PasswordHash { get; private set; }
        public string GeneratedPassword { get; private set; }

        public AddUserWindow()
        {
            InitializeComponent();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text.Trim();
            string email = EmailTextBox.Text.Trim();
            string role = (RoleComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(role))
            {
                ErrorMessage.Text = "Заполните все поля!";
                ErrorMessage.Visibility = Visibility.Visible;
                return;
            }
            if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                ErrorMessage.Text = "Некорректный формат email";
                ErrorMessage.Visibility = Visibility.Visible;
                return;
            }

            var user = new User();
            GeneratedPassword = user.GeneratePass();
            PasswordHash = PasswordHelper.HashPassword(GeneratedPassword);

            Username = username;
            Email = email;
            Role = role;

            DialogResult = true; 
            Close();
        }

        private void CancelLink_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false; 
            Close();
        }

    }
}