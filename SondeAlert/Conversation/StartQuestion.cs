namespace ElectricFox.SondeAlert.Conversation
{
    public sealed class StartQuestion : IRequestHandler
    {
        private const string response = 
            "Welcome to SondeAlert!\n\nTo get started, please enter your home GPS coordinates as a comma separated pair of decimals, e.g. \n\n51.5056, -0.0754";

        public string GetResponse(ConversationFlow flow, string input)
        {
            flow.ConversationFlowPoint = ConversationFlowPoint.AwaitingCoords;
            return response;
        }
    }
}
