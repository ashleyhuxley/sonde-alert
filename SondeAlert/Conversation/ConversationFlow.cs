namespace ElectricFox.SondeAlert.Conversation
{
    /// <summary>
    /// Represents a Telegram conversation flow with the bot for a particular user.
    /// Basic state machine that keeps track of where in the conversation the user is
    /// and gather information.
    /// </summary>
    public class ConversationFlow
    {
        public ConversationFlowPoint ConversationFlowPoint { get; set; }

        public double Range { get; set; } = 0;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string? Callsign { get; set; }

        private Dictionary<ConversationFlowPoint, IRequestHandler> questions =>
            new()
            {
                { ConversationFlowPoint.Start, new StartRequestHandler() },
                { ConversationFlowPoint.AwaitingCoords, new CoordinatesRequestHandler() },
                { ConversationFlowPoint.Active, new DeactivateRequestHandler() },
                { ConversationFlowPoint.AwaitingRange, new RangeRequestHandler() },
                { ConversationFlowPoint.Callsign, new CallsignRequestHandler() },
            };

        public ConversationFlow(ConversationFlowPoint startingPoint)
        {
            ConversationFlowPoint = startingPoint;
        }

        /// <summary>
        /// Process an incoming message from the user based on where they are in the flow
        /// </summary>
        /// <param name="message">The incoming message</param>
        /// <returns>The response from the bot</returns>
        public string GetResponse(string message)
        {
            var question = questions[this.ConversationFlowPoint];
            return question.GetResponse(this, message);
        }
    }
}
