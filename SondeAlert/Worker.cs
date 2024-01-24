using ElectricFox.SondeAlert.Mqtt;
using Microsoft.Extensions.Options;

namespace ElectricFox.SondeAlert;

public sealed class Worker : BackgroundService
{
    private readonly ILogger<Worker> logger;

    private readonly SondeAlertOptions options;

    public Worker(
        ILogger<Worker> logger,
        IOptions<SondeAlertOptions> options)
    {
        this.logger = logger;
        this.options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        MqttListener listener = new(this.options, this.logger);
        listener.OnNearbySondeAlert += OnNearbySondeAlert;
        await listener.StartAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }

        await listener.StopAsync(stoppingToken);
    }

    private void OnNearbySondeAlert(SondeAlertArgs args)
    {
        this.logger.LogInformation($"Nearby sonde found: {args.SondeSerial} at {args.PredicatedLandingTimeUtc} coords: {args.PredictedlandingLocation.Latitude}, {args.PredictedlandingLocation.Longitude}");
    }
}
