using ElectricFox.SondeAlert.Options;
using ElectricFox.SondeAlert.Telegram;
using Microsoft.Extensions.Options;

namespace ElectricFox.SondeAlert
{
    public sealed class TelegramWorker : BackgroundService
    {
        private readonly ITelegramBot bot;
        private readonly UserProfiles userProfiles;

        private readonly ILogger<TelegramWorker> logger;
        private readonly SondeAlertOptions options;

        public TelegramWorker(
            ITelegramBot bot,
            ILogger<TelegramWorker> logger,
            UserProfiles profiles,
            IOptions<SondeAlertOptions> options
        )
        {
            this.bot = bot;
            this.logger = logger;
            this.userProfiles = profiles;
            this.options = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            this.userProfiles.LoadUserProfiles();

            await this.bot.StartAsync(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                // Send a Telegram message once every n seconds
                logger.LogDebug("Processing message from queue");
                await this.bot.ProcessMessageFromQueueAsync(stoppingToken);

                await Task.Delay(
                    TimeSpan.FromSeconds(this.options.ProcessTimerSeconds),
                    stoppingToken
                );
            }
        }
    }
}
