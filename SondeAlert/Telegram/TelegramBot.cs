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

        private readonly IUpdateHandler updateHandler;

        private TelegramBotClient? botClient;

        private readonly Queue<OutgoingMessage> messageQueue = new();

        public TelegramBot(
            IOptions<TelegramOptions> options,
            ILogger<TelegramBot> logger,
            IUpdateHandler updateHandler
        )
        {
            this.logger = logger;
            this.options = options.Value.Verify();
            this.updateHandler = updateHandler;
        }

        /// <summary>
        /// Start the bot and listen for incoming messages
        /// </summary>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            this.botClient = new TelegramBotClient(options.BotApiKey);

            ReceiverOptions receiverOptions = new() { AllowedUpdates = Array.Empty<UpdateType>() };

            this.botClient.StartReceiving(updateHandler, receiverOptions, cancellationToken);

            var me = await this.botClient.GetMe().ConfigureAwait(false);

            this.logger.LogInformation(
                "Logged in to Telegram as {firstName} with ID {id}.",
                me.FirstName,
                me.Id
            );
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

            await this.botClient
                .SendMessage(
                    chatId: message.ChatId,
                    text: message.MessageText,
                    parseMode: message.ParseMode,
                    cancellationToken: cancellationToken
                )
                .ConfigureAwait(false);
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

            await this.botClient
                .SendMessage(
                    chatId: message.ChatId,
                    text: message.MessageText,
                    parseMode: message.ParseMode,
                    cancellationToken: cancellationToken
                )
                .ConfigureAwait(false);
        }
    }
}
