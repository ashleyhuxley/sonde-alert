namespace ElectricFox.SondeAlert
{
    public sealed record class NotificationCacheEntry
    (
        long ChatId,
        string SondeSerial
    )
    { }
}
