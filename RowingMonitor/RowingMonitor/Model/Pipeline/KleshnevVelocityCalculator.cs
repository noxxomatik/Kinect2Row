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
    class KleshnevVelocityCalculator
    {
        public delegate void KleshnevCalculationFinishedEventHandler(Object sender,
            KleshnevEventArgs e);
        public event KleshnevCalculationFinishedEventHandler KleshnevCalculationFinished;

        private ActionBlock<JointData> input;
        private BroadcastBlock<KleshnevData> output;

        private List<KleshnevData> kleshnevData = new List<KleshnevData>();
        private List<double> timeLog = new List<double>();

        public BroadcastBlock<KleshnevData> Output { get => output; set => output = value; }
        public ActionBlock<JointData> Input { get => input; set => input = value; }

        public KleshnevVelocityCalculator()
        {
            Input = new ActionBlock<JointData>(jointData =>
            {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                Output.Post(CalculateKleshnevVelocities(jointData).Last());

                stopwatch.Stop();
                // log times
                timeLog.Add(stopwatch.Elapsed.TotalMilliseconds);
                if (timeLog.Count == 100) {
                    Logger.LogTimes(timeLog, this.ToString(),
                        "mean time to calculate Kleshnev velocities");
                    timeLog = new List<double>();
                }
            });
            Output = new BroadcastBlock<KleshnevData>(kleshnevData =>
            {
                return kleshnevData;
            });
        }

        public void Update(JointData jointData)
        {
            KleshnevCalculationFinished(this, new KleshnevEventArgs(
                CalculateKleshnevVelocities(jointData)));
        }

        public List<KleshnevData> CalculateKleshnevVelocities(JointData velocityJointData)
        {
            // copy JointData to KleshnevData
            KleshnevData newKleshnevData = new KleshnevData
            {
                RelTimestamp = velocityJointData.RelTimestamp,
                AbsTimestamp = velocityJointData.AbsTimestamp,
                Index = velocityJointData.Index
            };
            
            // leg velocity equals velocity of chip
            double legs = velocityJointData.Joints[JointType.SpineBase].Position.Z;

            // handle velocity equals hand velocity
            double handleR = velocityJointData.Joints[JointType.HandRight].Position.Z;
            double handleL = velocityJointData.Joints[JointType.HandLeft].Position.Z;

            // trunk velocity equals cshoulder velocity minus leg velocity
            double trunk = velocityJointData.Joints[JointType.SpineShoulder].Position.Z - legs;

            // arms velocity equals handle velocity minus cshoulder velocity
            double armsR = handleR - velocityJointData.Joints[JointType.SpineShoulder].Position.Z;
            double armsL = handleL - velocityJointData.Joints[JointType.SpineShoulder].Position.Z;

            // add results to KleshnevData
            Dictionary<KleshnevVelocityType, double> velocities = new Dictionary<KleshnevVelocityType, double>();
            velocities.Add(KleshnevVelocityType.Legs, legs);
            velocities.Add(KleshnevVelocityType.HandleRight, handleR);
            velocities.Add(KleshnevVelocityType.HandleLeft, handleL);
            velocities.Add(KleshnevVelocityType.Trunk, trunk);
            velocities.Add(KleshnevVelocityType.ArmsRight, armsR);
            velocities.Add(KleshnevVelocityType.ArmsLeft, armsL);
            newKleshnevData.Velocities = velocities;

            kleshnevData.Add(newKleshnevData);

            return kleshnevData;
        }
    }

    public struct KleshnevData
    {
        private double relTimestamp;
        private double absTimestamp;
        private Dictionary<KleshnevVelocityType, double> velocities;
        private long index;

        public double RelTimestamp { get => relTimestamp; set => relTimestamp = value; }
        public double AbsTimestamp { get => absTimestamp; set => absTimestamp = value; }
        public Dictionary<KleshnevVelocityType, double> Velocities
        {
            get => velocities; set => velocities = value;
        }
        public long Index { get => index; set => index = value; }
        /// <summary>
        /// Returns true if this element conatains no Kleshnev velocities.
        /// </summary>
        public bool IsEmpty
        {
            get {
                if (velocities == null) { return true; }
                else { return false; }
            }
        }
    }
}
