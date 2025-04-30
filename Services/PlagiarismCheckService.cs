using PlagiarismGuard.Data;
using PlagiarismGuard.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace PlagiarismGuard.Services
{
    public class PlagiarismCheckService
    {
        private readonly PlagiarismContext _context;

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

        public Check PerformCheck(int documentId, int userId)
        {
            var documentText = _context.DocumentTexts
                .FirstOrDefault(dt => dt.DocumentId == documentId)?.TextContent;
            if (string.IsNullOrEmpty(documentText))
                throw new Exception("Текст документа не найден");

            return PerformCheckText(documentText, documentId, userId);
        }

        public Check PerformCheckText(string inputText, int documentId, int userId)
        {
            if (string.IsNullOrEmpty(inputText))
                throw new Exception("Текст для проверки не указан");

            var otherDocs = _context.DocumentTexts
                .Where(dt => dt.DocumentId != documentId)
                .ToList();
            var results = new List<CheckResult>();

            foreach (var otherDoc in otherDocs)
            {
                if (string.IsNullOrEmpty(otherDoc.TextContent))
                    continue;

                var (similarity, matchedText) = CalculateSimilarity(inputText, otherDoc.TextContent);
                if (similarity > 0.8)
                {
                    var checkResult = new CheckResult
                    {
                        SourceDocumentId = otherDoc.DocumentId,
                        MatchedText = matchedText,
                        Similarity = (float)similarity
                    };
                    results.Add(checkResult);
                }
            }

            var check = new Check
            {
                DocumentId = documentId,
                UserId = userId,
                Similarity = results.Any() ? results.Average(r => r.Similarity) : 0,
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

            var sentences1 = Regex.Split(text1, @"(?<=[.!?])\s+").Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
            var sentences2 = Regex.Split(text2, @"(?<=[.!?])\s+").Where(s => !string.IsNullOrWhiteSpace(s)).ToList();

            if (!sentences1.Any() || !sentences2.Any())
                return (0, "Нет совпадений");

            var matchedFragments = new List<string>();
            double totalSimilarity = 0;
            int validComparisons = 0;

            foreach (var sentence1 in sentences1)
            {
                double bestSimilarity = 0;
                string bestMatch = null;

                foreach (var sentence2 in sentences2)
                {
                    int distance = LevenshteinDistance(sentence1, sentence2);
                    int maxLength = Math.Max(sentence1.Length, sentence2.Length);
                    double similarity = maxLength == 0 ? 0 : 1.0 - (double)distance / maxLength;

                    if (similarity > bestSimilarity)
                    {
                        bestSimilarity = similarity;
                        bestMatch = sentence1;
                    }
                }

                if (bestSimilarity > 0.8)
                {
                    matchedFragments.Add(bestMatch);
                    totalSimilarity += bestSimilarity;
                    validComparisons++;
                }
            }

            double averageSimilarity = validComparisons > 0 ? totalSimilarity / validComparisons : 0;
            string matchedText = matchedFragments.Any() ? string.Join("; ", matchedFragments) : "Нет совпадений";

            return (averageSimilarity, matchedText);
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