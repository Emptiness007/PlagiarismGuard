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

namespace PlagiarismGuard
{
    public partial class MainWindow : Window
    {
        private readonly PlagiarismContext _context;
        private readonly TextExtractorService _textExtractor;
        private readonly PlagiarismCheckService _plagiarismChecker;
        private int _currentDocumentId;

        public MainWindow(int userId)
        {
            InitializeComponent();
            _context = new PlagiarismContext();
            _textExtractor = new TextExtractorService();
            _plagiarismChecker = new PlagiarismCheckService(_context);
            CurrentUser.Instance.Id = userId;
            LoadUserChecks();
        }

        private void ImportButton_Click(object sender, RoutedEventArgs e)
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

                    // Проверка на существование документа
                    if (_plagiarismChecker.DocumentExists(text))
                    {
                        MessageBox.Show("Документ с таким содержимым уже существует в системе!", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                        DocumentTextBox.Text = text; // Все равно отображаем текст в TextBox
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
                        TextHash = _plagiarismChecker.ComputeTextHash(text), // Сохраняем хэш
                        ProcessedAt = DateTime.Now
                    };
                    _context.DocumentTexts.Add(documentText);
                    _context.SaveChanges();

                    _currentDocumentId = document.Id;
                    DocumentTextBox.Text = text; // Отображаем текст в TextBox
                    symbolCount.Text = $"Всего слов: {text.Split(' ').Length}";
                    MessageBox.Show("Документ успешно загружен!");
                    LoadUserDocuments();
                    LoadUserChecks();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при обработке документа: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void CheckButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentDocumentId == 0)
            {
                MessageBox.Show("Сначала загрузите документ!");
                return;
            }

            try
            {
                string textToCheck = DocumentTextBox.Text; // Берем текст из TextBox
                if (string.IsNullOrEmpty(textToCheck))
                {
                    MessageBox.Show("Текст для проверки пуст!");
                    return;
                }

                var check = _plagiarismChecker.PerformCheckText(textToCheck, _currentDocumentId, CurrentUser.Instance.Id);
                var results = _context.CheckResults
                    .Where(cr => cr.CheckId == check.Id)
                    .ToList();

                if (results.Any())
                {
                    float avgSimilarity = results.Average(r => r.Similarity) * 100;
                    SourceDataGrid.ItemsSource = results.Select((r, index) => new
                    {
                        SourceNo = index + 1,
                        SourceName = _context.Documents.First(d => d.Id == r.SourceDocumentId).FileName,
                        Excerpt = r.MatchedText,
                        Similarity = $"{r.Similarity * 100:F2}%"
                    });
                    ProgressBar.Value = avgSimilarity;
                    TextBlock.Text = $"Процент сходства - {avgSimilarity:F2}%";
                }
                else
                {
                    SourceDataGrid.ItemsSource = null;
                    ProgressBar.Value = 0;
                    TextBlock.Text = "Совпадений не найдено";
                }

                LoadUserChecks();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при проверке: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GenerateReportButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentDocumentId == 0)
            {
                MessageBox.Show("Сначала загрузите документ!");
                return;
            }

            try
            {
                var document = _context.Documents.FirstOrDefault(d => d.Id == _currentDocumentId);
                var checks = _context.Checks
                    .Where(c => c.DocumentId == _currentDocumentId)
                    .OrderByDescending(c => c.CheckedAt)
                    .Take(1)
                    .ToList();

                if (!checks.Any())
                {
                    MessageBox.Show("Нет данных о проверке для этого документа!");
                    return;
                }

                var check = checks.First();
                var results = _context.CheckResults
                    .Where(cr => cr.CheckId == check.Id)
                    .ToList();

                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "Word Document (*.docx)|*.docx|Old Word Document (*.doc)|*.doc",
                    DefaultExt = "docx",
                    FileName = $"PlagiarismReport_{document.FileName}"
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
                        doc.InsertParagraph($"Документ: {document.FileName}")
                            .FontSize(14);
                        doc.InsertParagraph($"Пользователь: {CurrentUser.Instance.Id}")
                            .FontSize(14);
                        doc.InsertParagraph($"Дата проверки: {check.CheckedAt:dd.MM.yyyy HH:mm}")
                            .FontSize(14);
                        doc.InsertParagraph($"Процент сходства: {check.Similarity * 100:F2}%")
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
                else
                {
                    MessageBox.Show("Сохранение отчета отменено.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании отчета: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadUserDocuments()
        {
            var userDocuments = _context.Documents
                .Where(d => d.UserId == CurrentUser.Instance.Id)
                .ToList();
        }

        private void LoadUserChecks()
        {
        }
    }
}