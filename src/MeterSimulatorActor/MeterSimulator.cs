using System;

namespace MeterSimulatorActor
{
    public class MeterSimulator
    {
        private const int SecondsPerYear = 60 * 60 * 24 * 365;
        private Random random;

        public MeterSimulator()
        {
            random = new Random();
        }

        public MeterSimulator(int randomSeed)
        {
            random = new Random(randomSeed);
        }

        public double SimulatedConsumption(DateTime dt, double consumptionFactor)
        {
            var dayOfYear = dt.DayOfYear;
            var timeOfDay = dt.TimeOfDay;
            var dayOfYearRatio = dayOfYear / 365.0;
            var timeOfDayRatio = timeOfDay.Ticks / (double)TimeSpan.FromHours(24).Ticks;
            var pi = Math.PI;

            var seasonFactor = 2 + Math.Cos(2 * pi * dayOfYearRatio);
            var timeOfDayFactor = 2 + Math.Cos(pi + 4 * pi * timeOfDayRatio);
            var noise = GaussianRandom(random, 1.0, 0.2);

            return noise * consumptionFactor / SecondsPerYear * seasonFactor * timeOfDayFactor;
        }

        private double GaussianRandom(Random random, double mean, double stdDev)
        {
            var u1 = random.NextDouble(); //these are uniform(0,1) random doubles
            var u2 = random.NextDouble();
            var randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2); //random normal(0,1)
            return mean + stdDev * randStdNormal; //random normal(mean,stdDev^2)
        }
    }
}
