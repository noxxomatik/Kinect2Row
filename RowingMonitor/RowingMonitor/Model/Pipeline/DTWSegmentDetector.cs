using Microsoft.Kinect;
using RowingMonitor.Model.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RowingMonitor.Model.Pipeline
{
    class DTWSegmentDetector : SegmentDetector
    {
        private SubsequenceDTW subsequenceDTW;

        private int indexOffset = -1;

        private int currentIndex = 1;

        private List<JointData> jointDataHistory = new List<JointData>();

        // Logger
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(
            System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public DTWSegmentDetector(float distanceThreshold, int minimumSubsequenceLength)
        {
            subsequenceDTW = new SubsequenceDTW(GetTemplateFromSettings(), distanceThreshold, minimumSubsequenceLength);
        }

        public override void Update(JointData jointData, JointType jointType, 
            string axis)
        {
            // if first time of update, set the index offset
            if (indexOffset == -1) {
                indexOffset = (int)jointData.Index;
            }

            jointDataHistory.Add(jointData);

            Subsequence subsequence = subsequenceDTW.compareDataStream(GetJointDataValue(jointData, jointType, axis), currentIndex);
            if (subsequence.Status == SubsequenceStatus.OPTIMAL) {  
                // -1 because index t of DTW starts with 1
                int startIndex = subsequence.TStart + indexOffset - 1;
                int endIndex = subsequence.TEnd + indexOffset - 1;
                int detectionIndex = subsequence.TDetected + indexOffset - 1;

                log.Info("Optimal subsequence detected with distance: " + subsequence.Distance 
                    + " | Detection latency: " + (detectionIndex-endIndex));

                if (detectionIndex != jointData.Index)
                {
                    throw new Exception("Index offset is faulty.");
                }

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
                        if (data.Index == startIndex)
                        {
                            startHit.AbsTimestamp = data.AbsTimestamp;
                        }
                        else if (data.Index == endIndex)
                        {
                            endHit.AbsTimestamp = data.AbsTimestamp;
                        }
                        segment.Add(data);                        
                    }
                    else if(data.Index > endIndex) {
                        buffer.Add(data);
                    }
                }
                jointDataHistory = new List<JointData>(buffer);

                hits.Add(startHit);
                hits.Add(endHit);
                OnSegmentDetected(new SegmentDetectedEventArgs(hits));
            }

            currentIndex++;
        }

        protected override void OnSegmentDetected(SegmentDetectedEventArgs e)
        {
            base.OnSegmentDetected(e);
        }

        private List<double> GetTemplateFromSettings()
        {
            List<double> template = new List<double>();
            string templateText = Properties.Settings.Default.Template;
            string[] splittedText = templateText.Split(',');
            
            foreach (string value in splittedText) {
                template.Add(Double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat));
            }

            return template;
        }
    }
}
