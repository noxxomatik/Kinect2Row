using Microsoft.Kinect;
using RowingMonitor.Model.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace RowingMonitor.Model.Pipeline
{
    /// <summary>
    /// Shifts  the origin to the middle point between the foot ankle joints.
    /// Also rotates all joints until origin and hip joint form a horizontal line.
    /// </summary>
    public class Shifter
    {
        public delegate void ShiftedFrameArrivedEventHandler(Object sender,
            ShiftedFrameArrivedEventArgs e);
        public event ShiftedFrameArrivedEventHandler ShiftedFrameArrived;

        private ActionBlock<JointData> input;
        private BroadcastBlock<JointData> output;

        // Logger
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(
            System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public BroadcastBlock<JointData> Output { get => output; set => output = value; }
        public ActionBlock<JointData> Input { get => input; set => input = value; }

        public Shifter()
        {
            Input = new ActionBlock<JointData>(jointData =>
            {
                Output.Post(ShiftAndRotate(jointData));
            });
            Output = new BroadcastBlock<JointData>(jointData =>
            {
                return jointData;
            });
        }

        public JointData ShiftAndRotate(JointData jointData)
        {
            if (jointData.Joints[JointType.AnkleLeft].TrackingState == TrackingState.NotTracked
                || jointData.Joints[JointType.AnkleLeft].TrackingState == TrackingState.NotTracked) {
                throw new BodyNotFullyTrackedException();
            }

            /*
             * The origin (x=0, y=0, z=0) is located at the center of the IR sensor on Kinect
             * X grows to the sensor’s left
             * Y grows up (note that this direction is based on the sensor’s tilt)
             * Z grows out in the direction the sensor is facing
             * 1 unit = 1 meter
            */

            // create the cankle
            Vector4 ankleCenter = new Vector4();
            ankleCenter.X = (jointData.Joints[JointType.AnkleLeft].Position.X
                + jointData.Joints[JointType.AnkleRight].Position.X) / 2;
            ankleCenter.Y = (jointData.Joints[JointType.AnkleLeft].Position.Y
                + jointData.Joints[JointType.AnkleRight].Position.Y) / 2;
            ankleCenter.Z = (jointData.Joints[JointType.AnkleLeft].Position.Z
                + jointData.Joints[JointType.AnkleRight].Position.Z) / 2;

            // transform all points that ankleCenter is origin
            Dictionary<JointType, Joint> newJoints = new Dictionary<JointType, Joint>();
            foreach (KeyValuePair<JointType, Joint> joint in jointData.Joints) {
                Joint newJoint = joint.Value;
                newJoint.Position.X -= ankleCenter.X;
                newJoint.Position.Y -= ankleCenter.Y;
                newJoint.Position.Z -= ankleCenter.Z;
                newJoints.Add(joint.Key, newJoint);
            }

            // get the angular difference between the line between ankleCenter and SpineBase
            // angle around x-axis
            double diffAngle = Math.Atan2(newJoints[JointType.SpineBase].Position.Y,
                newJoints[JointType.SpineBase].Position.Z);
            log.Info("Angle difference: " + (diffAngle * (180.0 / Math.PI)) + " degrees");

            // create the rotation matrix around x-axis (counter clockwise)
            double[,] rotMat = {
                {1.0, 0.0, 0.0},
                {0.0, Math.Cos(diffAngle), -(Math.Sin(diffAngle))},
                {0.0, Math.Sin(diffAngle), Math.Cos(diffAngle)}
            };

            // multiply (dot product) rotmat and position
            Dictionary<JointType, Joint> shiftedJoints = new Dictionary<JointType, Joint>();
            foreach (KeyValuePair<JointType, Joint> joint in newJoints) {
                Joint newJoint = joint.Value;
                double x = rotMat[0, 0] * joint.Value.Position.X
                    + rotMat[0, 1] * joint.Value.Position.Y
                    + rotMat[0, 2] * joint.Value.Position.Z;
                newJoint.Position.X = Convert.ToSingle(x);

                double y = rotMat[1, 0] * joint.Value.Position.X
                    + rotMat[1, 1] * joint.Value.Position.Y
                    + rotMat[1, 2] * joint.Value.Position.Z;
                newJoint.Position.Y = Convert.ToSingle(y);

                double z = rotMat[2, 0] * joint.Value.Position.X
                    + rotMat[2, 1] * joint.Value.Position.Y
                    + rotMat[2, 2] * joint.Value.Position.Z;
                newJoint.Position.Z = Convert.ToSingle(z);

                shiftedJoints.Add(joint.Key, newJoint);
            }


            JointData newJointData = KinectDataHandler.ReplaceJointsInJointData(
                jointData,
                DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond,
                shiftedJoints, DataStreamType.ShiftedPosition);

            return newJointData;
        }

        public void Updata(JointData jointData)
        {
            JointData shiftedJointData = ShiftAndRotate(jointData);
            ShiftedFrameArrived(this, new ShiftedFrameArrivedEventArgs(shiftedJointData));
        }
    }
}
