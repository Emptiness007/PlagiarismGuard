using PlagiarismGuard.Data;
using PlagiarismGuard.Models;
using PlagiarismGuard.Pages;
using PlagiarismGuard.Services;
using PlagiarismGuard.Windows;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace PlagiarismGuard
{
    public partial class MainWindow : Window
    {
        private readonly PlagiarismContext _context;
        private readonly TextExtractorService _textExtractor;
        private readonly PlagiarismCheckService _plagiarismChecker;
        private bool isSidebarOpen = false;

        public MainWindow(int userId)
        {
            InitializeComponent();
            _context = new PlagiarismContext();
            _textExtractor = new TextExtractorService();
            _plagiarismChecker = new PlagiarismCheckService(_context);
            CurrentUser.Instance.Id = userId;
            LoadUserData();
            ConfigureUIForRole();
            InitializeNavigation();
        }

        private void InitializeNavigation()
        {
            MainFrame.Navigate(new CheckPage(_context, _textExtractor, _plagiarismChecker));
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
                ImportButton.Visibility = Visibility.Collapsed;
            }
            else
            {
                UsersText.Visibility = Visibility.Collapsed;
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

            CheckPageText.Tag = "Active";
            MainFrame.Navigate(new CheckPage(_context, _textExtractor, _plagiarismChecker));
            isSidebarOpen = false;
            Sidebar.Visibility = Visibility.Collapsed;
        }

        private void HistoryButton_Click(object sender, RoutedEventArgs e)
        {
            //MainFrame.Navigate(new HistoryPage());
            //isSidebarOpen = false;
            //Sidebar.Visibility = Visibility.Collapsed;
        }

        private void DocumentsButton_Click(object sender, RoutedEventArgs e)
        {
            HistoryText.Tag = null;
            CheckPageText.Tag = null;
            UsersText.Tag = null;

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

            UsersText.Tag = "Active";
            MainFrame.Navigate(new UsersPage(_context));
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