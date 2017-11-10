using Microsoft.Kinect;
using RowingMonitor.Model.EventArguments;
using RowingMonitor.Model.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace RowingMonitor.Model.Pipeline
{
    /// <summary>
    /// Abstract class of a segment detector to generalize the usage 
    /// of different methods for segment detection.
    /// </summary>
    public abstract class SegmentDetector
    {
        /// <summary>
        /// DEPRECATED
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public delegate void SegmentDetectedEventHandler(Object sender,
            SegmentDetectedEventArgs e);
        /// <summary>
        /// DEPRECATED
        /// </summary>
        public event SegmentDetectedEventHandler SegmentDetected;

        protected List<SegmentHit> hits = new List<SegmentHit>();

        private ActionBlock<JointData> input;
        private BroadcastBlock<List<SegmentHit>> output;

        private JointType detectionJointType = JointType.HandRight;
        private String detectionAxis = "Z";        

        /// <summary>
        /// Uses the detection method of the derives class to detect
        /// segments in the given signal.
        /// </summary>
        /// <param name="jointData">The JointData which will be observed.</param>
        /// <param name="jointType">The JointType of the JointData which will be observed.</param>
        /// <param name="axis">The axis of the JointType which will be observed.</param>
        /// <returns></returns>
        public abstract List<SegmentHit> Detect(JointData jointData,
            JointType jointType, String axis);        

        /// <summary>
        /// DEPRECATED
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnSegmentDetected(SegmentDetectedEventArgs e)
        {
            SegmentDetected?.Invoke(this, e);
        }

        /// <summary>
        /// DEPRECATED
        /// </summary>
        /// <param name="jointData"></param>
        /// <param name="jointType"></param>
        /// <param name="axis"></param>
        public abstract void Update(JointData jointData, JointType jointType,
            String axis);

        /// <summary>
        /// The BroadcastBlock for sending the detected SegmentHit to all linked target blocks.
        /// </summary>
        public BroadcastBlock<List<SegmentHit>> Output { get => output; set => output = value; }
        /// <summary>
        /// The JointType which will be observed.
        /// </summary>
        public JointType DetectionJointType { get => detectionJointType; set => detectionJointType = value; }
        /// <summary>
        /// The axis of the DetectionJointType which will be observed.
        /// </summary>
        public string DetectionAxis { get => detectionAxis; set => detectionAxis = value; }
        /// <summary>
        /// The ActionBlock that recieves JointData for detection.
        /// </summary>
        public ActionBlock<JointData> Input { get => input; set => input = value; }
    }   
}
