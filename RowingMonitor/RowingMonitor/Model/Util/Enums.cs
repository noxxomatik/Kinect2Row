using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RowingMonitor.Model.Util
{
    /// <summary>
    /// The type of body segment velocity which is calculated by KleshnevVelocityCalculator.
    /// </summary>
    public enum KleshnevVelocityType
    {
        /// <summary>
        /// Velocity of legs.
        /// </summary>
        Legs,
        /// <summary>
        /// Aggregated velocities of Legs, Trunk and ArmsRight.
        /// </summary>
        HandleRight,
        /// <summary>
        /// Aggregated velocities of Legs, Trunk and ArmsLeft.
        /// </summary>
        HandleLeft,
        /// <summary>
        /// Velocity of the trunk.
        /// </summary>
        Trunk,
        /// <summary>
        /// Velocity of the right arm.
        /// </summary>
        ArmsRight,
        /// <summary>
        /// Velocity of the left arm.
        /// </summary>
        ArmsLeft
    }

    /// <summary>
    /// The data stream source of the corresponding JointData.
    /// </summary>
    public enum DataStreamType
    {
        /// <summary>
        /// JointData directly from the sensor.
        /// </summary>
        RawPosition,
        /// <summary>
        /// Smoothed JointData.
        /// </summary>
        SmoothedPosition,
        /// <summary>
        /// Transformed JointData.
        /// </summary>
        ShiftedPosition,
        /// <summary>
        /// JointData which contains the calculated velocity of the joints.
        /// </summary>
        Velocity,
        /// <summary>
        /// Data from a segmentation module.
        /// </summary>
        SegmentHits,
        /// <summary>
        /// KleshnevVelocityData.
        /// </summary>
        KleshnevVelocity,
        /// <summary>
        /// The calculated peak of KleshnevVelocityData.
        /// </summary>
        KleshnevPeak,
        /// <summary>
        /// Undefined data.
        /// </summary>
        Other
    }

    /// <summary>
    /// The type of a segment hit.
    /// </summary>
    public enum HitType
    {
        /// <summary>
        /// The start of a segment.
        /// </summary>
        SegmentStart,
        /// <summary>
        /// An internal segment hit.
        /// </summary>
        SegmentInternal,
        /// <summary>
        /// The end of a segment.
        /// </summary>
        SegmentEnd,
        /// <summary>
        /// The end of an old segment and the start of a new segment.
        /// </summary>
        SegmentEndStart
    }

    /// <summary>
    /// The current state of a segment.
    /// </summary>
    public enum SegmentState
    {
        /// <summary>
        /// The segment has a start segment hit.
        /// </summary>
        SegmentStarted,
        /// <summary>
        /// A segment end hit was reached.
        /// </summary>
        SegmentEnded,
        /// <summary>
        /// No segment hits were found.
        /// </summary>
        NoSegment
    }
}
