using Microsoft.Kinect;
using RowingMonitor.Model.Util;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace RowingMonitor.Model.Pipeline
{
    /// <summary>
    /// Calculates all needed rowing analysis values and makes them accessible to the pipeline.
    /// </summary>
    public partial class RowingMetadataCalculator
    {
        // dataflow connections
        private ActionBlock<JointData> input;
        private ActionBlock<KleshnevData> inputKleshnevData;
        private ActionBlock<List<SegmentHit>> inputSegmentHits;
        private ActionBlock<JointData> inputVelocityJointData;
        private BroadcastBlock<RowingMetadata> output;

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
        public RowingMetadataCalculator()
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
                    if (tmpSegmentBounds != null && SegmentHitHandler.IsSegmentValid(segmentHits, tmpSegmentBounds) 
                    && (segmentBoundsBuffer == null || tmpSegmentBounds[1] != segmentBoundsBuffer[1])) {
                        newSegment = true;
                        segmentBoundsBuffer = tmpSegmentBounds;
                    }
                }

                // do segment based calculations if a complete segment was passed
                if (newSegment) {
                    Logger.Log(this.ToString(), "Segment bounds: " + segmentBoundsBuffer[0] + " - " + segmentBoundsBuffer[1]);

                    UpdateRowingMetaData(true);

                    // log template data for DTW
                    LogTemplateData(JointType.HandRight, "Z");

                    newSegment = false;
                }
                // do realtime based calculations
                else {
                    UpdateRowingMetaData(false);
                }
                segmentHitsRecieved = true;                
            });

            Output = new BroadcastBlock<RowingMetadata>(rowingMetaData =>
            {
                return rowingMetaData;
            });
        }

        /// <summary>
        /// Log the normalized template of the segment and its
        /// minimum and maximum values for realtime normalization.        
        /// <param name="jointType"></param>
        /// <param name="axis"/><param/>
        /// </summary>
        private void LogTemplateData(JointType jointType, string axis)
        {
            // the values
            List<float> values = new List<float>();
            // get the segment joint data
            List<JointData> segmentJointData = 
                SegmentHitHandler.FilterSegmentJointData(jointDataBuffer, segmentBoundsBuffer);
            // filter the joint data for the joint type and axis and
            // remember the values, minimum and maximum
            foreach(JointData jointData in segmentJointData) {
                float value = JointDataHandler.GetJointDataValue(jointData, jointType, axis);                
                values.Add(value);
            }
            // to normalize the minimum an maximum is needed
            float min = values.Min();
            float max = values.Max();
            // normalize the values
            List<float> normalizedValues = values.Select(x => x = (x - min) / (max - min)).ToList();
            // output the result in the log
            Logger.Log(this.ToString(), "Normalized template of last segment: " 
                + String.Join(";", normalizedValues.Select(x => x.ToString(CultureInfo.CurrentUICulture.NumberFormat))));
            Logger.Log(this.ToString(), "Segment minimum: " + min.ToString(CultureInfo.CurrentUICulture.NumberFormat));
            Logger.Log(this.ToString(), "Segment maximum: " + max.ToString(CultureInfo.CurrentUICulture.NumberFormat));
        }

        private void UpdateRowingMetaData(bool segmentEnded)
        {
            if (kleshnevDataBuffer.Count > 0) {
                RowingMetadata metaData = new RowingMetadata();

                // use index and timestamp of kleshnev data since it is the 
                // last data in the pipline and jointData and velocity 
                // should have these indices also

                KleshnevData lastData = kleshnevDataBuffer.Last();
                metaData.Index = lastData.Index;
                metaData.AbsTimestamp = lastData.AbsTimestamp;
                metaData.RelTimestamp = lastData.RelTimestamp;

                metaData = CalculateRealtimeMetadata(metaData);

                if (segmentEnded) {
                    metaData = CalculateSegmentMetadata(metaData);
                }

                Output.Post(metaData);
            }            
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
        public BroadcastBlock<RowingMetadata> Output { get => output; set => output = value; }
    }
}
