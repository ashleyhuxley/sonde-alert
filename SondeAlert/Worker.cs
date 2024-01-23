using Geolocation;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client;
using System.Text.Json;

namespace ElectricFox.SondeAlert;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;

    private readonly SondeAlertOptions _options;

    public Worker(
        ILogger<Worker> logger,
        IOptions<SondeAlertOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    private void Configure(MqttClientWebSocketOptionsBuilder builder)
    {
        builder.WithUri("ws-reader.v2.sondehub.org");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var mqttFactory = new MqttFactory();
        using (var mqttClient = mqttFactory.CreateMqttClient())
        {
            mqttClient.ApplicationMessageReceivedAsync += MqttClient_ApplicationMessageReceivedAsync;

            // Connect to MQTT
            var mqttClientOptions = new MqttClientOptionsBuilder()
                .WithWebSocketServer(Configure)
                .Build();

            var response = await mqttClient.ConnectAsync(mqttClientOptions, stoppingToken);

            _logger.LogTrace($"Connection Result Code: {response.ResultCode}");

            _logger.LogInformation("The MQTT client is connected.");

            // Subscribe to the Prediction topic
            var mqttSubscribeOptions = mqttFactory.CreateSubscribeOptionsBuilder()
                .WithTopicFilter( f => { f.WithTopic("prediction/#"); })
                .Build();

            await mqttClient.SubscribeAsync(mqttSubscribeOptions, stoppingToken);

            _logger.LogInformation("MQTT client subscribed to topic.");

            // Loop until stopped
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }

            // Disconnect
            var mqttClientDisconnectOptions = mqttFactory.CreateClientDisconnectOptionsBuilder().Build();
            await mqttClient.DisconnectAsync(mqttClientDisconnectOptions, CancellationToken.None);
        }
    }

    private async Task MqttClient_ApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs arg)
    {
        // Deserialize incoming MQTT message
        var prediction = JsonSerializer.Deserialize<Prediction>(arg.ApplicationMessage.ConvertPayloadToString());
        if (prediction is null)
        {
            _logger.LogWarning("Unable to decode message");
            return;
        }

        _logger.LogTrace($"Have prediciotn for serial {prediction.serial}");

        if (!prediction.data.Any())
        {
            _logger.LogWarning("No prediction data found");
            return;
        }

        // Get the final predicted position of the sonde
        var predictedDestination = prediction.data.OrderBy(d => d.time).Last();

        // Calculate distance to home
        var home = new Coordinate(_options.HomeLat, _options.HomeLon);
        var coord = new Coordinate(predictedDestination.lat, predictedDestination.lon);

        var distance = GeoCalculator.GetDistance(home, coord, 2, DistanceUnit.Miles);

        _logger.LogInformation($"Sonde {prediction.serial} is predicted to land at {predictedDestination.time.ToDateTime()}, {distance} miles away");

        if (distance < _options.AlertRangeKm)
        {
            // TODO: Implement notification
        }
    }
}
