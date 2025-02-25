using ElectricFox.SondeAlert.Options;
using Geolocation;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client;
using System.Text.Json;

namespace ElectricFox.SondeAlert.Mqtt
{
    public class MqttListener : IMqttListener
    {
        public event Action<SondeAlertArgs>? OnSondeDataReady;

        private readonly SondeHubOptions _options;

        private readonly ILogger<MqttListener> _logger;

        private bool _disposedValue;

        private IMqttClient? _mqttClient;

        private const string PredictionTopic = "prediction/#";

        /// <summary>
        /// Creates a new MqttListener to listen for nearby Radiosonde landing prediciotns
        /// </summary>
        /// <param name="options">MQTT and location/range settings</param>
        /// <param name="logger">A logger to which to log information</param>
        public MqttListener(IOptions<SondeHubOptions> options, ILogger<MqttListener> logger)
        {
            _options = options.Value.Verify();
            _logger = logger;
        }

        /// <summary>
        /// Start listening for nearby sonde landings
        /// </summary>
        public async Task StartAsync(CancellationToken stoppingToken)
        {
            // Create client
            var mqttFactory = new MqttFactory();
            _mqttClient = mqttFactory.CreateMqttClient();
            _mqttClient.ApplicationMessageReceivedAsync += ClientMessageReceivedAsync;

            // Connect to MQTT
            var mqttClientOptions = new MqttClientOptionsBuilder()
                .WithWebSocketServer(builder => builder.WithUri(_options.MqttUrl))
                .Build();

            var response = await _mqttClient
                .ConnectAsync(mqttClientOptions, stoppingToken)
                .ConfigureAwait(false);

            _logger.LogInformation(
                "The MQTT client is connected with response code {responseCode}.",
                response.ResultCode
            );

            // Subscribe to the Prediction topic
            var mqttSubscribeOptions = mqttFactory
                .CreateSubscribeOptionsBuilder()
                .WithTopicFilter(f =>
                {
                    f.WithTopic(PredictionTopic);
                })
                .Build();

            await _mqttClient
                .SubscribeAsync(mqttSubscribeOptions, stoppingToken)
                .ConfigureAwait(false);

            _logger.LogInformation($"MQTT client subscribed to {PredictionTopic}.");
        }

        /// <summary>
        /// Stop listening and disconnect from MQTT broker
        /// </summary>
        public async Task StopAsync(CancellationToken stoppingToken)
        {
            if (_mqttClient is null)
            {
                return;
            }

            var mqttFactory = new MqttFactory();
            var mqttClientDisconnectOptions = mqttFactory
                .CreateClientDisconnectOptionsBuilder()
                .Build();
            await _mqttClient
                .DisconnectAsync(mqttClientDisconnectOptions, stoppingToken)
                .ConfigureAwait(false);
        }

        private Task ClientMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs arg)
        {
            // Deserialize incoming MQTT message
            var prediction = JsonSerializer.Deserialize<Prediction>(
                arg.ApplicationMessage.ConvertPayloadToString()
            );
            if (prediction is null)
            {
                _logger.LogWarning("Unable to decode message");
                return Task.CompletedTask;
            }

            if (!prediction.data.Any())
            {
                _logger.LogWarning("No prediction data found");
                return Task.CompletedTask;
            }

            // Get the final predicted position of the sonde
            var landingData = prediction.data.OrderBy(d => d.time).Last();

            var predictedDestination = new Coordinate(landingData.lat, landingData.lon);

            _logger.LogDebug(
                "Sonde {serial} predicted to land at {dateTime} location: {lat}, {lon}",
                prediction.serial,
                landingData.time.ToDateTime(),
                landingData.lat,
                landingData.lon
            );

            // Tell the world about it
            OnSondeDataReady?.Invoke(
                new SondeAlertArgs(
                    prediction.serial,
                    predictedDestination,
                    landingData.time.ToDateTime(),
                    prediction.type
                )
            );
            return Task.CompletedTask;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _mqttClient?.Dispose();
                }

                _mqttClient = null;
                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
