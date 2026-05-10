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

        if (_cts != null)
        {
            _cts.Cancel();
        }

        if (_chatTask != null)
        {
            await _chatTask;
        }

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

                var response = await _chatClient.SendMessageAsync(input, cancellationToken);
                Console.WriteLine($"{_settings.BotName}: {response}\n");
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
}
