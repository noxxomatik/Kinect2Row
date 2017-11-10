using Microsoft.Kinect;
using RowingMonitor.Model.EventArguments;
using RowingMonitor.Model.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace RowingMonitor.Model.Pipeline
{
    /// <summary>
    /// A SegmentDetector which uses dynamic time warping to detect subsequences
    /// of a template in the signal stream.
    /// </summary>
    public class DTWSegmentDetector : SegmentDetector
    {
        private SubsequenceDTW subsequenceDTW;

        private List<JointData> jointDataHistory = new List<JointData>();
        private List<double> timeLog = new List<double>();

        /// <summary>
        /// Creates a new instance of DTWSegmentDetector.
        /// </summary>
        /// <param name="distanceThreshold">Maximum distance between the subsequence 
        /// and the template.</param>
        /// <param name="minimumSubsequenceLength">Minimum length of a detected 
        /// subsequence.</param> 
        public DTWSegmentDetector(float distanceThreshold, int minimumSubsequenceLength)
        {
            subsequenceDTW = new SubsequenceDTW(GetTemplateFromSettings(), 
                distanceThreshold, minimumSubsequenceLength);

            Input = new ActionBlock<JointData>(jointData =>
            {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                Output.Post(Detect(jointData, DetectionJointType, DetectionAxis));

                stopwatch.Stop();
                // log times
                timeLog.Add(stopwatch.Elapsed.TotalMilliseconds);
                if (timeLog.Count == 100) {
                    Logger.LogTimes(timeLog, this.ToString(),
                        "mean time to detect hits");
                    timeLog = new List<double>();
                }
            });

            Output = new BroadcastBlock<List<SegmentHit>>(hits =>
            {
                return hits;
            });
        }

        /// <summary>
        /// DEPRECATED
        /// </summary>
        /// <param name="jointData"></param>
        /// <param name="jointType"></param>
        /// <param name="axis"></param>
        public override void Update(JointData jointData, JointType jointType,
            string axis)
        {
            Detect(jointData, jointType, axis);
        }

        /// <summary>
        /// DEPRECATED
        /// </summary>
        /// <param name="e"></param>
        protected override void OnSegmentDetected(SegmentDetectedEventArgs e)
        {
            base.OnSegmentDetected(e);
        }

        private List<double> GetTemplateFromSettings()
        {
            List<double> template = new List<double>();
            string templateText = Properties.Settings.Default.Template;
            string[] splittedText = templateText.Split(';');

            foreach (string value in splittedText) {
                template.Add(Double.Parse(value, NumberStyles.Float, 
                    CultureInfo.CurrentUICulture.NumberFormat));
            }

            return template;
        }

        /// <summary>
        /// Uses the SPRING DTW detection method to detect
        /// segments in the given signal.
        /// </summary>
        /// <param name="jointData">The JointData which will be observed.</param>
        /// <param name="jointType">The JointType of the JointData which will be observed.</param>
        /// <param name="axis">The axis of the JointType which will be observed.</param>
        /// <returns></returns>
        public override List<SegmentHit> Detect(JointData jointData, JointType jointType,
            string axis)
        {
            jointDataHistory.Add(jointData);

            // normalize jointData before comparing with the template
            // TODO: calculate minimum and maximum from mean
            float value = JointDataHandler.GetJointDataValue(jointData, jointType, axis);
            value = (value - Properties.Settings.Default.DTWStartMinimumPosition) 
                / (Properties.Settings.Default.DTWStartMaximumPosition 
                - Properties.Settings.Default.DTWStartMinimumPosition);

            Subsequence subsequence = 
                subsequenceDTW.compareDataStream(value, (int)jointData.Index + 1);

            if (subsequence.Status == SubsequenceStatus.Optimal) {
                // -1 because index t of DTW starts with 1
                int startIndex = subsequence.TStart  - 1;
                int endIndex = subsequence.TEnd - 1;
                int detectionIndex = subsequence.TDetected - 1;

                Logger.Log(this.ToString() ,"Optimal subsequence detected with distance: " 
                    + subsequence.Distance
                    + " | Detection latency: " + (detectionIndex - endIndex));

                double startAbsTimestamp = -1;
                double endAbsTimestamp = -1;

                // split jointData buffer in the detected segment and new past data
                List<JointData> segment = new List<JointData>();
                List<JointData> buffer = new List<JointData>();

                foreach (JointData data in jointDataHistory) {
                    if (data.Index >= startIndex && data.Index <= endIndex) {
                        if (data.Index == startIndex) {
                            startAbsTimestamp = data.AbsTimestamp;
                        }
                        else if (data.Index == endIndex) {
                            endAbsTimestamp = data.AbsTimestamp;
                        }
                        segment.Add(data);
                    }
                    else if (data.Index > endIndex) {
                        buffer.Add(data);
                    }
                }
                jointDataHistory = new List<JointData>(buffer);

                SegmentHit startHit = new SegmentHit(startIndex, jointData.Index, 
                    startAbsTimestamp, 
                    jointData.AbsTimestamp, HitType.SegmentStart);

                SegmentHit endHit = new SegmentHit(endIndex, jointData.Index, 
                    endAbsTimestamp, 
                    jointData.AbsTimestamp, HitType.SegmentEnd);

                hits.Add(startHit);
                hits.Add(endHit);
                OnSegmentDetected(new SegmentDetectedEventArgs(hits));
            }

            //currentIndex++;
            return hits;
        }
    }
}
