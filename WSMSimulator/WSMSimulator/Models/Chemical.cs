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


        private string? _chemicalName;
        [Required(AllowEmptyStrings = false)]
        [MinLength(1)]
        public string? ChemicalName { get { return _chemicalName; } set { _chemicalName = value is null ? value : value.Trim(); } }


        [Range(0.0, double.MaxValue, ErrorMessage = "The field {0} must be greater than {1}.")]
        public double MinQuantity { get; set; }

        [Range(0.0, double.MaxValue, ErrorMessage = "The field {0} must be greater than {1}.")]
        public double Quantity { get; set; }


        private string? _measureUnit;
        [Required(AllowEmptyStrings = false)]
        public string? MeasureUnit { get { return _measureUnit; } set { _measureUnit = value is null ? value : value.Trim(); } }


        public string? UsageDescription { get; set; }

    }
}
