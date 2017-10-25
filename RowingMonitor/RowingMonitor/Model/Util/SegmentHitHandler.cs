using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RowingMonitor.Model.Util
{
    /// <summary>
    /// A static class to deal with detected segment hits and check 
    /// if the detected segments are valid or incomplete.
    /// 
    /// Also supplies methods to extract start and end points of segments.
    /// </summary>
    public static class SegmentHitHandler
    {
        /// <summary>
        /// Checks if a new segment started and has not yet ended.
        /// </summary>
        /// <param name="hits">List of detected hits.</param>
        /// <returns></returns>
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
        /// Filters the joint data of a completed segment.
        /// </summary>
        /// <param name="buffer">The list of joint data that shall be filtered.</param>
        /// <param name="bounds">Start and end index of the segment.</param>
        /// <returns>Returns a list of all joint data that ist part of the segment.</returns>
        public static List<JointData> FilterSegmentJointData(List<JointData> buffer, long[] bounds)
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
        /// Return the bounding indices values of the last complete segment.
        /// </summary>
        /// <param name="hits">List of detected hits.</param>
        /// <returns>Start and end index of the lastdetected segment.</returns>
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
        /// Checks if the segment is valid by comparing its duration with the minimum 
        /// segment time from the settings.
        /// </summary>
        /// <param name="hits"></param>
        /// <param name="bounds"></param>
        /// <returns>True if the segment duration is greater then the 
        /// minimum segment duration in the settings</returns>
        public static bool IsSegmentValid(List<SegmentHit> hits, long[] bounds)
        {
            if (hits != null && bounds != null && hits.Count >= 2) {
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

    /// <summary>
    /// Holds information about a detected segment hit.
    /// </summary>
    public struct SegmentHit
    {
        /// <summary>
        /// Creates new segment hit data.
        /// </summary>
        /// <param name="index">Index of the joint data that this hit belongs to.</param>
        /// <param name="detectionIndex">Index of the joint data where this hit was detected.</param>
        /// <param name="absTimestamp">Absolute timestamp of the joint data that this hit belongs to.</param>
        /// <param name="detectionAbsTimestamp">Absolute timestamp of the joint data where this hit was detected.</param>
        /// <param name="hitType">Type of this hit in the context of a segment.</param>
        public SegmentHit(long index, long detectionIndex, double absTimestamp, 
            double detectionAbsTimestamp, HitType hitType)
        {
            Index = index;
            DetectionIndex = detectionIndex;
            AbsTimestamp = absTimestamp;
            DetectionAbsTimestamp = detectionAbsTimestamp;
            HitType = hitType;
        }

        /// <summary>
        /// Index of the joint data that this hit belongs to.
        /// </summary>
        public readonly long Index;
        /// <summary>
        /// Index of the joint data where this hit was detected.
        /// </summary>
        public readonly long DetectionIndex;
        /// <summary>
        /// Absolute timestamp of the joint data that this hit belongs to.
        /// </summary>
        public readonly double AbsTimestamp;
        /// <summary>
        /// Absolute timestamp of the joint data where this hit was detected.
        /// </summary>
        public readonly double DetectionAbsTimestamp;
        /// <summary>
        /// Type of this hit in the context of a segment.
        /// </summary>
        public readonly HitType HitType;
    }
}
