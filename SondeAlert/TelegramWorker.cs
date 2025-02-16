using ElectricFox.SondeAlert.Conversation;
using ElectricFox.SondeAlert.Options;
using ElectricFox.SondeAlert.Telegram;
using Geolocation;
using Microsoft.Extensions.Options;
using Telegram.Bot.Types.Enums;

namespace ElectricFox.SondeAlert
{
    public sealed class TelegramWorker : BackgroundService
    {
        private readonly ITelegramBot bot;
        private readonly UserProfiles userProfiles;
        private readonly Dictionary<long, ConversationFlow> ConversationFlows = new();
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

            // Setup Telegram bot
            this.bot.OnMessageReceived += this.Bot_OnMessageReceived;

            await this.bot.StartAsync(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                // Send a Telegram message once every n seconds
                await this.bot.ProcessMessageFromQueueAsync(stoppingToken);

                await Task.Delay(this.options.ProcessTimerSeconds * 1000, stoppingToken);
            }
        }

        private async void Bot_OnMessageReceived(long chatId, string message)
        {
            if (!this.ConversationFlows.ContainsKey(chatId))
            {
                var startingPoint = this.userProfiles.HasProfile(chatId)
                    ? ConversationFlowPoint.Active
                    : ConversationFlowPoint.Start;

                this.ConversationFlows.Add(chatId, new ConversationFlow(startingPoint));
            }

            var flow = this.ConversationFlows[chatId];
            var response = flow.GetResponse(message);

            if (flow.ConversationFlowPoint == ConversationFlowPoint.Active)
            {
                var profile = new UserProfile
                {
                    ChatId = chatId,
                    Home = new Coordinate(flow.Latitude, flow.Longitude),
                    Range = flow.Range,
                };

                this.userProfiles.AddUserProfile(profile);
            }
            else if (flow.ConversationFlowPoint == ConversationFlowPoint.Deactivate)
            {
                this.userProfiles.RemoveUserProfile(chatId);
                this.ConversationFlows.Remove(chatId);
            }

            var outgoingMessage = new OutgoingMessage(chatId, response, ParseMode.Html);

            await this.bot.SendAsync(outgoingMessage, CancellationToken.None);
        }
    }
}
