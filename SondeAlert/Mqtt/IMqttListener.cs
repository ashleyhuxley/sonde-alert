namespace ElectricFox.SondeAlert.Mqtt
{
    public interface IMqttListener : IDisposable
    {
        event Action<SondeAlertArgs>? OnSondeDataReady;
        Task StartAsync(CancellationToken stoppingToken);
        Task StopAsync(CancellationToken stoppingToken);
    }
}
