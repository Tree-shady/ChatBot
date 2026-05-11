using System.Collections.Concurrent;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ChatBot;

public class ChatBotService : IHostedService
{
    private readonly IHostApplicationLifetime _lifetime;
    private readonly IChatClient _chatClient;
    private readonly ILogger<ChatBotService> _logger;
    private readonly ChatBotSettings _settings;
    private Task? _chatTask;
    private CancellationTokenSource? _cts;
    private readonly ConcurrentQueue<DateTime> _messageTimes = new();
    private int _totalMessages;
    private int _totalTokens;

    public ChatBotService(
        IHostApplicationLifetime lifetime,
        IChatClient chatClient,
        ILogger<ChatBotService> logger,
        IOptions<ChatBotSettings> settings)
    {
        _lifetime = lifetime;
        _chatClient = chatClient;
        _logger = logger;
        _settings = settings.Value;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting chat bot service...");

        await _chatClient.InitializeAsync(cancellationToken);

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _chatTask = RunChatLoopAsync(_cts.Token);

        _logger.LogInformation("Chat bot service started.");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping chat bot service...");

        _cts?.Cancel();
        if (_chatTask != null)
        {
            await _chatTask;
        }
        _cts?.Dispose();

        _logger.LogInformation("Chat bot service stopped.");
    }

    private async Task RunChatLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            Console.WriteLine($"\n=== {_settings.BotName} ===");
            Console.WriteLine(_settings.WelcomeMessage);
            Console.WriteLine($"输入 '{string.Join("', '", _settings.ExitCommands)}' 可以退出。\n");

            while (!cancellationToken.IsCancellationRequested)
            {
                if (!await CheckRateLimitAsync(cancellationToken))
                {
                    Console.WriteLine($"{_settings.BotName}: 您发送消息过于频繁，请稍后再试。\n");
                    continue;
                }

                Console.Write("你: ");
                var input = await Console.In.ReadLineAsync(cancellationToken);

                if (string.IsNullOrWhiteSpace(input))
                {
                    continue;
                }

                if (IsExitCommand(input))
                {
                    Console.WriteLine($"{_settings.BotName}: 再见！");
                    if (_settings.ShowStatistics)
                    {
                        PrintStatistics();
                    }
                    _lifetime.StopApplication();
                    break;
                }

                _totalMessages++;
                _messageTimes.Enqueue(DateTime.UtcNow);
                await SendMessageWithStreamingAsync(input, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Chat loop canceled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred in chat loop.");
        }
    }

    private bool IsExitCommand(string input)
    {
        var normalizedInput = input.Trim().ToLowerInvariant();
        return _settings.ExitCommands.Any(cmd => 
            cmd.Equals(normalizedInput, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<bool> CheckRateLimitAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var oneMinuteAgo = now.AddMinutes(-1);

        while (_messageTimes.TryPeek(out var oldest) && oldest < oneMinuteAgo)
        {
            _messageTimes.TryDequeue(out _);
        }

        return _messageTimes.Count < _settings.RateLimitPerMinute;
    }

    private void PrintStatistics()
    {
        Console.WriteLine($"\n========== 会话统计 ==========");
        Console.WriteLine($"对话轮次: {_totalMessages}");
        Console.WriteLine($"================================");
    }

    public void UpdateTokenCount(int tokens)
    {
        Interlocked.Add(ref _totalTokens, tokens);
    }

    private async Task SendMessageWithStreamingAsync(string message, CancellationToken cancellationToken)
    {
        Console.Write($"{_settings.BotName}: ");
        
        await foreach (var chunk in _chatClient.SendMessageStreamAsync(message, cancellationToken))
        {
            foreach (var c in chunk)
            {
                Console.Write(c);
                await Task.Delay(_settings.TypingDelayMs, cancellationToken);
            }
        }
        
        Console.WriteLine("\n");
    }
}
