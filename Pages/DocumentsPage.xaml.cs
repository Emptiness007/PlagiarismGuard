using Microsoft.Win32;
using PlagiarismGuard.Data;
using PlagiarismGuard.Models;
using PlagiarismGuard.Services;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace PlagiarismGuard.Pages
{
    public partial class DocumentsPage : Page
    {
        private readonly PlagiarismContext _context;
        private readonly TextExtractorService _textExtractor;
        private readonly PlagiarismCheckService _plagiarismChecker;

        public DocumentsPage(PlagiarismContext context, TextExtractorService textExtractor, PlagiarismCheckService plagiarismChecker)
        {
            InitializeComponent();
            _context = context;
            _textExtractor = textExtractor;
            _plagiarismChecker = plagiarismChecker;

            LoadDocuments();
        }

        private void LoadDocuments()
        {
            if (CurrentUser.Instance.Role == "admin")
            {
                var documents = _context.Documents.ToList();
                DocumentsDataGrid.ItemsSource = documents;
            }
            else
            {
                var documents = _context.Documents
                    .Where(d => d.UserId == CurrentUser.Instance.Id)
                    .ToList();
                DocumentsDataGrid.ItemsSource = documents;
                UploadButton.Visibility = Visibility.Collapsed;
            }
        }

        private void UploadButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Text Files (*.txt)|*.txt|Word Documents (*.docx)|*.docx|PDF Files (*.pdf)|*.pdf"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                string fileName = Path.GetFileName(filePath);
                string format = Path.GetExtension(filePath).ToLower().TrimStart('.');
                string serverPath = Path.Combine("uploads", "documents", Guid.NewGuid() + "." + format);

                try
                {
                    string text = _textExtractor.ExtractText(filePath, format);

                    if (_plagiarismChecker.DocumentExists(text))
                    {
                        MessageBox.Show("Документ с таким содержимым уже существует в системе!", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    Directory.CreateDirectory(Path.GetDirectoryName(serverPath));
                    File.Copy(filePath, serverPath);

                    var document = new Models.Document
                    {
                        UserId = CurrentUser.Instance.Id,
                        FileName = fileName,
                        FilePath = serverPath,
                        FileSize = new FileInfo(filePath).Length,
                        UploadedAt = DateTime.Now,
                        Format = format
                    };
                    _context.Documents.Add(document);
                    _context.SaveChanges();

                    var documentText = new DocumentText
                    {
                        DocumentId = document.Id,
                        TextContent = text,
                        TextHash = _plagiarismChecker.ComputeTextHash(text),
                        ProcessedAt = DateTime.Now
                    };
                    _context.DocumentTexts.Add(documentText);
                    _context.SaveChanges();

                    MessageBox.Show("Документ успешно загружен на сервер!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadDocuments();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при загрузке документа: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    if (CurrentUser.Instance.Role != "admin" && document.UserId != CurrentUser.Instance.Id)
                    {
                        MessageBox.Show("Вы можете удалять только свои документы!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var documentText = _context.DocumentTexts.FirstOrDefault(dt => dt.DocumentId == documentId);
                    if (documentText != null)
                    {
                        _context.DocumentTexts.Remove(documentText);
                    }

                    var checks = _context.Checks.Where(c => c.DocumentId == documentId).ToList();
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
                    _context.SaveChanges();

                    MessageBox.Show("Документ успешно удален!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadDocuments();
                }
            }
        }
    }
}