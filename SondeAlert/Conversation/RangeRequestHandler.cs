namespace ElectricFox.SondeAlert.Conversation
{
    public sealed class RangeRequestHandler : IRequestHandler
    {
        public string GetResponse(ConversationFlow flow, string input)
        {
            if (!double.TryParse(input, out var range))
            {
                return "Invalid input. Please try again.";
            }

            if (range < 0 || range > 300)
            {
                return "Range must be between 0 and 300km.";
            }

            flow.Range = range;
            flow.ConversationFlowPoint = ConversationFlowPoint.Callsign;

            return "Thanks. If you would like to also be alerted of APRS messages sent to you, please enter your callsign. If you do not require this service, please type 'no'.";
        }
    }
}
