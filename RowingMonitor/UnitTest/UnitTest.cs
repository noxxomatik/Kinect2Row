using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RowingMonitor.Model;
using System.Collections.Generic;
using Microsoft.Kinect;
using System.Linq;
using System.Diagnostics;

namespace UnitTest
{
    [TestClass]
    public class UnitTest
    {
        JointData[] returnedJointData = new JointData[6];
        int index = 0;

        [TestMethod]
        public void TestCalculateVelocity()
        {
            VelocityCalculator calc = new VelocityCalculator();
            calc.CalculatedFrameArrived += Calc_CalculatedFrameArrived;
            JointData[] jointData = new JointData[6];
            float[] values = { 1, 2, 4, 7, 11, 16 };
            float[] results = { 1.0f, 1.5f, 2.5f, 3.5f, 4.5f, 5.0f };
            for (int i = 0; i < 6; i++) {
                JointData joint = new JointData {
                    RelTimestamp = 1000 + i * 1000,
                    AbsTimestamp = 0 + i * 1000,
                    Index = i
                };
                Dictionary<JointType, Joint> joints = new Dictionary<JointType, Joint>();
                Joint newJoint = new Joint();
                newJoint.Position.X = values[i];
                newJoint.Position.Y = values[i];
                newJoint.Position.Z = values[i];
                joints.Add(JointType.AnkleLeft, newJoint);
                joint.Joints = joints;
                jointData[i] = joint;
            }
            for (int i = 0; i < 6; i++) {
                calc.CalculateVelocity(jointData[i]);
            }
            // wait for the results
            System.Threading.Thread.Sleep(5000);

            // the last element cannot be tested since it needs one frame as buffer
            for (int i = 0; i < 5; i++) {
                Assert.AreEqual(returnedJointData[i].Joints[JointType.AnkleLeft].Position.X,
                    results[i]);
            }
        }

        private void Calc_CalculatedFrameArrived(object sender, CalculatedFrameArrivedEventArgs e)
        {
            Debug.WriteLine("Event");
            returnedJointData[index] = e.KinectDataContainer.VelocityJointData.Last();
            index++;
        }
    }
}
