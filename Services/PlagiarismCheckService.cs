using HtmlAgilityPack;
using PlagiarismGuard.Data;
using PlagiarismGuard.Models;
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
        private const int MaxTextLength = 10000;
        private const int MinSentenceLength = 80;
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

        public async Task<Check> PerformCheckTextAsync(string inputText, int documentId, int userId, IProgress<string> progress = null, CancellationToken cancellationToken = default)
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
            var processedSentences = new HashSet<string>();

            int totalSentences = sentences.Count;
            int processedCount = 0;

            foreach (var sentence in sentences)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (sentence.Length < MinSentenceLength)
                {
                    System.Diagnostics.Debug.WriteLine($"Пропущено короткое предложение: {sentence} (длина: {sentence.Length})");
                    processedCount++;
                    progress?.Report($"Проверка документа: {(int)((double)processedCount / totalSentences * 100)}%");
                    continue;
                }

                if (processedSentences.Contains(sentence))
                {
                    processedCount++;
                    progress?.Report($"Проверка документа: {(int)((double)processedCount / totalSentences * 100)}%");
                    continue;
                }

                foreach (var refDoc in referenceDocs)
                {
                    if (string.IsNullOrEmpty(refDoc.TextContent))
                        continue;

                    var match = CalculateSimilarity(sentence, refDoc.TextContent);

                    if (match != null)
                    {
                        if (!matchedByDocument.ContainsKey(refDoc.DocumentId))
                        {
                            matchedByDocument[refDoc.DocumentId] = (new List<string>(), new List<double>());
                        }

                        if (!matchedByDocument[refDoc.DocumentId].MatchedFragments.Contains(match.Value.MatchedText))
                        {
                            matchedByDocument[refDoc.DocumentId].MatchedFragments.Add(match.Value.MatchedText);
                            matchedByDocument[refDoc.DocumentId].Similarities.Add(match.Value.Similarity);
                        }

                        processedSentences.Add(sentence);
                        break;
                    }
                }

                processedCount++;
                progress?.Report($"Проверка документа: {(int)((double)processedCount / totalSentences * 100)}%");
            }

            var links = ExtractLinks(inputText);
            linkResults.AddRange(await VerifyLinksAsync(links, sentences, progress, cancellationToken));

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

            progress?.Report("Проверка документа: 100%");
            return check;
        }

        private (double Similarity, string MatchedText)? CalculateSimilarity(string text1, string text2)
        {
            if (string.IsNullOrEmpty(text1) || string.IsNullOrEmpty(text2))
                return null;

            var sentences2 = Regex.Split(text2, @"(?<=[.!?])\s+").Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
            if (!sentences2.Any())
                return null;

            (double Similarity, string MatchedText)? bestMatch = null;

            foreach (var sentence2 in sentences2)
            {
                int distance = LevenshteinDistance(text1, sentence2);
                int maxLength = Math.Max(text1.Length, sentence2.Length);
                double similarity = maxLength == 0 ? 0 : 1.0 - (double)distance / maxLength;

                if (similarity > 0.8)
                {
                    if (bestMatch == null || similarity > bestMatch.Value.Similarity)
                    {
                        bestMatch = (similarity, text1);
                    }
                }
            }

            return bestMatch;
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

        private async Task<List<LinkCheckResult>> VerifyLinksAsync(List<string> links, List<string> sentences, IProgress<string> progress, CancellationToken cancellationToken)
        {
            var linkResults = new List<LinkCheckResult>();
            var tasks = links.Select(link => VerifySingleLinkAsync(link, sentences, cancellationToken)).ToList();
            var results = await Task.WhenAll(tasks);
            linkResults.AddRange(results.Where(r => r != null));
            progress?.Report("Проверка документа: 100%");
            return linkResults;
        }

        private async Task<LinkCheckResult> VerifySingleLinkAsync(string link, List<string> sentences, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                string webContent = await FetchWebContentAsync(link);
                if (string.IsNullOrEmpty(webContent) || webContent.Length < 50)
                {
                    return new LinkCheckResult
                    {
                        Url = link,
                        IsMatchFound = false,
                        Status = "Контент отсутствует"
                    };
                }

                if (IsEnglishContent(webContent))
                {
                    return new LinkCheckResult
                    {
                        Url = link,
                        IsMatchFound = false,
                        Status = "Контент на английском"
                    };
                }

                bool isMatchFound = false;

                foreach (var sentence in sentences)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var match = CalculateSimilarity(sentence, webContent);
                    if (match != null)
                    {
                        isMatchFound = true;
                        break;
                    }
                }

                return new LinkCheckResult
                {
                    Url = link,
                    IsMatchFound = isMatchFound,
                    Status = isMatchFound ? "Совпадение найдено" : "Совпадений нет"
                };
            }
            catch (Exception ex)
            {
                return new LinkCheckResult
                {
                    Url = link,
                    IsMatchFound = false,
                    Status = "Ошибка при проверке"
                };
            }
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

                var nodesToRemove = rootNode.SelectNodes("//script | //style | //nav | //footer | //header | //iframe | //noscript | //aside");
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
                if (result.Length > MaxTextLength)
                    result = result.Substring(0, MaxTextLength);

                result = Regex.Replace(result, @"\s+", " ").Trim();

                return result;
            }
            catch (Exception ex)
            {
                return string.Empty;
            }
        }

        private bool IsEnglishContent(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            int latinCharCount = text.Count(c => (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'));
            double latinRatio = (double)latinCharCount / text.Length;

            string[] commonEnglishWords = { "the", "and", "is", "in", "to", "of", "a", "for", "on", "with" };
            bool hasEnglishWords = commonEnglishWords.Any(word => Regex.IsMatch(text, $@"\b{word}\b", RegexOptions.IgnoreCase));

            return latinRatio > 0.7 && hasEnglishWords;
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
