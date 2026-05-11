using System.Net.Http.Json;
using System.Text;
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

        if (!string.IsNullOrEmpty(_settings.ApiKey))
        {
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _settings.ApiKey);
        }

        _logger.LogInformation("HTTP chat client initialized with base URL: {BaseUrl}", _settings.BaseUrl);
        return Task.CompletedTask;
    }

    public async Task<string> SendMessageAsync(string message, CancellationToken cancellationToken = default)
    {
        var fullResponse = new StringBuilder();
        await foreach (var chunk in SendMessageStreamAsync(message, cancellationToken))
        {
            fullResponse.Append(chunk);
        }
        return fullResponse.ToString();
    }

    public async IAsyncEnumerable<string> SendMessageStreamAsync(string message, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Sending streaming message to HTTP API: {Message}", message);

        _conversationHistory.Add(new ChatMessage { Role = "user", Content = message });

        var result = await SendMessageStreamInternalAsync(message, cancellationToken);
        
        foreach (var chunk in result.Chunks)
        {
            yield return chunk;
        }
        
        if (!string.IsNullOrEmpty(result.ErrorMessage))
        {
            yield return result.ErrorMessage;
        }
    }

    private async Task<StreamResult> SendMessageStreamInternalAsync(string message, CancellationToken cancellationToken)
    {
        var result = new StreamResult();
        
        try
        {
            var requestBody = new Dictionary<string, object>
            {
                { "model", string.IsNullOrEmpty(_settings.Model) ? "llama3.2" : _settings.Model },
                { "messages", _conversationHistory },
                { "max_tokens", _settings.MaxTokens },
                { "temperature", _settings.Temperature },
                { "stream", true }
            };

            using var response = await _httpClient.PostAsJsonAsync("/v1/chat/completions", requestBody, cancellationToken);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);

            var fullReply = new StringBuilder();
            string? line;
            
            while ((line = await reader.ReadLineAsync()) != null && !cancellationToken.IsCancellationRequested)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                if (line.StartsWith("data: "))
                {
                    var jsonData = line["data: ".Length..];
                    
                    if (jsonData == "[DONE]")
                        break;

                    try
                    {
                        var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonData);
                        if (jsonElement.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                        {
                            var firstChoice = choices[0];
                            if (firstChoice.TryGetProperty("delta", out var delta) && 
                                delta.TryGetProperty("content", out var content))
                            {
                                var chunk = content.GetString() ?? "";
                                if (!string.IsNullOrEmpty(chunk))
                                {
                                    fullReply.Append(chunk);
                                    result.Chunks.Add(chunk);
                                }
                            }
                        }
                    }
                    catch (JsonException)
                    {
                        continue;
                    }
                }
            }

            var reply = fullReply.ToString();
            if (!string.IsNullOrEmpty(reply))
            {
                _conversationHistory.Add(new ChatMessage { Role = "assistant", Content = reply });
                _logger.LogDebug("Received complete response from HTTP API: {Reply}", reply);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Streaming request was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send streaming message to HTTP API");
            result.ErrorMessage = $"抱歉，调用 API 时出现错误: {ex.Message}";
        }
        
        return result;
    }

    private class StreamResult
    {
        public List<string> Chunks { get; } = new();
        public string? ErrorMessage { get; set; }
    }
}

public class ChatMessage
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}
