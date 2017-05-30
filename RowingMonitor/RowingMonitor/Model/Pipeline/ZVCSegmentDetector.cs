using Microsoft.Kinect;
using RowingMonitor.Model.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RowingMonitor.Model.Pipeline
{
    class ZVCSegmentDetector : SegmentDetector
    {
        private JointData lastJointData;

        public override void Update(JointData jointData, 
            JointType jointType, String axis)
        {
            if (lastJointData.RelTimestamp != 0) {
                float value = GetJointDataValue(jointData, jointType, axis);
                // if value is 0 then crossing is at this exact index
                if (value == 0) {
                    hitIndices.Add(jointData.Index);
                    hitTimestamps.Add(jointData.AbsTimestamp);
                    OnSegmentDetected(new SegmentDetectedEventArgs(hitIndices, hitTimestamps));
                }
                else {
                    float lastValue = GetJointDataValue(lastJointData, jointType, axis);
                    // if sign is negativ then crossing was between the two frames
                    if (value * lastValue < 0) {
                        hitIndices.Add(jointData.Index);
                        hitTimestamps.Add(jointData.AbsTimestamp);
                        OnSegmentDetected(new SegmentDetectedEventArgs(hitIndices, hitTimestamps));
                    }
                }
            }
            lastJointData = jointData;
        }

        protected override void OnSegmentDetected(SegmentDetectedEventArgs e)
        {
            base.OnSegmentDetected(e);
        }
    }
}
