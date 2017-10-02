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
    /// The VelocityCalculator class calcutlates the velocities of the joints.
    /// </summary>
    public class VelocityCalculator
    {
        public delegate void CalculatedFrameArrivedEventHandler(Object sender,
            CalculatedFrameArrivedEventArgs e);
        public event CalculatedFrameArrivedEventHandler CalculatedFrameArrived;

        private JointData lastJointData;
        private JointData penultimateJointData;

        private ActionBlock<JointData> input;
        private BroadcastBlock<JointData> output;

        private List<double> timeLog = new List<double>();

        public BroadcastBlock<JointData> Output { get => output; set => output = value; }
        public ActionBlock<JointData> Input { get => input; set => input = value; }

        /// <summary>
        /// Creates a new VelocityCalculator class.
        /// </summary>
        public VelocityCalculator()
        {
            Input = new ActionBlock<JointData>(jointData =>
            {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                Output.Post(CalculateVelocity(jointData));

                stopwatch.Stop();
                // log times
                timeLog.Add(stopwatch.Elapsed.TotalMilliseconds);
                if (timeLog.Count == 100) {
                    Logger.LogTimes(timeLog, this.ToString(),
                        "mean time to calculate the velocity");
                    timeLog = new List<double>();
                }
            });
            Output = new BroadcastBlock<JointData>(jointData =>
            {
                return jointData;
            });
        }

        public void Update(JointData jointData)
        {
            CalculatedFrameArrived(this, new CalculatedFrameArrivedEventArgs(
                CalculateVelocity(jointData)));
        }

        /// <summary>
        /// Calculates the velocity as 1st derivative (gradient) of position.
        /// Calculation needs one frame as buffer.
        /// </summary>
        /// <param name="jointData">The joint data of which the velocity gets calculated.</param>
        public JointData CalculateVelocity(JointData jointData)
        {
            // check if jointData is NaN
            if (jointData.Joints[JointType.SpineBase].Position.X == float.NaN) {
                throw new Exception("Joint data position is not a number.");
            }

            JointData newJointData;

            // check if first value
            if (lastJointData.RelTimestamp == 0) {
                // save to history
                lastJointData = jointData;

                Dictionary<JointType, Joint> joints = new Dictionary<JointType, Joint>();
                foreach (KeyValuePair<JointType, Joint> joint in jointData.Joints) {
                    Joint newJoint = new Joint();
                    newJoint.Position.X = 0.0f;
                    newJoint.Position.Y = 0.0f;
                    newJoint.Position.Z = 0.0f;
                    joints.Add(joint.Key, newJoint);
                }

                newJointData = JointDataHandler.ReplaceJointsInJointData(
                    lastJointData,
                    DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond,
                    joints, DataStreamType.Velocity);
            }
            // check if second value -> use the boundaries formula
            else if (penultimateJointData.RelTimestamp == 0) {
                Dictionary<JointType, Joint> newJoints =
                    new Dictionary<JointType, Joint>();
                double time = (jointData.RelTimestamp - lastJointData.RelTimestamp) / 1000;
                foreach (KeyValuePair<JointType, Joint> joint in jointData.Joints) {
                    Joint newJoint = joint.Value;
                    newJoint.Position.X = Convert.ToSingle(
                        (jointData.Joints[joint.Key].Position.X
                        - lastJointData.Joints[joint.Key].Position.X) / time);
                    newJoint.Position.Y = Convert.ToSingle(
                        (jointData.Joints[joint.Key].Position.Y
                        - lastJointData.Joints[joint.Key].Position.Y) / time);
                    newJoint.Position.Z = Convert.ToSingle(
                        (jointData.Joints[joint.Key].Position.Z
                        - lastJointData.Joints[joint.Key].Position.Z) / time);
                    newJoints.Add(joint.Key, newJoint);
                }

                newJointData = JointDataHandler.ReplaceJointsInJointData(
                    lastJointData,
                    DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond,
                    newJoints, DataStreamType.Velocity);

                // save to history
                penultimateJointData = lastJointData;
                lastJointData = jointData;
            }
            // if two old values are present -> use interior formula
            else {
                Dictionary<JointType, Joint> newJoints = new Dictionary<JointType, Joint>();
                double time = (jointData.RelTimestamp - penultimateJointData.RelTimestamp) / 1000;
                foreach (KeyValuePair<JointType, Joint> joint in jointData.Joints) {
                    Joint newJoint = joint.Value;
                    newJoint.Position.X = Convert.ToSingle(
                        (jointData.Joints[joint.Key].Position.X
                        - penultimateJointData.Joints[joint.Key].Position.X) / time);
                    newJoint.Position.Y = Convert.ToSingle(
                        (jointData.Joints[joint.Key].Position.Y
                        - penultimateJointData.Joints[joint.Key].Position.Y) / time);
                    newJoint.Position.Z = Convert.ToSingle(
                        (jointData.Joints[joint.Key].Position.Z
                        - penultimateJointData.Joints[joint.Key].Position.Z) / time);
                    newJoints.Add(joint.Key, newJoint);
                }

                newJointData = JointDataHandler.ReplaceJointsInJointData(
                    lastJointData,
                    DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond,
                    newJoints, DataStreamType.Velocity);

                // save to history
                penultimateJointData = lastJointData;
                lastJointData = jointData;
            }

            // log times
            timeLog.Add(newJointData.Timestamps.Last());
            if (timeLog.Count == 100) {
                Logger.LogTimes(timeLog, this.ToString(), "mean time to calculate velocity");
                timeLog = new List<double>();
            }

            return newJointData;
        }
    }
}
