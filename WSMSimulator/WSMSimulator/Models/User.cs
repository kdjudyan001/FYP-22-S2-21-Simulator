using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace WSMSimulator.Models
{
    [BsonIgnoreExtraElements]
    public class User
    {
        [BsonId]
        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string? UserId { get; set; }


        private string? _username;
        [Required(AllowEmptyStrings = false)]
        [MinLength(3, ErrorMessage = "Minimum username length is 3.")]
        public string? Username { get { return _username; } set { _username = value is null ? null : value.Trim(); } }


        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        [Required(AllowEmptyStrings = false)]
        [RegularExpression(@"^(?=\P{Ll}*\p{Ll})(?=\P{Lu}*\p{Lu})(?=\P{N}*\p{N})(?=[\p{L}\p{N}]*[^\p{L}\p{N}])[\s\S]{8,}$",
            ErrorMessage = "Password must be at least 8 letters long and contains at least one lowercase, uppercase, number, and symbol.")]
        public string? Password { get; set; }


        public virtual string? Type { get; set; }


        [BsonDateTimeOptions(DateOnly = false, Kind = DateTimeKind.Utc, Representation = MongoDB.Bson.BsonType.DateTime)]
        public DateTime? CreatedAt { get; set; }


        private string? _fullName;
        [Required(AllowEmptyStrings = false)]
        public string? FullName { get { return _fullName; } set { _fullName = value is null ? value : value.Trim(); } }


        [MinLength(1)]
        [MaxLength(1)]
        [RegularExpression(@"^M|F$", ErrorMessage = "Gender must be \"M\" or \"F\"")]
        public string? Gender { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        [Phone]
        public string? Phone { get; set; }

    }
}
