namespace ElectricFox.SondeAlert.Options
{
    public class AprsOptions
    {
        public int AprsTimerSeconds { get; set; } = 60 * 10;

        public string AprsUrl { get; set; } = "https://api.aprs.fi/api/get";

        public string AprsApiKey { get; set; } = string.Empty;
    }
}
