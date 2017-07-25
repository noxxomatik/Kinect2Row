using Microsoft.Kinect;
using RowingMonitor.Model.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace RowingMonitor.Model.Pipeline
{
    public partial class RowingMetaDataCalculator
    {
        // temp calculation values
        private double startTimestamp = -1;
        private int strokeCount = 0;

        // stroke length
        private double minArmsZSeg = Double.PositiveInfinity;
        private double maxArmsZSeg = Double.NegativeInfinity;
        private double maxStrokeLengthSeg = Double.NegativeInfinity;
        private List<double> strokeLengths = new List<double>();

        // stroke time
        private double strokeStartTimestampSeg = -1;
        private List<double> strokeTimes = new List<double>();

        // seat travel distance
        private double minBaseZSeg = Double.PositiveInfinity;
        private double maxBaseZSeg = Double.NegativeInfinity;
        private double maxSeatTravelDistance = Double.NegativeInfinity;
        private List<double> seatTravelDistances = new List<double>();

        // handle velocity
        private double maxHandleVelocitySeg = Double.NegativeInfinity;
        private List<double> maxHandleVelocities = new List<double>();

        // Legs velocity
        private double maxLegsVelocitySeg = Double.NegativeInfinity;
        private List<double> maxLegsVelocities = new List<double>();

        // Arms velocity
        private double maxArmsVelocitySeg = Double.NegativeInfinity;
        private List<double> maxArmsVelocities = new List<double>();

        // Trunk velocity
        private double maxTrunkVelocitySeg = Double.NegativeInfinity;
        private List<double> maxTrunkVelocities = new List<double>();

        // trunk angle
        private double maxCatchAngleSeg = 0;
        private double maxFinishAngleSeg = 0;

        // catch factor
        private KleshnevData lastKleshnevData = new KleshnevData();
        private double lastCatchFactor = 0;
        private List<double> catchFactors = new List<double>();
        private long lastIndexChecked = -1;
        private double seatCrossedTimestamp = -1;
        private double handleCrossedTimestamp = -1;

        /// <summary>
        /// Calculate the values, which can be obtained in realtime.
        /// </summary>
        /// <param name="metaData"></param>
        private RowingMetaData CalculateRealtimeMetaData(RowingMetaData metaData)
        {
            // TODO: decide between segment started and no segment
            metaData.SegmentState = SegmentState.SegmentStarted;
            metaData.SessionTime = GetCurrentSessionTime();

            metaData.StrokeLength = GetStrokeLength(metaData);

            metaData.StrokeTime = GetStrokeTime(metaData);

            metaData.SeatTravelDistance = GetSeatTravelDistance(metaData);

            metaData.MaxHandleVelocity = GetMaxHandleVelocity(metaData);
            metaData.MaxLegsVelocity = GetMaxLegsVelocity(metaData);
            metaData.MaxArmsVelocity = GetMaxArmsVelocity(metaData);
            metaData.MaxTrunkVelocity = GetMaxTrunkVelocity(metaData);

            metaData.TrunkAngle = GetTrunkAngle(metaData);
            metaData.MaxCatchTrunkAngle = maxCatchAngleSeg;
            metaData.MaxFinishTrunkAngle = maxFinishAngleSeg;

            metaData.CatchFactor = GetCatchFactor();

            return metaData;
        }

        private double GetTrunkAngle(RowingMetaData metaData)
        {
            JointData jointData = GetJointDataByIndex(jointDataBuffer, metaData.Index);
            if (!jointData.IsEmpty) {
                // SpineBase is the new origin
                Point origin = new Point();
                origin.X = jointData.Joints[JointType.SpineBase].Position.Z;
                origin.Y = jointData.Joints[JointType.SpineBase].Position.Y;

                // calculate vector shoulder from SpineShoulder with the new origin
                Point shoulder = new Point();
                shoulder.X = jointData.Joints[JointType.SpineShoulder].Position.Z - origin.X;
                shoulder.Y = jointData.Joints[JointType.SpineShoulder].Position.Y - origin.Y;

                // calculate angle between shoulder vector and vector (0, 1)
                double tmpCurrentAngle = Math.Atan2(shoulder.X, shoulder.Y);
                
                if (tmpCurrentAngle < 0) {
                    maxCatchAngleSeg = tmpCurrentAngle < maxCatchAngleSeg ? 
                        tmpCurrentAngle : maxCatchAngleSeg;
                }
                if (tmpCurrentAngle > 0) {
                    maxFinishAngleSeg = tmpCurrentAngle > maxFinishAngleSeg ? 
                        tmpCurrentAngle : maxFinishAngleSeg;
                }
                return tmpCurrentAngle;
            }
            return 0;
        }

        private double GetMaxTrunkVelocity(RowingMetaData metaData)
        {
            KleshnevData klshData = GetKleshnevDataByIndex(kleshnevDataBuffer, metaData.Index);
            if (!klshData.IsEmpty) {
                double vel = klshData.Velocities[KleshnevVelocityType.Trunk];
                maxTrunkVelocitySeg = vel > maxTrunkVelocitySeg ? vel : maxTrunkVelocitySeg;
                return maxTrunkVelocitySeg;
            }
            return 0;
        }

        private double GetMaxArmsVelocity(RowingMetaData metaData)
        {
            KleshnevData klshData = GetKleshnevDataByIndex(kleshnevDataBuffer, metaData.Index);
            if (!klshData.IsEmpty) {
                double vel = (klshData.Velocities[KleshnevVelocityType.ArmsRight]
                    + klshData.Velocities[KleshnevVelocityType.ArmsLeft]) / 2;
                maxArmsVelocitySeg = vel > maxArmsVelocitySeg ? vel : maxArmsVelocitySeg;
                return maxArmsVelocitySeg;
            }
            return 0;
        }

        private double GetMaxLegsVelocity(RowingMetaData metaData)
        {
            KleshnevData klshData = GetKleshnevDataByIndex(kleshnevDataBuffer, metaData.Index);
            if (!klshData.IsEmpty) {
                double vel = klshData.Velocities[KleshnevVelocityType.Legs];
                maxLegsVelocitySeg = vel > maxLegsVelocitySeg ? vel : maxLegsVelocitySeg;
                return maxLegsVelocitySeg;
            }
            return 0;
        }

        private double GetMaxHandleVelocity(RowingMetaData metaData)
        {
            KleshnevData klshData = GetKleshnevDataByIndex(kleshnevDataBuffer, metaData.Index);
            if (!klshData.IsEmpty) {
                double vel = (klshData.Velocities[KleshnevVelocityType.HandleRight]
                    + klshData.Velocities[KleshnevVelocityType.HandleLeft]) / 2;
                maxHandleVelocitySeg = vel > maxHandleVelocitySeg ? vel : maxHandleVelocitySeg;
                return maxHandleVelocitySeg;
            }
            return 0;
        }

        private double GetSeatTravelDistance(RowingMetaData metaData)
        {
            JointData jointData = GetJointDataByIndex(jointDataBuffer, metaData.Index);
            if (!jointData.IsEmpty) {
                double z = jointData.Joints[JointType.SpineBase].Position.Z;
                minBaseZSeg = z < minBaseZSeg ? z : minBaseZSeg;
                maxBaseZSeg = z > maxBaseZSeg ? z : maxBaseZSeg;
                double seatTravel = maxBaseZSeg - minBaseZSeg;
                maxSeatTravelDistance = seatTravel > maxBaseZSeg ? seatTravel : maxBaseZSeg;
                return maxBaseZSeg;
            }
            return 0;
        }

        private double GetStrokeTime(RowingMetaData metaData)
        {
            JointData jointData = GetJointDataByIndex(jointDataBuffer, metaData.Index);
            if (!jointData.IsEmpty) {
                if (strokeStartTimestampSeg == -1) {
                    strokeStartTimestampSeg = jointData.Timestamps[0];
                }
                return jointData.Timestamps[0] - strokeStartTimestampSeg;
            }
            return 0;
        }

        private double GetStrokeLength(RowingMetaData metaData)
        {
            JointData jointData = GetJointDataByIndex(jointDataBuffer, metaData.Index);
            if (!jointData.IsEmpty) {
                double z = (jointData.Joints[Microsoft.Kinect.JointType.HandLeft].Position.Z
                    + jointData.Joints[Microsoft.Kinect.JointType.HandRight].Position.Z) / 2;
                minArmsZSeg = z < minArmsZSeg ? z : minArmsZSeg;
                maxArmsZSeg = z > maxArmsZSeg ? z : maxArmsZSeg;
                double strokeLength = maxArmsZSeg - minArmsZSeg;
                maxStrokeLengthSeg = strokeLength > maxStrokeLengthSeg ? strokeLength : maxStrokeLengthSeg;
                return maxStrokeLengthSeg;
            }
            return 0;
        }

        /// <summary>
        /// Calculate values that need all realtime segment values.
        /// </summary>
        /// <param name="metaData"></param>
        private RowingMetaData CalculateSegmentMetaData(RowingMetaData metaData)
        {
            metaData.SegmentState = SegmentState.SegmentEnded;

            strokeCount++;
            metaData.StrokeCount = strokeCount;            
            metaData.RowingStyleFactor = GetRowingStyleFactor();

            metaData.MeanStrokeLength = GetMeanStrokeLength();
            metaData.MeanStrokeTime = GetMeanStrokeTime(metaData);

            metaData.MeanSeatTravelDistance = GetMeanSeatTravelDistance();

            metaData.MeanMaxHandleVelocity = GetMeanMaxHandleVelocity();
            metaData.MeanMaxLegsVelocity = GetMeanMaxLegsVelocity();
            metaData.MeanMaxArmsVelocity = GetMeanMaxArmsVelocity();
            metaData.MeanMaxTrunkVelocity = GetMeanMaxTrunkVelocity();

            metaData.StrokesPerMinute = GetStrokesPerMinute(metaData);

            metaData.MeanCatchFactor = GetMeanCatchFactor();

            ResetSegmentData();

            return metaData;
        }

        private double GetMeanCatchFactor()
        {
            return catchFactors.Sum() / catchFactors.Count;
        }

        private double GetStrokesPerMinute(RowingMetaData metaData)
        {
            return metaData.StrokeCount / (metaData.SessionTime / 60000);
        }

        private double GetMeanMaxTrunkVelocity()
        {
            maxTrunkVelocities.Add(maxTrunkVelocitySeg);
            if (maxTrunkVelocities.Count > 0) {
                return maxTrunkVelocities.Sum() / maxTrunkVelocities.Count;
            }
            return 0;
        }

        private double GetMeanMaxArmsVelocity()
        {
            maxArmsVelocities.Add(maxArmsVelocitySeg);
            if (maxArmsVelocities.Count > 0) {
                return maxArmsVelocities.Sum() / maxArmsVelocities.Count;
            }
            return 0;
        }

        private double GetMeanMaxLegsVelocity()
        {
            maxLegsVelocities.Add(maxLegsVelocitySeg);
            if (maxLegsVelocities.Count > 0) {
                return maxLegsVelocities.Sum() / maxLegsVelocities.Count;
            }
            return 0;
        }

        private double GetMeanMaxHandleVelocity()
        {
            maxHandleVelocities.Add(maxHandleVelocitySeg);
            if (maxHandleVelocities.Count > 0) {
                return maxHandleVelocities.Sum() / maxHandleVelocities.Count;
            }
            return 0;
        }

        private double GetMeanSeatTravelDistance()
        {
            seatTravelDistances.Add(maxBaseZSeg);
            if (seatTravelDistances.Count > 0) {
                return seatTravelDistances.Sum() / seatTravelDistances.Count;
            }
            return 0;
        }

        private double GetMeanStrokeTime(RowingMetaData metaData)
        {
            strokeTimes.Add(metaData.StrokeTime);
            if (strokeTimes.Count > 0) {
                return strokeTimes.Sum() / strokeTimes.Count;
            }
            return 0;
        }

        private double GetMeanStrokeLength()
        {
            strokeLengths.Add(maxStrokeLengthSeg);
            if (strokeLengths.Count > 0) {
                return strokeLengths.Sum() / strokeLengths.Count;
            }
            return 0;
        }

        private double GetCurrentSessionTime()
        {
            if (startTimestamp == -1) {
                startTimestamp = jointDataBuffer[0].Timestamps[0];
            }
            return DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond - startTimestamp;
        }

        private double GetRowingStyleFactor()
        {
            // TODO
            return -1;
        }

        private double GetCatchFactor()
        {
            if (lastKleshnevData.IsEmpty) {
                lastKleshnevData = kleshnevDataBuffer.Last();
                return -1;
            }
            KleshnevData currentKleshnevData = kleshnevDataBuffer.Last();
            // check for zero crossings
            // only crossings that travel from negative to positive are required
            // seat crossing
            if (lastKleshnevData.Velocities[KleshnevVelocityType.Legs] < 0 
                && currentKleshnevData.Velocities[KleshnevVelocityType.Legs] >= 0) {
                if (currentKleshnevData.Velocities[KleshnevVelocityType.Legs] == 0) {
                    seatCrossedTimestamp = currentKleshnevData.AbsTimestamp;
                }
                else {
                    // get the timestamp between the two points
                    // y = m * x + b
                    // y = 0 -> x = -(b / m)
                    // m = (y2 - y1) / (x2- x1)
                    // b = y2 - m * x2
                    double m = (currentKleshnevData.Velocities[KleshnevVelocityType.Legs]
                        - lastKleshnevData.Velocities[KleshnevVelocityType.Legs])
                        / (currentKleshnevData.AbsTimestamp - lastKleshnevData.AbsTimestamp);
                    double b = currentKleshnevData.Velocities[KleshnevVelocityType.Legs]
                        - m * currentKleshnevData.AbsTimestamp;
                    seatCrossedTimestamp = -(b / m);
                }
            }
            // handle crossing
            if (lastKleshnevData.Velocities[KleshnevVelocityType.HandleRight] < 0
                && currentKleshnevData.Velocities[KleshnevVelocityType.HandleRight] >= 0) {
                if (currentKleshnevData.Velocities[KleshnevVelocityType.HandleRight] == 0) {
                    handleCrossedTimestamp = currentKleshnevData.AbsTimestamp;
                }
                else {
                    double m = (currentKleshnevData.Velocities[KleshnevVelocityType.HandleRight]
                        - lastKleshnevData.Velocities[KleshnevVelocityType.HandleRight])
                        / (currentKleshnevData.AbsTimestamp - lastKleshnevData.AbsTimestamp);
                    double b = currentKleshnevData.Velocities[KleshnevVelocityType.HandleRight]
                        - m * currentKleshnevData.AbsTimestamp;
                    handleCrossedTimestamp = -(b / m);
                }
            }
            // if both timestamps are set calculate the catch factor
            if (seatCrossedTimestamp != -1 && handleCrossedTimestamp != -1) {
                double catchFactor = seatCrossedTimestamp - handleCrossedTimestamp;
                catchFactors.Add(catchFactor);
                seatCrossedTimestamp = -1;
                handleCrossedTimestamp = -1;
                lastCatchFactor = catchFactor;
            }
            lastKleshnevData = kleshnevDataBuffer.Last();
            return lastCatchFactor;
        }

        /// <summary>
        /// Reset minimum and maximum values in a segment.
        /// </summary>
        private void ResetSegmentData()
        {
            // stroke length
            minArmsZSeg = Double.PositiveInfinity;
            maxArmsZSeg = Double.NegativeInfinity;
            maxStrokeLengthSeg = Double.NegativeInfinity;

            // stroke time
            strokeStartTimestampSeg = -1;

            // seat travel distance
            minBaseZSeg = Double.PositiveInfinity;
            maxBaseZSeg = Double.NegativeInfinity;
            maxSeatTravelDistance = Double.NegativeInfinity;

            // handle velocity
            maxHandleVelocitySeg = Double.NegativeInfinity;

            // Legs velocity
            maxLegsVelocitySeg = Double.NegativeInfinity;

            // Arms velocity
            maxArmsVelocitySeg = Double.NegativeInfinity;

            // Trunk velocity
            maxTrunkVelocitySeg = Double.NegativeInfinity;

            // trunk angle
            maxCatchAngleSeg = 0;
            maxFinishAngleSeg = 0;
        }

        private JointData GetJointDataByIndex(List<JointData> buffer, long index)
        {
            for (int i = buffer.Count - 1; i >= 0; i--) {
                if (buffer[i].Index == index) {
                    return buffer[i];
                }
            }
            return new JointData();
        }

        private KleshnevData GetKleshnevDataByIndex(List<KleshnevData> buffer, long index)
        {
            for (int i = buffer.Count - 1; i >= 0; i--) {
                if (buffer[i].Index == index) {
                    return buffer[i];
                }
            }
            return new KleshnevData();
        }
    }
}
