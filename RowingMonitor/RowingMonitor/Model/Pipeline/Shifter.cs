using Microsoft.Kinect;
using RowingMonitor.Model.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RowingMonitor.Model
{
    /// <summary>
    /// Shifts  the origin to the middle point between the foot ankle joints.
    /// Also rotates all joints until origin and hip joint form a horizontal line.
    /// </summary>
    class Shifter
    {
        public delegate void ShiftedFrameArrivedEventHandler(Object sender,
            ShiftedFrameArrivedEventArgs e);
        public event ShiftedFrameArrivedEventHandler ShiftedFrameArrived;

        public void ShiftAndRotate(JointData jointData)
        {
            if (jointData.Joints[JointType.AnkleLeft].TrackingState == TrackingState.NotTracked
                || jointData.Joints[JointType.AnkleLeft].TrackingState == TrackingState.NotTracked) {
                throw new BodyNotFullyTrackedException();
            }
            //TODO
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
            double diffAngle = Math.Atan2(newJoints[JointType.SpineBase].Position.Z,
                newJoints[JointType.SpineBase].Position.Y);
            // add another 90°
            //diffAngle += 1.5708;
            //Debug.WriteLine("diffAngle " + diffAngle);

            // create the rotation matrix around x-axis
            double[,] rotMat = {
                {1.0, 0.0, 0.0},
                {0.0, Math.Cos(diffAngle), -(Math.Sin(diffAngle))},
                {0.0, Math.Sin(diffAngle), Math.Cos(diffAngle)}
            };

            // multiply (dot prodct) rotmat and position
            Dictionary<JointType, Joint> shiftedJoints = new Dictionary<JointType, Joint>();
            foreach (KeyValuePair<JointType, Joint> joint in newJoints) {
                Joint newJoint = joint.Value;
                newJoint.Position.X = Convert.ToSingle(
                    rotMat[0, 0] * newJoint.Position.X
                    + rotMat[0, 1] * newJoint.Position.Y
                    + rotMat[0, 2] * newJoint.Position.Z);
                newJoint.Position.Y = Convert.ToSingle(
                    rotMat[1, 0] * newJoint.Position.X
                    + rotMat[1, 1] * newJoint.Position.Y
                    + rotMat[1, 2] * newJoint.Position.Z);
                newJoint.Position.Z = Convert.ToSingle(
                    rotMat[2, 0] * newJoint.Position.X
                    + rotMat[2, 1] * newJoint.Position.Y
                    + rotMat[2, 2] * newJoint.Position.Z);
                shiftedJoints.Add(joint.Key, newJoint);
            }


            JointData newJointData = KinectDataHandler.ReplaceJointsInJointData(
                jointData,
                DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond,
                shiftedJoints);

            ShiftedFrameArrived(this, new ShiftedFrameArrivedEventArgs(newJointData));
        }
    }
}
