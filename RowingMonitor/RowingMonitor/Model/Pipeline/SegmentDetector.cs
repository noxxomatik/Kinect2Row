using Microsoft.Kinect;
using RowingMonitor.Model.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace RowingMonitor.Model.Pipeline
{
    abstract class SegmentDetector
    {
        public delegate void SegmentDetectedEventHandler(Object sender,
            SegmentDetectedEventArgs e);
        public event SegmentDetectedEventHandler SegmentDetected;

        protected List<SegmentHit> hits = new List<SegmentHit>();

        private TransformBlock<JointData, List<SegmentHit>> detectionBlock;

        private JointType detectionJointType = JointType.HandRight;
        private String detectionAxis = "Z";

        public TransformBlock<JointData, List<SegmentHit>> DetectionBlock { get => detectionBlock; set => detectionBlock = value; }
        public JointType DetectionJointType { get => detectionJointType; set => detectionJointType = value; }
        public string DetectionAxis { get => detectionAxis; set => detectionAxis = value; }

        public abstract void Update(JointData jointData, JointType jointType,
            String axis);

        public abstract List<SegmentHit> Detect(JointData jointData,
            JointType jointType, String axis);

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
