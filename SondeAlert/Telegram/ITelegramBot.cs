namespace ElectricFox.SondeAlert.Telegram
{
    public interface ITelegramBot
    {
        event Action<long, string> OnMessageReceived;
        void Enqueue(OutgoingMessage message);
        Task SendAsync(OutgoingMessage message, CancellationToken cancellationToken);
        Task StartAsync(CancellationToken cancellationToken);
        Task ProcessMessageFromQueueAsync(CancellationToken cancellationToken);
    }
}
