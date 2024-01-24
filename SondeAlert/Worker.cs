using ElectricFox.SondeAlert.Mqtt;
using Microsoft.Extensions.Options;
using ElectricFox.SondeAlert.Telegram;
using Geolocation;

namespace ElectricFox.SondeAlert;

public sealed class Worker : BackgroundService
{
    private readonly ILogger<Worker> logger;

    private readonly SondeAlertOptions options;

    private TelegramBot? bot;

    private const int TelegramMessageDelaySeconds = 5;

    private List<string> SentAlertCache = new();

    public Worker(
        ILogger<Worker> logger,
        IOptions<SondeAlertOptions> options)
    {
        this.logger = logger;
        this.options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrEmpty(options.TelegramBotApiKey))
        {
            throw new SondeAlertConfigurationException($"{nameof(options.TelegramBotApiKey)} is not specified");
        }

        if (this.options.Subscribers is null || !this.options.Subscribers.Any())
        {
            throw new SondeAlertConfigurationException($"No subscribers specified");
        }

        // Set up Telegram bot
        bot = new(
            this.options.TelegramBotApiKey, 
            this.logger, 
            this.options.Subscribers);

        await bot.StartAsync();

        // Set up MQTT listener
        MqttListener listener = new(this.options, this.logger);
        listener.OnSondeDataReady += OnSondeDataReady;
        await listener.StartAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            // Send a Telegram message once every n seconds
            await bot.ProcessMessageFromQueueAsync();
            await Task.Delay(TelegramMessageDelaySeconds * 1000, stoppingToken);
        }

        await listener.StopAsync(stoppingToken);
    }

    private void OnSondeDataReady(SondeAlertArgs args)
    {
        // We already know about this one, ignore.
        if (this.SentAlertCache.Contains(args.SondeSerial))
        {
            return;
        }

        // Calculate distance to home
        var home = new Coordinate(this.options.HomeLat, this.options.HomeLon);
        var distance = GeoCalculator.GetDistance(home, args.PredictedlandingLocation, 2, DistanceUnit.Kilometers);

        if (distance > options.AlertRangeKm)
        {
            // Not interested.
            return;
        }

        var lat = args.PredictedlandingLocation.Latitude.FormatCoordinate();
        var lon = args.PredictedlandingLocation.Longitude.FormatCoordinate();

        string messageText = $"Nearby sonde found: {args.SondeSerial} at {args.PredicatedLandingTimeUtc} coords: {lat}, {lon}";

        this.logger.LogInformation(messageText);

        if (bot is null)
        {
            return;
        }

        // Get it out to the subscribers
        bot.EnqueueAlert(args.SondeSerial, args.PredictedlandingLocation, args.PredicatedLandingTimeUtc);

        // Make a note so we don't send duplicates
        this.SentAlertCache.Add(args.SondeSerial);
    }
}
