using ElectricFox.SondeAlert;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

IConfigurationRoot configRoot = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddEnvironmentVariables()
    .Build();

builder.Services.Configure<SondeAlertOptions>(configRoot.GetSection("SondeAlert"));

var host = builder.Build();
host.Run();
