﻿using Microsoft.Kinect;
using RowingMonitor.Model.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace RowingMonitor.Model.Pipeline
{
    public class VelocityCalculator
    {
        public delegate void CalculatedFrameArrivedEventHandler(Object sender,
            CalculatedFrameArrivedEventArgs e);
        public event CalculatedFrameArrivedEventHandler CalculatedFrameArrived;

        private JointData lastJointData;
        private JointData penultimateJointData;

        private ActionBlock<JointData> input;
        private BroadcastBlock<JointData> output;

        public BroadcastBlock<JointData> Output { get => output; set => output = value; }
        public ActionBlock<JointData> Input { get => input; set => input = value; }

        public VelocityCalculator()
        {
            Input = new ActionBlock<JointData>(jointData =>
            {
                Output.Post(CalculateVelocity(jointData));
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
        /// <param name="jointData"></param>
        public JointData CalculateVelocity(JointData jointData)
        {
            // check if jointData is NaN
            if (jointData.Joints[JointType.SpineBase].Position.X == float.NaN) {
                int x = 0;
            }

            // check if first value
            if (lastJointData.RelTimestamp == 0) {
                lastJointData = jointData;

                Dictionary<JointType, Joint> joints = new Dictionary<JointType, Joint>();
                foreach (KeyValuePair<JointType, Joint> joint in jointData.Joints) {
                    Joint newJoint = new Joint();
                    newJoint.Position.X = 0.0f;
                    newJoint.Position.Y = 0.0f;
                    newJoint.Position.Z = 0.0f;
                    joints.Add(joint.Key, newJoint);
                }
                JointData newJointData = KinectDataHandler.ReplaceJointsInJointData(
                    lastJointData,
                    DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond,
                    joints, DataStreamType.Velocity);

                return newJointData;
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
                    newJoints, DataStreamType.Velocity);

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
                    newJoints, DataStreamType.Velocity);

                // save to history
                penultimateJointData = lastJointData;
                lastJointData = jointData;

                return newJointData;
            }
        }
    }
}
