using ElectricFox.SondeAlert.Options;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ElectricFox.SondeAlert.Telegram
{
    internal class TelegramBot : ITelegramBot
    {
        private readonly TelegramOptions options;

        private readonly ILogger<TelegramBot> logger;

        private TelegramBotClient? botClient;

        private readonly Queue<OutgoingMessage> messageQueue = new();

        public event Action<long, string>? OnMessageReceived;

        public TelegramBot(
            IOptions<TelegramOptions> options,
            ILogger<TelegramBot> logger)
        {
            this.logger = logger;
            this.options = options.Value.Verify();
        }

        /// <summary>
        /// Start the bot and listen for incoming messages
        /// </summary>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            this.botClient = new TelegramBotClient(options.BotApiKey);

            ReceiverOptions receiverOptions = new()
            {
                AllowedUpdates = Array.Empty<UpdateType>()
            };

            this.botClient.StartReceiving(
                updateHandler: this.HandleUpdateAsync,
                pollingErrorHandler: this.HandlePollingErrorAsync,
                receiverOptions: receiverOptions,
                cancellationToken: cancellationToken
            );

            var me = await this.botClient.GetMeAsync().ConfigureAwait(false);

            this.logger.LogInformation($"Logged in to Telegram as {me.FirstName} with ID {me.Id}.");
        }

        /// <summary>
        /// Send the next message from the queue, if there is one
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to cancel the async operation</param>
        /// <exception cref="InvalidOperationException">Attempting to send a message before starting the bot will throw this exception.</exception>
        public async Task ProcessMessageFromQueueAsync(CancellationToken cancellationToken)
        {
            if (this.botClient is null || this.messageQueue.Count == 0)
            {
                return;
            }

            var message = this.messageQueue.Dequeue();

            await this.botClient.SendTextMessageAsync(
                chatId: message.ChatId,
                text: message.MessageText,
                parseMode: message.ParseMode,
                cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
        }

        private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (this.botClient is null) { return; }
            if (update.Message is not { } message) { return; }
            if (message.Text is not { } messageText) { return; }

            var chatId = message.Chat.Id;

            this.logger.LogDebug($"Received a '{messageText}' message in chat {chatId}.");

            // Reject anything over 100 characters. Probably spam.
            if (messageText.Length > 100)
            {
                await this.botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Sorry, that message is too long.".EscapeText(),
                    parseMode: ParseMode.MarkdownV2,
                    cancellationToken: cancellationToken)
                        .ConfigureAwait(false);

                return;
            }

            OnMessageReceived?.Invoke(chatId, messageText);
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

        /// <summary>
        /// Enqueue a message to be sent later
        /// </summary>
        /// <param name="message">The message to be enqueued</param>
        public void Enqueue(OutgoingMessage message)
        {
            this.messageQueue.Enqueue(message);
        }

        /// <summary>
        /// Send a message immedialtely
        /// </summary>
        /// <param name="message">The message to send</param>
        /// <param name="cancellationToken">A cancellation token to cancel the async operation</param>
        /// <exception cref="InvalidOperationException">Attempting to send a message before starting the bot will throw this exception.</exception>
        public async Task SendAsync(OutgoingMessage message, CancellationToken cancellationToken)
        {
            if (this.botClient is null)
            {
                throw new InvalidOperationException("Cannot send message: Bot is not started");
            }

            await this.botClient.SendTextMessageAsync(
                chatId: message.ChatId,
                text: message.MessageText,
                parseMode: message.ParseMode,
                cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
        }
    }
}
