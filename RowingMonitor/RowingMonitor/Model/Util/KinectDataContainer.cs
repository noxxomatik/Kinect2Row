using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace RowingMonitor.Model
{
    /// <summary>
    /// Represents a container for all read and current data of the Kinect sensor.
    /// 
    /// This class uses the singleton pattern with static initialization.
    /// Only one container per session can be active.
    /// </summary>
    public sealed class KinectDataContainer
    {
        private static readonly KinectDataContainer instance = new KinectDataContainer();

        private Body[] bodies;

        private List<JointData> rawJointData = new List<JointData>();

        private List<JointData> smoothedJointData = new List<JointData>();

        private List<JointData> velocityJointData = new List<JointData>();

        private List<JointData> shiftedJointData = new List<JointData>();

        private double relStartTime = -1;

        private WriteableBitmap colorBitmap = null;

        /* Properties */
        /// <summary>
        /// Get and sets the current body frame. As long as those body objects are not disposed 
        /// and not set to null in the array, those body objects will be re-used
        /// </summary>
        public Body[] Bodies { get => bodies; set => bodies = value;}

        /// <summary>
        /// List of all captured joint data.
        /// </summary>
        public List<JointData> RawJointData { get => rawJointData; }

        /// <summary>
        /// List of all smoothed joint data.
        /// </summary>
        public List<JointData> SmoothedJointData { get => smoothedJointData; }

        /// <summary>
        /// List of all velocity joint data,
        /// </summary>
        public List<JointData> VelocityJointData { get => velocityJointData; }

        /// <summary>
        /// List of all shifted joint data.
        /// </summary>
        public List<JointData> ShiftedJointData { get => shiftedJointData; set => shiftedJointData = value; }

        /// <summary>
        /// Last recorded color image.
        /// </summary>
        public WriteableBitmap ColorBitmap { get => colorBitmap; set => colorBitmap = value; }

        private KinectDataContainer() { }

        public static KinectDataContainer Instance
        {
            get {
                return instance;
            }
        }        

        /// <summary>
        /// Return the longest tracked body.
        /// </summary>
        /// <returns></returns>
        public Body GetFirstTrackedBody()
        {
            if (bodies != null) {
                foreach (Body body in bodies) {
                    if (body.IsTracked) {
                        return body;
                    }
                }
                return null;
            }
            else
                return null;
        }

        public void AddNewRawJointData(double timestamp, IReadOnlyDictionary<JointType, Joint> joints)
        {
            JointData jointData = NewJointData(timestamp, joints);
            rawJointData.Add(jointData);
        }

        public void AddNewSmoothedJointData(double timestamp, IReadOnlyDictionary<JointType, Joint> joints)
        {
            JointData jointData = NewJointData(timestamp, joints);
            smoothedJointData.Add(jointData);
        }

        public void AddNewVelocityJointData(double timestamp, IReadOnlyDictionary<JointType, Joint> joints)
        {
            JointData jointData = NewJointData(timestamp, joints);
            velocityJointData.Add(jointData);
        }

        public void AddNewShiftedJointData(double timestamp, IReadOnlyDictionary<JointType, Joint> joints)
        {
            JointData jointData = NewJointData(timestamp, joints);
            shiftedJointData.Add(jointData);
        }

        private JointData NewJointData(double timestamp, IReadOnlyDictionary<JointType, Joint> joints)
        {
            if (relStartTime == -1) {
                relStartTime = timestamp;
            }
            JointData jointData = new JointData {
                RelTimestamp = timestamp,
                AbsTimestamp = timestamp - relStartTime,
                Joints = joints
            };
            return jointData;
        }
    }

    public struct JointData
    {
        private double relTimestamp;
        private double absTimestamp;
        private IReadOnlyDictionary<JointType, Joint> joints;

        public double RelTimestamp { get => relTimestamp; set => relTimestamp = value; }
        public double AbsTimestamp { get => absTimestamp; set => absTimestamp = value; }
        public IReadOnlyDictionary<JointType, Joint> Joints { get => joints; set => joints = value; }
    }
}
