namespace ElectricFox.SondeAlert.Conversation
{
    public sealed class DeactivateQuestion : IRequestHandler
    {
        public string GetResponse(ConversationFlow flow, string input)
        {
            if (input.ToLowerInvariant() != "/stop")
            {
                return "Unknown command.";
            }

            flow.ConversationFlowPoint = ConversationFlowPoint.Start;
            flow.Range = 0;
            flow.Latitude = 0;
            flow.Longitude = 0;
            return "Thanks. Your subscription has been deactivated.";
        }
    }
}
