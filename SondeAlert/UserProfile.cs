using Geolocation;

namespace ElectricFox.SondeAlert
{
    public class UserProfile
    {
        public long ChatId { get; set; }
        public double Range { get; set; }
        public Coordinate Home { get; set; }
    }
}
