﻿using PlagiarismGuard.Data;
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
using static PlagiarismGuard.Windows.CustomMessageBox;

namespace PlagiarismGuard.Windows
{
    /// <summary>
    /// Логика взаимодействия для ForgotPassword.xaml
    /// </summary>
    public partial class ForgotPassword : Window
    {
        private readonly PlagiarismContext _context;
        private string _confirmationCode;

        public ForgotPassword(PlagiarismContext context)
        {
            InitializeComponent();
            _context = context;
        }

        private void ResetPasswordButton_Click(object sender, RoutedEventArgs e)
        {
            string email = EmailInput.Text.Trim();

            if (string.IsNullOrEmpty(email))
            {
                ErrorMessage.Text = "Введите email";
                ErrorMessage.Visibility = Visibility.Visible;
                return;
            }

            var user = _context.Users.FirstOrDefault(u => u.Email == email);
            if (user == null)
            {
                ErrorMessage.Text = "Пользователь с таким email не найден";
                ErrorMessage.Visibility = Visibility.Visible;
                return;
            }

            _confirmationCode = new Random().Next(100000, 999999).ToString();

            try
            {
                SendMail.SendMessage($"Код для восстановления пароля: {_confirmationCode}", email);
            }
            catch (System.Net.Mail.SmtpException)
            {
                ErrorMessage.Text = "Ошибка при отправке кода";
                ErrorMessage.Visibility = Visibility.Visible;
                return;
            }

            var confirmationWindow = new Confirmation(email, _confirmationCode);
            if (confirmationWindow.ShowDialog() == true)
            {
                string newPassword = user.GeneratePass();
                string newPasswordHash = PasswordHelper.HashPassword(newPassword);

                user.PasswordHash = newPasswordHash;
                _context.SaveChanges();

                try
                {
                    SendMail.SendMessage($"Ваш новый временный пароль: {newPassword}\nПожалуйста, смените его после входа в систему.", email);
                    CustomMessageBox.Show("Новый пароль отправлен на ваш email. Используйте его для входа.", "Успех", MessageType.Information, Window.GetWindow(this));
                    Close();
                }
                catch (System.Net.Mail.SmtpException)
                {
                    ErrorMessage.Text = "Пароль сброшен, но не удалось отправить его на email. Обратитесь в поддержку.";
                    ErrorMessage.Visibility = Visibility.Visible;
                }
            }
            else
            {
                ErrorMessage.Text = "Подтверждение email не выполнено";
                ErrorMessage.Visibility = Visibility.Visible;
            }
        }

        private void BackToLogin_Click(object sender, MouseButtonEventArgs e)
        {
            var authWindow = new Authorization();
            authWindow.Show();
            Close();
        }
    }
}
