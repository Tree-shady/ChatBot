using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ChatBot;

public class GeminiChatClient : IChatClient
{
    private readonly ILogger<GeminiChatClient> _logger;
    private readonly ApiProviderSettings _settings;
    private readonly HttpClient _httpClient;
    private readonly List<GeminiContent> _conversationHistory = new();

    public GeminiChatClient(
        ILogger<GeminiChatClient> logger,
        IOptions<ApiProviderSettings> settings,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _settings = settings.Value;
        _httpClient = httpClientFactory.CreateClient("Gemini");
    }

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing Gemini chat client...");
        _conversationHistory.Clear();

        if (string.IsNullOrEmpty(_settings.ApiKey))
        {
            throw new InvalidOperationException("Google AI API key is not configured.");
        }

        if (!string.IsNullOrEmpty(_settings.BaseUrl))
        {
            _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
        }
        else
        {
            _httpClient.BaseAddress = new Uri("https://generativelanguage.googleapis.com");
        }

        _logger.LogInformation("Gemini chat client initialized.");
        return Task.CompletedTask;
    }

    public async Task<string> SendMessageAsync(string message, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Sending message to Gemini: {Message}", message);

        _conversationHistory.Add(new GeminiContent
        {
            Role = "user",
            Parts = new List<GeminiPart> { new GeminiPart { Text = message } }
        });

        try
        {
            var modelId = string.IsNullOrEmpty(_settings.Model) ? "gemini-1.5-flash" : _settings.Model;
            var url = $"/v1beta/models/{Uri.EscapeDataString(modelId)}:generateContent?key={Uri.EscapeDataString(_settings.ApiKey)}";

            var requestBody = new
            {
                contents = _conversationHistory,
                generationConfig = new
                {
                    maxOutputTokens = _settings.MaxTokens,
                    temperature = _settings.Temperature
                },
                systemInstruction = new GeminiContent
                {
                    Role = "system",
                    Parts = new List<GeminiPart> { new GeminiPart { Text = _settings.SystemPrompt } }
                }
            };

            var response = await _httpClient.PostAsJsonAsync(url, requestBody, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseData = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
            
            string reply = "";
            if (responseData.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
            {
                var firstCandidate = candidates[0];
                if (firstCandidate.TryGetProperty("content", out var content) &&
                    content.TryGetProperty("parts", out var parts) && parts.GetArrayLength() > 0)
                {
                    var firstPart = parts[0];
                    if (firstPart.TryGetProperty("text", out var text))
                    {
                        reply = text.GetString() ?? "";
                    }
                }
            }

            if (string.IsNullOrEmpty(reply))
            {
                reply = "API返回了空响应";
            }

            _conversationHistory.Add(new GeminiContent
            {
                Role = "model",
                Parts = new List<GeminiPart> { new GeminiPart { Text = reply } }
            });

            _logger.LogDebug("Received response from Gemini: {Reply}", reply);

            return reply;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send message to Gemini");
            return $"抱歉，调用 Gemini API 时出现错误: {ex.Message}";
        }
    }

    public async IAsyncEnumerable<string> SendMessageStreamAsync(string message, CancellationToken cancellationToken = default)
    {
        var response = await SendMessageAsync(message, cancellationToken);
        foreach (char c in response)
        {
            yield return c.ToString();
            await Task.Delay(20, cancellationToken);
        }
    }
}

public class GeminiContent
{
    public string Role { get; set; } = string.Empty;
    public List<GeminiPart> Parts { get; set; } = new();
}

public class GeminiPart
{
    public string Text { get; set; } = string.Empty;
}
