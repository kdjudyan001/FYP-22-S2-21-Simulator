namespace WSMSimulator.Settings
{
    public class MongoDbSettings
    {
        public string? ConnectionString { get; set; }
        public string? DatabaseName { get; set; }
        public string? ChemicalCollection { get; set; }
        public string? EquipmentCollection { get; set; }
        public string? UserCollection { get; set; }
    }
}
