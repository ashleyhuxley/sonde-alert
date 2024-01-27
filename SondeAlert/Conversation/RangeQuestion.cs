namespace ElectricFox.SondeAlert.Conversation
{
    public sealed class RangeQuestion : IRequestHandler
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
            flow.ConversationFlowPoint = ConversationFlowPoint.Active;
            return $"Thanks! You will now be alerted to sondes landing within {flow.Range}km of {flow.Latitude}, {flow.Longitude}.\n\nTo stop receiving alerts, type <pre>/stop</pre> at any time. Your data will be removed.";
        }
    }
}
