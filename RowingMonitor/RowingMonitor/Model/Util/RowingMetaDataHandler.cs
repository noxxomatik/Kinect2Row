using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RowingMonitor.Model.Util
{
    public class RowingMetaDataHandler
    {
        private RowingMetaDataHandler() { }
    }

    /// <summary>
    /// Calculated meta data that describes the current rowing session.
    /// </summary>
    public struct RowingMetaData
    {
        private long index;
        private double absTimestamp;
        private double relTimestamp;
        private SegmentState segmentState;
        private double sessionTime;
        private int strokeCount;
        private double strokeRate;
        private double catchFactor;
        private double rowingStyleFactor;
        private double strokeLength;
        private double meanStrokeLength;
        private double strokeTime;
        private double meanStrokeTime;
        private double seatTravelDistance;
        private double meanSeatTravelDistance;
        private double maxHandleVelocity;
        private double meanMaxHandleVelocity;
        private double maxLegsVelocity;
        private double meanMaxLegsVelocity;
        private double maxArmsVelocity;
        private double meanMaxArmsVelocity;
        private double maxTrunkVelocity;
        private double meanMaxTrunkVelocity;
        private double strokesPerMinute;
        private double trunkAngle;
        private double maxCatchTrunkAngle;
        private double maxFinishTrunkAngle;

        /// <summary>
        /// Incrementing number of frames.
        /// </summary>
        public long Index { get => index; set => index = value; }
        /// <summary>
        /// Time in milliseconds since first frame.
        /// </summary>
        public double AbsTimestamp { get => absTimestamp; set => absTimestamp = value; }
        /// <summary>
        /// Time in milliseconds since Kinect sensor started.
        /// </summary>
        public double RelTimestamp { get => relTimestamp; set => relTimestamp = value; }
        /// <summary>
        /// State of the segment that this meta data belongs to.
        /// </summary>
        public SegmentState SegmentState { get => segmentState; set => segmentState = value; }
        /// <summary>
        /// Full time of this monitoring session in milliseconds.
        /// </summary>
        public double SessionTime { get => sessionTime; set => sessionTime = value; }
        /// <summary>
        /// Absolute count of strokes in this session.
        /// </summary>
        public int StrokeCount { get => strokeCount; set => strokeCount = value; }
        /// <summary>
        /// ???
        /// </summary>
        public double StrokeRate { get => strokeRate; set => strokeRate = value; }
        /// <summary>
        /// ???
        /// </summary>
        public double CatchFactor { get => catchFactor; set => catchFactor = value; }
        /// <summary>
        /// ???
        /// </summary>
        public double RowingStyleFactor { get => rowingStyleFactor; set => rowingStyleFactor = value; }
        /// <summary>
        /// Length in meter of the stroke in this segment.
        /// </summary>
        public double StrokeLength { get => strokeLength; set => strokeLength = value; }
        /// <summary>
        /// Mean length in meter of all strokes in this session.
        /// </summary>
        public double MeanStrokeLength { get => meanStrokeLength; set => meanStrokeLength = value; }
        /// <summary>
        /// Distance in meter that the seat travelled in this segment.
        /// </summary>
        public double SeatTravelDistance { get => seatTravelDistance; set => seatTravelDistance = value; }
        /// <summary>
        /// Mean distance in meter that the seat travelled in all segments of this session.
        /// </summary>
        public double MeanSeatTravelDistance { get => meanSeatTravelDistance; set => meanSeatTravelDistance = value; }
        /// <summary>
        /// Maximum handle velocity in m/s of this segment.
        /// </summary>
        public double MaxHandleVelocity { get => maxHandleVelocity; set => maxHandleVelocity = value; }
        /// <summary>
        /// Mean maximum handle velocity in m/s of all segments in this session.
        /// </summary>
        public double MeanMaxHandleVelocity { get => meanMaxHandleVelocity; set => meanMaxHandleVelocity = value; }
        /// <summary>
        /// Maximum legs velocity in m/s of this segment.
        /// </summary>
        public double MaxLegsVelocity { get => maxLegsVelocity; set => maxLegsVelocity = value; }
        /// <summary>
        /// Mean maximum legs velocity in m/s of all segments in this session.
        /// </summary>
        public double MeanMaxLegsVelocity { get => meanMaxLegsVelocity; set => meanMaxLegsVelocity = value; }
        /// <summary>
        /// Maximum arms velocity in m/s of this segment.
        /// </summary>
        public double MaxArmsVelocity { get => maxArmsVelocity; set => maxArmsVelocity = value; }
        /// <summary>
        /// Mean maximum arms velocity in m/s of all segments in this session.
        /// </summary>
        public double MeanMaxArmsVelocity { get => meanMaxArmsVelocity; set => meanMaxArmsVelocity = value; }
        /// <summary>
        /// Maximum trunk velocity in m/s of this segment.
        /// </summary>
        public double MaxTrunkVelocity { get => maxTrunkVelocity; set => maxTrunkVelocity = value; }
        /// <summary>
        /// Mean maximum trunk velocity in m/s of all segments in this session.
        /// </summary>
        public double MeanMaxTrunkVelocity { get => meanMaxTrunkVelocity; set => meanMaxTrunkVelocity = value; }
        /// <summary>
        /// Achived strokes per minute in this session.
        /// </summary>
        public double StrokesPerMinute { get => strokesPerMinute; set => strokesPerMinute = value; }
        /// <summary>
        /// Current trunk angle in radians.
        /// </summary>
        public double TrunkAngle { get => trunkAngle; set => trunkAngle = value; }
        /// <summary>
        /// Current maximum trunk angle in radians of the catch phase.
        /// </summary>
        public double MaxCatchTrunkAngle { get => maxCatchTrunkAngle; set => maxCatchTrunkAngle = value; }
        /// <summary>
        /// Current maximum trunk angle in radians of the finish phase.
        /// </summary>
        public double MaxFinishTrunkAngle { get => maxFinishTrunkAngle; set => maxFinishTrunkAngle = value; }
        /// <summary>
        /// Time in milliseconds of the stroke in this segment.
        /// </summary>
        public double StrokeTime { get => strokeTime; set => strokeTime = value; }
        /// <summary>
        /// Mean time in milliseconds of a stroke in all segments of this session.
        /// </summary>
        public double MeanStrokeTime { get => meanStrokeTime; set => meanStrokeTime = value; }
    }
}
