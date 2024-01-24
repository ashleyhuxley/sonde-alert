namespace ElectricFox.SondeAlert.Telegram
{
    internal sealed record class QueuedMessage (
        string MessageContent,
        long ChatId)
    { }
}
