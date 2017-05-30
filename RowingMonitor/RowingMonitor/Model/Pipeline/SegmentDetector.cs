using Microsoft.Kinect;
using RowingMonitor.Model.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RowingMonitor.Model.Pipeline
{
    abstract class SegmentDetector
    {
        public delegate void SegmentDetectedEventHandler(Object sender,
            SegmentDetectedEventArgs e);
        public event SegmentDetectedEventHandler SegmentDetected;

        protected List<long> hitIndices = new List<long>();

        protected List<double> hitTimestamps = new List<double>();

        public abstract void Update(JointData jointData, JointType jointType,
            String axis);

        protected float GetJointDataValue(JointData jointData, 
            JointType jointType, String axis)
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

        protected virtual void OnSegmentDetected(SegmentDetectedEventArgs e)
        {
            SegmentDetected?.Invoke(this, e);
        }
    }
}
