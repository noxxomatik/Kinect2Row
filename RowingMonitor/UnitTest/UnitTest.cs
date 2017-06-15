using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RowingMonitor.Model;
using System.Collections.Generic;
using Microsoft.Kinect;
using System.Linq;
using System.Diagnostics;
using RowingMonitor.Model.Util;
using RowingMonitor.Model.Pipeline;

namespace UnitTest
{
    [TestClass]
    public class UnitTest
    {
        JointData[] returnedJointData = new JointData[6];
        int index = 0;

        JointData shiftedJointData;

        List<JointData> filteredJointData = new List<JointData>();

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
                    Index = i,
                    Timestamps = new List<double>()
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
                calc.Update(jointData[i]);
            }

            // the last element cannot be tested since it needs one frame as buffer
            for (int i = 0; i < 5; i++) {
                Assert.AreEqual(returnedJointData[i].Joints[JointType.AnkleLeft].Position.X,
                    results[i]);
            }
        }

        private void Calc_CalculatedFrameArrived(object sender, CalculatedFrameArrivedEventArgs e)
        {
            if (e.CalculatedJointData.Timestamps != null) {
                returnedJointData[index] = e.CalculatedJointData;
                index++;
            }
        }

        [TestMethod]
        public void TestSubsequenceDTW()
        {
            List<double> template = new List<double> { 11, 6, 9, 4 };
            List<double> data = new List<double> { 5, 12, 6, 10, 6, 11, 6, 9, 4 };

            List<Subsequence> resultSubsequences = new List<Subsequence>();
            resultSubsequences.Add(new Subsequence {
                Distance = 4,
                TStart = 2,
                TEnd = 5,
                Status = SubsequenceStatus.OPTIMAL
            });
            resultSubsequences.Add(new Subsequence {
                Distance = 0,
                TStart = 6,
                TEnd = 9,
                Status = SubsequenceStatus.NOT_OPTIMAL
            });

            List<Subsequence> reportedSubsequences = new List<Subsequence>();

            SubsequenceDTW dtw = new SubsequenceDTW(template, 15, 4);
            for (int i = 0; i < data.Count; i++) {
                Subsequence sequence = dtw.compareDataStream(data[i], i + 1);
                if (sequence.Status == SubsequenceStatus.OPTIMAL)
                    reportedSubsequences.Add(sequence);
                else if (sequence.Status == SubsequenceStatus.NOT_OPTIMAL && i == data.Count - 1)
                    reportedSubsequences.Add(sequence);
            }

            Assert.AreEqual(resultSubsequences.Count, reportedSubsequences.Count);
            foreach (Subsequence sequence in reportedSubsequences) {
                Assert.AreEqual(resultSubsequences[reportedSubsequences.IndexOf(sequence)].Distance, sequence.Distance);
                Assert.AreEqual(resultSubsequences[reportedSubsequences.IndexOf(sequence)].TStart, sequence.TStart);
                Assert.AreEqual(resultSubsequences[reportedSubsequences.IndexOf(sequence)].TEnd, sequence.TEnd);
                Assert.AreEqual(resultSubsequences[reportedSubsequences.IndexOf(sequence)].Status, sequence.Status);
            }            
        }

        [TestMethod]
        public void TestShifter()
        {
            Shifter shifter = new Shifter();
            shifter.ShiftedFrameArrived += Shifter_ShiftedFrameArrived;

            JointData jointData = new JointData
            {
                RelTimestamp = 0,
                AbsTimestamp = 0,
                Index = 0,
                Timestamps = new List<double>()
            };

            Dictionary<JointType, Joint> joints = new Dictionary<JointType, Joint>();

            Joint ankleR = new Joint();
            ankleR.Position.X = 1;
            ankleR.Position.Y = 0;
            ankleR.Position.Z = 0;
            ankleR.TrackingState = TrackingState.Tracked;
            joints.Add(JointType.AnkleRight, ankleR);

            Joint ankleL = new Joint();
            ankleL.Position.X = -1;
            ankleL.Position.Y = 0;
            ankleL.Position.Z = 0;
            ankleL.TrackingState = TrackingState.Tracked;
            joints.Add(JointType.AnkleLeft, ankleL);

            Joint spine = new Joint();
            spine.Position.X = 0;
            spine.Position.Y = 2;
            spine.Position.Z = 0;
            joints.Add(JointType.SpineBase, spine);

            jointData.Joints = joints;

            shifter.Updata(jointData);

            // wait for the results
            System.Threading.Thread.Sleep(1000);

            Joint spineComp = new Joint();
            spineComp.Position.X = 0;
            spineComp.Position.Y = 0;
            spineComp.Position.Z = 2;

            // convert to int because pi is not precise in the calculation
            Assert.AreEqual(Convert.ToInt16(spineComp.Position.X),
                Convert.ToInt16(shiftedJointData.Joints[JointType.SpineBase].Position.X));
            Assert.AreEqual(Convert.ToInt16(spineComp.Position.Y),
                Convert.ToInt16(shiftedJointData.Joints[JointType.SpineBase].Position.Y));
            Assert.AreEqual(Convert.ToInt16(spineComp.Position.Z),
                Convert.ToInt16(shiftedJointData.Joints[JointType.SpineBase].Position.Z));
        }

        private void Shifter_ShiftedFrameArrived(object sender, ShiftedFrameArrivedEventArgs e)
        {           
            shiftedJointData = e.ShiftedJointData;            
        }

        [TestMethod]
        public void TestOneEuroFilter()
        {
            OneEuroFilter filter = new OneEuroFilter(1.0, 1.0);

            // fake data
            double[] timestamps = { 0.0, 0.1, 0.2, 0.4, 0.6, 1.0, 1.2, 1.4, 1.5, 1.8 };
            double[] signals = { -1.0, -0.5, 0.0, 0.4, 0.6, 2.0, 2.1, 2.2, 2.0, 1.5 };
            double[] results = { -1.0, -0.6760214152038321, -0.16850816525110807, 0.3105865482378153,
                0.543105127768453, 1.875189088370876, 2.053657939176787, 2.1626524203690374,
                2.0932515287064404, 1.6140895329959704 };

            for (int i = 0; i < 10; i++) {
                Assert.AreEqual(results[i],
                    filter.Filter(signals[i], i == 0? 120.0 : (1.0/(timestamps[i] - timestamps[i-1]))));
            }
        }

        [TestMethod]
        public void TestOneEuroFilterSmoothing()
        {
            OneEuroSmoothingFilter filter = new OneEuroSmoothingFilter();

            filter.SmoothedFrameArrived += Filter_SmoothedFrameArrived;

            filter.Beta = 1.0;
            filter.Fcmin = 1.0;

            // fake data
            double[] timestamps = { 0.0, 0.1, 0.2, 0.4, 0.6, 1.0, 1.2, 1.4, 1.5, 1.8 };
            float[] signals = { -1.0f, -0.5f, 0.0f, 0.4f, 0.6f, 2.0f, 2.1f, 2.2f, 2.0f, 1.5f };
            float[] results = { -1.0f, -0.6760214152038321f, -0.16850816525110807f, 0.3105865482378153f,
                0.543105127768453f, 1.875189088370876f, 2.053657939176787f,
                2.1626524203690374f, 2.0932515287064404f, 1.6140895329959704f };

            for (int i = 0; i < 10; i++) {
                // prepare data
                Joint joint = new Joint();
                joint.JointType = JointType.SpineBase;
                joint.TrackingState = TrackingState.Tracked;
                joint.Position.X = signals[i];
                joint.Position.Y = signals[i];
                joint.Position.Z = signals[i];

                JointData jointData = new JointData();
                jointData.RelTimestamp = timestamps[i] * 1000;
                jointData.AbsTimestamp = 0.0;
                jointData.Index = 0;
                jointData.Timestamps = new List<double>();
                Dictionary<JointType, Joint> joints = new Dictionary<JointType, Joint>();
                joints.Add(JointType.SpineBase, joint);
                jointData.Joints = joints;

                filter.Update(jointData);

                Assert.AreEqual(results[i], 
                    filteredJointData[i].Joints[JointType.SpineBase].Position.X, 0.000001);
            }                        
        }

        private void Filter_SmoothedFrameArrived(object sender, SmoothedFrameArrivedEventArgs e)
        {
            filteredJointData.Add(e.SmoothedJointData);
        }
    }
}
