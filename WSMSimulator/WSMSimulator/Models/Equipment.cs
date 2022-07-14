using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace WSMSimulator.Models
{
    [BsonIgnoreExtraElements]
    public class Equipment
    {
        [BsonId]
        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string? EquipmentId { get; set; }

        [Required(AllowEmptyStrings = false)]
        public string? Type { get; set; }

    }
}
