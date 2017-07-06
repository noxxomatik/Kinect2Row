using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RowingMonitor.Model.Util
{
    class Logger
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(
            System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private Logger() { }

        public static void LogTimes(List<double> times, string module, string additionalInfo) {
            double sum = 0;
            for (int i = 0; i < times.Count; i++) {
                sum += times[i];
            }
            double mean = sum / times.Count;
            log.Info(module + ": " + mean + "ms (" + additionalInfo + ")");
        }

        public static void LogTimestamps(List<double> timestamps, string module, string additionalInfo)
        {
            double sum = 0;
            for (int i = 1; i < timestamps.Count; i++) {
                sum += timestamps[i] - timestamps[i - 1];
            }
            double mean = sum / timestamps.Count;
            log.Info(module + ": " + mean + "ms (" + additionalInfo + ")");
        }

        public static void Log(string module, string message)
        {
            log.Info(module + ": " + message);
        }
    }
}
