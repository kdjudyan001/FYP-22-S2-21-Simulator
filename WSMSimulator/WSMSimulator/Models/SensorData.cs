using MongoDB.Bson.Serialization.Attributes;

namespace WSMSimulator.Models
{
    [BsonIgnoreExtraElements]
    public class SensorData
    {
        [BsonDateTimeOptions(DateOnly = false, Kind = DateTimeKind.Utc, Representation = MongoDB.Bson.BsonType.DateTime)]
        public DateTime? Timestamp { get; set; }

        public double Value { get; set; }

    }
}
