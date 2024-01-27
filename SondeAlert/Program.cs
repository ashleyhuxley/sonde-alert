using ElectricFox.SondeAlert;
using ElectricFox.SondeAlert.Mqtt;
using ElectricFox.SondeAlert.Options;
using ElectricFox.SondeAlert.Telegram;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

IConfigurationRoot configRoot = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddEnvironmentVariables()
    .Build();

builder.Services.Configure<SondeAlertOptions>(configRoot.GetSection("SondeAlert"));
builder.Services.Configure<TelegramOptions>(configRoot.GetSection("Telegram"));
builder.Services.Configure<SondeHubOptions>(configRoot.GetSection("SondeHub"));

builder.Services.AddSingleton<ITelegramBot, TelegramBot>();
builder.Services.AddSingleton<IMqttListener, MqttListener>();

var host = builder.Build();
host.Run();
