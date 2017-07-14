using Microsoft.Kinect;
using RowingMonitor.Model.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace RowingMonitor.Model.Pipeline
{
    public class RowingMetaData
    {
        // dataflow connections
        private ActionBlock<JointData> input;
        private ActionBlock<KleshnevData> inputKleshnevData;
        private ActionBlock<List<SegmentHit>> inputPlotHits;

        // buffers
        private List<JointData> jointDataBuffer = new List<JointData>();
        private List<JointData> lastSegmentJointDataBuffer = new List<JointData>();
        private List<SegmentHit> segmentHitsBuffer = new List<SegmentHit>();
        private List<KleshnevData> kleshnevDataBuffer = new List<KleshnevData>();
        private long[] segmentBoundsBuffer;

        // flags
        private bool newSegment = false;

        public RowingMetaData()
        {
            Input = new ActionBlock<JointData>(jointData =>
            {
                jointDataBuffer.Add(jointData);
            });

            InputKleshnevData = new ActionBlock<KleshnevData>(kleshnevData =>
            {
                kleshnevDataBuffer.Add(kleshnevData);
            });

            InputPlotHits = new ActionBlock<List<SegmentHit>>(segmentHits =>
            {
                // if new hits occured
                if (segmentHits.Count > segmentHitsBuffer.Count) {
                    segmentHitsBuffer = new List<SegmentHit>(segmentHits);

                    // check if a new complete segment is passed
                    long[] tmpSegmentBounds = SegmentHitHandler.GetLastSegmentStartEnd(segmentHitsBuffer);
                    if (!tmpSegmentBounds.Equals(segmentBoundsBuffer)) {
                        newSegment = true;
                        segmentBoundsBuffer = tmpSegmentBounds;
                    }
                }   
                
                // do segment based calculations if a complete segment was passed
                if (newSegment) {
                    Logger.Log(this.ToString(), "Segment bounds: " + segmentBoundsBuffer[0] + " - " + segmentBoundsBuffer[1]);
                    newSegment = false;
                }
            });
        }

        private List<JointData> FilterLastSegmentJointData(List<JointData> buffer, long[] bounds)
        {
            List<JointData> tmpLastSegmentJointDataBuffer = new List<JointData>();
            // do not search through all buffer objects, search last in first out
            for (int i = buffer.Count - 1; i >= 0; i--) {
                if (buffer[i].Index >= bounds[0] && buffer[i].Index <= bounds[1]) {
                    tmpLastSegmentJointDataBuffer.Add(buffer[i]);
                }
            }
            tmpLastSegmentJointDataBuffer.Reverse();
            return tmpLastSegmentJointDataBuffer;
        }

        private double CalculateHandleTravelDistance(List<JointData> buffer)
        {
            double minZ = Double.PositiveInfinity;
            double maxZ = Double.NegativeInfinity;
            foreach (JointData jointData in buffer) {
                // calculate handle position as mean of LeftHand and RightHand
                double meanZ = (jointData.Joints[JointType.HandLeft].Position.Z 
                    + jointData.Joints[JointType.HandRight].Position.Z) / 2;
                minZ = meanZ < minZ ? meanZ : minZ;
                maxZ = meanZ > maxZ ? meanZ : maxZ;
            }
            return maxZ - minZ;
        }

        private double CalculateSeatTravelDistance(List<JointData> buffer)
        {
            double minZ = Double.PositiveInfinity;
            double maxZ = Double.NegativeInfinity;
            foreach (JointData jointData in buffer) {
                double z = jointData.Joints[JointType.SpineBase].Position.Z;
                minZ = z < minZ ? z : minZ;
                maxZ = z > maxZ ? z : maxZ;
            }
            return maxZ - minZ;
        }

        private double CalculateSegmentTime(List<JointData> buffer)
        {
            return buffer.Last().AbsTimestamp - buffer[0].AbsTimestamp;
        }

        public ActionBlock<JointData> Input { get => input; set => input = value; }
        public ActionBlock<KleshnevData> InputKleshnevData { get => inputKleshnevData; set => inputKleshnevData = value; }
        public ActionBlock<List<SegmentHit>> InputPlotHits { get => inputPlotHits; set => inputPlotHits = value; }
    }
}
