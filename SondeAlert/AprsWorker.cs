using ElectricFox.SondeAlert.Aprs;
using ElectricFox.SondeAlert.Options;
using ElectricFox.SondeAlert.Redis;
using ElectricFox.SondeAlert.Telegram;
using Microsoft.Extensions.Options;
using Telegram.Bot.Types.Enums;

namespace ElectricFox.SondeAlert
{
    public sealed class AprsWorker : BackgroundService
    {
        private readonly ILogger<AprsWorker> _logger;
        private readonly AprsOptions _options;
        private readonly UserProfiles _userProfiles;
        private readonly AprsFiClient _aprsClient;
        private readonly ITelegramBot _bot;
        private readonly NotificationCache _redisCache;

        public AprsWorker(
            ILogger<AprsWorker> logger,
            IOptions<AprsOptions> options,
            UserProfiles userProfiles,
            AprsFiClient aprsClient,
            ITelegramBot bot,
            NotificationCache redisCache
        )
        {
            _logger = logger;
            _options = options.Value;
            _userProfiles = userProfiles;
            _aprsClient = aprsClient;
            _bot = bot;
            _redisCache = redisCache;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _userProfiles.LoadUserProfiles();
            var allProfiles = this._userProfiles.GetAllProfiles();

            while (!stoppingToken.IsCancellationRequested)
            {
                var allCalls = allProfiles
                    .Select(p => p.Callsign ?? string.Empty)
                    .Where(c => !string.IsNullOrEmpty(c))
                    .ToArray();

                if (allCalls.Any())
                {
                    _logger.LogInformation(
                        "Getting APRS messages for {callsignCount} callsigns",
                        allCalls.Length
                    );

                    var messages = await _aprsClient.GetAprsMessages(allCalls);

                    foreach (var message in messages)
                    {
                        var recipient = allProfiles.FirstOrDefault(
                            p => p.Callsign == message.DestinationCallsign
                        );

                        if (
                            recipient?.Callsign is null
                            || !await _redisCache.ShouldSendAprsNotification(
                                recipient.Callsign,
                                message.MessageId
                            )
                        )
                        {
                            continue;
                        }

                        var timestamp = Convert.ToInt64(message.Time).ToDateTime();

                        string messageText =
                            "<b>APRS Message</b>\n\n"
                            + $"<b>Message ID</b> {message.MessageId}"
                            + $"<b>From:</b> {message.SourceCallsign}\n"
                            + $"<b>To:</b> {message.DestinationCallsign}\n"
                            + $"<b>Message:</b> {message.Message}\n"
                            + $"<b>Received:</b> {timestamp:dd:MM:yyyy hh:mm} UTC\n\n"
                            + "<i>Powered by <a href=\"https://aprs.fi/\">APRS.fi</a></i>";

                        var telegramMessage = new OutgoingMessage(
                            recipient.ChatId,
                            messageText,
                            ParseMode.Html
                        );

                        this._bot.Enqueue(telegramMessage);

                        await _redisCache.SaveAprsNotification(recipient.Callsign, message.MessageId);
                    }
                }

                await Task.Delay(
                    TimeSpan.FromSeconds(this._options.AprsTimerSeconds),
                    stoppingToken
                );
            }
        }
    }
}
