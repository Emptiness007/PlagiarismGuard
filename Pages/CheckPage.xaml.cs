using Microsoft.Win32;
using PlagiarismGuard.Data;
using PlagiarismGuard.Models;
using PlagiarismGuard.Services;
using PlagiarismGuard.Windows;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Xceed.Document.NET;
using Xceed.Words.NET;
using static PlagiarismGuard.Windows.CustomMessageBox;

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
        private bool _isResizing; 
        private Point _startPoint; 
        private double _initialHeight; 

        public CheckPage(PlagiarismContext context, TextExtractorService textExtractor, PlagiarismCheckService plagiarismChecker, ReportGeneratorService reportGenerator)
        {
            InitializeComponent();
            _context = context;
            _textExtractor = textExtractor;
            _plagiarismChecker = plagiarismChecker;
            _currentDocumentId = 0;
            _reportGenerator = reportGenerator;

            ResizeHandle.MouseLeftButtonDown += ResizeHandle_MouseLeftButtonDown;
            ResizeHandle.MouseMove += ResizeHandle_MouseMove;
            ResizeHandle.MouseLeftButtonUp += ResizeHandle_MouseLeftButtonUp;
            DocumentTextBox.TextChanged += DocumentTextBox_TextChanged;
        }

        private void DocumentTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string text = DocumentTextBox.Text;
            int wordCount = string.IsNullOrWhiteSpace(text) ? 0 : text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            symbolCount.Text = $"Всего слов: {wordCount}";
        }

        private void ResizeHandle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isResizing = true;
            _startPoint = e.GetPosition(this);
            _initialHeight = DocumentTextBox.ActualHeight;
            ResizeHandle.CaptureMouse();
        }

        private void ResizeHandle_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isResizing)
            {
                Point currentPoint = e.GetPosition(this);
                double deltaY = currentPoint.Y - _startPoint.Y;
                double newHeight = _initialHeight + deltaY;

                if (newHeight >= DocumentTextBox.MinHeight && newHeight <= DocumentTextBox.MaxHeight)
                {
                    DocumentTextBox.Height = newHeight;
                }
            }
        }

        private void ResizeHandle_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isResizing = false;
            ResizeHandle.ReleaseMouseCapture();
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
                byte[] fileContent = File.ReadAllBytes(filePath);
                string text = _textExtractor.ExtractText(fileContent, format);

                try
                {
                    string textHash = _plagiarismChecker.ComputeTextHash(text);
                    var existingDocumentText = _context.DocumentTexts
                        .FirstOrDefault(dt => dt.TextHash == textHash);

                    if (existingDocumentText != null)
                    {
                        _currentDocumentId = existingDocumentText.DocumentId;
                        DocumentTextBox.Text = text;
                        return;
                    }

                    var document = new Models.Document
                    {
                        UserId = CurrentUser.Instance.Id,
                        FileName = fileName,
                        FileContent = fileContent,
                        FileSize = fileContent.Length,
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
                    MessageBox.Show("Документ успешно загружен в базу данных!");
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show(Window.GetWindow(this), $"Ошибка при обработке документа: {ex.Message}\nInner Exception: {ex.InnerException?.Message}", "Ошибка", MessageType.Error);
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
                    CustomMessageBox.Show(Window.GetWindow(this), "Текст для проверки пуст!", "Предупреждение", MessageType.Warning);
                    return;
                }

                CheckButton.IsEnabled = false;
                var progressWindow = new ProgressWindow();
                progressWindow.Show();

                try
                {
                    var adminDocuments = _context.DocumentTexts
                        .Where(dt => _context.Documents.Any(d => d.Id == dt.DocumentId &&
                                                                _context.Users.Any(u => u.Id == d.UserId && u.Role == "admin")))
                        .ToList();

                    if (!adminDocuments.Any())
                    {
                        CustomMessageBox.Show(Window.GetWindow(this), "На сервере нет документов, загруженных администратором, для проверки. Обратитесь к администратору.", "Предупреждение", MessageType.Warning);
                        return;
                    }

                    _lastCheck = await _plagiarismChecker.PerformCheckTextAsync(textToCheck, _currentDocumentId, CurrentUser.Instance.Id);
                    var results = _context.CheckResults
                        .Where(cr => cr.CheckId == _lastCheck.Id)
                        .ToList();
                    var linkResults = _context.LinkCheckResults
                        .Where(lcr => lcr.CheckId == _lastCheck.Id)
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
                    }
                    else
                    {
                        SourceDataGrid.ItemsSource = null;
                        ProgressBar.Value = 0;
                        TextBlock.Text = "Совпадений не найдено";
                    }

                    if (linkResults.Any())
                    {
                        LinkDataGrid.ItemsSource = linkResults.Select((lr, index) => new
                        {
                            LinkNo = index + 1,
                            Url = lr.Url,
                            Status = lr.Status
                        });
                        LinkDataGrid.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        LinkDataGrid.ItemsSource = null;
                        LinkDataGrid.Visibility = Visibility.Collapsed;
                    }
                }
                finally
                {
                    progressWindow.Close();
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show(Window.GetWindow(this), $"Ошибка при проверке: {ex.Message}\nInner Exception: {ex.InnerException?.Message}", "Ошибка", MessageType.Error);
            }
            finally
            {
                CheckButton.IsEnabled = true;
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
                CustomMessageBox.Show(Window.GetWindow(this), "Сначала выполните проверку!", "Предупреждение", MessageType.Warning);
                return;
            }

            var results = _context.CheckResults
            .Where(cr => cr.CheckId == _lastCheck.Id)
            .ToList();
            var linkResults = _context.LinkCheckResults
                .Where(lcr => lcr.CheckId == _lastCheck.Id)
                .ToList();

            _reportGenerator.GeneratePlagiarismReport(_lastCheck, results, linkResults);
        }
    }
    
}