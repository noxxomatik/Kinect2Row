using Microsoft.Kinect;
using RowingMonitor.Model.EventArguments;
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
    /// Calculates the body segments and handle velocities defined by Kleshnev.
    /// 
    /// Kleshnev, Valery (2014): Rowing Biomechanics Newsletter. December 2014. Hg. v. Biorow (165).
    /// </summary>
    public class KleshnevVelocityCalculator
    {
        /// <summary>
        /// DEPRECATED
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public delegate void KleshnevCalculationFinishedEventHandler(Object sender,
            KleshnevEventArgs e);
        /// <summary>
        /// DEPRECATED
        /// </summary>
        public event KleshnevCalculationFinishedEventHandler KleshnevCalculationFinished;

        private ActionBlock<JointData> input;
        private BroadcastBlock<KleshnevData> output;

        private List<KleshnevData> kleshnevData = new List<KleshnevData>();
        private List<double> timeLog = new List<double>();

        /// <summary>
        /// Creates a new instance of KleshnevVelocityCalculator.
        /// </summary>
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

        /// <summary>
        /// DEPRECATED
        /// </summary>
        /// <param name="jointData"></param>
        public void Update(JointData jointData)
        {
            KleshnevCalculationFinished(this, new KleshnevEventArgs(
                CalculateKleshnevVelocities(jointData)));
        }

        /// <summary>
        /// Calculate the body segment and handle velocities from the velocities of the joints.
        /// </summary>
        /// <param name="velocityJointData">JointData containing velocity values.</param>
        /// <returns>KleshnevData containing the body segment velocities.</returns>
        public List<KleshnevData> CalculateKleshnevVelocities(JointData velocityJointData)
        {
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
            Dictionary<KleshnevVelocityType, double> velocities = new Dictionary<KleshnevVelocityType, double>
            {
                { KleshnevVelocityType.Legs, legs },
                { KleshnevVelocityType.HandleRight, handleR },
                { KleshnevVelocityType.HandleLeft, handleL },
                { KleshnevVelocityType.Trunk, trunk },
                { KleshnevVelocityType.ArmsRight, armsR },
                { KleshnevVelocityType.ArmsLeft, armsL }
            };

            // copy JointData and velocities to KleshnevData
            KleshnevData newKleshnevData = new KleshnevData(velocityJointData.Index, velocityJointData.RelTimestamp,
                velocityJointData.AbsTimestamp, velocities);

            kleshnevData.Add(newKleshnevData);

            return kleshnevData;
        }

        /// <summary>
        /// The BroadcastBlock that sends the KleshnevData to linked target blocks.
        /// </summary>
        public BroadcastBlock<KleshnevData> Output { get => output; set => output = value; }
        /// <summary>
        /// The ActionBlock that recieves the JointData with velocity values.
        /// </summary>
        public ActionBlock<JointData> Input { get => input; set => input = value; }
    }

    /// <summary>
    /// KleshnevData holds the velocities of the body segments 
    /// in the sagittal plane defined by Kleshnev.
    /// </summary>
    public struct KleshnevData
    {
        /// <summary>
        /// Time in milliseconds since Kinect sensor started.
        /// </summary>
        public readonly double RelTimestamp;
        /// <summary>
        /// Time in milliseconds since first frame.
        /// </summary>
        public readonly double AbsTimestamp;
        /// <summary>
        /// The body segment velocities.
        /// </summary>
        public readonly Dictionary<KleshnevVelocityType, double> Velocities;
        /// <summary>
        /// Incrementing number of frames.
        /// </summary>
        public readonly long Index;

        /// <summary>
        /// Creates a new collection of the body segment velocities and
        /// its associated meta data.
        /// </summary>
        /// <param name="index">Incrementing number of frames.</param>
        /// <param name="relTimestamp">Time in milliseconds since Kinect sensor started.</param>
        /// <param name="absTimestamp">Time in milliseconds since first frame.</param>
        /// <param name="velocities">The body segment velocities.</param>
        public KleshnevData(long index, double relTimestamp, double absTimestamp, 
            Dictionary<KleshnevVelocityType, double> velocities)
        {
            Index = index;
            RelTimestamp = relTimestamp;
            AbsTimestamp = absTimestamp;
            Velocities = velocities;
        }       

        /// <summary>
        /// Returns true if this element conatains no velocities.
        /// </summary>
        public bool IsEmpty
        {
            get {
                if (Velocities == null) { return true; }
                else { return false; }
            }
        }
    }
}
