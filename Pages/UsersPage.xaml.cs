using PlagiarismGuard.Data;
using PlagiarismGuard.Models;
using PlagiarismGuard.Windows;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace PlagiarismGuard.Pages
{
    public partial class UsersPage : Page
    {
        private readonly PlagiarismContext _context;

        public UsersPage(PlagiarismContext context)
        {
            InitializeComponent();
            _context = context;

            LoadUsers();
        }

        private void LoadUsers()
        {
            var users = _context.Users.ToList();
            UsersDataGrid.ItemsSource = users;
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
                        MessageBox.Show("Пользователь с таким именем уже существует!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    if (_context.Users.Any(u => u.Email == addUserWindow.Email))
                    {
                        MessageBox.Show("Пользователь с таким email уже существует!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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

                    MessageBox.Show($"Пользователь успешно добавлен!\nЛогин: {addUserWindow.Username}\nПароль: {addUserWindow.GeneratedPassword}", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadUsers();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при добавлении пользователя: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentUser.Instance.Role != "admin")
            {
                MessageBox.Show("Только администратор может удалять пользователей!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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
                        MessageBox.Show("Вы не можете удалить самого себя!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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

                        if (File.Exists(document.FilePath))
                        {
                            try
                            {
                                File.Delete(document.FilePath);
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show($"Ошибка при удалении файла: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }

                        _context.Documents.Remove(document);
                    }

                    _context.Users.Remove(user);
                    _context.SaveChanges();

                    MessageBox.Show("Пользователь успешно удален!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadUsers();
                }
            }
        }
    }
}