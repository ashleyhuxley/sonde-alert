namespace ElectricFox.SondeAlert.Options
{
    public sealed class SondeAlertOptions
    {
        public string? ProfilePath { get; set; }

        public SondeAlertOptions Verify()
        {
            if (string.IsNullOrEmpty(ProfilePath))
            {
                throw new SondeAlertConfigurationException("SondeAlert ProfilePath must not be empty");
            }

            return this;
        }
    }
}
