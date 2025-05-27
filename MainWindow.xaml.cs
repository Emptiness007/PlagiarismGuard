using PlagiarismGuard.Data;
using PlagiarismGuard.Models;
using PlagiarismGuard.Pages;
using PlagiarismGuard.Services;
using PlagiarismGuard.Windows;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using static PlagiarismGuard.Windows.CustomMessageBox;

namespace PlagiarismGuard
{
    public partial class MainWindow : Window
    {
        private readonly PlagiarismContext _context;
        private readonly TextExtractorService _textExtractor;
        private readonly PlagiarismCheckService _plagiarismChecker;
        private readonly ReportGeneratorService _reportGeneratorService;
        private bool isSidebarOpen = false;

        public MainWindow(int userId)
        {
            InitializeComponent();
            _context = new PlagiarismContext();
            _textExtractor = new TextExtractorService();
            _plagiarismChecker = new PlagiarismCheckService(_context);
            _reportGeneratorService = new ReportGeneratorService(_context);
            ImportButton.Visibility = Visibility.Collapsed;
            CurrentUser.Instance.Id = userId;
            LoadUserData();
            ConfigureUIForRole();
        }

        private void LoadUserData()
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == CurrentUser.Instance.Id);
            if (user != null)
            {
                UsernameText.Text = $"Добро пожаловать, {user.Username}";
                CurrentUser.Instance.Role = user.Role;
            }
        }

        private void ConfigureUIForRole()
        {
            if (CurrentUser.Instance.Role == "admin")
            {
                UsersText.Visibility = Visibility.Visible;
                DatabaseText.Visibility = Visibility.Visible;
                ImportButton.Visibility = Visibility.Collapsed;
                CheckPageText.Visibility = Visibility.Collapsed;
            }
            else
            {
                UsersText.Visibility = Visibility.Collapsed;
                DatabaseText.Visibility = Visibility.Collapsed;
            }
        }

        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            isSidebarOpen = !isSidebarOpen;
            Sidebar.Visibility = isSidebarOpen ? Visibility.Visible : Visibility.Collapsed;
        }

        private void CheckPageButton_Click(object sender, RoutedEventArgs e)
        {
            HistoryText.Tag = null;
            DocumentsText.Tag = null;
            UsersText.Tag = null;
            DatabaseText.Tag = null;
            ImportButton.Visibility = Visibility.Visible;

            CheckPageText.Tag = "Active";
            MainFrame.Navigate(new CheckPage(_context, _textExtractor, _plagiarismChecker, _reportGeneratorService));
            isSidebarOpen = false;
            Sidebar.Visibility = Visibility.Collapsed;
        }

        private void HistoryButton_Click(object sender, RoutedEventArgs e)
        {
            CheckPageText.Tag = null;
            DocumentsText.Tag = null;
            UsersText.Tag = null;
            DatabaseText.Tag = null;

            HistoryText.Tag = "Active";
            MainFrame.Navigate(new HistoryPage(_context, _reportGeneratorService));
            isSidebarOpen = false;
            Sidebar.Visibility = Visibility.Collapsed;
        }

        private void DocumentsButton_Click(object sender, RoutedEventArgs e)
        {
            HistoryText.Tag = null;
            CheckPageText.Tag = null;
            UsersText.Tag = null;
            DatabaseText.Tag = null;

            DocumentsText.Tag = "Active";
            MainFrame.Navigate(new DocumentsPage(_context, _textExtractor, _plagiarismChecker));
            isSidebarOpen = false;
            Sidebar.Visibility = Visibility.Collapsed;
        }

        private void UsersButton_Click(object sender, RoutedEventArgs e)
        {
            HistoryText.Tag = null;
            CheckPageText.Tag = null;
            DocumentsText.Tag = null;
            DatabaseText.Tag = null;

            UsersText.Tag = "Active";
            MainFrame.Navigate(new UsersPage(_context));
            isSidebarOpen = false;
            Sidebar.Visibility = Visibility.Collapsed;
        }

        private void DatabaseButton_Click(object sender, RoutedEventArgs e)
        {
            HistoryText.Tag = null;
            CheckPageText.Tag = null;
            DocumentsText.Tag = null;
            UsersText.Tag = null;
            DatabaseText.Tag = "Active";

            var configWindow = new DatabaseConfigWindow();
            if (configWindow.ShowDialog() == true)
            {
                try
                {
                    ConfigurationManager.SaveConnectionString(configWindow.ConnectionString);
                    CustomMessageBox.Show("Настройки базы данных успешно сохранены! Перезапустите приложение для применения изменений.", "Успех", MessageType.Information, Window.GetWindow(this));
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show($"Ошибка при сохранении настроек: {ex.Message}", "Ошибка", MessageType.Error, Window.GetWindow(this));
                }
            }

            isSidebarOpen = false;
            Sidebar.Visibility = Visibility.Collapsed;
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            var authWindow = new Authorization();
            authWindow.Show();
            Close();
        }

        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            if (MainFrame.Content is CheckPage checkPage)
            {
                checkPage.ImportDocument();
            }
        }
    }
}