namespace ElectricFox.SondeAlert.Conversation
{
    public interface IRequestHandler
    {
        string GetResponse(ConversationFlow flow, string input);
    }
}
