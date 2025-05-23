﻿using PlagiarismGuard.Data;
using PlagiarismGuard.Models;
using PlagiarismGuard.Services;
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
    /// Логика взаимодействия для Authorization.xaml
    /// </summary>
    public partial class Authorization : Window
    {
        private readonly PlagiarismContext _context;
        public Authorization()
        {
            InitializeComponent();
            _context = new PlagiarismContext();
        }
        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string username = Username.Text.Trim();
            string password = Password.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ErrorMessage.Text = "Заполните все поля";
                ErrorMessage.Visibility = Visibility.Visible;
                return;
            }

            var user = _context.Users.FirstOrDefault(u => u.Username == username);
            if (user == null || !VerifyPassword(password, user.PasswordHash))
            {
                ErrorMessage.Text = "Неверный логин или пароль";
                ErrorMessage.Visibility = Visibility.Visible;
                return;
            }

            CurrentUser.Instance = new CurrentUser { Id = user.Id, Username = user.Username, Role = user.Role };

            var mainWindow = new MainWindow(user.Id);
            mainWindow.Show();
            Close();
        }

        private void RegisterLink_Click(object sender, MouseButtonEventArgs e)
        {
            var registerWindow = new Windows.Registration();
            registerWindow.Show();
            Close();
        }


        private bool VerifyPassword(string password, string hashedPassword)
        {
            string hashedInput = PasswordHelper.HashPassword(password);
            return hashedInput == hashedPassword;
        }

        private void ForgotPasswordLink_Click(object sender, MouseButtonEventArgs e)
        {
            var forgotPasswordWindow = new ForgotPassword(_context);
            forgotPasswordWindow.ShowDialog();
        }
    }
}
