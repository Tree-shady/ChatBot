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
    private const int TypingDelayMs = 30;

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
            Console.WriteLine($"输入 '{_settings.ExitCommand}' 可以退出。\n");

            while (!cancellationToken.IsCancellationRequested)
            {
                Console.Write("你: ");
                var input = await Console.In.ReadLineAsync(cancellationToken);

                if (string.IsNullOrWhiteSpace(input))
                {
                    continue;
                }

                if (input.Equals(_settings.ExitCommand, StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"{_settings.BotName}: 再见！");
                    _lifetime.StopApplication();
                    break;
                }

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

    private async Task SendMessageWithStreamingAsync(string message, CancellationToken cancellationToken)
    {
        Console.Write($"{_settings.BotName}: ");
        
        await foreach (var chunk in _chatClient.SendMessageStreamAsync(message, cancellationToken))
        {
            foreach (var c in chunk)
            {
                Console.Write(c);
                await Task.Delay(TypingDelayMs, cancellationToken);
            }
        }
        
        Console.WriteLine("\n");
    }
}
