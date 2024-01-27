namespace ElectricFox.SondeAlert.Options
{
    public sealed class TelegramOptions
    {
        public string? BotApiKey { get; set; }
        public long[]? Admins { get; set; }

        public TelegramOptions Verify()
        {
            if (string.IsNullOrEmpty(BotApiKey))
            {
                throw new SondeAlertConfigurationException("Telegram BotApiKey cannot be empty.");
            }

            return this;
        }
    }
}
