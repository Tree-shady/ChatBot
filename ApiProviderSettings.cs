namespace ChatBot;

public class ApiProviderSettings
{
    public string Provider { get; set; } = "openai";
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string SystemPrompt { get; set; } = "你是一个友好的聊天助手。";
    public string BaseUrl { get; set; } = string.Empty;
    public int MaxTokens { get; set; } = 1000;
    public double Temperature { get; set; } = 0.7;
}

public static class LlmProviders
{
    public const string OpenAI = "openai";
    public const string Anthropic = "anthropic";
    public const string Gemini = "gemini";
    public const string Local = "local";
}
