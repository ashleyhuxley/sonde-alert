using ElectricFox.SondeAlert.Mqtt;
using ElectricFox.SondeAlert.Options;
using ElectricFox.SondeAlert.Telegram;
using Geolocation;
using Microsoft.Extensions.Options;
using Telegram.Bot.Types.Enums;

namespace ElectricFox.SondeAlert;

public sealed class MqttWorker : BackgroundService
{
    private readonly ILogger<MqttWorker> logger;

    private readonly IMqttListener mqttListener;

    private readonly SondeAlertOptions options;

    private readonly List<NotificationCacheEntry> NotificationCache = new();

    private readonly UserProfiles userProfiles;

    private readonly ITelegramBot bot;

    public MqttWorker(
        ILogger<MqttWorker> logger,
        IOptions<SondeAlertOptions> options,
        IMqttListener mqttListener,
        UserProfiles userProfiles,
        ITelegramBot bot
    )
    {
        this.logger = logger;
        this.options = options.Value.Verify();
        this.mqttListener = mqttListener;
        this.userProfiles = userProfiles;
        this.bot = bot;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        this.logger.LogInformation("Worker started.");

        // Set up MQTT listener
        this.mqttListener.OnSondeDataReady += this.OnSondeDataReady;
        await this.mqttListener.StartAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(this.options.ProcessTimerSeconds * 1000, stoppingToken);
        }

        await this.mqttListener.StopAsync(stoppingToken);
    }

    private void OnSondeDataReady(SondeAlertArgs args)
    {
        foreach (var profile in this.userProfiles.GetAllProfiles())
        {
            var cacheEntry = new NotificationCacheEntry(profile.ChatId, args.SondeSerial);

            if (this.NotificationCache.Contains(cacheEntry))
            {
                continue;
            }

            var distance = GeoCalculator.GetDistance(
                profile.Home,
                args.PredictedlandingLocation,
                2,
                DistanceUnit.Kilometers
            );

            if (distance > profile.Range)
            {
                continue;
            }

            var lat = args.PredictedlandingLocation.Latitude.FormatCoordinate();
            var lon = args.PredictedlandingLocation.Longitude.FormatCoordinate();

            var sondeHubUrl = string.Format(UrlConstants.SondeHubUrl, args.SondeSerial);
            var mapsUrl = string.Format(UrlConstants.GoogleMapsUrl, lat, lon);
            var landingTime = args.PredicatedLandingTimeUtc.ToString();

            var messageText =
                $"<b>Nearby Sonde Landing Alert!</b>\n\nTime: {landingTime}\nLocation: {lat}, {lon}\n\nType: {args.Type}\n\n{sondeHubUrl}\n\n{mapsUrl}";

            var message = new OutgoingMessage(profile.ChatId, messageText, ParseMode.Html);

            this.bot.Enqueue(message);
            this.NotificationCache.Add(cacheEntry);
        }
    }
}
