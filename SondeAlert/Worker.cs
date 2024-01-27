using ElectricFox.SondeAlert.Mqtt;
using Microsoft.Extensions.Options;
using ElectricFox.SondeAlert.Telegram;
using Geolocation;
using Telegram.Bot.Types.Enums;
using ElectricFox.SondeAlert.Options;
using ElectricFox.SondeAlert.Conversation;
using System.Text.Json;

namespace ElectricFox.SondeAlert;

public sealed class Worker : BackgroundService
{
    private readonly ILogger<Worker> logger;

    private readonly IMqttListener mqttListener;

    private readonly ITelegramBot bot;

    private readonly SondeAlertOptions options;

    private readonly List<UserProfile> UserProfiles = new();

    private readonly Dictionary<long, ConversationFlow> ConversationFlows = new();

    private readonly List<NotificationCacheEntry> NotificationCache = new();

    public Worker(
        ILogger<Worker> logger,
        IOptions<SondeAlertOptions> options,
        ITelegramBot bot,
        IMqttListener mqttListener)
    {
        this.logger = logger;
        this.options = options.Value.Verify();
        this.bot = bot;
        this.mqttListener = mqttListener;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        this.logger.LogInformation("Worker started.");

        this.LoadUserProfiles();

        // Setup Telegram bot
        this.bot.OnMessageReceived += this.Bot_OnMessageReceived;

        await this.bot.StartAsync(stoppingToken);

        // Set up MQTT listener
        this.mqttListener.OnSondeDataReady += this.OnSondeDataReady;
        await this.mqttListener.StartAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            // Send a Telegram message once every n seconds
            await this.bot.ProcessMessageFromQueueAsync(stoppingToken);
            await Task.Delay(this.options.ProcessTimerSeconds * 1000, stoppingToken);
        }

        await this.mqttListener.StopAsync(stoppingToken);
    }

    private void LoadUserProfiles()
    {
        if (this.options.ProfilePath is null || !File.Exists(this.options.ProfilePath))
        {
            this.logger.LogWarning("Profiles file does not exist, skipping load.");
            return;
        }

        this.logger.LogInformation($"Loading profiles from {this.options.ProfilePath}");

        try
        {
            var profilesJson = File.ReadAllText(this.options.ProfilePath);
            var profiles = JsonSerializer.Deserialize<IEnumerable<UserProfile>>(profilesJson);

            if (profiles is null)
            {
                throw new JsonException("Profiles deserialized to null");
            }

            this.UserProfiles.Clear();
            this.UserProfiles.AddRange(profiles);
        }
        catch (Exception ex)
        {
            this.logger.LogCritical(ex, $"Exception saving profiles: {ex.Message}");
            return;
        }

        this.logger.LogInformation($"{this.UserProfiles.Count} profiles loaded.");
    }

    private void SaveProfiles()
    {
        if (this.options.ProfilePath is null)
        {
            return;
        }

        this.logger.LogInformation($"Saving {this.UserProfiles.Count} user profiles to {this.options.ProfilePath}");

        try
        {
            var json = JsonSerializer.Serialize(this.UserProfiles);
            File.WriteAllText(this.options.ProfilePath, json);
        }
        catch (Exception ex)
        {
            this.logger.LogCritical(ex, $"Exception saving profiles: {ex.Message}");
        }
    }

    private async void Bot_OnMessageReceived(long chatId, string message)
    {
        if (!this.ConversationFlows.ContainsKey(chatId))
        {
            var startingPoint = this.UserProfiles.Any(p => p.ChatId == chatId) 
                ? ConversationFlowPoint.Active 
                : ConversationFlowPoint.Start;

            this.ConversationFlows.Add(chatId, new ConversationFlow(startingPoint));
        }

        var flow = this.ConversationFlows[chatId];
        var response = flow.GetResponse(message);

        if (flow.ConversationFlowPoint == ConversationFlowPoint.Active)
        {
            this.UserProfiles.Add(new UserProfile
            {
                ChatId = chatId,
                Home = new Coordinate(flow.Latitude, flow.Longitude),
                Range = flow.Range
            });

            this.SaveProfiles();
        }
        else if (flow.ConversationFlowPoint == ConversationFlowPoint.Deactivate)
        {
            var profile = this.UserProfiles.SingleOrDefault(p => p.ChatId == chatId);
            if (profile is not null)
            {
                this.UserProfiles.Remove(profile);
                this.SaveProfiles();
            }
            
            this.ConversationFlows.Remove(chatId);
        }

        var outgoingMessage = new OutgoingMessage(chatId, response, ParseMode.Html);

        await this.bot.SendAsync(outgoingMessage, CancellationToken.None);
    }

    private void OnSondeDataReady(SondeAlertArgs args)
    {
        foreach (var profile in this.UserProfiles)
        {
            var cacheEntry = new NotificationCacheEntry(profile.ChatId, args.SondeSerial);

            if (this.NotificationCache.Contains(cacheEntry))
            {
                continue;
            }

            var distance = GeoCalculator.GetDistance(profile.Home, args.PredictedlandingLocation, 2, DistanceUnit.Kilometers);
            if (distance > profile.Range)
            {
                continue;
            }

            var lat = args.PredictedlandingLocation.Latitude.FormatCoordinate();
            var lon = args.PredictedlandingLocation.Longitude.FormatCoordinate();

            var sondeHubUrl = string.Format(UrlConstants.SondeHubUrl, args.SondeSerial);
            var mapsUrl = string.Format(UrlConstants.GoogleMapsUrl, lat, lon);
            var landingTime = args.PredicatedLandingTimeUtc.ToString();

            var messageText = $"<b>Nearby Sonde Landing Alert!</b>\n\nTime: {landingTime}\nLocation: {lat}, {lon}\n\n{sondeHubUrl}\n\n{mapsUrl}";

            var message = new OutgoingMessage(profile.ChatId, messageText, ParseMode.Html);

            this.bot.Enqueue(message);
            this.NotificationCache.Add(cacheEntry);
        }
    }
}
