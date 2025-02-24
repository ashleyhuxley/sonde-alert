using ElectricFox.SondeAlert.Mqtt;
using ElectricFox.SondeAlert.Redis;
using ElectricFox.SondeAlert.Telegram;
using Geolocation;
using Telegram.Bot.Types.Enums;

namespace ElectricFox.SondeAlert;

public sealed class MqttWorker : BackgroundService
{
    private readonly ILogger<MqttWorker> logger;

    private readonly IMqttListener mqttListener;

    private readonly NotificationCache redisCache;

    private readonly UserProfiles userProfiles;

    private readonly ITelegramBot bot;

    public MqttWorker(
        ILogger<MqttWorker> logger,
        IMqttListener mqttListener,
        UserProfiles userProfiles,
        ITelegramBot bot,
        NotificationCache redisCache
    )
    {
        this.logger = logger;
        this.mqttListener = mqttListener;
        this.userProfiles = userProfiles;
        this.bot = bot;
        this.redisCache = redisCache;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Worker started.");

        mqttListener.OnSondeDataReady += OnSondeDataReady;
        await mqttListener.StartAsync(stoppingToken);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
        finally
        {
            mqttListener.OnSondeDataReady -= OnSondeDataReady;
            await mqttListener.StopAsync(stoppingToken);
            logger.LogInformation("Worker stopped.");
        }
    }

    private void OnSondeDataReady(SondeAlertArgs args)
    {
        _ = Task.Run(() => HandleSondeDataAsync(args));
    }

    private async Task HandleSondeDataAsync(SondeAlertArgs args)
    {
        foreach (var profile in this.userProfiles.GetAllProfiles())
        {
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

            if (!await redisCache.ShouldSendSondeNotification(profile.ChatId, args.SondeSerial))
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

            await this.redisCache.SaveSondeNotification(profile.ChatId, args.SondeSerial);
        }
    }
}
