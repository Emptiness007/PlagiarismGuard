using Microsoft.Win32;
using PlagiarismGuard.Data;
using PlagiarismGuard.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using Xceed.Document.NET;
using Xceed.Words.NET;

namespace PlagiarismGuard.Services
{
    public class ReportGeneratorService
    {
        private readonly PlagiarismContext _context;

        public ReportGeneratorService(PlagiarismContext context)
        {
            _context = context;
        }

        public void GeneratePlagiarismReport(Check check, List<CheckResult> results)
        {
            try
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "Word Document (*.docx)|*.docx|Old Word Document (*.doc)|*.doc",
                    DefaultExt = "docx",
                    FileName = $"PlagiarismReport_{DateTime.Now:yyyyMMddHHmmss}"
                };

                if (saveFileDialog.ShowDialog() != true)
                    return;

                string filePath = saveFileDialog.FileName;
                bool isDoc = Path.GetExtension(filePath).ToLower() == ".doc";

                if (isDoc)
                {
                    MessageBox.Show(
                        "Формат .doc не поддерживается библиотекой Xceed.Words.NET. Отчет будет сохранен в формате .docx.",
                        "Предупреждение",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                    filePath = Path.ChangeExtension(filePath, ".docx");
                }

                using (var doc = DocX.Create(filePath))
                {
                    doc.InsertParagraph("Отчет по проверке на плагиат")
                        .FontSize(16)
                        .Bold()
                        .Alignment = Alignment.center;
                    doc.InsertParagraph($"Дата проверки: {check.CheckedAt:dd.MM.yyyy HH:mm}")
                        .FontSize(14);
                    doc.InsertParagraph($"Процент плагиата: {check.Similarity:F2}%")
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
                            table.Rows[i + 1].Cells[1].Paragraphs[0].Append(
                                _context.Documents.First(d => d.Id == result.SourceDocumentId).FileName
                            );
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

                MessageBox.Show(
                    $"Отчет успешно сохранен в {filePath}",
                    "Успех",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Ошибка при создании отчета: {ex.Message}\nInner Exception: {ex.InnerException?.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }
    }
}