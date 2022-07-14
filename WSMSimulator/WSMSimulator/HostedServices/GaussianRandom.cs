namespace WSMSimulator.HostedServices
{
    public class GaussianRandom
    {
        private readonly Random _random;

        public GaussianRandom()
        {
            _random = new Random();
        }

        /// <summary>
        /// Generate random double with Gaussian distribution.
        /// Values bayond min and max will be clamped.
        /// </summary>
        /// <param name="mean"></param>
        /// <param name="stdDev"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public double NextDouble(double mean, double stdDev, double min, double max)
        {
            double u1 = 1.0 - _random.NextDouble();
            double u2 = 1.0 - _random.NextDouble();

            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) *
                         Math.Sin(2.0 * Math.PI * u2); // random normal
            double randNormal = mean + stdDev * randStdNormal; // random normal(mean,stdDev^2)

            // Return clamped values
            return Math.Clamp(randNormal, min, max);
        }
    }
}
