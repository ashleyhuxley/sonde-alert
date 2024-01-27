namespace ElectricFox.SondeAlert.Conversation
{
    public sealed class CoordinatesRequestHandler : IRequestHandler
    {
        public string GetResponse(ConversationFlow flow, string input)
        {
            var coordsStr = input.Split(',');
            if (coordsStr.Length != 2)
            {
                return "Invalid input! Please try again.";
            }

            double lat, lon;
            if (!double.TryParse(coordsStr[0], out lat) || !double.TryParse(coordsStr[1], out lon))
            {
                return "Invalid input! Please try again.";
            }

            if (lat < -90 || lat > 90 || lon < -180 || lon > 180)
            {
                return "Invalid GPS Coordinates.";
            }

            flow.Latitude = lat;
            flow.Longitude = lon;
            flow.ConversationFlowPoint = ConversationFlowPoint.AwaitingRange;

            return "Thanks! Now please enter the radius from your home coordinates in which you'd like to be notified of landings, in Km.";
        }
    }
}
