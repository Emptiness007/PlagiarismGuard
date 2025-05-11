using System.IO;
using System.Text;
using Xceed.Words.NET;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using Xceed.Document.NET;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using System.Text.RegularExpressions;

namespace PlagiarismGuard.Services
{
    public class TextExtractorService
    {
            public string ExtractText(byte[] fileContent, string format) 
            { 
                switch (format.ToLower())
                { 
                    case "docx": return ExtractFromDocx(fileContent); 
                    case "pdf": return ExtractFromPdf(fileContent); 
                    default: throw new NotSupportedException("Формат файла не поддерживается"); 
                } 
            }

        private string ExtractFromDocx(byte[] fileContent)
        {
            try
            {
                using (var stream = new MemoryStream(fileContent))
                using (var doc = DocX.Load(stream))
                {
                    StringBuilder text = new StringBuilder();
                    bool skipTitlePage = true;
                    int paragraphCount = 0;
                    const int maxTitlePageParagraphs = 30;

                    foreach (var paragraph in doc.Paragraphs)
                    {
                        if (string.IsNullOrWhiteSpace(paragraph.Text))
                            continue;

                        paragraphCount++;

                        if (IsHeading(paragraph))
                        {
                            if (paragraph.StyleName != null && paragraph.StyleName.Equals("Heading 1", StringComparison.OrdinalIgnoreCase))
                            {
                                skipTitlePage = false;
                            }
                            continue;
                        }

                        if (IsCaption(paragraph))
                            continue;

                        if (skipTitlePage)
                        {
                            if (IsTitlePageContent(paragraph) || paragraphCount <= maxTitlePageParagraphs)
                            {
                                continue;
                            }
                            else
                            {
                                skipTitlePage = false;
                            }
                        }

                        if (paragraph.IsListItem)
                        {
                            string listPrefix = GetListPrefix(paragraph);
                            text.AppendLine($"{listPrefix} {paragraph.Text}");
                        }
                        else
                        {
                            text.AppendLine(paragraph.Text);
                        }
                    }

                    return text.ToString().Trim();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при извлечении текста из .docx: {ex.Message}");
            }
        }

        private string GetListPrefix(Paragraph paragraph)
        {
            if (!paragraph.IsListItem)
                return string.Empty;

            int level = (int)paragraph.IndentLevel;
            string indent = new string(' ', level * 2);

            switch (paragraph.ListItemType)
            {
                case ListItemType.Bulleted:
                    return $"{indent}•";
                case ListItemType.Numbered:
                    return $"{indent}";
                default:
                    return $"{indent}*";
            }
        }

        private string ExtractFromPdf(byte[] fileContent)
        {
            try
            {
                StringBuilder text = new StringBuilder();
                bool skipTitlePage = true;
                int lineCount = 0;
                const int maxTitlePageLines = 30;

                using (var stream = new MemoryStream(fileContent))
                using (var pdfReader = new PdfReader(stream))
                using (var pdfDoc = new PdfDocument(pdfReader))
                {
                    for (int page = 2; page <= pdfDoc.GetNumberOfPages(); page++)
                    {
                        var strategy = new LocationTextExtractionStrategy();
                        string pageText = PdfTextExtractor.GetTextFromPage(pdfDoc.GetPage(page), strategy);
                        var lines = Regex.Split(pageText, @"\r\n|\n")
                            .Select(line => line.Trim())
                            .Where(line => !string.IsNullOrWhiteSpace(line))
                            .ToList();

                        foreach (var line in lines)
                        {
                            lineCount++;

                            if (IsPdfTableOfContents(line))
                                continue;

                            if (IsPdfHeading(line))
                                continue;

                            if (IsPdfCaption(line))
                                continue;

                            if (skipTitlePage)
                            {
                                if (IsPdfTitlePageContent(line) || lineCount <= maxTitlePageLines)
                                {
                                    continue;
                                }
                                else
                                {
                                    skipTitlePage = false;
                                }
                            }

                            text.AppendLine(line);
                        }
                    }
                }

                return text.ToString().Trim();
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при извлечении текста из .pdf: {ex.Message}");
            }
        }

        private bool IsHeading(Paragraph paragraph)
        {
            if (paragraph.StyleName != null &&
                (paragraph.StyleName.StartsWith("Heading", StringComparison.OrdinalIgnoreCase) ||
                 paragraph.StyleName.Contains("Заголовок", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
            string trimmedText = paragraph.Text.Trim();
            if (!string.IsNullOrEmpty(trimmedText) && !trimmedText.EndsWith(".") && trimmedText.Length > 2)
            {
                if (!paragraph.IsListItem && !IsCaption(paragraph))
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsPdfHeading(string line)
        {
            string trimmedLine = line.Trim();
            if (string.IsNullOrEmpty(trimmedLine))
                return false;

            // Эвристика: заголовки короткие, не заканчиваются точкой, содержат ключевые слова
            if (!trimmedLine.EndsWith(".") && trimmedLine.Length < 100)
            {
                string[] headingKeywords = { "ГЛАВА", "Глава", "SECTION", "Chapter", "Раздел" };
                if (headingKeywords.Any(keyword => trimmedLine.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
                    return true;

                // Проверка на номер главы (например, "1. Введение", "Глава 1")
                if (Regex.IsMatch(trimmedLine, @"^\d+\.\s|^Глава\s+\d+", RegexOptions.IgnoreCase))
                    return true;
            }

            return false;
        }

        private bool IsCaption(Paragraph paragraph)
        {
            if (paragraph.StyleName != null &&
                paragraph.StyleName.Equals("Caption", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            string[] captionKeywords = { "ГЛАВА", "Глава", "Рисунок", "Таблица", "Figure", "Table", "Caption" };
            if (captionKeywords.Any(keyword => paragraph.Text.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
            return false;
        }

        private bool IsPdfCaption(string line)
        {
            string[] captionKeywords = { "Рисунок", "Таблица", "Figure", "Table", "Caption" };
            return captionKeywords.Any(keyword => line.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        private bool IsTitlePageContent(Paragraph paragraph)
        {
            string[] titlePageKeywords = { "Содержание", "Курсовая работа", "Дипломная работа", "Дипломный проект", "Курсовой проект", "учреждение", "ЗАДАНИЕ", "Пояснительная записка", "Министерство", "Факультет", "Кафедра", "Выполнил", "Проверил", "Оглавление" };
            return titlePageKeywords.Any(keyword => paragraph.Text.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        private bool IsPdfTitlePageContent(string line)
        {
            string[] titlePageKeywords = { "Курсовая работа", "Дипломная работа", "Дипломный проект", "Курсовой проект", "учреждение", "ЗАДАНИЕ", "Пояснительная записка", "Министерство", "Факультет", "Кафедра", "Выполнил", "Проверил" };
            return titlePageKeywords.Any(keyword => line.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        private bool IsPdfTableOfContents(string line)
        {
            string trimmedLine = line.Trim();
            if (string.IsNullOrEmpty(trimmedLine))
                return false;

            // Ключевое слово "Оглавление" или "Содержание"
            if (trimmedLine.Contains("Оглавление", StringComparison.OrdinalIgnoreCase) ||
                trimmedLine.Contains("Содержание", StringComparison.OrdinalIgnoreCase))
                return true;

            // Эвристика: строки вида "1.1 Текст", "1. Текст" с номерами пунктов
            if (Regex.IsMatch(trimmedLine, @"^\d+(\.\d+)*\s+.*\d*$", RegexOptions.IgnoreCase))
                return true;

            return false;
        }
    }
}