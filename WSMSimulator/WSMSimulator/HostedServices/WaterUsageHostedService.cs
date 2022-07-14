using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using MQTTnet;
using MQTTnet.Client;
using Newtonsoft.Json;
using WSMSimulator.Models;

namespace WSMSimulator.HostedServices
{
    public class WaterUsageHostedService : IHostedService, IDisposable
    {
        private const string TopicWaterUsage = "WSM/WaterUsage";

        private readonly ILogger<WaterUsageHostedService> _logger;
        private readonly IConfiguration _config;
        private readonly IMongoCollection<User> _userCollection;
        private readonly GaussianRandom _random;
        private readonly IMqttClient _mqttClient;
        private Timer? _timer = null;
        private MqttClientOptions _mqttClientOptions;

        public WaterUsageHostedService(ILogger<WaterUsageHostedService> logger,
            IConfiguration config, IOptions<MongoDBSettings> settings)
        {
            _logger = logger;
            _config = config;

            var mongoClient = new MongoClient(
                settings.Value.ConnectionString);
            var mongoDatabase = mongoClient.GetDatabase(
                settings.Value.DatabaseName);
            _userCollection = mongoDatabase.GetCollection<User>(
                settings.Value.UserCollection);

            _random = new GaussianRandom();

            // Create MQTT client
            _mqttClient = new MqttFactory().CreateMqttClient();
            _mqttClient.ConnectedAsync += ConnectedAsync;
            _mqttClient.DisconnectedAsync += DisconnectedAsync;

            // Initialize options
            _mqttClientOptions = new MqttClientOptionsBuilder()
                .WithTcpServer(_config.GetValue<string>("MqttOptions:Server"), _config.GetValue<int>("MqttOptions:DefaultEndpointPort"))
                .WithClientId("water-usage-simulator")
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
                "Simulator: Water Usage Hosted Service is starting.");

            _ = _mqttClient.ConnectAsync(_mqttClientOptions, CancellationToken.None);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "Simulator: Water Usage Hosted Service is stopping.");

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
                        .WithTopic(TopicWaterUsage)
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
                // Query User collection to get a random UserId
                string? UserId = GetRandomUserIdAsync().Result;

                if (UserId != null)
                {
                    // Generate random sensor data
                    SensorReadingDTO sensorReading = new SensorReadingDTO()
                    {
                        Id = UserId,
                        Data = new SensorData()
                        {
                            Timestamp = DateTime.UtcNow,
                            Value = _random.NextDouble(10, 10, 0, double.MaxValue)
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

        public async Task<string?> GetRandomUserIdAsync()
        {
            // https://stackoverflow.com/questions/46616574/mongodb-random-results-in-c-sharp-linq
            var sample = await _userCollection.AsQueryable()
                .Where(doc => doc.Type == "Customer")
                .Sample(1)
                .FirstOrDefaultAsync();
            return sample is not null ? sample.UserId : null;
        }
    }
}
