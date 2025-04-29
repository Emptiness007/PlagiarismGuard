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
        private int _currentDocumentId;
        private Check _lastCheck;

        public CheckPage(PlagiarismContext context, TextExtractorService textExtractor, PlagiarismCheckService plagiarismChecker)
        {
            InitializeComponent();
            _context = context;
            _textExtractor = textExtractor;
            _plagiarismChecker = plagiarismChecker;
            _currentDocumentId = 0;
        }

        public void ImportDocument()
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
                string text = _textExtractor.ExtractText(filePath, format);

                try
                {
                    string textHash = _plagiarismChecker.ComputeTextHash(text);
                    var existingDocumentText = _context.DocumentTexts
                        .FirstOrDefault(dt => dt.TextHash == textHash);

                    if (existingDocumentText != null)
                    {
                        MessageBox.Show("Документ с таким содержимым уже существует в системе!", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
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

        private void CheckText()
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
                    .Where(dt => _context.Documents.Any(d => d.Id == dt.DocumentId && _context.Users.Any(u => u.Id == d.UserId && u.Role == "admin")))
                    .ToList();

                if (!adminDocuments.Any())
                {
                    MessageBox.Show("На сервере нет документов для проверки. Обратитесь к администратору.");
                    return;
                }

                _lastCheck = _plagiarismChecker.PerformCheckText(textToCheck, _currentDocumentId, CurrentUser.Instance.Id);
                var results = _context.CheckResults
                    .Where(cr => cr.CheckId == _lastCheck.Id)
                    .ToList();

                if (results.Any())
                {
                    float avgSimilarity = results.Average(r => r.Similarity) * 100;
                    SourceDataGrid.ItemsSource = results.Select((r, index) => new
                    {
                        SourceNo = index + 1,
                        SourceName = _context.Documents.First(d => d.Id == r.SourceDocumentId).FileName,
                        Excerpt = r.MatchedText,
                        Similarity = $"{r.Similarity * 100:F2}%" // Это поле уже передается в DataGrid
                    });
                    ProgressBar.Value = avgSimilarity; // Устанавливаем значение ProgressBar
                    TextBlock.Text = $"Процент сходства - {avgSimilarity:F2}%";
                }
                else
                {
                    SourceDataGrid.ItemsSource = null;
                    ProgressBar.Value = 0;
                    TextBlock.Text = "Совпадений не найдено";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при проверке: {ex.Message}\nInner Exception: {ex.InnerException?.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CheckButton_Click(object sender, RoutedEventArgs e)
        {
            CheckText();
        }

        private void GenerateReportButton_Click(object sender, RoutedEventArgs e)
        {
            if (_lastCheck == null)
            {
                MessageBox.Show("Сначала выполните проверку!");
                return;
            }

            try
            {
                var results = _context.CheckResults
                    .Where(cr => cr.CheckId == _lastCheck.Id)
                    .ToList();

                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "Word Document (*.docx)|*.docx|Old Word Document (*.doc)|*.doc",
                    DefaultExt = "docx",
                    FileName = $"PlagiarismReport_{DateTime.Now:yyyyMMddHHmmss}"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    string filePath = saveFileDialog.FileName;
                    bool isDoc = Path.GetExtension(filePath).ToLower() == ".doc";

                    if (isDoc)
                    {
                        MessageBox.Show("Формат .doc не поддерживается библиотекой Xceed.Words.NET. Отчет будет сохранен в формате .docx.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Information);
                        filePath = Path.ChangeExtension(filePath, ".docx");
                    }

                    using (var doc = DocX.Create(filePath))
                    {
                        doc.InsertParagraph("Отчет по проверке на плагиат")
                            .FontSize(16)
                            .Bold()
                            .Alignment = Alignment.center;
                        doc.InsertParagraph($"Пользователь: {CurrentUser.Instance.Id}")
                            .FontSize(14);
                        doc.InsertParagraph($"Дата проверки: {_lastCheck.CheckedAt:dd.MM.yyyy HH:mm}")
                            .FontSize(14);
                        doc.InsertParagraph($"Процент сходства: {_lastCheck.Similarity * 100:F2}%")
                            .FontSize(14);
                        doc.InsertParagraph();

                        if (results.Any())
                        {
                            var table = doc.AddTable(results.Count + 1, 4);
                            table.Rows[0].Cells[0].Paragraphs[0].Append("№").Bold();
                            table.Rows[0].Cells[1].Paragraphs[0].Append("Источник").Bold();
                            table.Rows[0].Cells[2].Paragraphs[0].Append("Фрагмент").Bold();
                            table.Rows[0].Cells[3].Paragraphs[0].Append("Сходство").Bold();

                            for (int i = 0; i < results.Count; i++)
                            {
                                var result = results[i];
                                table.Rows[i + 1].Cells[0].Paragraphs[0].Append((i + 1).ToString());
                                table.Rows[i + 1].Cells[1].Paragraphs[0].Append(_context.Documents.First(d => d.Id == result.SourceDocumentId).FileName);
                                table.Rows[i + 1].Cells[2].Paragraphs[0].Append(result.MatchedText);
                                table.Rows[i + 1].Cells[3].Paragraphs[0].Append($"{result.Similarity * 100:F2}%");
                            }

                            doc.InsertTable(table);
                        }
                        else
                        {
                            doc.InsertParagraph("Совпадений не найдено").FontSize(12);
                        }

                        doc.Save();
                    }

                    MessageBox.Show($"Отчет успешно сохранен в {filePath}", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании отчета: {ex.Message}\nInner Exception: {ex.InnerException?.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}