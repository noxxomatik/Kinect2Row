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

        private ActionBlock<JointData> input;
        private BroadcastBlock<List<SegmentHit>> output;

        private JointType detectionJointType = JointType.HandRight;
        private String detectionAxis = "Z";

        public BroadcastBlock<List<SegmentHit>> Output { get => output; set => output = value; }
        public JointType DetectionJointType { get => detectionJointType; set => detectionJointType = value; }
        public string DetectionAxis { get => detectionAxis; set => detectionAxis = value; }
        public ActionBlock<JointData> Input { get => input; set => input = value; }

        public abstract void Update(JointData jointData, JointType jointType,
            String axis);

        public abstract List<SegmentHit> Detect(JointData jointData,
            JointType jointType, String axis);        

        protected virtual void OnSegmentDetected(SegmentDetectedEventArgs e)
        {
            SegmentDetected?.Invoke(this, e);
        }
    }   
}
