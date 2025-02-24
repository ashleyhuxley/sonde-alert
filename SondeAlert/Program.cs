using ElectricFox.SondeAlert;
using ElectricFox.SondeAlert.Aprs;
using ElectricFox.SondeAlert.Mqtt;
using ElectricFox.SondeAlert.Options;
using ElectricFox.SondeAlert.Redis;
using ElectricFox.SondeAlert.Telegram;
using NLog;
using NLog.Extensions.Logging;
using NLog.Web;
using Telegram.Bot.Polling;

var configBuilder = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddEnvironmentVariables()
    .AddUserSecrets<Program>();

LogManager.Setup().LoadConfigurationFromAppSettings();

var logger = LogManager.GetCurrentClassLogger();
logger.Debug("Initialising SondeAlert");

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddLogging(c =>
{
    c.ClearProviders();
    c.AddNLog();
});

IConfigurationRoot configRoot = configBuilder.Build();

builder.Services.Configure<AprsOptions>(configRoot.GetSection("Aprs"));
builder.Services.Configure<RedisOptions>(configRoot.GetSection("Redis"));
builder.Services.Configure<SondeAlertOptions>(configRoot.GetSection("SondeAlert"));
builder.Services.Configure<TelegramOptions>(configRoot.GetSection("Telegram"));
builder.Services.Configure<SondeHubOptions>(configRoot.GetSection("SondeHub"));

builder.Services.AddSingleton<UserProfiles>();
builder.Services.AddHttpClient<AprsFiClient>();
builder.Services.AddSingleton<UpdateHandler>();
builder.Services.AddSingleton<ITelegramBot, TelegramBot>();
builder.Services.AddSingleton<IMqttListener, MqttListener>();
builder.Services.AddSingleton<IUpdateHandler, UpdateHandler>();
builder.Services.AddSingleton<NotificationCache>();

builder.Services.AddHostedService<MqttWorker>();
builder.Services.AddHostedService<TelegramWorker>();
builder.Services.AddHostedService<AprsWorker>();

var host = builder.Build();
host.Run();
