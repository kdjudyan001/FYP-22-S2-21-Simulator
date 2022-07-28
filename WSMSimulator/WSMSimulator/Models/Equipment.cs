using MongoDB.Bson.Serialization.Attributes;

namespace WSMSimulator.Models
{
    [BsonIgnoreExtraElements]
    public class Equipment
    {
        [BsonId]
        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string? EquipmentId { get; set; }

        public string? Type { get; set; }
        public bool IsActive { get; set; } = true;

    }
}
