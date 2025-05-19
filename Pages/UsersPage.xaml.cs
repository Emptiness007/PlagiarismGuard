using PlagiarismGuard.Data;
using PlagiarismGuard.Models;
using PlagiarismGuard.Windows;
using System;
using System.DirectoryServices;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using static PlagiarismGuard.Windows.CustomMessageBox;

namespace PlagiarismGuard.Pages
{
    public partial class UsersPage : Page
    {
        private readonly PlagiarismContext _context;
        private string _searchQuery = string.Empty;
        private int _sortOption = 0;

        public UsersPage(PlagiarismContext context)
        {
            InitializeComponent();
            _context = context;

            LoadUsers();
        }

        private void LoadUsers()
        {
            var query = _context.Users.AsQueryable();

            // Поиск по логину или почте
            if (!string.IsNullOrWhiteSpace(_searchQuery))
            {
                query = query.Where(u => u.Username.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase) ||
                                         u.Email.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase));
            }

            // Сортировка по дате создания
            query = _sortOption switch
            {
                1 => query.OrderByDescending(u => u.CreatedAt),
                2 => query.OrderBy(u => u.CreatedAt),
                _ => query
            };

            UsersDataGrid.ItemsSource = query.ToList();
        }

        private void AddUserButton_Click(object sender, RoutedEventArgs e)
        {
            var addUserWindow = new AddUserWindow();
            if (addUserWindow.ShowDialog() == true)
            {
                try
                {
                    if (_context.Users.Any(u => u.Username == addUserWindow.Username))
                    {
                        CustomMessageBox.Show( "Пользователь с таким именем уже существует!", "Ошибка", MessageType.Error, Window.GetWindow(this));
                        return;
                    }
                    if (_context.Users.Any(u => u.Email == addUserWindow.Email))
                    {
                        CustomMessageBox.Show( "Пользователь с таким email уже существует!", "Ошибка", MessageType.Error, Window.GetWindow(this));
                        return;
                    }

                    var user = new User
                    {
                        Username = addUserWindow.Username,
                        Email = addUserWindow.Email,
                        Role = addUserWindow.Role,
                        PasswordHash = addUserWindow.PasswordHash,
                        CreatedAt = DateTime.Now
                    };
                    _context.Users.Add(user);
                    _context.SaveChanges();

                    CustomMessageBox.Show($"Пользователь успешно добавлен!\nЛогин: {addUserWindow.Username}\nПароль: {addUserWindow.GeneratedPassword}", "Успех", MessageType.Information, Window.GetWindow(this));
                    LoadUsers();
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show($"Ошибка при добавлении пользователя: {ex.Message}", "Ошибка", MessageType.Error, Window.GetWindow(this));
                }
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentUser.Instance.Role != "admin")
            {
                CustomMessageBox.Show("Только администратор может удалять пользователей!", "Ошибка", MessageType.Error, Window.GetWindow(this));
                return;
            }

            var button = sender as Button;
            if (button != null)
            {
                int userId = (int)button.Tag;
                var user = _context.Users.FirstOrDefault(u => u.Id == userId);

                if (user != null)
                {
                    if (user.Id == CurrentUser.Instance.Id)
                    {
                        CustomMessageBox.Show( "Вы не можете удалить самого себя!", "Ошибка", MessageType.Error, Window.GetWindow(this));
                        return;
                    }

                    var userDocuments = _context.Documents.Where(d => d.UserId == userId).ToList();
                    foreach (var document in userDocuments)
                    {
                        var documentText = _context.DocumentTexts.FirstOrDefault(dt => dt.DocumentId == document.Id);
                        if (documentText != null)
                        {
                            _context.DocumentTexts.Remove(documentText);
                        }

                        var checks = _context.Checks.Where(c => c.DocumentId == document.Id).ToList();
                        foreach (var check in checks)
                        {
                            var checkResults = _context.CheckResults.Where(cr => cr.CheckId == check.Id).ToList();
                            _context.CheckResults.RemoveRange(checkResults);
                            _context.Checks.Remove(check);
                        }


                        _context.Documents.Remove(document);
                    }

                    _context.Users.Remove(user);
                    _context.SaveChanges();

                    CustomMessageBox.Show( "Пользователь успешно удален!", "Успех", MessageType.Information, Window.GetWindow(this));
                    LoadUsers();
                }
            }
        }
        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _searchQuery = SearchTextBox.Text.Trim();
            LoadUsers();
        }

        private void SortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _sortOption = SortComboBox.SelectedIndex;
            LoadUsers();
        }
    }
}