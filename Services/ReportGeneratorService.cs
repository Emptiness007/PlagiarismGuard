using Microsoft.Win32;
using PlagiarismGuard.Data;
using PlagiarismGuard.Models;
using PlagiarismGuard.Windows;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using Xceed.Document.NET;
using Xceed.Drawing;
using Xceed.Words.NET;
using static PlagiarismGuard.Windows.CustomMessageBox;
using VerticalAlignment = Xceed.Document.NET.VerticalAlignment;

namespace PlagiarismGuard.Services
{
    public class ReportGeneratorService
    {
        private readonly PlagiarismContext _context;

        public ReportGeneratorService(PlagiarismContext context)
        {
            _context = context;
        }

        public void GeneratePlagiarismReport(Check check, List<CheckResult> results, List<LinkCheckResult> linkResults)
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
                    CustomMessageBox.Show("Формат .doc не поддерживается библиотекой Xceed.Words.NET. Отчет будет сохранен в формате .docx.",
                        "Предупреждение", MessageType.Information);
                    filePath = Path.ChangeExtension(filePath, ".docx");
                }

                using (var doc = DocX.Create(filePath))
                {
                    doc.PageLayout.Orientation = Orientation.Portrait;
                    doc.MarginTop = 50;
                    doc.MarginBottom = 50;
                    doc.MarginLeft = 70;
                    doc.MarginRight = 50;

                    doc.InsertParagraph("Отчет по проверке на плагиат")
                        .Font("Arial")
                        .FontSize(18)
                        .Bold()
                        .Color(Color.DarkBlue)
                        .Alignment = Alignment.center;
                    doc.InsertParagraph()
                        .SpacingAfter(20);

                    var infoPara = doc.InsertParagraph()
                        .Font("Arial")
                        .FontSize(12);
                    infoPara.Append($"Дата проверки: {check.CheckedAt:dd.MM.yyyy HH:mm}\n")
                           .Append($"Проверяемый документ: {_context.Documents.Where(x => x.Id == check.DocumentId).First().FileName}\n")
                           .Append($"Процент плагиата: {check.Similarity:F2}%")
                           .SpacingAfter(10);

                    doc.InsertParagraph("Совпадения с документами")
                        .Font("Arial")
                        .FontSize(14)
                        .Bold()
                        .Color(Color.DarkBlue)
                        .SpacingBefore(10)
                        .SpacingAfter(10);

                    // Таблица совпадений с документами
                    if (results.Any())
                    {
                        var table = doc.AddTable(results.Count + 1, 4);
                        table.Design = TableDesign.None;
                        table.Alignment = Alignment.center;

                        // Заголовки таблицы
                        var headerRow = table.Rows[0];
                        headerRow.Cells[0].Paragraphs[0].Append("№").Bold().Color(Color.Black);
                        headerRow.Cells[1].Paragraphs[0].Append("Источник").Bold().Color(Color.Black);
                        headerRow.Cells[2].Paragraphs[0].Append("Фрагмент").Bold().Color(Color.Black);
                        headerRow.Cells[3].Paragraphs[0].Append("Сходство").Bold().Color(Color.Black);
                        headerRow.Cells[0].FillColor = Color.LightGray;
                        headerRow.Cells[1].FillColor = Color.LightGray;
                        headerRow.Cells[2].FillColor = Color.LightGray;
                        headerRow.Cells[3].FillColor = Color.LightGray;
                        foreach (var cell in headerRow.Cells)
                        {
                            cell.VerticalAlignment = VerticalAlignment.Center;
                            cell.Paragraphs[0].Alignment = Alignment.center;
                        }

                        for (int i = 0; i < results.Count; i++)
                        {
                            var result = results[i];
                            var row = table.Rows[i + 1];
                            row.Cells[0].Paragraphs[0].Append((i + 1).ToString()).Alignment = Alignment.center;
                            row.Cells[1].Paragraphs[0].Append(
                                _context.Documents.First(d => d.Id == result.SourceDocumentId).FileName
                            );
                            row.Cells[2].Paragraphs[0].Append(result.MatchedText);
                            row.Cells[3].Paragraphs[0].Append($"{result.Similarity:F2}%").Alignment = Alignment.center;
                            foreach (var cell in row.Cells)
                            {
                                cell.VerticalAlignment = VerticalAlignment.Center;
                            }
                        }

                        table.AutoFit = AutoFit.Contents;

                        // Границы таблицы
                        table.SetBorder(TableBorderType.InsideH, new Border(BorderStyle.Tcbs_single, BorderSize.one, 0, Color.Black));
                        table.SetBorder(TableBorderType.InsideV, new Border(BorderStyle.Tcbs_single, BorderSize.one, 0, Color.Black));
                        table.SetBorder(TableBorderType.Top, new Border(BorderStyle.Tcbs_single, BorderSize.one, 0, Color.Black));
                        table.SetBorder(TableBorderType.Bottom, new Border(BorderStyle.Tcbs_single, BorderSize.one, 0, Color.Black));
                        table.SetBorder(TableBorderType.Left, new Border(BorderStyle.Tcbs_single, BorderSize.one, 0, Color.Black));
                        table.SetBorder(TableBorderType.Right, new Border(BorderStyle.Tcbs_single, BorderSize.one, 0, Color.Black));

                        doc.InsertTable(table);
                    }
                    else
                    {
                        doc.InsertParagraph("Совпадений с документами не найдено")
                            .Font("Arial")
                            .FontSize(12)
                            .Italic()
                            .SpacingAfter(10);
                    }

                    doc.InsertParagraph("Проверка ссылок")
                        .Font("Arial")
                        .FontSize(14)
                        .Bold()
                        .Color(Color.DarkBlue)
                        .SpacingBefore(20)
                        .SpacingAfter(10);

                    // Таблица проверки ссылок
                    if (linkResults.Any())
                    {
                        var linkTable = doc.AddTable(linkResults.Count + 1, 3);
                        linkTable.Design = TableDesign.None;
                        linkTable.Alignment = Alignment.center;

                        // Заголовки таблицы
                        var linkHeaderRow = linkTable.Rows[0];
                        linkHeaderRow.Cells[0].Paragraphs[0].Append("№").Bold().Color(Color.Black);
                        linkHeaderRow.Cells[1].Paragraphs[0].Append("Ссылка").Bold().Color(Color.Black);
                        linkHeaderRow.Cells[2].Paragraphs[0].Append("Статус").Bold().Color(Color.Black);
                        linkHeaderRow.Cells[0].FillColor = Color.LightGray;
                        linkHeaderRow.Cells[1].FillColor = Color.LightGray;
                        linkHeaderRow.Cells[2].FillColor = Color.LightGray;
                        foreach (var cell in linkHeaderRow.Cells)
                        {
                            cell.VerticalAlignment = VerticalAlignment.Center;
                            cell.Paragraphs[0].Alignment = Alignment.center;
                        }

                        // Заполнение таблицы
                        for (int i = 0; i < linkResults.Count; i++)
                        {
                            var linkResult = linkResults[i];
                            var row = linkTable.Rows[i + 1];
                            row.Cells[0].Paragraphs[0].Append((i + 1).ToString()).Alignment = Alignment.center;
                            row.Cells[1].Paragraphs[0].Append(linkResult.Url);
                            row.Cells[2].Paragraphs[0].Append(linkResult.Status).Alignment = Alignment.center;
                            foreach (var cell in row.Cells)
                            {
                                cell.VerticalAlignment = VerticalAlignment.Center;
                            }
                        }
                        linkTable.SetColumnWidth(0, 50);
                        linkTable.AutoFit = AutoFit.Contents;

                        // Границы таблицы
                        linkTable.SetBorder(TableBorderType.InsideH, new Border(BorderStyle.Tcbs_single, BorderSize.one, 0, Color.Black));
                        linkTable.SetBorder(TableBorderType.InsideV, new Border(BorderStyle.Tcbs_single, BorderSize.one, 0, Color.Black));
                        linkTable.SetBorder(TableBorderType.Top, new Border(BorderStyle.Tcbs_single, BorderSize.one, 0, Color.Black));
                        linkTable.SetBorder(TableBorderType.Bottom, new Border(BorderStyle.Tcbs_single, BorderSize.one, 0, Color.Black));
                        linkTable.SetBorder(TableBorderType.Left, new Border(BorderStyle.Tcbs_single, BorderSize.one, 0, Color.Black));
                        linkTable.SetBorder(TableBorderType.Right, new Border(BorderStyle.Tcbs_single, BorderSize.one, 0, Color.Black));

                        doc.InsertTable(linkTable);
                    }
                    else
                    {
                        doc.InsertParagraph("Ссылки не найдены")
                            .Font("Arial")
                            .FontSize(12)
                            .Italic()
                            .SpacingAfter(10);
                    }


                    doc.Save();
                }

                CustomMessageBox.Show( $"Отчет успешно сохранен в {filePath}", "Успех", MessageType.Information);
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show( $"Ошибка при создании отчета: {ex.Message}\nПодробности: {ex.InnerException?.Message ?? "Нет дополнительной информации"}",
                    "Ошибка", MessageType.Error);
            }
        }
    }
}