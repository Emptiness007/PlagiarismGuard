using PlagiarismGuard.Data;
using PlagiarismGuard.Models;
using PlagiarismGuard.Windows;
using System;
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
                        CustomMessageBox.Show(Window.GetWindow(this), "Пользователь с таким именем уже существует!", "Ошибка", MessageType.Error);
                        return;
                    }
                    if (_context.Users.Any(u => u.Email == addUserWindow.Email))
                    {
                        CustomMessageBox.Show(Window.GetWindow(this), "Пользователь с таким email уже существует!", "Ошибка", MessageType.Error);
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

                    CustomMessageBox.Show(Window.GetWindow(this), $"Пользователь успешно добавлен!\nЛогин: {addUserWindow.Username}\nПароль: {addUserWindow.GeneratedPassword}", "Успех", MessageType.Information);
                    LoadUsers();
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show(Window.GetWindow(this), $"Ошибка при добавлении пользователя: {ex.Message}", "Ошибка", MessageType.Error);
                }
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentUser.Instance.Role != "admin")
            {
                CustomMessageBox.Show(Window.GetWindow(this), "Только администратор может удалять пользователей!", "Ошибка", MessageType.Error);
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
                        CustomMessageBox.Show(Window.GetWindow(this), "Вы не можете удалить самого себя!", "Ошибка", MessageType.Error);
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

                    CustomMessageBox.Show(Window.GetWindow(this), "Пользователь успешно удален!", "Успех", MessageType.Information);
                    LoadUsers();
                }
            }
        }
    }
}