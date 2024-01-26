namespace ElectricFox.SondeAlert.Telegram
{
    public class ConversationFlow
    {
        private ConversationFlowPoint point;

        public decimal Range { get; private set; } = 0;

        public double Latitude { get; private set; }
        public double Longitude { get; private set; }

        public ConversationFlow()
        {
            point = ConversationFlowPoint.Start;
        }

        public string GetResponse(string message)
        {
            switch (point)
            {
                case ConversationFlowPoint.Start:
                    point = ConversationFlowPoint.AwaitingCoords;
                    return "Welcome to SondeAlert! To get started, please enter your home GPS coordinates as a comma separated pair of decimals, e.g. \n\n51.5056, -0.0754";
                case ConversationFlowPoint.AwaitingCoords:
                    return ProcessCoordinates(message);
                case ConversationFlowPoint.AwaitingRange:
                    return ProcessRange(message);
                case ConversationFlowPoint.Active:
                    return ProcessDeactivate(message);
            }

            return "Invalid input!";
        }

        private string ProcessDeactivate(string message)
        {
            if (message.ToLowerInvariant() != "/stop")
            {
                return "Unknown command.";
            }

            point = ConversationFlowPoint.Start;
            this.Range = 0;
            this.Latitude = 0;
            this.Longitude = 0;
            return "Thanks. Your subscription has been deactivated.";
        }

        private string ProcessRange(string message)
        {
            if (!decimal.TryParse(message, out var range))
            {
                return "Invalid input. Please try again.";
            }

            this.Range = range;
            point = ConversationFlowPoint.Active;
            return $"Thanks! You will now be alerted to sondes landing within {this.Range}km of {this.Latitude}, {this.Longitude}.\n\nTo stop receiving alerts, type /stop at any time. Your data will be removed.";
        }

        private string ProcessCoordinates(string message)
        {
            var coordsStr = message.Split(',');
            if (coordsStr.Length != 2)
            {
                return "Invalid input! Please try again.";
            }

            double lat, lon;
            if (!double.TryParse(coordsStr[0], out lat) || !double.TryParse(coordsStr[1], out lon))
            {
                return "Invalid input! Please try again.";
            }

            Latitude = lat;
            Longitude = lon;

            point = ConversationFlowPoint.AwaitingRange;
            return "Thanks! Now please enter the radius from your home coordinates in which you'd like to be notified of landings, in Km.";
        }
    }

    public enum ConversationFlowPoint
    {
        Start,
        AwaitingCoords,
        AwaitingRange,
        Active
    }
}
