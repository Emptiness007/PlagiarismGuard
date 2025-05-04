using PlagiarismGuard.Data;
using PlagiarismGuard.Models;
using ScanDocumentsPriemDev.Classes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PlagiarismGuard.Services
{
    public class PlagiarismCheckService
    {
        private readonly PlagiarismContext _context;
        private readonly CopyleaksApiService _copyleaksService = new CopyleaksApiService();

        public PlagiarismCheckService(PlagiarismContext context)
        {
            _context = context;
        }

        public bool DocumentExists(string textContent)
        {
            if (string.IsNullOrEmpty(textContent))
                return false;

            string textHash = ComputeTextHash(textContent);
            return _context.DocumentTexts.Any(dt => dt.TextHash == textHash);
        }

        public string ComputeTextHash(string text)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(text);
                byte[] hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        public async Task<Check> PerformCheckTextAsync(string inputText, int documentId, int userId)
        {
            if (string.IsNullOrEmpty(inputText))
                throw new Exception("Текст для проверки не указан");

            // Разбиваем входной текст на предложения
            var sentences = Regex.Split(inputText, @"(?<=[.!?])\s+")
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();
            var adminDocs = _context.DocumentTexts
                .Where(dt => dt.DocumentId != documentId &&
                            _context.Documents.Any(d => d.Id == dt.DocumentId &&
                                                    _context.Users.Any(u => u.Id == d.UserId && u.Role == "admin")))
                .ToList();

            // Группировка совпадений по документам
            var results = new List<CheckResult>();
            var matchedByDocument = new Dictionary<int, (List<string> MatchedFragments, List<double> Similarities)>();

            foreach (var sentence in sentences)
            {
                foreach (var adminDoc in adminDocs)
                {
                    if (string.IsNullOrEmpty(adminDoc.TextContent))
                        continue;

                    var (similarity, matchedText) = CalculateSimilarity(sentence, adminDoc.TextContent);

                    if (similarity > 0.8 && matchedText != "Нет совпадений")
                    {
                        if (!matchedByDocument.ContainsKey(adminDoc.DocumentId))
                        {
                            matchedByDocument[adminDoc.DocumentId] = (new List<string>(), new List<double>());
                        }

                        // Добавляем совпадающее предложение и его сходство
                        matchedByDocument[adminDoc.DocumentId].MatchedFragments.Add(matchedText);
                        matchedByDocument[adminDoc.DocumentId].Similarities.Add(similarity);
                    }
                }
            }

            // Создаем CheckResult для каждого документа
            foreach (var docId in matchedByDocument.Keys)
            {
                var (matchedFragments, similarities) = matchedByDocument[docId];
                if (matchedFragments.Any())
                {
                    // Объединяем все совпадающие предложения в одну строку
                    string combinedMatchedText = string.Join("; ", matchedFragments);
                    // Вычисляем среднее сходство для документа
                    double averageSimilarity = similarities.Average();

                    results.Add(new CheckResult
                    {
                        SourceDocumentId = docId,
                        MatchedText = combinedMatchedText,
                        Similarity = (float)averageSimilarity
                    });
                }
            }

            float? aiGeneratedPercentage = null;
            try
            {
                string accessToken = await _copyleaksService.LoginAsync();
                string scanId = Guid.NewGuid().ToString().Replace("-", ""); // Удаляем дефисы для соответствия требованиям
                await _copyleaksService.SubmitAiDetectionScanAsync(accessToken, inputText, scanId);
                var aiResult = await _copyleaksService.GetAiDetectionResultAsync(accessToken, scanId);
                aiGeneratedPercentage = aiResult.Probability * 100;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"AI Detection error: {ex.Message}");
            }

            int totalInputLength = inputText.Length;
            int totalMatchedLength = results.Sum(r => r.MatchedText.Length);
            float plagiarismPercentage = totalInputLength > 0 ?
                (float)totalMatchedLength / totalInputLength * 100 : 0;

            var check = new Check
            {
                DocumentId = documentId,
                UserId = userId,
                Similarity = plagiarismPercentage,
                AiGeneratedPercentage = aiGeneratedPercentage,
                CheckedAt = DateTime.Now
            };

            _context.Checks.Add(check);
            _context.SaveChanges();

            foreach (var result in results)
            {
                result.CheckId = check.Id;
                _context.CheckResults.Add(result);
            }
            _context.SaveChanges();

            return check;
        }

        private (double Similarity, string MatchedText) CalculateSimilarity(string text1, string text2)
        {
            if (string.IsNullOrEmpty(text1) || string.IsNullOrEmpty(text2))
                return (0, "Нет совпадений");

            var sentences2 = Regex.Split(text2, @"(?<=[.!?])\s+").Where(s => !string.IsNullOrWhiteSpace(s)).ToList();

            if (!sentences2.Any())
                return (0, "Нет совпадений");

            double bestSimilarity = 0;
            string bestMatch = null;

            foreach (var sentence2 in sentences2)
            {
                int distance = LevenshteinDistance(text1, sentence2);
                int maxLength = Math.Max(text1.Length, sentence2.Length);
                double similarity = maxLength == 0 ? 0 : 1.0 - (double)distance / maxLength;

                if (similarity > bestSimilarity)
                {
                    bestSimilarity = similarity;
                    bestMatch = text1; // Сохраняем исходное предложение из проверяемого текста
                }
            }

            return bestSimilarity > 0.8 ? (bestSimilarity, bestMatch) : (0, "Нет совпадений");
        }

        private int LevenshteinDistance(string s, string t)
        {
            if (string.IsNullOrEmpty(s)) return t?.Length ?? 0;
            if (string.IsNullOrEmpty(t)) return s.Length;

            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            for (int i = 0; i <= n; i++)
                d[i, 0] = i;
            for (int j = 0; j <= m; j++)
                d[0, j] = j;

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }
            return d[n, m];
        }
    }
}