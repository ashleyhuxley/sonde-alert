namespace ElectricFox.SondeAlert.Options
{
    public sealed class SondeAlertOptions
    {
        public string? ProfilePath { get; set; }

        public int ProcessTimerSeconds { get; set; } = 5;

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
