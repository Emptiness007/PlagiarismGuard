using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace PlagiarismGuard.Windows
{
    public partial class ComparisonWindow : Window
    {
        public ComparisonWindow(Window owner, string checkedDocumentText, string sourceDocumentText, string matchedText)
        {
            InitializeComponent();
            Owner = owner;
            LoadDocuments(checkedDocumentText, sourceDocumentText, matchedText);
        }

        private void LoadDocuments(string checkedDocumentText, string sourceDocumentText, string matchedText)
        {
            if (string.IsNullOrEmpty(matchedText))
            {
                MessageBox.Show("Текст совпадения отсутствует.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Загрузка проверяемого документа
            LoadTextWithHighlight(CheckedDocumentTextBox, checkedDocumentText, matchedText);

            // Загрузка документа с совпадением
            LoadTextWithHighlight(SourceDocumentTextBox, sourceDocumentText, matchedText);
        }

        private void LoadTextWithHighlight(RichTextBox richTextBox, string fullText, string matchedText)
        {
            richTextBox.Document.Blocks.Clear();
            var paragraph = new Paragraph();
            int currentIndex = 0;

            // Нормализация текстов для поиска
            string normalizedFullText = NormalizeText(fullText);
            System.Diagnostics.Debug.WriteLine($"LoadTextWithHighlight: Normalized FullText Length={normalizedFullText.Length}");

            // Разделяем matchedText на фрагменты, если он содержит "; "
            var matchedFragments = matchedText.Split(new[] { "; " }, StringSplitOptions.RemoveEmptyEntries)
                                             .Select(f => NormalizeText(f))
                                             .Where(f => !string.IsNullOrWhiteSpace(f))
                                             .ToList();

            if (!matchedFragments.Any())
            {
                System.Diagnostics.Debug.WriteLine("LoadTextWithHighlight: Нет валидных фрагментов в matchedText");
                paragraph.Inlines.Add(new Run(fullText));
                richTextBox.Document.Blocks.Add(paragraph);
                return;
            }

            foreach (var fragment in matchedFragments)
            {
                System.Diagnostics.Debug.WriteLine($"Processing fragment: '{fragment}', Length={fragment.Length}");
                currentIndex = 0; // Сбрасываем индекс для каждого фрагмента

                while (currentIndex < fullText.Length)
                {
                    int matchIndex = normalizedFullText.IndexOf(fragment, currentIndex, StringComparison.OrdinalIgnoreCase);
                    if (matchIndex == -1)
                    {
                        System.Diagnostics.Debug.WriteLine($"Fragment '{fragment}' не найден в fullText с позиции {currentIndex}");
                        break;
                    }

                    // Добавляем текст до совпадения
                    if (matchIndex > currentIndex)
                    {
                        string textBefore = fullText.Substring(currentIndex, matchIndex - currentIndex);
                        paragraph.Inlines.Add(new Run(textBefore));
                        System.Diagnostics.Debug.WriteLine($"Added text before match: Length={textBefore.Length}");
                    }

                    // Добавляем совпадающий фрагмент с выделением
                    string matchedPortion = fullText.Substring(matchIndex, fragment.Length);
                    var matchedRun = new Run(matchedPortion)
                    {
                        Background = Brushes.Yellow,
                        Foreground = Brushes.Red // Для контраста
                    };
                    paragraph.Inlines.Add(matchedRun);
                    System.Diagnostics.Debug.WriteLine($"Added matched fragment: '{matchedPortion}'");

                    currentIndex = matchIndex + fragment.Length;
                }
            }

            // Добавляем оставшийся текст
            if (currentIndex < fullText.Length)
            {
                string remainingText = fullText.Substring(currentIndex);
                paragraph.Inlines.Add(new Run(remainingText));
                System.Diagnostics.Debug.WriteLine($"Added remaining text: Length={remainingText.Length}");
            }

            richTextBox.Document.Blocks.Add(paragraph);
        }

        private string NormalizeText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            // Удаляем лишние пробелы и переносы строк, нормализуем текст
            return Regex.Replace(text.Trim(), @"\s+", " ");
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}