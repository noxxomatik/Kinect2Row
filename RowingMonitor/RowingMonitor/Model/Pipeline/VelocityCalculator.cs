using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RowingMonitor.Model
{
    public class VelocityCalculator
    {
        public delegate void CalculatedFrameArrivedEventHandler(Object sender, 
            CalculatedFrameArrivedEventArgs e);
        public event CalculatedFrameArrivedEventHandler CalculatedFrameArrived;

        private JointData lastJointData;
        private JointData penultimateJointData;        

        /// <summary>
        /// Calculates the velocity as 1st derivative (gradient) of position.
        /// Calculation needs one frame as buffer.
        /// </summary>
        /// <param name="jointData"></param>
        public void CalculateVelocity(JointData jointData)
        {
            // check if first value
            if (lastJointData.RelTimestamp == 0) {
                lastJointData = jointData;
            }
            // check if second value -> use the boundaries formula
            else if (penultimateJointData.RelTimestamp == 0) {
                Dictionary<JointType, Joint> newJoints = new Dictionary<JointType, Joint>();
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
                KinectDataContainer kdc = KinectDataContainer.Instance;
                kdc.AddNewVelocityJointData(jointData.RelTimestamp, newJoints, lastJointData.Index);

                // save to history
                penultimateJointData = lastJointData;
                lastJointData = jointData;

                CalculatedFrameArrived(this, new CalculatedFrameArrivedEventArgs());
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
                KinectDataContainer kdc = KinectDataContainer.Instance;
                kdc.AddNewVelocityJointData(jointData.RelTimestamp, newJoints, lastJointData.Index);

                // save to history
                penultimateJointData = lastJointData;
                lastJointData = jointData;

                CalculatedFrameArrived(this, new CalculatedFrameArrivedEventArgs());
            }
        }
    }
}
