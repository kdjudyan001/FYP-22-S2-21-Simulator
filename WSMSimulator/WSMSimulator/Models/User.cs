using MongoDB.Bson.Serialization.Attributes;

namespace WSMSimulator.Models
{
    [BsonIgnoreExtraElements]
    public class User
    {
        [BsonId]
        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string? UserId { get; set; }

        public string? Type { get; set; }

    }
}
