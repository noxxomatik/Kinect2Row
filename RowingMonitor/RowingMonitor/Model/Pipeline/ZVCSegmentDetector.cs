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
    class ZVCSegmentDetector : SegmentDetector
    {
        private JointData lastJointData;

        private bool lastSlopeRising;

        private bool endStartHitIsRisingVelocity;

        private int minHitGap;

        private List<double> timeLog = new List<double>();

        /// <summary>
        /// Creates a new zero velocity segment detector.
        /// </summary>
        /// <param name="minimumHitGap">Minimum count ouf indices between two hits.</param>
        /// <param name="startSegmentWithRisingVelocity">Define if the start/end point 
        /// of a segment has a rising or falling slope</param>
        public ZVCSegmentDetector(int minimumHitGap, bool startSegmentWithRisingVelocity = true)
        {
            minHitGap = minimumHitGap;
            endStartHitIsRisingVelocity = startSegmentWithRisingVelocity ? true : false;

            Input = new ActionBlock<JointData>(jointData =>
            {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                Output.Post(Detect(jointData, DetectionJointType,
                    DetectionAxis));

                stopwatch.Stop();
                // log times
                timeLog.Add(stopwatch.Elapsed.TotalMilliseconds);
                if (timeLog.Count == 100) {
                    Logger.LogTimes(timeLog, this.ToString(),
                        "mean time to detect hits");
                    timeLog = new List<double>();
                }
            });

            Output = new BroadcastBlock<List<SegmentHit>>(hits =>
            {
                return hits;
            });
        }

        public override void Update(JointData jointData,
            JointType jointType, String axis)
        {
            Detect(jointData, jointType, axis);
        }

        // select hits
        private void AddHits(JointData jointData, float value, float lastValue, bool slopeRising)
        {
            // check if it is the first hit
            if (hits.Count == 0) {
                SegmentHit hit = new SegmentHit(jointData.Index, jointData.Index, 
                    jointData.AbsTimestamp, jointData.AbsTimestamp, HitType.SegmentStart);
                hits.Add(hit);
                OnSegmentDetected(new SegmentDetectedEventArgs(hits));
            }
            // check if hit is beyond the minimum hit gap and has another slope
            else if (jointData.Index - hits.Last().Index >= minHitGap) {
                // check the slope
                if (slopeRising != lastSlopeRising) {
                    // determine the hit type
                    if (slopeRising) {
                        SegmentHit hit = new SegmentHit(jointData.Index, jointData.Index, jointData.AbsTimestamp, 
                            jointData.AbsTimestamp, 
                            endStartHitIsRisingVelocity ? HitType.SegmentEndStart : HitType.SegmentInternal);
                        hits.Add(hit);
                        OnSegmentDetected(new SegmentDetectedEventArgs(hits));
                    }
                    else {
                        SegmentHit hit = new SegmentHit(jointData.Index, jointData.Index, jointData.AbsTimestamp, 
                            jointData.AbsTimestamp, 
                            endStartHitIsRisingVelocity ? HitType.SegmentInternal : HitType.SegmentEndStart);
                        hits.Add(hit);
                        OnSegmentDetected(new SegmentDetectedEventArgs(hits));
                    }
                }
                else {
                    Logger.Log(this.ToString(), "Hit dropped because it has the same slope as the last hit.");
                }
            }
            else {
                Logger.Log(this.ToString() ,"Hit dropped because it was inside the minimum hit gap.");
            }
        }

        protected override void OnSegmentDetected(SegmentDetectedEventArgs e)
        {
            base.OnSegmentDetected(e);
        }

        public override List<SegmentHit> Detect(JointData jointData, JointType jointType, string axis)
        {
            if (lastJointData.RelTimestamp != 0) {
                float value = JointDataHandler.GetJointDataValue(jointData, jointType, axis);
                float lastValue = JointDataHandler.GetJointDataValue(lastJointData, jointType, axis);

                // set slope rising true if the current value is greater then the last
                bool slopeRising = value - lastValue > 0 ? true : false;

                // zero velocity crossing
                // if value is 0 then crossing is at this exact index
                if (value == 0) {
                    AddHits(jointData, value, lastValue, slopeRising);
                    lastSlopeRising = slopeRising;
                }
                else {
                    // if sign is negativ then crossing was between the two frames
                    if (value * lastValue < 0) {
                        AddHits(jointData, value, lastValue, slopeRising);
                        lastSlopeRising = slopeRising;
                    }
                }
            }
            lastJointData = jointData;
            return hits;
        }
    }
}
