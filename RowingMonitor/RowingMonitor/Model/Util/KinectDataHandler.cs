using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RowingMonitor.Model.Util
{
    public class KinectDataHandler
    {
        private static readonly KinectDataHandler instance = new KinectDataHandler();

        private Body[] bodies;

        private long lastIndex;

        private double relStartTime = -1;

        private KinectDataHandler() { }

        public static KinectDataHandler Instance
        {
            get {
                return instance;
            }
        }

        public double RelStartTime { get => relStartTime; set => relStartTime = value; }
        public long LastIndex { get => lastIndex; set => lastIndex = value; }
        public Body[] Bodies { get => bodies; set => bodies = value; }

        public JointData CreateNewJointData(double relTimestamp,
            double creationTimestamp,
            IReadOnlyDictionary<JointType, Joint> joints)
        {
            if (relStartTime == -1) {
                relStartTime = relTimestamp;
            }

            JointData jointData = new JointData
            {
                RelTimestamp = relTimestamp,
                AbsTimestamp = relTimestamp - relStartTime,
                Index = LastIndex,
                Joints = joints
            };
            jointData.Timestamps = new List<double>();
            jointData.Timestamps.Add(creationTimestamp);
            jointData.DataStreamType = DataStreamType.RawPosition;

            LastIndex++;
            return jointData;
        }

        public static JointData ReplaceJointsInJointData(
            JointData oldJointData,
            double creationTimestamp,
            IReadOnlyDictionary<JointType, Joint> newJoints,
            DataStreamType dataStreamType)
        {
            JointData jointData = new JointData
            {
                RelTimestamp = oldJointData.RelTimestamp,
                AbsTimestamp = oldJointData.AbsTimestamp,
                Timestamps = oldJointData.Timestamps,
                Index = oldJointData.Index,
                Joints = newJoints,
                DataStreamType = dataStreamType
            };

            // add the creationTimestamp to the right pipeline step
            jointData.Timestamps.Add(creationTimestamp);

            return jointData;
        }

        /// <summary>
        /// Return the longest tracked body.
        /// </summary>
        /// <returns></returns>
        public Body GetFirstTrackedBody()
        {
            if (Bodies != null) {
                foreach (Body body in Bodies) {
                    if (body.IsTracked) {
                        return body;
                    }
                }
                return null;
            }
            else
                return null;
        }
    }

    public struct JointData
    {
        private double relTimestamp;
        private double absTimestamp;
        private IReadOnlyDictionary<JointType, Joint> joints;
        private long index;
        private List<double> timestamps;
        private DataStreamType dataStreamType;

        /// <summary>
        /// Time since Kinect sensor started.
        /// </summary>
        public double RelTimestamp { get => relTimestamp; set => relTimestamp = value; }
        /// <summary>
        /// Time since first frame.
        /// </summary>
        public double AbsTimestamp { get => absTimestamp; set => absTimestamp = value; }
        /// <summary>
        /// Positions of all joints.
        /// </summary>
        public IReadOnlyDictionary<JointType, Joint> Joints {
            get => joints; set => joints = value; }
        /// <summary>
        /// Incrementing number of frames.
        /// </summary>
        public long Index { get => index; set => index = value; }
        /// <summary>
        /// List of all timestamps that were set in the pipeline
        /// </summary>
        public List<double> Timestamps { get => timestamps; set => timestamps = value; }
        /// <summary>
        /// Type of the data.
        /// </summary>
        public DataStreamType DataStreamType {
            get => dataStreamType; set => dataStreamType = value; }
    }

    public struct SegmentHit
    {
        private long index;
        private long detectionIndex;
        private double absTimestamp;
        private double detectionAbsTimestamp;
        private HitType hitType;

        /// <summary>
        /// Index of the joint data that this hit belongs to.
        /// </summary>
        public long Index { get => index; set => index = value; }
        /// <summary>
        /// Index of the joint data where this hit was detected.
        /// </summary>
        public long DetectionIndex { get => detectionIndex; set => detectionIndex = value; }
        /// <summary>
        /// Absolute timestamp of the joint data that this hit belongs to.
        /// </summary>
        public double AbsTimestamp { get => absTimestamp; set => absTimestamp = value; }
        /// <summary>
        /// Absolute timestamp of the joint data where this hit was detected.
        /// </summary>
        public double DetectionAbsTimestamp { get => detectionAbsTimestamp; set => detectionAbsTimestamp = value; }
        /// <summary>
        /// Type of this hit in the context of a segment.
        /// </summary>
        public HitType HitType { get => hitType; set => hitType = value; }
    }
}
