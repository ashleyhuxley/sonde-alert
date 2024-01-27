using Telegram.Bot.Types.Enums;

namespace ElectricFox.SondeAlert.Telegram
{
    /// <summary>
    /// Represents an outgoing Telegram message
    /// </summary>
    /// <param name="ChatId">The telegram Chat ID to where the message should be sent</param>
    /// <param name="MessageText">The message content</param>
    /// <param name="ParseMode">Markdown or HTML</param>
    public record class OutgoingMessage
    (
        long ChatId,
        string MessageText,
        ParseMode ParseMode
    )
    { }
}
