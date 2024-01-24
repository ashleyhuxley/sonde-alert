using Geolocation;

namespace ElectricFox.SondeAlert.Mqtt
{
    public sealed record class SondeAlertArgs
        (
            string SondeSerial,
            Coordinate PredictedlandingLocation,
            DateTime PredicatedLandingTimeUtc
        )
    { }
}
