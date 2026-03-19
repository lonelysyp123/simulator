using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EssSimulator.Helper
{
    public class HelpClass
    {
        public static  double POWER_ERROR = .01;
        public static  double ENERGY_ERROR = .01;
        public static  double VOLTAGE_ERROR = .01;
        public static  double CURRENT_ERROR = .01;
        public static  double SOC_ERROR = .01;
        public static  double SOH_ERROR = .01;
        public static  double RESISTOR_ERROR = .01;
        public static  double TEMPERATURE_ERROR = .01;
        private static long RANDOM_SEED = 1721815968;
        private static Random RANDOM = null;
        public static double AddGaussianNoise(double error, double value)
        {
            Random rd = new Random();
            return value * (1 + rd.NextDouble() * error);
        }

        public static int CapValue(int value, int lowerBound, int upperBound)
        {
            return Math.Min(Math.Max(value, lowerBound), upperBound);
        }

        public static double CapValue(double value, double lowerBound, double upperBound)
        {
            return Math.Min(Math.Max(value, lowerBound), upperBound);
        }


    }
}
