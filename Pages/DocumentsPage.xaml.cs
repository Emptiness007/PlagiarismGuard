using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using PlagiarismGuard.Data;
using PlagiarismGuard.Models;
using PlagiarismGuard.Services;
using PlagiarismGuard.Windows;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using static PlagiarismGuard.Windows.CustomMessageBox;

namespace PlagiarismGuard.Pages
{
    public partial class DocumentsPage : Page
    {
        public bool IsAdmin => CurrentUser.Instance.Role == "admin";
        private readonly PlagiarismContext _context;
        private readonly TextExtractorService _textExtractor;
        private readonly PlagiarismCheckService _plagiarismChecker;
        private string _searchQuery = string.Empty;
        private int _sortOption = 0;

        public DocumentsPage(PlagiarismContext context, TextExtractorService textExtractor, PlagiarismCheckService plagiarismChecker)
        {
            InitializeComponent();
            _context = context;
            _textExtractor = textExtractor;
            _plagiarismChecker = plagiarismChecker;
            PlagiarismCheckColumn.Visibility = IsAdmin ? Visibility.Visible : Visibility.Collapsed;
            UploadButton.Visibility = IsAdmin ? Visibility.Visible :Visibility.Collapsed;

            LoadDocuments();
        }

        private void LoadDocuments()
        {
            IQueryable<Document> query = IsAdmin
                ? _context.Documents.Include(d => d.User)
                : _context.Documents.Include(d => d.User).Where(d => d.UserId == CurrentUser.Instance.Id);

            if (!string.IsNullOrWhiteSpace(_searchQuery))
            {
                query = query.Where(d => d.FileName.ToLower().Contains(_searchQuery.ToLower(), StringComparison.OrdinalIgnoreCase));
            }

            query = _sortOption switch
            {
                1 => query.OrderByDescending(d => d.UploadedAt),
                2 => query.OrderBy(d => d.UploadedAt),
                _ => query
            };

            DocumentsDataGrid.ItemsSource = query.ToList();
        }
        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _searchQuery = SearchTextBox.Text.Trim();
            LoadDocuments();
        }

        private void SortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _sortOption = SortComboBox.SelectedIndex;
            LoadDocuments();
        }

        private async void UploadButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Word Documents (*.docx)|*.docx|PDF Files (*.pdf)|*.pdf",
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() != true)
                return;

            string[] filePaths = openFileDialog.FileNames;
            if (filePaths.Length == 0)
                return;

            var progressWindow = new ProgressWindow(Window.GetWindow(this));
            progressWindow.Show();
            int totalFiles = filePaths.Length;
            int processedFiles = 0;
            var errors = new List<string>();
            var successes = new List<string>();

            foreach (var filePath in filePaths)
            {
                string fileName = Path.GetFileName(filePath);
                string format = Path.GetExtension(filePath).ToLower().TrimStart('.');

                try
                {
                    var result = await Task.Run(() =>
                    {
                        byte[] fileContent = File.ReadAllBytes(filePath);
                        string text = _textExtractor.ExtractText(fileContent, format);

                        return (Success: true, Message: "", Document: new Models.Document
                        {
                            UserId = CurrentUser.Instance.Id,
                            FileName = fileName,
                            FileContent = fileContent,
                            FileSize = fileContent.Length,
                            UploadedAt = DateTime.Now,
                            Format = format
                        }, Text: text);
                    });

                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        processedFiles++;
                        progressWindow.UpdateProgress($"Загружено {processedFiles} из {totalFiles} документов");

                        if (_plagiarismChecker.DocumentExists(result.Text))
                        {
                            errors.Add($"Документ '{fileName}' уже существует в системе!");
                            return;
                        }

                        _context.Documents.Add(result.Document);
                        _context.SaveChanges();

                        var documentText = new DocumentText
                        {
                            DocumentId = result.Document.Id,
                            TextContent = result.Text,
                            TextHash = _plagiarismChecker.ComputeTextHash(result.Text),
                            ProcessedAt = DateTime.Now
                        };
                        _context.DocumentTexts.Add(documentText);
                        _context.SaveChanges();

                        successes.Add(fileName);
                    });
                }
                catch (Exception ex)
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        processedFiles++;
                        progressWindow.UpdateProgress($"Загружено {processedFiles} из {totalFiles} документов");
                        errors.Add($"Ошибка при загрузке '{fileName}': {ex.Message}");
                    });
                }
            }

            await Application.Current.Dispatcher.InvokeAsync(() => progressWindow.Close());

            if (successes.Any() && errors.Any())
            {
                CustomMessageBox.Show($"Успешно загружено {successes.Count} документов:\n{string.Join("\n", successes)}\n\nОшибки ({errors.Count}):\n{string.Join("\n", errors)}", "Результат загрузки", MessageType.Information, Window.GetWindow(this));
            }
            else if (successes.Any())
            {
                CustomMessageBox.Show($"Все документы ({successes.Count}) успешно загружены:\n{string.Join("\n", successes)}", "Успех", MessageType.Information, Window.GetWindow(this));
            }
            else
            {
                CustomMessageBox.Show($"Ни один документ не загружен. Ошибки:\n{string.Join("\n", errors)}", "Ошибка", MessageType.Error, Window.GetWindow(this));
            }
            LoadDocuments();
        }

        private void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null)
            {
                int documentId = (int)button.Tag;
                var document = _context.Documents.FirstOrDefault(d => d.Id == documentId);

                if (document != null)
                {
                    if (CurrentUser.Instance.Role != "admin" && document.UserId != CurrentUser.Instance.Id)
                    {
                        CustomMessageBox.Show("Вы можете скачивать только свои документы!", "Ошибка", MessageType.Error, Window.GetWindow(this));
                        return;
                    }

                    SaveFileDialog saveFileDialog = new SaveFileDialog
                    {
                        Filter = document.Format == "docx" ? "Word Documents (*.docx)|*.docx" : "PDF Files (*.pdf)|*.pdf",
                        FileName = document.FileName,
                        DefaultExt = document.Format
                    };

                    if (saveFileDialog.ShowDialog() == true)
                    {
                        try
                        {
                            File.WriteAllBytes(saveFileDialog.FileName, document.FileContent);
                            CustomMessageBox.Show("Документ успешно скачан!", "Успех", MessageType.Information, Window.GetWindow(this));
                        }
                        catch (Exception ex)
                        {
                            CustomMessageBox.Show($"Ошибка при скачивании документа: {ex.Message}", "Ошибка", MessageType.Error, Window.GetWindow(this));
                        }
                    }
                }
            }
        }

        private void PlagiarismCheck_Checked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox != null)
            {
                int documentId = (int)checkBox.Tag;
                var document = _context.Documents.FirstOrDefault(d => d.Id == documentId);
                if (document != null && CurrentUser.Instance.Role == "admin")
                {
                    document.IsUsedForPlagiarismCheck = true;
                    _context.SaveChanges();
                }
            }
        }

        private void PlagiarismCheck_Unchecked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox != null)
            {
                int documentId = (int)checkBox.Tag;
                var document = _context.Documents.FirstOrDefault(d => d.Id == documentId);
                if (document != null && CurrentUser.Instance.Role == "admin")
                {
                    document.IsUsedForPlagiarismCheck = false;
                    _context.SaveChanges();
                }
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null)
            {
                int documentId = (int)button.Tag;
                var document = _context.Documents.FirstOrDefault(d => d.Id == documentId);

                if (document != null)
                {

                    var documentText = _context.DocumentTexts.FirstOrDefault(dt => dt.DocumentId == documentId);
                    if (documentText != null)
                    {
                        _context.DocumentTexts.Remove(documentText);
                    }
                    var checks = _context.Checks.Where(c => c.DocumentId == documentId).ToList();

                    foreach (var check in checks)
                    {
                        var linkCheckResults = _context.LinkCheckResults.Where(lcr => lcr.CheckId == check.Id).ToList();
                        if (linkCheckResults.Any())
                        {
                            _context.LinkCheckResults.RemoveRange(linkCheckResults);
                        }

                        var checkResults = _context.CheckResults.Where(cr => cr.CheckId == check.Id).ToList();
                        if (checkResults.Any())
                        {
                            _context.CheckResults.RemoveRange(checkResults);
                        }
                        _context.Checks.Remove(check);
                    }

                    _context.SaveChanges();

                    _context.Documents.Remove(document);
                    _context.SaveChanges();

                    CustomMessageBox.Show("Документ успешно удален!", "Успех", MessageType.Information, Window.GetWindow(this));
                    LoadDocuments();
                }
            }
        }
    }
}