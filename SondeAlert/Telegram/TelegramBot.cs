using Geolocation;
using System.Threading;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ElectricFox.SondeAlert.Telegram
{
    internal class TelegramBot
    {
        private readonly string apiKey;

        private readonly ILogger logger;

        private readonly IEnumerable<long> subscribers;

        private TelegramBotClient? botClient;

        private readonly CancellationTokenSource cts = new();

        private readonly Queue<QueuedMessage> messageQueue = new();

        private readonly Dictionary<long, ConversationFlow> conversations = new();

        public TelegramBot(
            string apiKey,
            ILogger logger,
            IEnumerable<long> subscribers)
        {
            this.logger = logger;
            this.apiKey = apiKey;
            this.subscribers = subscribers;
        }

        /// <summary>
        /// Start the bot and listen for incoming messages
        /// </summary>
        public async Task StartAsync()
        {
            this.botClient = new TelegramBotClient(apiKey);

            ReceiverOptions receiverOptions = new()
            {
                AllowedUpdates = Array.Empty<UpdateType>()
            };

            botClient.StartReceiving(
                updateHandler: HandleUpdateAsync,
                pollingErrorHandler: HandlePollingErrorAsync,
                receiverOptions: receiverOptions,
                cancellationToken: cts.Token
            );

            var me = await botClient.GetMeAsync().ConfigureAwait(false);

            this.logger.LogInformation($"Logged in to Telegram as {me.FirstName} with ID {me.Id}.");
        }

        /// <summary>
        /// Add an alert to the internal queue, to be sent to subscribers
        /// </summary>
        /// <param name="serial">Serial number of the sonde to alert</param>
        /// <param name="landingLocation">Predicted landing location GPS coords</param>
        /// <param name="landingTime">Predicted landing time UTC</param>
        public void EnqueueAlert(string serial, Coordinate landingLocation, DateTime landingTime)
        {
            var lat = landingLocation.Latitude.FormatCoordinate();
            var lon = landingLocation.Longitude.FormatCoordinate();

            var sondeHubUrl = string.Format(UrlConstants.SondeHubUrl, serial);
            var mapsUrl = string.Format(UrlConstants.GoogleMapsUrl, lat, lon);

            var messageText = $"*Nearby Sonde Landing Alert*\\!\n\nTime: {landingTime}\nLocation: {lat}, {lon}\n\n{sondeHubUrl}\n\n{mapsUrl}";

            foreach (var subscriber in subscribers)
            {
                messageQueue.Enqueue(new QueuedMessage(messageText.EscapeText(), subscriber));
            }
        }

        /// <summary>
        /// Send a message from the queue, if there is one.
        /// </summary>
        public async Task ProcessMessageFromQueueAsync(CancellationToken cancellationToken)
        {
            if (this.botClient is null || this.messageQueue.Count == 0)
            {
                return;
            }

            var message = this.messageQueue.Dequeue();

            await this.botClient.SendTextMessageAsync(
                chatId: message.ChatId,
                text: message.MessageContent,
                parseMode: ParseMode.MarkdownV2,
                cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
        }

        private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (this.botClient is null) { return; }
            if (update.Message is not { } message) { return; }
            if (message.Text is not { } messageText) { return; }

            var chatId = message.Chat.Id;

            this.logger.LogInformation($"Received a '{messageText}' message in chat {chatId}.");

            if (messageText.Length > 100)
            {
                await this.botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Sorry, that message is too long.".EscapeText(),
                    parseMode: ParseMode.MarkdownV2,
                    cancellationToken: cancellationToken)
                        .ConfigureAwait(false);
            }

            if (!conversations.ContainsKey(chatId))
            {
                conversations.Add(chatId, new ConversationFlow());
            }

            var flow = conversations[chatId];
            var response = flow.GetResponse(messageText);

            await this.botClient.SendTextMessageAsync(
                chatId: chatId,
                text: response.EscapeText(),
                parseMode: ParseMode.MarkdownV2,
                cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
        }

        private Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            this.logger.LogError(ErrorMessage);
            return Task.CompletedTask;
        }
    }
}
