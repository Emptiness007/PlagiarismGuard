using System;
using System.IO;
using System.Text;
using Xceed.Words.NET;
using System.Linq;
using Xceed.Document.NET;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace PlagiarismGuard.Services
{
    public class TextExtractorService
    {
        public string ExtractText(byte[] fileContent, string format)
        {
            switch (format.ToLower())
            {
                case "docx": return ExtractFromDocx(fileContent);
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
                    var tables = doc.Tables.ToList();
                    foreach (var table in tables)
                    {
                        table.Remove();
                    }

                    StringBuilder text = new StringBuilder();
                    bool skipTitlePage = true;
                    int paragraphCount = 0;
                    const int maxTitlePageParagraphs = 25;

                    foreach (var paragraph in doc.Paragraphs)
                    {
                        string rawText = paragraph.Text;
                        if (string.IsNullOrWhiteSpace(rawText))
                            continue;

                        paragraphCount++;

                        if (IsHeadingParagraph(paragraph))
                            continue;

                        string cleanedText = rawText.Trim();

                        if (IsCaption(paragraph))
                            continue;

                        if (skipTitlePage)
                        {
                            if (IsTitlePageContent(paragraph) || paragraphCount <= maxTitlePageParagraphs)
                                continue;
                            else
                                skipTitlePage = false;
                        }

                        if (string.IsNullOrWhiteSpace(cleanedText))
                            continue;

                        if (paragraph.IsListItem)
                        {
                            string prefix = GetListPrefix(paragraph);
                            text.AppendLine($"{prefix} {cleanedText}");
                        }
                        else
                        {
                            text.AppendLine(cleanedText);
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

        private bool IsHeadingParagraph(Paragraph paragraph)
        {
            string text = paragraph.Text.Trim();
            if (string.IsNullOrWhiteSpace(text))
                return false;

            // Проверяем, содержит ли параграф ссылку
            bool containsLink = paragraph.Hyperlinks.Any() || // Проверяем наличие гиперссылок
                               Regex.IsMatch(text, @"(https?://[^\s]+)|(www\.[^\s]+)|([^\s]+\.(com|org|net|ru|edu))"); // Проверяем текст на URL

            // Параграф считается заголовком, если он НЕ заканчивается на '.', ';' или ':' И НЕ содержит ссылку
            return !text.EndsWith(".") && !text.EndsWith(";") && !text.EndsWith(":") && !containsLink;
        }

        private bool IsCaption(Paragraph paragraph)
        {
            string textLower = paragraph.Text.ToLower();
            string[] keywords = { "рисунок", "таблица", "figure", "table", "caption" };
            return keywords.Any(k => textLower.Contains(k));
        }

        private bool IsTitlePageContent(Paragraph paragraph)
        {
            string textLower = paragraph.Text.ToLower();
            string[] keywords = {
                "содержание", "курсовая работа", "дипломная работа", "дипломный проект",
                "курсовой проект", "учреждение", "задание", "пояснительная записка",
                "министерство", "факультет", "кафедра", "выполнил", "проверил", "оглавление",
                "автор", "научный руководитель", "аннотация", "abstract", "table of contents",
                "список сокращений"
            };
            return keywords.Any(k => textLower.Contains(k));
        }

        private string GetListPrefix(Paragraph paragraph)
        {
            if (!paragraph.IsListItem) return string.Empty;

            int level = (int)paragraph.IndentLevel;
            string indent = new string(' ', level * 2);

            return paragraph.ListItemType switch
            {
                ListItemType.Bulleted => $"{indent}•",
                ListItemType.Numbered => $"{indent}",
                _ => $"{indent}*"
            };
        }
    }
}