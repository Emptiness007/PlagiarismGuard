using System;
using System.IO;
using System.Text;
using Xceed.Words.NET;
using System.Linq;
using Xceed.Document.NET;
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
                    // 🧹 Удаляем все таблицы из документа
                    var tables = doc.Tables.ToList();
                    foreach (var table in tables)
                    {
                        table.Remove(); // удаляет таблицу и весь её текст
                    }

                    StringBuilder text = new StringBuilder();
                    bool skipTitlePage = true;
                    int paragraphCount = 0;
                    const int maxTitlePageParagraphs = 30;

                    foreach (var paragraph in doc.Paragraphs)
                    {
                        if (string.IsNullOrWhiteSpace(paragraph.Text))
                            continue;

                        paragraphCount++;

                        if (IsHeadingParagraph(paragraph))
                        {
                            if (paragraph.StyleName != null &&
                                paragraph.StyleName.Equals("Heading 1", StringComparison.OrdinalIgnoreCase))
                            {
                                skipTitlePage = false;
                                continue;
                            }
                            else
                            {
                                continue; // игнорируем все заголовки
                            }
                        }

                        if (IsCaption(paragraph))
                            continue;

                        if (skipTitlePage)
                        {
                            if (IsTitlePageContent(paragraph) || paragraphCount <= maxTitlePageParagraphs)
                                continue;
                            else
                                skipTitlePage = false;
                        }

                        if (paragraph.IsListItem)
                        {
                            string prefix = GetListPrefix(paragraph);
                            text.AppendLine($"{prefix} {paragraph.Text}");
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


        private static bool IsHeadingParagraph(Paragraph paragraph)
        {
            string style = paragraph.StyleName ?? "";
            string text = paragraph.Text.Trim();

            // Стандартные стили заголовков
            if (!string.IsNullOrEmpty(style) &&
                (style.StartsWith("Heading", StringComparison.OrdinalIgnoreCase) ||
                 style.StartsWith("Заголовок", StringComparison.OrdinalIgnoreCase)))
                return true;

            // Регулярка для многоуровневых заголовков: 1, 1.1, 2.3.4 и т.д.
            if (Regex.IsMatch(text, @"^\d+(\.\d+)*[\.\)]?\s+.+$"))
                return true;

            // Ключевые слова-заголовки (точное сравнение)
            string[] knownHeadings = {
                "Введение",
                "Заключение",
                "Список использованных источников",
                "Список литературы",
                "Приложение",
                "Библиография"
            };

            return knownHeadings.Any(h => string.Equals(h, text, StringComparison.OrdinalIgnoreCase));
        }

        private bool IsCaption(Paragraph paragraph)
        {
            if (!string.IsNullOrEmpty(paragraph.StyleName) &&
                paragraph.StyleName.Equals("Caption", StringComparison.OrdinalIgnoreCase))
                return true;

            string[] keywords = { "ГЛАВА", "Глава", "Рисунок", "Таблица", "Figure", "Table", "Caption" };
            return keywords.Any(k => paragraph.Text.Contains(k, StringComparison.OrdinalIgnoreCase));
        }

        private bool IsTitlePageContent(Paragraph paragraph)
        {
            string[] keywords = {
                "Содержание", "Курсовая работа", "Дипломная работа", "Дипломный проект",
                "Курсовой проект", "учреждение", "ЗАДАНИЕ", "Пояснительная записка",
                "Министерство", "Факультет", "Кафедра", "Выполнил", "Проверил", "Оглавление"
            };
            return keywords.Any(k => paragraph.Text.Contains(k, StringComparison.OrdinalIgnoreCase));
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
