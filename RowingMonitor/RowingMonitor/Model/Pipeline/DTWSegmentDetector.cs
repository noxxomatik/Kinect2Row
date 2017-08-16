using Microsoft.Kinect;
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
    class DTWSegmentDetector : SegmentDetector
    {
        private SubsequenceDTW subsequenceDTW;

        private List<JointData> jointDataHistory = new List<JointData>();
        private List<double> timeLog = new List<double>();

        public DTWSegmentDetector(float distanceThreshold, int minimumSubsequenceLength)
        {
            subsequenceDTW = new SubsequenceDTW(GetTemplateFromSettings(), distanceThreshold, minimumSubsequenceLength);

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

        public override void Update(JointData jointData, JointType jointType,
            string axis)
        {
            Detect(jointData, jointType, axis);
        }

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
                template.Add(Double.Parse(value, NumberStyles.Float, CultureInfo.CurrentUICulture.NumberFormat));
            }

            return template;
        }

        public override List<SegmentHit> Detect(JointData jointData, JointType jointType, string axis)
        {
            // if first time of update, set the index offset
            /*if (indexOffset == -1) {
                indexOffset = (int)jointData.Index;
            }*/

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

                Logger.Log(this.ToString() ,"Optimal subsequence detected with distance: " + subsequence.Distance
                    + " | Detection latency: " + (detectionIndex - endIndex));

                //if (detectionIndex != jointData.Index) {
                //    throw new Exception("Index offset is faulty.");
                //}

                SegmentHit startHit = new SegmentHit();
                startHit.Index = startIndex;
                startHit.DetectionIndex = jointData.Index;
                startHit.DetectionAbsTimestamp = jointData.AbsTimestamp;
                startHit.HitType = HitType.SegmentStart;

                SegmentHit endHit = new SegmentHit();
                endHit.Index = endIndex;
                endHit.DetectionIndex = jointData.Index;
                endHit.DetectionAbsTimestamp = jointData.AbsTimestamp;
                endHit.HitType = HitType.SegmentEnd;

                // split jointData buffer in the detected segment and new past data
                List<JointData> segment = new List<JointData>();
                List<JointData> buffer = new List<JointData>();

                foreach (JointData data in jointDataHistory) {
                    if (data.Index >= startIndex && data.Index <= endIndex) {
                        if (data.Index == startIndex) {
                            startHit.AbsTimestamp = data.AbsTimestamp;
                        }
                        else if (data.Index == endIndex) {
                            endHit.AbsTimestamp = data.AbsTimestamp;
                        }
                        segment.Add(data);
                    }
                    else if (data.Index > endIndex) {
                        buffer.Add(data);
                    }
                }
                jointDataHistory = new List<JointData>(buffer);

                hits.Add(startHit);
                hits.Add(endHit);
                OnSegmentDetected(new SegmentDetectedEventArgs(hits));
            }

            //currentIndex++;
            return hits;
        }
    }
}
