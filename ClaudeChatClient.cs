using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ChatBot;

public class ClaudeChatClient : IChatClient
{
    private readonly ILogger<ClaudeChatClient> _logger;
    private readonly ApiProviderSettings _settings;
    private readonly HttpClient _httpClient;
    private readonly List<ClaudeMessage> _conversationHistory = new();

    public ClaudeChatClient(
        ILogger<ClaudeChatClient> logger,
        IOptions<ApiProviderSettings> settings,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _settings = settings.Value;
        _httpClient = httpClientFactory.CreateClient("Claude");
    }

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing Claude chat client...");
        _conversationHistory.Clear();

        if (!string.IsNullOrEmpty(_settings.BaseUrl))
        {
            _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
        }
        else
        {
            _httpClient.BaseAddress = new Uri("https://api.anthropic.com");
        }

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("x-api-key", _settings.ApiKey);
        _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

        _logger.LogInformation("Claude chat client initialized.");
        return Task.CompletedTask;
    }

    public async Task<string> SendMessageAsync(string message, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Sending message to Claude: {Message}", message);

        _conversationHistory.Add(new ClaudeMessage { Role = "user", Content = message });

        try
        {
            var requestBody = new
            {
                model = string.IsNullOrEmpty(_settings.Model) ? "claude-3-5-haiku-20241022" : _settings.Model,
                max_tokens = _settings.MaxTokens,
                temperature = _settings.Temperature,
                system = _settings.SystemPrompt,
                messages = _conversationHistory
            };

            var response = await _httpClient.PostAsJsonAsync("/v1/messages", requestBody, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseData = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
            
            string reply = "";
            if (responseData.TryGetProperty("content", out var content) && content.GetArrayLength() > 0)
            {
                var firstContent = content[0];
                if (firstContent.TryGetProperty("text", out var text))
                {
                    reply = text.GetString() ?? "";
                }
            }

            if (string.IsNullOrEmpty(reply))
            {
                reply = "API返回了空响应";
            }

            _conversationHistory.Add(new ClaudeMessage { Role = "assistant", Content = reply });

            _logger.LogDebug("Received response from Claude: {Reply}", reply);

            return reply;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send message to Claude");
            return $"抱歉，调用 Claude API 时出现错误: {ex.Message}";
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

public class ClaudeMessage
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}
