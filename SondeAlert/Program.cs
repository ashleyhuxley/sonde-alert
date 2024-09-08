using ElectricFox.SondeAlert;
using ElectricFox.SondeAlert.Mqtt;
using ElectricFox.SondeAlert.Options;
using ElectricFox.SondeAlert.Telegram;
using NLog;
using NLog.Extensions.Logging;
using NLog.Web;

var configBuilder = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddEnvironmentVariables();

LogManager.Setup().LoadConfigurationFromAppSettings();

var logger = LogManager.GetCurrentClassLogger();
logger.Debug("Initialising SondeAlert");

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddLogging(
    c => {
        c.ClearProviders();
        c.AddNLog();
    }
);

builder.Services.AddHostedService<Worker>();

IConfigurationRoot configRoot = configBuilder.Build();

builder.Services.Configure<SondeAlertOptions>(configRoot.GetSection("SondeAlert"));
builder.Services.Configure<TelegramOptions>(configRoot.GetSection("Telegram"));
builder.Services.Configure<SondeHubOptions>(configRoot.GetSection("SondeHub"));

builder.Services.AddSingleton<ITelegramBot, TelegramBot>();
builder.Services.AddSingleton<IMqttListener, MqttListener>();

var host = builder.Build();
host.Run();
