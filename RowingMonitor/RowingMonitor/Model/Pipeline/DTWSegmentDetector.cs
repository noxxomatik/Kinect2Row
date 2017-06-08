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

        public DTWSegmentDetector()
        {
            subsequenceDTW = new SubsequenceDTW(GetTemplateFromSettings(), 3, 2);
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
                //Debug.WriteLine("Distance: " + subsequence.Distance);

                // -1 because index t of DTW starts with 1
                int startIndex = subsequence.TStart + indexOffset - 1;
                int endIndex = subsequence.TEnd + indexOffset - 1;
                hitIndices.Add(startIndex);
                hitIndices.Add(endIndex);

                // split jointData Buffer in segment and ne past data
                List<JointData> segment = new List<JointData>();
                List<JointData> buffer = new List<JointData>();

                foreach (JointData data in jointDataHistory) {
                    if (data.Index >= startIndex && data.Index <= endIndex) {
                        if (data.Index == startIndex || data.Index == endIndex)
                            hitTimestamps.Add(data.AbsTimestamp);
                        segment.Add(data);                        
                    }
                    else if(data.Index > endIndex) {
                        buffer.Add(data);
                    }
                }
                jointDataHistory = new List<JointData>(buffer);

                OnSegmentDetected(new SegmentDetectedEventArgs(hitIndices, hitTimestamps));
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
