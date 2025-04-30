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

                    foreach (var paragraph in doc.Paragraphs)
                    {
                        if (string.IsNullOrWhiteSpace(paragraph.Text))
                            continue;

                        if (IsHeading(paragraph))
                            continue;

                        if (IsCaption(paragraph))
                            continue;

                        if (skipTitlePage && IsTitlePageContent(paragraph))
                            continue;

                        if (paragraph.StyleName != null && paragraph.StyleName.Equals("Heading 1", StringComparison.OrdinalIgnoreCase))
                        {
                            skipTitlePage = false;
                            continue;
                        }

                        text.AppendLine(paragraph.Text);
                    }

                    return text.ToString().Trim();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при извлечении текста из .docx: {ex.Message}");
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

            return false;
        }

        private bool IsCaption(Paragraph paragraph)
        {
            if (paragraph.StyleName != null &&
                paragraph.StyleName.Equals("Caption", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            string[] captionKeywords = { "Рисунок", "Таблица", "Figure", "Table", "Caption" };
            if (captionKeywords.Any(keyword => paragraph.Text.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
            if (paragraph.Text.Length < 100)
            {
                return true;
            }

            return false;
        }

        private bool IsTitlePageContent(Paragraph paragraph)
        {
            string[] titlePageKeywords = { "Содержание", "Курсовая работа", "Дипломная работа", "учреждение", "университет" };
            return titlePageKeywords.Any(keyword => paragraph.Text.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }
    }
}