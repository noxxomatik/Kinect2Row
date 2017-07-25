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
    /// <summary>
    /// Calculates all needed rowing analysis values and makes them accessible to the pipeline.
    /// </summary>
    public partial class RowingMetaDataCalculator
    {
        // dataflow connections
        private ActionBlock<JointData> input;
        private ActionBlock<KleshnevData> inputKleshnevData;
        private ActionBlock<List<SegmentHit>> inputSegmentHits;
        private ActionBlock<JointData> inputVelocityJointData;
        private BroadcastBlock<RowingMetaData> output;

        // buffers
        private List<JointData> jointDataBuffer = new List<JointData>();
        private List<JointData> velocityJointDataBuffer = new List<JointData>();
        private List<JointData> lastSegmentJointDataBuffer = new List<JointData>();
        private List<SegmentHit> segmentHitsBuffer = new List<SegmentHit>();
        private List<KleshnevData> kleshnevDataBuffer = new List<KleshnevData>();
        private long[] segmentBoundsBuffer;

        // flags
        private bool newSegment = false;
        private bool jointDataRecieved = false;
        private bool velJointDataRecieved = false;
        private bool kleshnevDataRecieved = false;
        private bool segmentHitsRecieved = false;

        /// <summary>
        /// Creates a new rowing meta data calculator instance.
        /// </summary>
        public RowingMetaDataCalculator()
        {
            Input = new ActionBlock<JointData>(jointData =>
            {
                jointDataBuffer.Add(jointData);
                jointDataRecieved = true;
            });

            InputVelocityJointData = new ActionBlock<JointData>(jointData =>
            {
                velocityJointDataBuffer.Add(jointData);
                velJointDataRecieved = true;
            });

            InputKleshnevData = new ActionBlock<KleshnevData>(kleshnevData =>
            {
                kleshnevDataBuffer.Add(kleshnevData);
                kleshnevDataRecieved = true;
            });

            // segment detector is the last element in the data stream pipeline,
            // so do all the calculations here
            InputSegmentHits = new ActionBlock<List<SegmentHit>>(segmentHits =>
            {
                // if new hits occured
                if (segmentHits.Count > segmentHitsBuffer.Count) {
                    segmentHitsBuffer = new List<SegmentHit>(segmentHits);

                    // check if a new complete segment is passed
                    long[] tmpSegmentBounds = SegmentHitHandler.GetLastSegmentStartEnd(segmentHitsBuffer);
                    if (tmpSegmentBounds != null && 
                    (segmentBoundsBuffer == null || !tmpSegmentBounds.Equals(segmentBoundsBuffer))) {
                        newSegment = true;
                        segmentBoundsBuffer = tmpSegmentBounds;
                    }
                }

                // do segment based calculations if a complete segment was passed
                if (newSegment) {
                    Logger.Log(this.ToString(), "Segment bounds: " + segmentBoundsBuffer[0] + " - " + segmentBoundsBuffer[1]);

                    UpdateRowingMetaData(true);

                    newSegment = false;
                }
                // do realtime based calculations
                else {
                    UpdateRowingMetaData(false);
                }
                segmentHitsRecieved = true;                
            });

            Output = new BroadcastBlock<RowingMetaData>(rowingMetaData =>
            {
                return rowingMetaData;
            });
        }

        private void UpdateRowingMetaData(bool segmentEnded)
        {
            if (kleshnevDataBuffer.Count > 0) {
                RowingMetaData metaData = new RowingMetaData();
                // use index and timestamp of kleshnev data since it is the last data in the pipline and jointData and velocity should have these indices also
                KleshnevData lastData = kleshnevDataBuffer.Last();
                metaData.Index = lastData.Index;
                metaData.AbsTimestamp = lastData.AbsTimestamp;
                metaData.RelTimestamp = lastData.RelTimestamp;

                metaData = CalculateRealtimeMetaData(metaData);

                if (segmentEnded) {
                    metaData = CalculateSegmentMetaData(metaData);
                }

                Output.Post(metaData);
            }            
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
        public ActionBlock<KleshnevData> InputKleshnevData
        {
            get => inputKleshnevData;
            set {
                inputKleshnevData = value;
            }
        }
        public ActionBlock<List<SegmentHit>> InputSegmentHits { get => inputSegmentHits; set => inputSegmentHits = value; }
        public ActionBlock<JointData> InputVelocityJointData
        {
            get => inputVelocityJointData;
            set {
                inputVelocityJointData = value;
            }
        }
        public BroadcastBlock<RowingMetaData> Output { get => output; set => output = value; }
    }
}
