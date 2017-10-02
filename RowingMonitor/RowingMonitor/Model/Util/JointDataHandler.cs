using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RowingMonitor.Model.Util
{
    /// <summary>
    /// The JointDataHandler class handles the generated kinect data and provides 
    /// static functions to work with the joint data.
    /// 
    /// This class implements the singleton pattern. Only one instance per application 
    /// is allowed since it also counts the genrated frames of the kinect sensor. The
    /// class has no public constructor.
    /// 
    /// Get an instance of the class with:
    /// JointDataHandler jdh = JointDataHandler.Instance;
    /// </summary>
    public class JointDataHandler
    {
        private static readonly JointDataHandler instance = new JointDataHandler();

        private Body[] bodies;
        private long lastIndex;
        private double relStartTime = -1;

        // circular buffers for the feet history
        private CircularBuffer footRightHist;
        private CircularBuffer footLeftHist;

        private JointDataHandler()
        {
            footRightHist = new CircularBuffer(10);
            footLeftHist = new CircularBuffer(10);
        }        

        /// <summary>
        /// Create a new JointData object for the processing pipeline.
        /// </summary>
        /// <param name="relTimestamp">The elapsed time since the sensor started.</param>
        /// <param name="creationTimestamp">The creation time of the new joint values.</param>
        /// <param name="joints">The dictionary of the tracked joints.</param>
        /// <returns>Returns the newly created joint data.</returns>
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
                Joints = FixFeet(joints)
            };
            jointData.Timestamps = new List<double>();
            jointData.Timestamps.Add(creationTimestamp);
            jointData.DataStreamType = DataStreamType.RawPosition;

            LastIndex++;
            return jointData;
        }

        /// <summary>
        /// Feet should be fixed joints since they are fixed to the footstretcher.
        /// Use the mean position of the last ten foot positions, if the foot joint is inferred.
        /// </summary>
        /// <param name="joints">The unfiltered joints.</param>
        /// <returns>The joints with fixed feet if they were inferred.</returns>
        private IReadOnlyDictionary<JointType, Joint> FixFeet(IReadOnlyDictionary<JointType, Joint> joints)
        {
            // check if one of the feet isn't tracked and has a history
            if ((joints[JointType.FootLeft].TrackingState != TrackingState.Tracked
                && footLeftHist.ToArray().Length > 0)
                || (joints[JointType.FootRight].TrackingState != TrackingState.Tracked
                && footRightHist.ToArray().Length > 0)) {

                // edit the joint dictionary
                Dictionary<JointType, Joint> newJoints = (Dictionary<JointType, Joint>)joints;

                // if both feet are not tracked
                if (joints[JointType.FootLeft].TrackingState != TrackingState.Tracked
                    && joints[JointType.FootRight].TrackingState != TrackingState.Tracked) {
                    //  replace the feet joints with the mean position of the last 10 positions 
                    ReplaceJointPosition(newJoints, JointType.FootLeft, GetMeanPosition(true));
                    ReplaceJointPosition(newJoints, JointType.FootRight, GetMeanPosition(false));
                }
                // if only the left foot isn't tracked
                else if (joints[JointType.FootLeft].TrackingState != TrackingState.Tracked) {
                    // save the right foot
                    footRightHist.Enqueue(joints[JointType.FootRight].Position);
                    // replace the left foot
                    ReplaceJointPosition(newJoints, JointType.FootLeft, GetMeanPosition(true));
                }
                // if only the right foot isn't tracked
                else {
                    footLeftHist.Enqueue(joints[JointType.FootLeft].Position);
                    ReplaceJointPosition(newJoints, JointType.FootRight, GetMeanPosition(false));
                }
                return newJoints;
            }
            // if all feet are tracked or have no history, save them
            else {
                footLeftHist.Enqueue(joints[JointType.FootLeft].Position);
                footRightHist.Enqueue(joints[JointType.FootRight].Position);

                return joints;
            }
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
        /// Calculates and returns the mean position of the left or right foot.
        /// </summary>
        /// <param name="footLeft">True if the left foot should be calculated.
        /// False for the right foot.</param>
        /// <returns>Returns the mean position as CameraSpacePoint.</returns>
        private CameraSpacePoint GetMeanPosition(bool footLeft)
        {
            JointType foot = footLeft ? JointType.FootLeft : JointType.FootRight;
            int histLength = footLeft ?
                footLeftHist.ToArray().Length : footRightHist.ToArray().Length;

            CameraSpacePoint newPosition = new CameraSpacePoint();

            foreach (CameraSpacePoint position in
                footLeft ? footLeftHist.ToArray() : footRightHist.ToArray()) {
                newPosition.X += position.X;
                newPosition.Y += position.Y;
                newPosition.Z += position.Z;
            }
            newPosition.X /= histLength;
            newPosition.Y /= histLength;
            newPosition.Z /= histLength;

            return newPosition;
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

        /// <summary>
        /// Replaces the joint dictionary of a JointData object with new joint values.
        /// </summary>
        /// <param name="oldJointData">The source JointData object.</param>
        /// <param name="creationTimestamp">The creation time of the new joint values.</param>
        /// <param name="newJoints">The new joints.</param>
        /// <param name="dataStreamType">The pipeline object that created the new joint data.</param>
        /// <returns>Returns the refreshed joint data.</returns>
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
        /// Replaces the position of a joint.
        /// </summary>
        /// <param name="joints">The dictionary of joints that should be altered.</param>
        /// <param name="jointToReplace">The JointType of the joint which position should be replaced.</param>
        /// <param name="newPosition">The new position.</param>
        public static void ReplaceJointPosition(Dictionary<JointType, Joint> joints,
            JointType jointToReplace, CameraSpacePoint newPosition)
        {
            Joint newJoint = new Joint();
            newJoint.JointType = joints[jointToReplace].JointType;
            newJoint.TrackingState = joints[jointToReplace].TrackingState;
            newJoint.Position = newPosition;
            if (joints.ContainsKey(JointType.FootLeft)) {
                joints.Remove(JointType.FootLeft);
            }
            joints.Add(JointType.FootLeft, newJoint);
        }

        /// <summary>
        /// Get the current instance
        /// </summary>
        public static JointDataHandler Instance
        {
            get {
                return instance;
            }
        }
        /// <summary>
        /// Timestamp of the first processed frame since the sensor started.
        /// </summary>
        public double RelStartTime { get => relStartTime; set => relStartTime = value; }
        /// <summary>
        /// Last processed frame index.
        /// </summary>
        public long LastIndex { get => lastIndex; set => lastIndex = value; }
        /// <summary>
        /// All currently tracked bodies.
        /// </summary>
        public Body[] Bodies { get => bodies; set => bodies = value; }
    }

    /// <summary>
    /// JointData is a collection of the tracked joints of one frame
    /// and its meta data which is passed through the processing pipeline.
    /// </summary>
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
        public double RelTimestamp
        {
            get => relTimestamp; set => relTimestamp = value;
        }
        /// <summary>
        /// Time in milliseconds since first frame.
        /// </summary>
        public double AbsTimestamp
        {
            get => absTimestamp; set => absTimestamp = value;
        }
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
        public long Index
        {
            get => index; set => index = value;
        }
        /// <summary>
        /// List of all timestamps that were set in the pipeline
        /// </summary>
        public List<double> Timestamps
        {
            get => timestamps; set => timestamps = value;
        }
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
