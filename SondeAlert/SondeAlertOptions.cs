namespace ElectricFox.SondeAlert
{
    public sealed class SondeAlertOptions
    {
        public string? SondeHubMqttUrl { get; set; }
        public double HomeLat { get; set; }
        public double HomeLon { get; set; }
        public double AlertRangeKm { get; set; }
    }
}
