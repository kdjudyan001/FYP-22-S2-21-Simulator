namespace WSMSimulator.Settings
{
    public class WaterPumpUsageSettings : BaseSettings
    {
        public double Min { get; set; } = 0;
        public double Max { get; set; } = double.MaxValue;
    }
}
