using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ChatBot;

public class SimpleChatClient : IChatClient
{
    private static readonly Random _random = new Random();
    private static readonly Dictionary<string, string> _responses = new Dictionary<string, string>
    {
        { "你好", "你好！" },
        { "hello", "Hello!" },
        { "时间", "现在是 " },
        { "日期", "今天是 " },
        { "帮助", "你可以和我聊天，或者说 'exit' 来退出。" },
        { "再见", "再见！期待下次和你聊天！" }
    };
    private static readonly string[] _defaultResponses =
    {
        "收到你的消息了：'{0}'",
        "这是一个有趣的话题！",
        "让我想想...",
        "我理解了，你在说'{0}'。",
        "好的，我记下了。"
    };

    private readonly ILogger<SimpleChatClient> _logger;
    private readonly ChatBotSettings _settings;
    private readonly List<string> _conversationHistory = new();

    public SimpleChatClient(
        ILogger<SimpleChatClient> logger,
        IOptions<ChatBotSettings> settings)
    {
        _logger = logger;
        _settings = settings.Value;
    }

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing chat client...");
        _conversationHistory.Clear();
        return Task.CompletedTask;
    }

    public async Task<string> SendMessageAsync(string message, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Received message: {Message}", message);
        _conversationHistory.Add(message);

        await Task.Delay(100, cancellationToken);

        var response = GenerateResponse(message);
        _conversationHistory.Add(response);

        return response;
    }

    private string GenerateResponse(string message)
    {
        var lowerMessage = message.ToLowerInvariant();

        foreach (var (keyword, response) in _responses)
        {
            if (lowerMessage.Contains(keyword))
            {
                if (keyword == "你好")
                {
                    return $"你好！我是{_settings.BotName}。";
                }
                if (keyword == "hello")
                {
                    return $"Hello! I'm {_settings.BotName}.";
                }
                if (keyword == "时间")
                {
                    return $"{response}{DateTime.Now:yyyy年MM月dd日 HH:mm:ss}";
                }
                if (keyword == "日期")
                {
                    return $"{response}{DateTime.Now:yyyy年MM月dd日}";
                }
                return response;
            }
        }

        var template = _defaultResponses[_random.Next(_defaultResponses.Length)];
        return string.Format(template, message);
    }
}
