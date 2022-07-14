using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using MQTTnet;
using MQTTnet.Client;
using Newtonsoft.Json;
using WSMSimulator.Models;

namespace WSMSimulator.HostedServices
{
    public class WaterPumpUsageHostedService : IHostedService, IDisposable
    {
        private const string TopicWaterPumpUsage = "WSM/WaterPumpUsage";

        private readonly ILogger<WaterPumpUsageHostedService> _logger;
        private readonly IConfiguration _config;
        private readonly IMongoCollection<Equipment> _equipmentCollection;
        private readonly IMqttClient _mqttClient;
        private readonly GaussianRandom _random;
        private Timer? _timer = null;
        private MqttClientOptions _mqttClientOptions;

        public WaterPumpUsageHostedService(ILogger<WaterPumpUsageHostedService> logger,
            IConfiguration config, IOptions<MongoDBSettings> settings)
        {
            _logger = logger;
            _config = config;

            var mongoClient = new MongoClient(
                settings.Value.ConnectionString);
            var mongoDatabase = mongoClient.GetDatabase(
                settings.Value.DatabaseName);
            _equipmentCollection = mongoDatabase.GetCollection<Equipment>(
                settings.Value.EquipmentCollection);

            // RNG
            _random = new GaussianRandom();

            // Create MQTT client
            _mqttClient = new MqttFactory().CreateMqttClient();
            _mqttClient.ConnectedAsync += ConnectedAsync;
            _mqttClient.DisconnectedAsync += DisconnectedAsync;

            // Initialize options
            _mqttClientOptions = new MqttClientOptionsBuilder()
                .WithTcpServer(_config.GetValue<string>("MqttOptions:Server"), _config.GetValue<int>("MqttOptions:DefaultEndpointPort"))
                .WithClientId("water-pump-usage-simulator")
                .Build();
        }

        private Task ConnectedAsync(MqttClientConnectedEventArgs arg)
        {
            _logger.LogInformation("Client connected.");

            // Start timer
            if (_timer == null)
            {
                _timer = new Timer(Publish, null, TimeSpan.Zero, TimeSpan.FromMinutes(15));
            }
            else
            {
                _timer?.Change(TimeSpan.Zero, TimeSpan.FromMinutes(15));
            }
            return Task.CompletedTask;
        }

        private async Task DisconnectedAsync(MqttClientDisconnectedEventArgs arg)
        {
            _logger.LogInformation("Client disconnected.");

            _timer?.Change(Timeout.Infinite, 0); // disable timer until connected

            await Task.Delay(TimeSpan.FromSeconds(5)); 

            _logger.LogInformation("Retry connection...");
            _ = _mqttClient.ConnectAsync(_mqttClientOptions, CancellationToken.None);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Simulator: Water Pump Usage Hosted Service is starting.");

            _ = _mqttClient.ConnectAsync(_mqttClientOptions, CancellationToken.None);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "Simulator: Water Pump Usage Hosted Service is stopping.");

            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _mqttClient.Dispose();
            _timer?.Dispose();
        }

        public void Publish(object? state)
        {
            try
            {
                SensorReadingDTO? data = GetRandomSensorData();

                if (data != null)
                {
                    string json = JsonConvert.SerializeObject(data);
                    var applicationMessage = new MqttApplicationMessageBuilder()
                        .WithTopic(TopicWaterPumpUsage)
                        .WithPayload(json)
                        .Build();

                    _mqttClient.PublishAsync(applicationMessage, CancellationToken.None);

                    _logger.LogInformation("Publish message {0}.", json);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }

        private SensorReadingDTO? GetRandomSensorData()
        {
            try
            {
                // Query Equipment collection to get a random EquipmentId
                string? pumpId = GetRandomPumpIdAsync().Result;

                if (pumpId != null)
                {
                    // Generate random sensor data
                    SensorReadingDTO sensorReading = new SensorReadingDTO()
                    {
                        Id = pumpId,
                        Data = new SensorData()
                        {
                            Timestamp = DateTime.UtcNow,
                            Value = _random.NextDouble(100, 50, 0, double.MaxValue)
                        }
                    };
                    return sensorReading;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception)
            {
                return null;
            }

        }

        public async Task<string?> GetRandomPumpIdAsync()
        {
            // https://stackoverflow.com/questions/46616574/mongodb-random-results-in-c-sharp-linq
            var sample = await _equipmentCollection.AsQueryable()
                .Where(doc => doc.Type == "Pump")
                .Sample(1)
                .FirstOrDefaultAsync();
            return sample is not null ? sample.EquipmentId : null;
        }
    }
}
