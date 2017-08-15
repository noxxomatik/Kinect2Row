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
        protected double lastValue = Double.NegativeInfinity;

        protected bool peakDetected = false;

        /// <summary>
        /// Check if the last value was a maximum. The value must be greater
        /// then 0.
        /// </summary>
        /// <param name="newValue">New value in the data series.</param>
        /// <returns>Returns true if the last value was a maximum.</returns>
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

        /// <summary>
        /// Resets the detection after a segment ended.
        /// </summary>
        public virtual void SegmentEnded()
        {
            lastValue = Double.NegativeInfinity;
            peakDetected = false;
        }
    }
}
