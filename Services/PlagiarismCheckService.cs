using HtmlAgilityPack;
using PlagiarismGuard.Data;
using PlagiarismGuard.Models;
using ScanDocumentsPriemDev.Classes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PlagiarismGuard.Services
{
    public class PlagiarismCheckService
    {
        private readonly PlagiarismContext _context;
        private readonly HttpClient _httpClient;
        public PlagiarismCheckService(PlagiarismContext context)
        {
            _context = context;
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(10)
            };
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

            string inputTextHash = ComputeTextHash(inputText);

            var sentences = Regex.Split(inputText, @"(?<=[.!?])\s+")
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();
            var referenceDocs = _context.DocumentTexts
                .Where(dt => dt.DocumentId != documentId &&
                            dt.TextHash != inputTextHash &&
                            _context.Documents.Any(d => d.Id == dt.DocumentId && d.IsUsedForPlagiarismCheck))
                .ToList();


            var results = new List<CheckResult>();
            var linkResults = new List<LinkCheckResult>();
            var matchedByDocument = new Dictionary<int, (List<string> MatchedFragments, List<double> Similarities)>();

            foreach (var sentence in sentences)
            {
                foreach (var refDoc in referenceDocs)
                {
                    if (string.IsNullOrEmpty(refDoc.TextContent))
                        continue;

                    var matches = CalculateSimilarity(sentence, refDoc.TextContent);

                    if (matches.Any())
                    {
                        if (!matchedByDocument.ContainsKey(refDoc.DocumentId))
                        {
                            matchedByDocument[refDoc.DocumentId] = (new List<string>(), new List<double>());
                        }

                        foreach (var (similarity, matchedText) in matches)
                        {
                            matchedByDocument[refDoc.DocumentId].MatchedFragments.Add(matchedText);
                            matchedByDocument[refDoc.DocumentId].Similarities.Add(similarity);
                        }
                    }
                }
            }

            var links = ExtractLinks(inputText);
            linkResults.AddRange(await VerifyLinksAsync(links, sentences));

            results.AddRange(matchedByDocument
                .Where(kvp => kvp.Value.MatchedFragments.Any())
                .Select(kvp => new CheckResult
                {
                    SourceDocumentId = kvp.Key,
                    MatchedText = string.Join("; ", kvp.Value.MatchedFragments),
                    Similarity = (float)kvp.Value.Similarities.Average()
                }));

            int totalInputLength = inputText.Length;
            int totalMatchedLength = results.Sum(r => r.MatchedText.Length);
            float plagiarismPercentage = totalInputLength > 0 ?
                (float)totalMatchedLength / totalInputLength * 100 : 0;

            var check = new Check
            {
                DocumentId = documentId,
                UserId = userId,
                Similarity = plagiarismPercentage,
                CheckedAt = DateTime.Now
            };

            _context.Checks.Add(check);
            _context.SaveChanges();

            foreach (var result in results)
            {
                result.CheckId = check.Id;
                _context.CheckResults.Add(result);
            }

            foreach (var linkResult in linkResults)
            {
                linkResult.CheckId = check.Id;
                _context.LinkCheckResults.Add(linkResult);
            }

            _context.SaveChanges();

            return check;
        }

        private List<string> ExtractLinks(string text)
        {
            var urlRegex = new Regex(@"(https?://[^\s""<>\[\]\{\}]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            return urlRegex.Matches(text)
                .Cast<Match>()
                .Select(m => m.Value)
                .Distinct()
                .ToList();
        }

        private async Task<List<LinkCheckResult>> VerifyLinksAsync(List<string> links, List<string> sentences)
        {
            var linkResults = new List<LinkCheckResult>();

            Debug.WriteLine($"Начало проверки ссылок. Всего ссылок: {links.Count}");

            foreach (var link in links)
            {
                Debug.WriteLine($"Проверка ссылки: {link}");
                bool isMatchFound = false;
                try
                {
                    string webContent = await FetchWebContentAsync(link);
                    if (!string.IsNullOrEmpty(webContent))
                    {
                        Debug.WriteLine($"Контент успешно извлечен для {link}. Длина контента: {webContent.Length} символов");
                        foreach (var sentence in sentences)
                        {
                            var matches = CalculateSimilarity(sentence, webContent);
                            if (matches.Any())
                            {
                                Debug.WriteLine($"Совпадения найдены для ссылки {link}. Предложение: '{sentence}', Совпадений: {matches.Count}");
                                foreach (var (similarity, matchedText) in matches)
                                {
                                    Debug.WriteLine($"Сходство: {similarity:F2}, Совпавший текст: '{matchedText}'");
                                }
                                isMatchFound = true;
                                break;
                            }
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"Контент не извлечен для {link}.");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Ошибка при проверке ссылки {link}: {ex.Message}");
                }

                Debug.WriteLine($"Результат проверки ссылки {link}: {(isMatchFound ? "Совпадение найдено" : "Совпадений нет")}");

                linkResults.Add(new LinkCheckResult
                {
                    Url = link,
                    IsMatchFound = isMatchFound
                });
            }

            Debug.WriteLine($"Проверка ссылок завершена. Обработано ссылок: {linkResults.Count}");
            return linkResults;
        }

        private async Task<string> FetchWebContentAsync(string url)
        {
            try
            {
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                string html = await response.Content.ReadAsStringAsync();

                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(html);

                var rootNode = htmlDoc.DocumentNode.SelectSingleNode("//body") ?? htmlDoc.DocumentNode;

                var nodesToRemove = rootNode.SelectNodes("//script | //style | //nav | //footer | //header | //iframe | //noscript | //aside | //div[contains(@class, 'advert')]");
                if (nodesToRemove != null)
                {
                    foreach (var node in nodesToRemove)
                    {
                        node.Remove();
                    }
                }

                var textBuilder = new StringBuilder();
                var textNodes = rootNode.DescendantsAndSelf()
                    .Where(n => n.NodeType == HtmlNodeType.Text && !string.IsNullOrWhiteSpace(n.InnerText))
                    .Select(n => n.InnerText.Trim());

                foreach (var text in textNodes)
                {
                    if (!string.IsNullOrEmpty(text) && text.Length > 2 && !Regex.IsMatch(text, @"^\s*[\{\}\[\]\(\);]+$"))
                    {
                        textBuilder.AppendLine(text);
                    }
                }

                string result = textBuilder.ToString();
                Debug.WriteLine($"Извлеченный контент ({url}): {result}");
                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при извлечении контента из {url}: {ex.Message}");
                return string.Empty;
            }
        }

        private List<(double Similarity, string MatchedText)> CalculateSimilarity(string text1, string text2)
        {
            var matches = new List<(double Similarity, string MatchedText)>();

            if (string.IsNullOrEmpty(text1) || string.IsNullOrEmpty(text2))
                return matches;

            var sentences2 = Regex.Split(text2, @"(?<=[.!?])\s+").Where(s => !string.IsNullOrWhiteSpace(s)).ToList();

            if (!sentences2.Any())
                return matches;

            foreach (var sentence2 in sentences2)
            {
                int distance = LevenshteinDistance(text1, sentence2);
                int maxLength = Math.Max(text1.Length, sentence2.Length);
                double similarity = maxLength == 0 ? 0 : 1.0 - (double)distance / maxLength;

                if (similarity > 0.8)
                {
                    matches.Add((similarity, text1));
                }
            }

            return matches;
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
