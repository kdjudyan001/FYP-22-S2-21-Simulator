using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace WSMSimulator.Models
{
    [BsonIgnoreExtraElements]
    public class Chemical
    {
        [BsonId]
        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string? ChemicalId { get; set; }

        public double MinQuantity { get; set; }

        public double Quantity { get; set; }


    }
}
