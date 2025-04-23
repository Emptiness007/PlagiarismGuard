using System.IO;
using System.Text;
using Xceed.Words.NET;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;

namespace PlagiarismGuard.Services
{
    public class TextExtractorService
    {
        public string ExtractText(string filePath, string format)
        {
            switch (format.ToLower())
            {
                case "txt":
                    return File.ReadAllText(filePath);
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
                    return doc.Text;
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
    }
}