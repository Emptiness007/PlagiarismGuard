using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace PlagiarismGuard.Services
{
    public class CopyleaksApiService
    {
        private readonly string _apiKey = "1f638e28-c08e-4703-be9b-d575964cc32b"; 
        private readonly string _email = "alenafilimonova98@gmail.com"; 
        private readonly HttpClient _httpClient;
        private const string IdentityBaseUrl = "https://id.copyleaks.com/v3"; 
        private const string ApiBaseUrl = "https://api.copyleaks.com/v2"; 

        public CopyleaksApiService()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "PlagiarismGuard/1.0");
            _httpClient.Timeout = TimeSpan.FromSeconds(30); 
        }

        public async Task<string> LoginAsync()
        {
            var loginRequest = new { email = _email, key = _apiKey };
            var content = new StringContent(JsonConvert.SerializeObject(loginRequest), Encoding.UTF8, "application/json");
            var requestUrl = $"{IdentityBaseUrl}/account/login/api";
            Debug.WriteLine($"Sending login request to: {requestUrl}");

            var response = await _httpClient.PostAsync(requestUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();
            Debug.WriteLine($"Login Status: {response.StatusCode}, Response: {responseContent}");

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Failed to login to Copyleaks API: {response.StatusCode}, Content: {responseContent}");

            var loginResponse = JsonConvert.DeserializeObject<dynamic>(responseContent);
            return loginResponse.access_token;
        }

        public async Task<string> SubmitAiDetectionScanAsync(string accessToken, string text, string scanId)
        {
            if (string.IsNullOrEmpty(scanId) || scanId.Length < 3 || scanId.Length > 36 || !Regex.IsMatch(scanId, @"^[a-z0-9!@$^&-+%=_(){}<>';:/."",~`|]+$"))
                throw new ArgumentException("Invalid scanId. Must be 3-36 characters and contain only allowed characters.");

            var submission = new
            {
                text = text,
                sandbox = false, 
                explain = true,   
                sensitivity = 2  
            };
            var content = new StringContent(JsonConvert.SerializeObject(submission), Encoding.UTF8, "application/json");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var requestUrl = $"{ApiBaseUrl}/writer-detector/{scanId}/check";
            Debug.WriteLine($"Submitting scan to: {requestUrl}");

            var response = await _httpClient.PostAsync(requestUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();
            Debug.WriteLine($"Submit Status: {response.StatusCode}, Response: {responseContent}");

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Failed to submit AI detection scan: {response.StatusCode}, Content: {responseContent}");

            return scanId;
        }

        public async Task<AiDetectionResult> GetAiDetectionResultAsync(string accessToken, string scanId)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var requestUrl = $"{ApiBaseUrl}/writer-detector/{scanId}/results";
            int maxAttempts = 10;
            int delayMs = 3000;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                Debug.WriteLine($"Attempt {attempt}, Requesting: {requestUrl}");
                var response = await _httpClient.GetAsync(requestUrl);
                var responseContent = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"Result Status: {response.StatusCode}, Response: {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonConvert.DeserializeObject<AiDetectionResult>(responseContent);
                    return result;
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    Debug.WriteLine("Result not ready, waiting...");
                    await Task.Delay(delayMs);
                    continue;
                }
                else
                {
                    throw new Exception($"Failed to get AI detection results: {response.StatusCode}, Content: {responseContent}");
                }
            }

            throw new Exception("AI detection result not available after maximum attempts");
        }
    }

    public class AiDetectionResult
    {
        [JsonProperty("ai")]
        public float Probability { get; set; } 
        [JsonProperty("explain")]
        public string Explanation { get; set; } 
    }
}