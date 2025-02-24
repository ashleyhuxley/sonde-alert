namespace ElectricFox.SondeAlert.Conversation
{
    public enum ConversationFlowPoint
    {
        Start,
        AwaitingCoords,
        AwaitingRange,
        Active,
        Deactivate,
        Callsign,
    }
}
