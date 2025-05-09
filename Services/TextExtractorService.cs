using System.IO;
using System.Text;
using Xceed.Words.NET;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using Xceed.Document.NET;

namespace PlagiarismGuard.Services
{
    public class TextExtractorService
    {
        public string ExtractText(string filePath, string format)
        {
            switch (format.ToLower())
            {
                case "docx":
                    return ExtractFromDocx(filePath);
                case "pdf":
                    return ExtractFromPdf(filePath);
                default:
                    throw new NotSupportedException("Формат файла не поддерживается");
            }
        }

        private string ExtractFromDocx(string filePath)
        {
            try
            {
                using (var doc = DocX.Load(filePath))
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

        private string ExtractFromPdf(string filePath)
        {
            try
            {
                StringBuilder text = new StringBuilder();
                using (var pdfReader = new PdfReader(filePath))
                using (var pdfDoc = new PdfDocument(pdfReader))
                {
                    for (int page = 1; page <= pdfDoc.GetNumberOfPages(); page++)
                    {
                        text.Append(PdfTextExtractor.GetTextFromPage(pdfDoc.GetPage(page)));
                    }
                }
                return text.ToString();
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

        private bool IsTitlePageContent(Paragraph paragraph)
        {
            string[] titlePageKeywords = {"Содержание", "Курсовая работа", "Дипломная работа", "Дипломный проект", "Курсовой проект", "учреждение", "ЗАДАНИЕ", "Пояснительная записка", "Министерство", "Факультет", "Кафедра", "Выполнил", "Проверил", "Оглавление"};
            return titlePageKeywords.Any(keyword => paragraph.Text.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }
    }
}