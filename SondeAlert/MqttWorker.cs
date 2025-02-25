using ElectricFox.SondeAlert.Mqtt;
using ElectricFox.SondeAlert.Redis;
using ElectricFox.SondeAlert.Telegram;
using Geolocation;
using Telegram.Bot.Types.Enums;

namespace ElectricFox.SondeAlert;

public sealed class MqttWorker : BackgroundService
{
    private readonly ILogger<MqttWorker> _logger;

    private readonly IMqttListener _mqttListener;

    private readonly NotificationCache _redisCache;

    private readonly UserProfiles _userProfiles;

    private readonly ITelegramBot _bot;

    public MqttWorker(
        ILogger<MqttWorker> logger,
        IMqttListener mqttListener,
        UserProfiles userProfiles,
        ITelegramBot bot,
        NotificationCache redisCache
    )
    {
        _logger = logger;
        _mqttListener = mqttListener;
        _userProfiles = userProfiles;
        _bot = bot;
        _redisCache = redisCache;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker started.");

        _userProfiles.LoadUserProfiles();

        _mqttListener.OnSondeDataReady += OnSondeDataReady;
        await _mqttListener.StartAsync(stoppingToken);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
        finally
        {
            _mqttListener.OnSondeDataReady -= OnSondeDataReady;
            await _mqttListener.StopAsync(stoppingToken);
            _logger.LogInformation("Worker stopped.");
        }
    }

    private void OnSondeDataReady(SondeAlertArgs args)
    {
        _ = Task.Run(() => HandleSondeDataAsync(args));
    }

    private async Task HandleSondeDataAsync(SondeAlertArgs args)
    {
        foreach (var profile in _userProfiles.GetAllProfiles())
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

            if (!await _redisCache.ShouldSendSondeNotification(profile.ChatId, args.SondeSerial))
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

            _bot.Enqueue(message);

            await _redisCache.SaveSondeNotification(profile.ChatId, args.SondeSerial);
        }
    }
}
