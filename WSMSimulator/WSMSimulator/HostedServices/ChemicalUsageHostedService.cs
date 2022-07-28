using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using MQTTnet;
using MQTTnet.Client;
using Newtonsoft.Json;
using WSMSimulator.Models;
using WSMSimulator.Settings;

namespace WSMSimulator.HostedServices
{
    public class ChemicalUsageHostedService : IHostedService, IDisposable
    {
        private const string TopicChemicalUsage = "WSM/ChemicalUsage";

        private readonly ILogger<ChemicalUsageHostedService> _logger;
        private readonly IConfiguration _config;
        private readonly IOptions<ChemicalUsageSettings> _simulation;
        private readonly IMqttClient _mqttClient;
        private readonly MqttClientOptions _mqttClientOptions;
        private readonly IMongoCollection<Chemical> _chemicalCollection;
        private readonly IMongoCollection<Equipment> _equipmentCollection;
        private readonly GaussianRandom _random;
        private Timer? _timer = null;

        public ChemicalUsageHostedService(ILogger<ChemicalUsageHostedService> logger, IConfiguration config,
            IOptions<MongoDbSettings> dbSettings, IOptions<ChemicalUsageSettings> simulation)
        {
            _logger = logger;
            _config = config;
            _simulation = simulation;

            var mongoClient = new MongoClient(
                dbSettings.Value.ConnectionString);
            var mongoDatabase = mongoClient.GetDatabase(
                dbSettings.Value.DatabaseName);
            _chemicalCollection = mongoDatabase.GetCollection<Chemical>(
                dbSettings.Value.ChemicalCollection);
            _equipmentCollection = mongoDatabase.GetCollection<Equipment>(
                dbSettings.Value.EquipmentCollection);

            // RNG
            _random = new GaussianRandom();

            // Create MQTT client
            _mqttClient = new MqttFactory().CreateMqttClient();
            _mqttClient.ConnectedAsync += ConnectedAsync;
            _mqttClient.DisconnectedAsync += DisconnectedAsync;

            // Initialize options
            _mqttClientOptions = new MqttClientOptionsBuilder()
                .WithTcpServer(_config.GetValue<string>("MqttOptions:Server"), _config.GetValue<int>("MqttOptions:DefaultEndpointPort"))
                .WithClientId("chemical-usage-simulator")
                .Build();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Simulator: Chemical Usage Hosted Service is starting.");

            _ = _mqttClient.ConnectAsync(_mqttClientOptions, CancellationToken.None);
            return Task.CompletedTask;
        }

        private Task ConnectedAsync(MqttClientConnectedEventArgs arg)
        {
            _logger.LogInformation("Client connected.");
            // Start timer
            if (_timer == null)
            {
                _timer = new Timer(Publish, null, TimeSpan.FromMinutes(_simulation.Value.DueTime), TimeSpan.FromMinutes(_simulation.Value.Period));
            }
            else
            {
                _timer?.Change(TimeSpan.FromMinutes(_simulation.Value.DueTime), TimeSpan.FromMinutes(_simulation.Value.Period));
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

        public Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "Simulator: Chemical Usage Hosted Service is stopping.");

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
                ChemicalUsageReadingDTO? data = GetRandomSensorData();

                if (data != null)
                {
                    string json = JsonConvert.SerializeObject(data);
                    var applicationMessage = new MqttApplicationMessageBuilder()
                        .WithTopic(TopicChemicalUsage)
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

        private ChemicalUsageReadingDTO? GetRandomSensorData()
        {
            try
            {
                // Query Chemical collection to get a random ChemicalId
                Chemical? chemical = GetRandomChemicalAsync().Result;
                Equipment? equipment = GetRandomPumpAsync().Result;

                if (chemical == null || equipment == null)
                    return null;

                _logger.LogInformation($"{chemical.ChemicalId} - {chemical.Quantity}");

                // Generate random sensor data
                SensorData sensorData = new SensorData()
                {
                    Timestamp = DateTime.UtcNow,
                    Value = _random.NextDouble(_simulation.Value.Mean, _simulation.Value.StdDev, _simulation.Value.Min, chemical.Quantity)
                };

                _logger.LogInformation($"RANDOM SENSOR DATA {sensorData.Value}");

                ChemicalUsageReadingDTO sensorReading = new ChemicalUsageReadingDTO()
                {
                    ChemicalId = chemical.ChemicalId,
                    EquipmentId = equipment.EquipmentId,
                    Data = sensorData
                };
                return sensorReading;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);

                return null;
            }

        }

        public async Task<Chemical?> GetRandomChemicalAsync()
        {
            // https://stackoverflow.com/questions/46616574/mongodb-random-results-in-c-sharp-linq
            var sample = await _chemicalCollection.AsQueryable()
                .Where(x => x.Quantity > 0 && x.Quantity > _simulation.Value.Min)
                .Sample(1)
                .FirstOrDefaultAsync();

            return sample is not null ? sample : null;
        }

        public async Task<Equipment?> GetRandomPumpAsync()
        {
            // https://stackoverflow.com/questions/46616574/mongodb-random-results-in-c-sharp-linq
            var sample = await _equipmentCollection.AsQueryable()
                .Where(x => x.Type == "Pump" && x.IsActive == true)
                .Sample(1)
                .FirstOrDefaultAsync();
            return sample is not null ? sample : null;
        }

    }
}
