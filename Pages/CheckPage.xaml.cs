using Microsoft.Win32;
using PlagiarismGuard.Data;
using PlagiarismGuard.Models;
using PlagiarismGuard.Services;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Xceed.Document.NET;
using Xceed.Words.NET;

namespace PlagiarismGuard.Pages
{
    public partial class CheckPage : Page
    {
        private readonly PlagiarismContext _context;
        private readonly TextExtractorService _textExtractor;
        private readonly PlagiarismCheckService _plagiarismChecker;
        private readonly ReportGeneratorService _reportGenerator;
        private int _currentDocumentId;
        private Check _lastCheck;

        public CheckPage(PlagiarismContext context, TextExtractorService textExtractor, PlagiarismCheckService plagiarismChecker, ReportGeneratorService reportGenerator)
        {
            InitializeComponent();
            _context = context;
            _textExtractor = textExtractor;
            _plagiarismChecker = plagiarismChecker;
            _currentDocumentId = 0;
            _reportGenerator = reportGenerator;
        }

        public void ImportDocument()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Word Documents (*.docx)|*.docx|PDF Files (*.pdf)|*.pdf"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                string fileName = Path.GetFileName(filePath);
                string format = Path.GetExtension(filePath).ToLower().TrimStart('.');
                string serverPath = Path.Combine("uploads", "documents", Guid.NewGuid() + "." + format);
                string text = _textExtractor.ExtractText(filePath, format);

                try
                {
                    string textHash = _plagiarismChecker.ComputeTextHash(text);
                    var existingDocumentText = _context.DocumentTexts
                        .FirstOrDefault(dt => dt.TextHash == textHash);

                    if (existingDocumentText != null)
                    {
                        _currentDocumentId = existingDocumentText.DocumentId;
                        DocumentTextBox.Text = text;
                        symbolCount.Text = $"Всего слов: {text.Split(' ').Length}";
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

                    _currentDocumentId = document.Id;
                    DocumentTextBox.Text = text;
                    symbolCount.Text = $"Всего слов: {text.Split(' ').Length}";
                    MessageBox.Show("Документ успешно загружен на сервер!");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при обработке документа: {ex.Message}\nInner Exception: {ex.InnerException?.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async Task CheckTextAsync()
        {
            try
            {
                string textToCheck = DocumentTextBox.Text;
                if (string.IsNullOrEmpty(textToCheck))
                {
                    MessageBox.Show("Текст для проверки пуст!");
                    return;
                }

                var adminDocuments = _context.DocumentTexts
                    .Where(dt => _context.Documents.Any(d => d.Id == dt.DocumentId &&
                                                            _context.Users.Any(u => u.Id == d.UserId && u.Role == "admin")))
                    .ToList();

                if (!adminDocuments.Any())
                {
                    MessageBox.Show("На сервере нет документов, загруженных администратором, для проверки. Обратитесь к администратору.");
                    return;
                }

                _lastCheck = await _plagiarismChecker.PerformCheckTextAsync(textToCheck, _currentDocumentId, CurrentUser.Instance.Id);
                var results = _context.CheckResults
                    .Where(cr => cr.CheckId == _lastCheck.Id)
                    .ToList();

                if (results.Any())
                {
                    float plagiarismPercentage = _lastCheck.Similarity;
                    SourceDataGrid.ItemsSource = results.Select((r, index) => new
                    {
                        SourceNo = index + 1,
                        SourceName = _context.Documents.First(d => d.Id == r.SourceDocumentId).FileName,
                        Excerpt = r.MatchedText,
                        Similarity = $"{r.Similarity * 100:F2}%"
                    });
                    ProgressBar.Value = plagiarismPercentage;
                    TextBlock.Text = $"Процент плагиата - {plagiarismPercentage:F2}%";

                    if (_lastCheck.AiGeneratedPercentage.HasValue)
                    {
                        TextBlock.Text += $"\nВероятность ИИ-генерации: {_lastCheck.AiGeneratedPercentage:F2}%";
                    }
                }
                else
                {
                    SourceDataGrid.ItemsSource = null;
                    ProgressBar.Value = 0;
                    TextBlock.Text = "Совпадений не найдено";
                    if (_lastCheck.AiGeneratedPercentage.HasValue)
                    {
                        TextBlock.Text += $"\nВероятность ИИ-генерации: {_lastCheck.AiGeneratedPercentage:F2}%";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при проверке: {ex.Message}\nInner Exception: {ex.InnerException?.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void CheckButton_Click(object sender, RoutedEventArgs e)
        {
            await CheckTextAsync();
        }


        private void GenerateReportButton_Click(object sender, RoutedEventArgs e)
        {
            if (_lastCheck == null)
            {
                MessageBox.Show("Сначала выполните проверку!");
                return;
            }

            var results = _context.CheckResults
                .Where(cr => cr.CheckId == _lastCheck.Id)
                .ToList();

            _reportGenerator.GeneratePlagiarismReport(_lastCheck, results);
        }
    }
}