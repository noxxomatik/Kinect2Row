using Microsoft.Kinect;
using RowingMonitor.Model.EventArguments;
using RowingMonitor.Model.Exceptions;
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
    /// Shifts the origin to the middle point between the foot joints plus an offset from foot to hip joint.
    /// Also rotates all joints until origin and hip joint form a horizontal line.
    /// </summary>
    public class Shifter
    {
        /// <summary>
        /// DEPRECATED
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public delegate void ShiftedFrameArrivedEventHandler(Object sender,
            ShiftedFrameArrivedEventArgs e);
        /// <summary>
        /// DEPRECATED
        /// </summary>
        public event ShiftedFrameArrivedEventHandler ShiftedFrameArrived;

        private ActionBlock<JointData> input;
        private BroadcastBlock<JointData> output;        

        private List<double> timeLog = new List<double>();

        /// <summary>
        /// Creates a new Shifter object and defines the Input and Output dataflow blocks.
        /// </summary>
        public Shifter()
        {
            Input = new ActionBlock<JointData>(jointData =>
            {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                Output.Post(ShiftAndRotate(jointData));

                stopwatch.Stop();
                // log times
                timeLog.Add(stopwatch.Elapsed.TotalMilliseconds);
                if (timeLog.Count == 100) {
                    Logger.LogTimes(timeLog, this.ToString(),
                        "mean time to shift the system");
                    timeLog = new List<double>();
                }
            });
            Output = new BroadcastBlock<JointData>(jointData =>
            {
                return jointData;
            });
        }

        /// <summary>
        /// Shifts the origin of all joint data to the center of the feet and rotates
        /// the coordinate system until the line between origin and spine base is horizontal.
        /// </summary>
        /// <param name="jointData">The joint data to be transformed.</param>
        /// <returns>Returns the transformed joint data.</returns>
        public JointData ShiftAndRotate(JointData jointData)
        {
            if (jointData.Joints[JointType.FootLeft].TrackingState == TrackingState.NotTracked
                || jointData.Joints[JointType.FootRight].TrackingState == TrackingState.NotTracked) {
                throw new BodyNotFullyTrackedException();
            }

            /*
             * The origin (x=0, y=0, z=0) is located at the center of the IR sensor on Kinect
             * X grows to the sensor’s left
             * Y grows up (note that this direction is based on the sensor’s tilt)
             * Z grows out in the direction the sensor is facing
             * 1 unit = 1 meter
            */

            // create the cfoot joint
            Vector4 footCenter = new Vector4();
            footCenter.X = (jointData.Joints[JointType.FootLeft].Position.X
                + jointData.Joints[JointType.FootRight].Position.X) / 2;
            footCenter.Y = (jointData.Joints[JointType.FootLeft].Position.Y
                + jointData.Joints[JointType.FootRight].Position.Y) / 2
                + Properties.Settings.Default.FootSpineBaseOffset;
            footCenter.Z = (jointData.Joints[JointType.FootLeft].Position.Z
                + jointData.Joints[JointType.FootRight].Position.Z) / 2;            

            // transform all points so that footCenter + the FootSpineBaseOffset in y is origin
            Dictionary<JointType, Joint> newJoints = new Dictionary<JointType, Joint>();
            foreach (KeyValuePair<JointType, Joint> joint in jointData.Joints) {
                Joint newJoint = joint.Value;
                newJoint.Position.X -= footCenter.X;
                newJoint.Position.Y -= footCenter.Y;
                newJoint.Position.Z -= footCenter.Z;
                newJoints.Add(joint.Key, newJoint);
            }

            // get the angular difference between the line between ankleCenter and SpineBase
            // angle around x-axis
            double diffAngle = Math.Atan2(newJoints[JointType.SpineBase].Position.Y,
                newJoints[JointType.SpineBase].Position.Z);

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

            JointData newJointData = JointDataHandler.ReplaceJointsInJointData(
                jointData,
                DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond,
                shiftedJoints, DataStreamType.ShiftedPosition);

            return newJointData;
        }

        /// <summary>
        /// DEPRECATED
        /// </summary>
        /// <param name="jointData"></param>
        public void Updata(JointData jointData)
        {
            JointData shiftedJointData = ShiftAndRotate(jointData);
            ShiftedFrameArrived(this, new ShiftedFrameArrivedEventArgs(shiftedJointData));
        }

        /// <summary>
        /// BroadcastBlock that sends the transformed JointData to all linked target blocks.
        /// </summary>
        public BroadcastBlock<JointData> Output { get => output; set => output = value; }

        /// <summary>
        /// ActionBlock that recieves JointData.
        /// </summary>
        public ActionBlock<JointData> Input { get => input; set => input = value; }
    }
}
