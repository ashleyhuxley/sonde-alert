namespace ElectricFox.SondeAlert.Options
{
    public sealed class SondeHubOptions
    {
        public string? MqttUrl { get; set; }

        public SondeHubOptions Verify()
        {
            if (string.IsNullOrEmpty(MqttUrl))
            {
                throw new SondeAlertConfigurationException("SondeHub MqttUrl must not be empty");
            }

            return this;
        }
    }
}
