using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ChatBot;

public class OpenAIChatClient : IChatClient
{
    private readonly ILogger<OpenAIChatClient> _logger;
    private readonly ApiProviderSettings _settings;
    private readonly HttpClient _httpClient;
    private readonly List<OpenAIMessage> _conversationHistory = new();

    public OpenAIChatClient(
        ILogger<OpenAIChatClient> logger,
        IOptions<ApiProviderSettings> settings,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _settings = settings.Value;
        _httpClient = httpClientFactory.CreateClient("OpenAI");
    }

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing OpenAI chat client...");
        _conversationHistory.Clear();

        if (string.IsNullOrEmpty(_settings.ApiKey))
        {
            throw new InvalidOperationException("OpenAI API key is not configured.");
        }

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_settings.ApiKey}");

        _logger.LogInformation("OpenAI chat client initialized.");
        return Task.CompletedTask;
    }

    public async Task<string> SendMessageAsync(string message, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Sending message to API: {Message}", message);

        _conversationHistory.Add(new OpenAIMessage { Role = "user", Content = message });

        try
        {
            var requestBody = new
            {
                model = _settings.Model,
                messages = _conversationHistory,
                max_tokens = _settings.MaxTokens,
                temperature = _settings.Temperature
            };

            string requestUrl;
            if (!string.IsNullOrEmpty(_settings.BaseUrl) && _settings.BaseUrl.Contains("/v1/chat/completions"))
            {
                requestUrl = _settings.BaseUrl;
            }
            else if (!string.IsNullOrEmpty(_settings.BaseUrl))
            {
                requestUrl = $"{_settings.BaseUrl.TrimEnd('/')}/v1/chat/completions";
            }
            else
            {
                requestUrl = "https://api.openai.com/v1/chat/completions";
            }

            _logger.LogDebug("Request URL: {Url}", requestUrl);
            _logger.LogDebug("Model: {Model}", _settings.Model);

            var response = await _httpClient.PostAsJsonAsync(requestUrl, requestBody, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogDebug("Response: {Response}", responseContent);

            var responseData = JsonDocument.Parse(responseContent).RootElement;

            string reply = "";

            if (responseData.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
            {
                var firstChoice = choices[0];
                if (firstChoice.TryGetProperty("message", out var msgObj))
                {
                    if (msgObj.TryGetProperty("content", out var content))
                    {
                        reply = content.GetString() ?? "";
                    }
                }
                else if (firstChoice.TryGetProperty("text", out var text))
                {
                    reply = text.GetString() ?? "";
                }
            }

            if (string.IsNullOrEmpty(reply))
            {
                reply = "API返回了空响应";
            }

            _conversationHistory.Add(new OpenAIMessage { Role = "assistant", Content = reply });

            _logger.LogDebug("Received reply: {Reply}", reply);

            return reply;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send message to API");
            return $"抱歉，调用 API 时出现错误: {ex.Message}";
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

public class OpenAIMessage
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}
