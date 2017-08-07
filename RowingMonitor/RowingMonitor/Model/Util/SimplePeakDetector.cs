using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RowingMonitor.Model.Util
{
    /// <summary>
    /// Detects the first maximum of a data series.
    /// </summary>
    public class SimplePeakDetector
    {
        private double lastValue = Double.NegativeInfinity;

        private bool peakDetected = false;

        public bool HasPeak(double newValue)
        {
            if(newValue > 0 && newValue > lastValue) {
                lastValue = newValue;
            }
            else if (!peakDetected && newValue > 0 && newValue < lastValue) {
                peakDetected = true;
                return true;
            }
            return false;
        }

        public void Reset()
        {
            lastValue = Double.NegativeInfinity;
            peakDetected = false;
        }
    }
}
