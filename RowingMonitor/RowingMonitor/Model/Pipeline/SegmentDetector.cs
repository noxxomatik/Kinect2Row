using Microsoft.Kinect;
using RowingMonitor.Model.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RowingMonitor.Model.Pipeline
{
    class SegmentDetector
    {
        public delegate void SegmentDetectedEventHandler(Object sender,
            SegmentDetectedEventArgs e);
        public event SegmentDetectedEventHandler SegmentDetected;

        private JointData lastJointData;

        private List<long> hitIndices = new List<long>();

        public void SegmentByZeroCrossings(JointData jointData, JointType jointType, 
            String axis)
        {
            if (lastJointData.RelTimestamp != 0) {
                float value = GetJointDataValue(jointData, jointType, axis);
                // if value is 0 then crossing is at this exact index
                if (value == 0) {
                    hitIndices.Add(jointData.Index);
                    SegmentDetected(this, new SegmentDetectedEventArgs(hitIndices));
                }
                else {
                    float lastValue = GetJointDataValue(lastJointData, jointType, axis);
                    // if sign is negativ then crossing was between the two frames
                    if (value * lastValue < 0) {
                        hitIndices.Add(jointData.Index);
                        SegmentDetected(this, new SegmentDetectedEventArgs(hitIndices));
                    }
                }
            }
            lastJointData = jointData;
        }

        private float GetJointDataValue(JointData jointData, JointType jointType, String axis)
        {
            switch (axis) {
                case "X":
                    return jointData.Joints[jointType].Position.X;
                case "Y":
                    return jointData.Joints[jointType].Position.Y;
                case "Z":
                    return jointData.Joints[jointType].Position.Z;
                default:
                    throw new Exception("Chose axis X, Y or Z.");
            }
        }
    }
}
