using Microsoft.Kinect;
using RowingMonitor.Model.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RowingMonitor.Model.Pipeline
{
    class ZVCSegmentDetector : SegmentDetector
    {
        private JointData lastJointData;

        private bool lastSlopeRising;

        private bool endStartHitIsRisingVelocity;

        private int minHitGap;

        // Logger
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(
            System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

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
        }

        public override void Update(JointData jointData, 
            JointType jointType, String axis)
        {
            if (lastJointData.RelTimestamp != 0) {
                float value = GetJointDataValue(jointData, jointType, axis);
                float lastValue = GetJointDataValue(lastJointData, jointType, axis);
                bool slopeRising = value - lastValue > 0 ? true : false;

                // zero velocity crossing
                // if value is 0 then crossing is at this exact index
                if (value == 0)
                {
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
        }

        // select hits
        private void AddHits(JointData jointData, float value, float lastValue, bool slopeRising)
        {
            // check if it is the first hit
            if (hits.Count == 0)
            {
                SegmentHit hit = new SegmentHit {
                    HitType = HitType.SegmentStart,
                    Index = jointData.Index,
                    DetectionIndex = jointData.Index,
                    AbsTimestamp = jointData.AbsTimestamp,
                    DetectionAbsTimestamp = jointData.AbsTimestamp                    
                };
                hits.Add(hit);
                OnSegmentDetected(new SegmentDetectedEventArgs(hits));
            }
            // check if hit is beyond the minimum hit gap and has another slope
            else if (jointData.Index - hits.Last().Index >= minHitGap)
            {
                // check the slope
                if (slopeRising != lastSlopeRising)
                {
                    // determine the hit type
                    if (slopeRising)
                    {
                        SegmentHit hit = new SegmentHit
                        {
                            HitType = endStartHitIsRisingVelocity ? HitType.SegmentEndStart : HitType.SegmentInternal,
                            Index = jointData.Index,
                            DetectionIndex = jointData.Index,
                            AbsTimestamp = jointData.AbsTimestamp,
                            DetectionAbsTimestamp = jointData.AbsTimestamp
                        };
                        hits.Add(hit);
                        OnSegmentDetected(new SegmentDetectedEventArgs(hits));
                    }
                    else
                    {
                        SegmentHit hit = new SegmentHit
                        {
                            HitType = endStartHitIsRisingVelocity ? HitType.SegmentInternal : HitType.SegmentEndStart,
                            Index = jointData.Index,
                            DetectionIndex = jointData.Index,
                            AbsTimestamp = jointData.AbsTimestamp,
                            DetectionAbsTimestamp = jointData.AbsTimestamp
                        };
                        hits.Add(hit);
                        OnSegmentDetected(new SegmentDetectedEventArgs(hits));
                    }                    
                }
                else
                {
                    log.Info("Hit dropped because it has the same slope as the last hit.");
                }
            }
            else
            {
                log.Info("Hit dropped because it was inside the minimum hit gap.");
            }
        }

        protected override void OnSegmentDetected(SegmentDetectedEventArgs e)
        {
            base.OnSegmentDetected(e);
        }
    }
}
