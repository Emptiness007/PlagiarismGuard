using PlagiarismGuard.Services;
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
using System.Windows.Threading;

namespace PlagiarismGuard.Windows
{
    /// <summary>
    /// Логика взаимодействия для Confirmation.xaml
    /// </summary>
    public partial class Confirmation : Window
    {
        private readonly string _email;
        private string _correctCode;
        private DispatcherTimer _timer;
        private int _secondsRemaining;

        public Confirmation(string email, string correctCode)
        {
            InitializeComponent();
            _email = email;
            _correctCode = correctCode;
            EmailDisplay.Text = email;

            _secondsRemaining = 60;
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            _secondsRemaining--;
            if (_secondsRemaining > 0)
            {
                TimerText.Text = $"Повторная отправка через {_secondsRemaining} сек.";
            }
            else
            {
                TimerText.Text = "Можно отправить код повторно";
                ResendButton.IsEnabled = true;
                _timer.Stop();
            }
        }

        private void ResendButton_Click(object sender, RoutedEventArgs e)
        {
            _correctCode = new Random().Next(100000, 999999).ToString();
            ConfirmButton.IsEnabled = false;
            ResendButton.IsEnabled = false;

            try
            {
                SendMail.SendMessage($"Ваш новый код подтверждения: {_correctCode}", _email);
                ErrorMessage.Text = "Новый код отправлен";
                ErrorMessage.Foreground = new SolidColorBrush(Colors.Green);
                ErrorMessage.Visibility = Visibility.Visible;

                _secondsRemaining = 60;
                TimerText.Text = $"Повторная отправка через {_secondsRemaining} сек.";
                _timer.Start();
            }
            catch (System.Net.Mail.SmtpException)
            {
                ErrorMessage.Text = "Ошибка при отправке кода";
                ErrorMessage.Foreground = new SolidColorBrush(Colors.Red);
                ErrorMessage.Visibility = Visibility.Visible;
                ResendButton.IsEnabled = true;
            }
            finally
            {
                ConfirmButton.IsEnabled = true;
            }
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            string enteredCode = CodeInput.Text.Trim();

            if (string.IsNullOrEmpty(enteredCode))
            {
                ErrorMessage.Text = "Введите код";
                ErrorMessage.Foreground = new SolidColorBrush(Colors.Red);
                ErrorMessage.Visibility = Visibility.Visible;
                return;
            }

            if (enteredCode == _correctCode)
            {
                DialogResult = true;
                Close();
            }
            else
            {
                ErrorMessage.Text = "Неверный код";
                ErrorMessage.Foreground = new SolidColorBrush(Colors.Red);
                ErrorMessage.Visibility = Visibility.Visible;
            }
        }
    }
}
