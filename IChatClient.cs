namespace ChatBot;

public interface IChatClient
{
    Task<string> SendMessageAsync(string message, CancellationToken cancellationToken = default);
    Task InitializeAsync(CancellationToken cancellationToken = default);
}
