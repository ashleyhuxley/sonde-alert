using Geolocation;

namespace ElectricFox.SondeAlert
{
    public sealed class UserProfile
    {
        public long ChatId { get; set; }
        public double Range { get; set; }
        public Coordinate Home { get; set; }
        public string? Callsign { get; set; }
    }
}
