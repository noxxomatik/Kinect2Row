using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RowingMonitor.Model
{
    class VelocityCalculator
    {
        public delegate void CalculatedFrameArrivedEventHandler(Object sender, 
            CalculatedFrameArrivedEventArgs e);
        public event CalculatedFrameArrivedEventHandler CalculatedFrameArrived;

        /// <summary>
        /// Calculates the velocity along the axis for every joint in m/s. 
        /// </summary>
        /// <param name="jointDataList">Given joint data list.</param>
        public void CalculateVelocity(List<JointData> jointDataList)
        {
            KinectDataContainer kdc = KinectDataContainer.Instance;           
            
            JointData currJointData = jointDataList.Last();
            Dictionary<JointType, Joint> newJoints = new Dictionary<JointType, Joint>();
            // first time
            if (kdc.VelocityJointData.Count() <= 0) {                
                foreach(KeyValuePair<JointType, Joint> joint in currJointData.Joints) {
                    Joint newJoint = joint.Value;
                    newJoint.Position.X = newJoint.Position.Y = newJoint.Position.Z = 0;
                    newJoints.Add(joint.Key, newJoint);
                }                
            }
            else {
                JointData prevJointData = jointDataList[jointDataList.Count() - 2];
                JointData prevVelJointData = kdc.VelocityJointData.Last();
                double time = (currJointData.RelTimestamp - prevJointData.RelTimestamp) / 1000;
                foreach (KeyValuePair<JointType, Joint> joint in currJointData.Joints) {
                    Joint newJoint = joint.Value;
                    newJoint.Position.X = Convert.ToSingle(
                        (currJointData.Joints[joint.Key].Position.X
                        - prevJointData.Joints[joint.Key].Position.X) / time);
                    newJoint.Position.Y = Convert.ToSingle(
                        (currJointData.Joints[joint.Key].Position.Y 
                        - prevJointData.Joints[joint.Key].Position.Y) / time);
                    newJoint.Position.Z = Convert.ToSingle(
                        (currJointData.Joints[joint.Key].Position.Z
                        - prevJointData.Joints[joint.Key].Position.Z) / time);
                    newJoints.Add(joint.Key, newJoint);
                }
            }
            kdc.AddNewVelocityJointData(currJointData.RelTimestamp, newJoints, currJointData.Index);

            CalculatedFrameArrived(this, new CalculatedFrameArrivedEventArgs());
        }
    }
}
