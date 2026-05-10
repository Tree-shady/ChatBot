using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ChatBot;

public class HttpChatClient : IChatClient
{
    private readonly ILogger<HttpChatClient> _logger;
    private readonly ApiProviderSettings _settings;
    private readonly HttpClient _httpClient;
    private readonly List<ChatMessage> _conversationHistory = new();

    public HttpChatClient(
        ILogger<HttpChatClient> logger,
        IOptions<ApiProviderSettings> settings,
        HttpClient httpClient)
    {
        _logger = logger;
        _settings = settings.Value;
        _httpClient = httpClient;
    }

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing HTTP chat client...");
        _conversationHistory.Clear();

        if (!string.IsNullOrEmpty(_settings.BaseUrl))
        {
            _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
        }

        _logger.LogInformation("HTTP chat client initialized with base URL: {BaseUrl}", _settings.BaseUrl);
        return Task.CompletedTask;
    }

    public async Task<string> SendMessageAsync(string message, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Sending message to HTTP API: {Message}", message);

        _conversationHistory.Add(new ChatMessage { Role = "user", Content = message });

        try
        {
            var requestBody = new Dictionary<string, object>
            {
                { "model", string.IsNullOrEmpty(_settings.Model) ? "llama3.2" : _settings.Model },
                { "messages", _conversationHistory },
                { "max_tokens", _settings.MaxTokens },
                { "temperature", _settings.Temperature }
            };

            var response = await _httpClient.PostAsJsonAsync("/v1/chat/completions", requestBody, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseData = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
            var reply = responseData.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";

            _conversationHistory.Add(new ChatMessage { Role = "assistant", Content = reply });

            _logger.LogDebug("Received response from HTTP API: {Reply}", reply);

            return reply;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send message to HTTP API");
            return $"抱歉，调用 API 时出现错误: {ex.Message}";
        }
    }
}

public class ChatMessage
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}
