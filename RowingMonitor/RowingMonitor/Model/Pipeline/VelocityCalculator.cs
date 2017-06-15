using Microsoft.Kinect;
using RowingMonitor.Model.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace RowingMonitor.Model
{
    public class VelocityCalculator
    {
        public delegate void CalculatedFrameArrivedEventHandler(Object sender,
            CalculatedFrameArrivedEventArgs e);
        public event CalculatedFrameArrivedEventHandler CalculatedFrameArrived;

        private JointData lastJointData;
        private JointData penultimateJointData;

        private TransformBlock<JointData, JointData> calculationBlock;

        public TransformBlock<JointData, JointData> CalculationBlock { get => calculationBlock; set => calculationBlock = value; }

        public VelocityCalculator()
        {
            CalculationBlock = new TransformBlock<JointData, JointData>(jointData =>
            {
                return CalculateVelocity(jointData);
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
        /// <param name="jointData"></param>
        public JointData CalculateVelocity(JointData jointData)
        {
            // check if first value
            if (lastJointData.RelTimestamp == 0) {
                lastJointData = jointData;
                return new JointData();
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

                JointData newJointData = KinectDataHandler.ReplaceJointsInJointData(
                    lastJointData,
                    DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond,
                    newJoints);

                // save to history
                penultimateJointData = lastJointData;
                lastJointData = jointData;

                return newJointData;
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

                JointData newJointData = KinectDataHandler.ReplaceJointsInJointData(
                    lastJointData,
                    DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond,
                    newJoints);

                // save to history
                penultimateJointData = lastJointData;
                lastJointData = jointData;

                return newJointData;
            }
        }
    }
}
