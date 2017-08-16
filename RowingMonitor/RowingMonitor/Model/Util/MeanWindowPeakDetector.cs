using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RowingMonitor.Model.Util
{
    /// <summary>
    /// Peak detection which uses the mean peak timestamps of past segments to
    /// improve the realtime peak detection whith a time window around these timestamps.
    /// </summary>
    public class MeanWindowPeakDetector : SimplePeakDetector
    {
        private double window;
        private double meanDetectionWindowStart;
        private double meanDetectionWindowEnd;
        private double segmentStartTime;
        private List<double> detectionTimestamps;
        private ConcurrentDictionary<double, double> segmentValues;

        /// <summary>
        /// Create a new instance of MeanWindowPeakDetection.
        /// </summary>
        /// <param name="timeWindow">Time window in milliseconds around the mean peak timestamp
        /// in whick new peaks will be detected in realtime.</param>
        public MeanWindowPeakDetector(double timeWindow)
        {
            window = timeWindow;
            detectionTimestamps = new List<double>();
            segmentValues = new ConcurrentDictionary<double, double>();
        }

        /// <summary>
        /// Check if the last value was a maximum. The value must be greater
        /// then 0. Respects the mean peak detction times in a segment.
        /// </summary>
        /// <param name="timestamp">Current balue timestamp in milliseconds.</param>
        /// <param name="newValue">Current segment value.</param>
        /// <returns>Returns true if a peak was detected.</returns>
        public bool HasPeak(double timestamp, double newValue)
        {
            if (segmentStartTime == 0) {
                segmentStartTime = timestamp;
            }

            // add all segment values to the segment dictionary
            double segmentTimestamp = timestamp - segmentStartTime;
            // sometimes the difference is the same then update the value
            if (segmentValues.ContainsKey(segmentTimestamp)) {
                segmentValues[segmentTimestamp] = newValue;
            }
            else {
                segmentValues.TryAdd(segmentTimestamp, newValue);
            }            

            // simple check if there is a peak
            if (HasPeak(newValue)) {
                // if it is the first peak then just return true
                if (detectionTimestamps.Count < 1) {
                    return true;
                }
                // if there were peaks in the past, check if the peak
                // is in the mean window of past peaks
                else {
                    if (segmentTimestamp >= meanDetectionWindowStart
                        && segmentTimestamp <= meanDetectionWindowEnd) {
                        return true;
                    }
                    else {
                        // reset the peak detected flag
                        lastValue = newValue;
                        peakDetected = false;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Resets the detection after a segment ended and finds the global maximum of a segment.
        /// Also uses the global maximum to improve the detection of future peaks.
        /// <returns>Returns the timestamp in milliseconds of the global maximum.</returns>
        /// </summary>
        public double SegmentEnded()
        {
            // check again for the global maximum and remember its timestamp
            double maxValue = 0;
            double globalMaxTimestamp = 0;
            foreach (KeyValuePair<double, double> point in segmentValues) {
                if (point.Value > maxValue) {
                    maxValue = point.Value;
                    globalMaxTimestamp = point.Key;
                }
            }
            detectionTimestamps.Add(globalMaxTimestamp);
            double globalTimestamp = segmentStartTime + globalMaxTimestamp;

            // get the mean detection timestamp and ist window bound
            double meanDetectionTimestamp = detectionTimestamps.Average();
            meanDetectionWindowStart = meanDetectionTimestamp - window / 2;
            meanDetectionWindowEnd = meanDetectionTimestamp + window / 2;

            // reset the segment values
            segmentValues = new ConcurrentDictionary<double, double>();
            segmentStartTime = 0;

            base.SegmentEnded();

            return globalTimestamp;
        }
    }
}
