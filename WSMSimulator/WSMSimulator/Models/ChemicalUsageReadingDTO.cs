using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace WSMSimulator.Models
{
    public class ChemicalUsageReadingDTO
    {
        public string? ChemicalId { get; set; }

        public string? EquipmentId { get; set; }

        public SensorData? Data { get; set; }

    }
}
