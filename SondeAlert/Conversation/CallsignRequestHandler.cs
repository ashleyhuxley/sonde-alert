namespace ElectricFox.SondeAlert.Conversation
{
    public sealed class CallsignRequestHandler : IRequestHandler
    {
        public string GetResponse(ConversationFlow flow, string input)
        {
            if (input.ToUpperInvariant() != "NO")
            {
                flow.Callsign = input;
            }

            flow.ConversationFlowPoint = ConversationFlowPoint.Active;

            return $"Thanks! You will now be alerted to sondes landing within {flow.Range}km of {flow.Latitude}, {flow.Longitude}.\n\nTo stop receiving alerts, type <pre>/stop</pre> at any time. Your data will be removed.";
        }
    }
}
