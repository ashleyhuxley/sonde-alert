using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using Telegram.Bot;
using ElectricFox.SondeAlert.Conversation;
using Geolocation;
using System.Collections.Concurrent;

namespace ElectricFox.SondeAlert.Telegram
{
    public class UpdateHandler : IUpdateHandler
    {
        private readonly ILogger<UpdateHandler> _logger;

        private readonly UserProfiles _userProfiles;

        private readonly ConcurrentDictionary<long, ConversationFlow> ConversationFlows = new();

        public UpdateHandler(ILogger<UpdateHandler> logger, UserProfiles userProfiles)
        {
            _logger = logger;
            _userProfiles = userProfiles;
        }

        public Task HandleErrorAsync(
            ITelegramBotClient botClient,
            Exception exception,
            HandleErrorSource source,
            CancellationToken cancellationToken
        )
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            _logger.LogError(ErrorMessage);
            return Task.CompletedTask;
        }

        public async Task HandleUpdateAsync(
            ITelegramBotClient botClient,
            Update update,
            CancellationToken cancellationToken
        )
        {
            if (update.Message is not { } message)
            {
                return;
            }
            if (message.Text is not { } messageText)
            {
                return;
            }

            var chatId = message.Chat.Id;

            _logger.LogDebug($"Received a '{messageText}' message in chat {chatId}.");

            // Reject anything over 100 characters. Probably spam.
            if (messageText.Length > 100)
            {
                await botClient
                    .SendMessage(
                        chatId: chatId,
                        text: "Sorry, that message is too long.".EscapeText(),
                        parseMode: ParseMode.MarkdownV2,
                        cancellationToken: cancellationToken
                    )
                    .ConfigureAwait(false);

                return;
            }

            var flow = ConversationFlows.GetOrAdd(
                chatId,
                _ =>
                    new ConversationFlow(
                        _userProfiles.HasProfile(chatId)
                            ? ConversationFlowPoint.Active
                            : ConversationFlowPoint.Start
                    )
            );

            var response = flow.GetResponse(messageText);

            if (flow.ConversationFlowPoint == ConversationFlowPoint.Active)
            {
                var profile = new UserProfile
                {
                    ChatId = chatId,
                    Home = new Coordinate(flow.Latitude, flow.Longitude),
                    Range = flow.Range,
                    Callsign = flow.Callsign
                };

                this._userProfiles.AddUserProfile(profile);
            }
            else if (flow.ConversationFlowPoint == ConversationFlowPoint.Deactivate)
            {
                this._userProfiles.RemoveUserProfile(chatId);
                this.ConversationFlows.TryRemove(chatId, out _);
            }

            await botClient
                .SendMessage(chatId, response, ParseMode.Html, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
