using System.ComponentModel.DataAnnotations;

namespace WSMSimulator.Models
{
    public class SensorReadingDTO
    {
        public string? Id { get; set; }

        [Required]
        public SensorData? Data { get; set; }

    }
}
