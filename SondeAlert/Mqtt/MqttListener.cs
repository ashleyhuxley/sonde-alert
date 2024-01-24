using Geolocation;
using MQTTnet;
using MQTTnet.Client;
using System.Text.Json;

namespace ElectricFox.SondeAlert.Mqtt
{
    internal class MqttListener : IDisposable
    {
        public delegate void NearbySondeAlert(SondeAlertArgs args);

        public event NearbySondeAlert? OnNearbySondeAlert;

        private readonly SondeAlertOptions options;

        private readonly ILogger logger;

        private bool disposedValue;

        private IMqttClient? mqttClient;

        private const string PredictionTopic = "prediction/#";

        /// <summary>
        /// Creates a new MqttListener to listen for nearby Radiosonde landing prediciotns
        /// </summary>
        /// <param name="options">MQTT and location/range settings</param>
        /// <param name="logger">A logger to which to log information</param>
        public MqttListener(SondeAlertOptions options, ILogger logger)
        {
            this.options = options;
            this.logger = logger;
        }

        /// <summary>
        /// Start listening for nearby sonde landings
        /// </summary>
        public async Task StartAsync(CancellationToken stoppingToken)
        {
            // Create client
            var mqttFactory = new MqttFactory();
            this.mqttClient = mqttFactory.CreateMqttClient();
            this.mqttClient.ApplicationMessageReceivedAsync += ClientMessageReceivedAsync;

            // Connect to MQTT
            var mqttClientOptions = new MqttClientOptionsBuilder()
                .WithWebSocketServer(Configure)
                .Build();

            var response = await this.mqttClient.ConnectAsync(mqttClientOptions, stoppingToken)
                .ConfigureAwait(false);

            this.logger.LogTrace($"Connection Result Code: {response.ResultCode}");

            this.logger.LogInformation("The MQTT client is connected.");

            // Subscribe to the Prediction topic
            var mqttSubscribeOptions = mqttFactory.CreateSubscribeOptionsBuilder()
                .WithTopicFilter(f => { f.WithTopic(PredictionTopic); })
                .Build();

            await this.mqttClient.SubscribeAsync(mqttSubscribeOptions, stoppingToken)
                .ConfigureAwait(false);

            this.logger.LogInformation("MQTT client subscribed to topic.");
        }

        /// <summary>
        /// Stop listening and disconnect from MQTT broker
        /// </summary>
        public async Task StopAsync(CancellationToken stoppingToken)
        {
            if (this.mqttClient is null)
            {
                return;
            }

            var mqttFactory = new MqttFactory();
            var mqttClientDisconnectOptions = mqttFactory.CreateClientDisconnectOptionsBuilder().Build();
            await this.mqttClient.DisconnectAsync(mqttClientDisconnectOptions, stoppingToken)
                .ConfigureAwait(false);
        }

        private void Configure(MqttClientWebSocketOptionsBuilder builder)
        {
            builder.WithUri(this.options.SondeHubMqttUrl);
        }

        private async Task ClientMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs arg)
        {
            // Deserialize incoming MQTT message
            var prediction = JsonSerializer.Deserialize<Prediction>(arg.ApplicationMessage.ConvertPayloadToString());
            if (prediction is null)
            {
                this.logger.LogWarning("Unable to decode message");
                return;
            }

            this.logger.LogTrace($"Have prediciotn for serial {prediction.serial}");

            if (!prediction.data.Any())
            {
                this.logger.LogWarning("No prediction data found");
                return;
            }

            // Get the final predicted position of the sonde
            var landingData = prediction.data.OrderBy(d => d.time).Last();

            // Calculate distance to home
            var home = new Coordinate(this.options.HomeLat, this.options.HomeLon);
            var predictedDestination = new Coordinate(landingData.lat, landingData.lon);

            var distance = GeoCalculator.GetDistance(home, predictedDestination, 2, DistanceUnit.Miles);

            this.logger.LogInformation($"Sonde {prediction.serial} is predicted to land at {landingData.time.ToDateTime()}, {distance} miles away");

            if (distance < options.AlertRangeKm)
            {
                OnNearbySondeAlert?.Invoke(new SondeAlertArgs(prediction.serial, predictedDestination, landingData.time.ToDateTime()));
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    this.mqttClient?.Dispose();
                }

                this.mqttClient = null;
                this.disposedValue = true;
            }
        }

        public void Dispose()
        {
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
