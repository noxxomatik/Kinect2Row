using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RowingMonitor.Model.Util
{
    public class JointDataHandler
    {
        private static readonly JointDataHandler instance = new JointDataHandler();

        private Body[] bodies;

        private long lastIndex;

        private double relStartTime = -1;

        private JointDataHandler() { }

        public static JointDataHandler Instance
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

        /// <summary>
        /// Get a specific joint data value.
        /// </summary>
        /// <param name="jointData">Joint data object.</param>
        /// <param name="jointType">Joint type of the value.</param>
        /// <param name="axis">Axis of the value (X, Y, Z).</param>
        /// <returns>Returns the requested value.</returns>
        public static float GetJointDataValue(JointData jointData,
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

        /// <summary>
        /// Short check to validate that the joint data has valid values.
        /// </summary>
        /// <param name="jointData">The joint data to be validated.</param>
        /// <returns>Returns true if the joint data is valid.</returns>
        public static bool IsValid(JointData jointData)
        {
            if (!float.IsNaN(jointData.Joints[JointType.AnkleLeft].Position.X)) {
                return true;
            }
            return false;
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
        /// Time in milliseconds since Kinect sensor started.
        /// </summary>
        public double RelTimestamp { get => relTimestamp; set => relTimestamp = value; }
        /// <summary>
        /// Time in milliseconds since first frame.
        /// </summary>
        public double AbsTimestamp { get => absTimestamp; set => absTimestamp = value; }
        /// <summary>
        /// Positions of all joints.
        /// </summary>
        public IReadOnlyDictionary<JointType, Joint> Joints
        {
            get => joints; set => joints = value;
        }
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
        public DataStreamType DataStreamType
        {
            get => dataStreamType; set => dataStreamType = value;
        }
        /// <summary>
        /// Returns true if this element conatins no joint data.
        /// </summary>
        public bool IsEmpty
        {
            get {
                if (joints == null) { return true; }
                else { return false; }
            }
        }
    }
}
