using Microsoft.Win32;
using PlagiarismGuard.Data;
using PlagiarismGuard.Models;
using PlagiarismGuard.Services;
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
        private bool _isDragging; 
        private Point _startPoint; 
        private double _initialHeight; 
        private Thickness _initialMargin;

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

            DragHandle.MouseLeftButtonDown += DragHandle_MouseLeftButtonDown;
            DragHandle.MouseMove += DragHandle_MouseMove;
            DragHandle.MouseLeftButtonUp += DragHandle_MouseLeftButtonUp;
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

        private void DragHandle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDragging = true;
            _startPoint = e.GetPosition(this);
            _initialMargin = ResultsGrid.Margin;
            DragHandle.CaptureMouse();
        }

        private void DragHandle_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                Point currentPoint = e.GetPosition(this);
                double deltaY = currentPoint.Y - _startPoint.Y;

                Thickness newMargin = new Thickness
                {
                    Left = _initialMargin.Left,
                    Top = Math.Max(0, _initialMargin.Top + deltaY),
                    Right = _initialMargin.Right,
                    Bottom = _initialMargin.Bottom
                };

                double gridHeight = ResultsGrid.ActualHeight > 0 ? ResultsGrid.ActualHeight : 100;
                double maxTop = ActualHeight - gridHeight;
                if (newMargin.Top <= maxTop)
                {
                    ResultsGrid.Margin = newMargin;
                }
                else
                {
                    ResultsGrid.Margin = new Thickness
                    {
                        Left = newMargin.Left,
                        Top = maxTop,
                        Right = newMargin.Right,
                        Bottom = newMargin.Bottom
                    };
                }

            }
        }

        private void DragHandle_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
            DragHandle.ReleaseMouseCapture();
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
                CheckButton.IsEnabled = false;

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
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при проверке: {ex.Message}\nInner Exception: {ex.InnerException?.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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