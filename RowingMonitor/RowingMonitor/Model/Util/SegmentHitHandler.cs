using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RowingMonitor.Model.Util
{
    public class SegmentHitHandler
    {
        private SegmentHitHandler() { }

        public static bool CheckIfNewSegmentStarted(List<SegmentHit> hits)
        {
            if (hits.Count > 0 &&
                (hits.Last().HitType == HitType.SegmentEndStart ||
                hits.Last().HitType == HitType.SegmentStart)) {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Return the bounding indices values of the last complete segment.
        /// </summary>
        /// <param name="hits">List of detected hits.</param>
        /// <returns>Start an d end index of the lastdetected segment.</returns>
        public static long[] GetLastSegmentStartEnd(List<SegmentHit> hits)
        {
            long[] segmentBounds = { -1, -1 };

            // segments can contain a start hit, an end hit, a end start hit and an internal hit
            // get the index for the last complete segment
            if (hits.Count >= 2) {
                for (int i = hits.Count - 1; i >= 0; i--) {
                    // find last end hit
                    if (segmentBounds[1] == -1 && (hits[i].HitType == HitType.SegmentEnd
                        || hits[i].HitType == HitType.SegmentEndStart)) {
                        segmentBounds[1] = hits[i].Index;
                        continue;
                    }
                    // find last start hit after end hit was found
                    if (segmentBounds[1] != -1 && (hits[i].HitType == HitType.SegmentStart
                        || hits[i].HitType == HitType.SegmentEndStart)) {
                        segmentBounds[0] = hits[i].Index;
                        return segmentBounds;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Returns the index of the last detected internal segment hit.
        /// </summary>
        /// <param name="hits">List of detected hits.</param>
        /// <returns>Index of last detected internal segment hit.</returns>
        public static long GetLastSegmentInternal(List<SegmentHit> hits)
        {
            if (hits.Count > 2) {
                for (int i = hits.Count - 1; i >= 0; i--) {
                    if (hits[i].HitType == HitType.SegmentInternal) {
                        return hits[i].Index;
                    }
                }
            }
            return -1;
        }

        /// <summary>
        /// Checks if the segment is valid by comparing its duration with the minimum 
        /// segment time from the settings.
        /// </summary>
        /// <param name="hits"></param>
        /// <param name="bounds"></param>
        /// <returns>True if the segment duration is greater then the 
        /// minimum segment duration in the settings</returns>
        public static bool IsSegmentValid(List<SegmentHit> hits, long[] bounds)
        {
            if (hits.Count >= 2) {
                double startTime = 0;
                double endTime = 0;
                for (int i = hits.Count - 1; i >= 0; i--) {
                    if (hits[i].Index == bounds[1]) {
                        endTime = hits[i].AbsTimestamp;
                    }
                    else if (hits[i].Index == bounds[0]) {
                        startTime = hits[i].AbsTimestamp;
                        break;
                    }
                }
                return (endTime - startTime) / 1000 > Properties.Settings.Default.MinSegmentTime;
            }
            return false;
        }
    }

    public struct SegmentHit
    {
        private long index;
        private long detectionIndex;
        private double absTimestamp;
        private double detectionAbsTimestamp;
        private HitType hitType;

        /// <summary>
        /// Index of the joint data that this hit belongs to.
        /// </summary>
        public long Index { get => index; set => index = value; }
        /// <summary>
        /// Index of the joint data where this hit was detected.
        /// </summary>
        public long DetectionIndex { get => detectionIndex; set => detectionIndex = value; }
        /// <summary>
        /// Absolute timestamp of the joint data that this hit belongs to.
        /// </summary>
        public double AbsTimestamp { get => absTimestamp; set => absTimestamp = value; }
        /// <summary>
        /// Absolute timestamp of the joint data where this hit was detected.
        /// </summary>
        public double DetectionAbsTimestamp { get => detectionAbsTimestamp; set => detectionAbsTimestamp = value; }
        /// <summary>
        /// Type of this hit in the context of a segment.
        /// </summary>
        public HitType HitType { get => hitType; set => hitType = value; }
    }
}
