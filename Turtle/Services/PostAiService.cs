using Azure;
using Azure.Core;
using Humanizer;
using Microsoft.Identity.Client;
using System.IO;
using System.Net.Http.Headers;
using System.Runtime.ConstrainedExecution;
using System.Security.Policy;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using static System.Net.WebRequestMethods;

namespace Turtle.Services
{
    // Clasa pentru rezultatul analizei de sentiment
    public class CategoriesResult
    {
        public List<string> SuggestedCategoriesNames { get; set; } = [];
        public bool Success { get; set; } = false;
        public string? ErrorMessage { get; set; }
    }

    // Interfata serviciului pentru dependency injection
    public interface GoogleCategoryAnalysisService
    {
        Task<CategoriesResult> AnalyzeCategoriesAsync(string title, string content);
    }

    // Implementarea serviciului de analiza de categorii folosind OpenAI API
    public class CategoryAnalysisService : GoogleCategoryAnalysisService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly ILogger<GoogleCategoryAnalysisService> _logger;

        // URL-ul de bază pentru API-ul Google Generative AI
        private const string BaseUrl = "https://generativelanguage.googleapis.com/v1beta/models/";
        // Modelul folosit - gemini-2.5-flash-lite
        private const string ModelName = "gemini-2.5-flash-lite";

        public CategoryAnalysisService(IConfiguration configuration,
                ILogger<CategoryAnalysisService> logger)
        {
            _httpClient = new HttpClient();
            _apiKey = configuration["GoogleAi:ApiKey"] ?? throw new ArgumentNullException("GoogleAi:ApiKey not configured");
            _logger = logger;

            // Configurare HttpClient pentru Google AI API
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<CategoriesResult> AnalyzeCategoriesAsync(string title, string content)
        {
            try
            {
                var allowedCategories = new List<string>()
                {
                    "Comedy",
                    "Action",
                    "Gore",
                    "Horror",
                    "Music",
                    "Gaming"
                };
                // Construim prompt-ul pentru analiza de sentiment
                var prompt = $@"
                        You are a post classification assistant. 
                        Analyze the provided post's title and content, and respond ONLY with a JSON object in this exact format:
                        {{ ""categories"": [""category1"", ""category2"", ...] }}

                        Rules:
                        - Only select categories from this list: {string.Join(", ", allowedCategories)}.
                        - Only include categories that clearly match the content of the post.
                        - Categories must be concise, capitalized, and without spaces (use underscores if needed).
                        - Categories must match exactly the names in the list (capitalization matters).
                        - Do NOT include any other text, explanations, or notes. Only the JSON object.

                        Classify the following post:
                            Title: ""{title}""
                            Content: ""{content}""
                            Return the relevant categories for this post.
                        ";

                // Construim request-ul pentru OpenAI API
                var requestBody = new GoogleAiRequest
                {
                    Contents = new List<GoogleAiContent>
                    {
                        new GoogleAiContent
                            {
                                Parts = new List<GoogleAiPart>
                                {
                                    new GoogleAiPart { Text = prompt }
                                }
                            }
                    },

                    // Configurări pentru generare - temperature scăzută pentru rezultate consistente
                    GenerationConfig = new GoogleAiGenerationConfig
                    {
                        Temperature = 0.1,
                        MaxOutputTokens = 100
                    }
                };

                var jsonContent = JsonSerializer.Serialize(requestBody,
                        new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        });


                var stringContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Construim URL-ul complet cu cheia API ca parametru
                // Google AI folosește X-goog-api-key sau parametru în URL
                var requestUrl = $"{BaseUrl}{ModelName}:generateContent?key={_apiKey}";
                _logger.LogInformation("Sending category analysis request to Google AI API");

                // Trimitem request-ul catre OpenAI API
                var response = await _httpClient.PostAsync(requestUrl, stringContent);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Google AI API error: {StatusCode} -   {Content}", response.StatusCode, responseContent);
                    return new CategoriesResult
                    {
                        Success = false,

                        ErrorMessage = $"API Error: {response.StatusCode}"
                    };
                }

                // Parsăm răspunsul de la Google AI
                var googleResponse = JsonSerializer.Deserialize<GoogleAiResponse>(responseContent,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                        });

                // Structura: candidates[0].content.parts[0].text
                var assistantMessage = googleResponse?.Candidates?.FirstOrDefault()?
                                                      .Content?.Parts?.FirstOrDefault()?.Text;


                if (string.IsNullOrEmpty(assistantMessage))
                {
                    return new CategoriesResult
                    {
                        Success = false,

                        ErrorMessage = "Empty response from API"

                    };
                }

                _logger.LogInformation("Google AI response: {Response}", assistantMessage);

                // Curățăm răspunsul de eventuale caractere markdown (```json... ```)
                var cleanedResponse = CleanJsonResponse(assistantMessage);

                // Parsam JSON-ul din raspunsul asistentului
                var categoriesData = JsonSerializer.Deserialize<CategoriesResponse>(cleanedResponse);
                if (categoriesData == null)
                {
                    return new CategoriesResult
                    {
                        Success = false,

                        ErrorMessage = "Failed to parse sentiment response"
                    };
                }

                // Validam si normalizam label-ul

                var categories = categoriesData.Categories;

                foreach (string category in categories)
                {
                    if (!allowedCategories.Contains(category))
                    {
                        return new CategoriesResult
                        {
                            Success = false,

                            ErrorMessage = "Invalid category name"
                        };
                    }
                }

                return new CategoriesResult
                {
                    SuggestedCategoriesNames = categories,
                    Success = true,
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing categories");
                return new CategoriesResult
                {
                    Success = false,

                    ErrorMessage = ex.Message

                };
            }
        }

        /// <summary>
        /// Curăță răspunsul JSON de eventuale caractere markdown
        /// Gemini poate returna răspunsul înconjurat de ```json ...```
        /// </summary>
        private string CleanJsonResponse(string response)
        {
            var cleaned = response.Trim();
            // Eliminăm blocurile de cod markdown dacă există
            if (cleaned.StartsWith("```json"))
            {
                cleaned = cleaned.Substring(7);
            }
            else if (cleaned.StartsWith("```"))
            {
                cleaned = cleaned.Substring(3);
            }
            if (cleaned.EndsWith("```"))
            {
                cleaned = cleaned.Substring(0, cleaned.Length - 3);
            }
            return cleaned.Trim();
        }
    }

    /// <summary>
    /// Clasa pentru request-ul către Google AI
    /// </summary>
    public class GoogleAiRequest
    {
        [JsonPropertyName("contents")]
        public List<GoogleAiContent> Contents { get; set; } = new();
        [JsonPropertyName("generationConfig")]
        public GoogleAiGenerationConfig? GenerationConfig
        {
            get; set;
        }
    }

    /// <summary>
    /// Conținutul mesajului pentru Google AI
    /// </summary>
    public class GoogleAiContent
    {
        [JsonPropertyName("parts")]
        public List<GoogleAiPart> Parts { get; set; } = new();
    }

    /// <summary>
    /// O parte din conținut (text, imagine, etc.)
    /// </summary>
    public class GoogleAiPart
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
    }

    /// <summary>
    /// Configurări pentru generarea răspunsului
    /// </summary>
    public class GoogleAiGenerationConfig
    {
        [JsonPropertyName("temperature")]
        public double Temperature { get; set; } = 0.7;
        [JsonPropertyName("maxOutputTokens")]
        public int MaxOutputTokens { get; set; } = 1024;
    }

    /// <summary>
    /// Răspunsul de la Google AI API
    /// </summary>
    public class GoogleAiResponse
    {
        [JsonPropertyName("candidates")]
        public List<GoogleAiCandidate>? Candidates { get; set; }
    }
    /// <summary>
    /// Un candidat din răspuns (Google AI poate returna mai mulți candidați)
    /// </summary>
    public class GoogleAiCandidate
    {
        [JsonPropertyName("content")]
        public GoogleAiContent? Content { get; set; }
    }

    public class CategoriesResponse
    {
        [JsonPropertyName("categories")]
        public List<string> Categories { get; set; } = [];
    }
}