using RowingMonitor.Model.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace RowingMonitor.Model.Pipeline
{
    class RowingSonification
    {
        private bool mute;

        private ActionBlock<KleshnevData> input;
        private ActionBlock<List<SegmentHit>> inputSegmentHits;

        private double lastLegsVelocity = Double.NegativeInfinity;
        private double lastTrunkVelocity = Double.NegativeInfinity;
        private double lastArmsVelocity = Double.NegativeInfinity;

        private bool legsPeakPlayed = false;
        private bool trunkPeakPlayed = false;
        private bool armsPeakPlayed = false;

        private long[] lastBounds;

        public RowingSonification()
        {
            Input = new ActionBlock<KleshnevData>(kleshnevData =>
            {
                CheckForPeaks(kleshnevData);
            });

            InputSegmentHits = new ActionBlock<List<SegmentHit>>(hits =>
            {
                CheckForSegmentEnd(hits);
            });
        }

        private void CheckForPeaks(KleshnevData kleshnevData)
        {
            // TODO: very simple peak detection, do something more intelligent
            double legsVelocity = kleshnevData.Velocities[KleshnevVelocityType.Legs];
            double trunkVelocity = kleshnevData.Velocities[KleshnevVelocityType.Trunk];
            double armsVelocity = (kleshnevData.Velocities[KleshnevVelocityType.ArmsLeft] 
                + kleshnevData.Velocities[KleshnevVelocityType.ArmsRight]) / 2;

            if (legsVelocity > 0 && legsVelocity > lastLegsVelocity)
            {
                lastLegsVelocity = legsVelocity;
            }
            else if (!legsPeakPlayed && legsVelocity > 0 && legsVelocity < lastLegsVelocity)
            {
                PlayLegPeak();
                legsPeakPlayed = true;
            }

            if (trunkVelocity > 0 && trunkVelocity > lastTrunkVelocity)
            {
                lastTrunkVelocity = trunkVelocity;
            }
            else if (!trunkPeakPlayed && trunkVelocity > 0 && trunkVelocity < lastTrunkVelocity)
            {
                PlayTrunkPeak();
                trunkPeakPlayed = true;
            }

            if (armsVelocity > 0 && armsVelocity > lastArmsVelocity)
            {
                lastArmsVelocity = armsVelocity;
            }
            else if (!armsPeakPlayed && armsVelocity > 0 && armsVelocity < lastArmsVelocity)
            {
                PlayArmsPeak();
                armsPeakPlayed = true;
            }
        }

        private void CheckForSegmentEnd(List<SegmentHit> hits)
        {
            long[] bounds = SegmentHitHandler.GetLastSegmentStartEnd(hits);
            if (lastBounds == null)
            {
                lastBounds = bounds;
                return;
            }
            if (lastBounds.SequenceEqual(bounds))
            {
                return;
            }
            lastBounds = bounds;

            // reset
            lastLegsVelocity = Double.NegativeInfinity;
            lastTrunkVelocity = Double.NegativeInfinity;
            lastArmsVelocity = Double.NegativeInfinity;

            legsPeakPlayed = false;
            trunkPeakPlayed = false;
            armsPeakPlayed = false;

            PlaySegmentEnd();
        }

        private void PlayLegPeak()
        {
            if (!Mute)
            {
                Task.Run(() =>
                {
                    System.Console.Beep(2000, 30);
                });
            }            
        }

        private void PlayTrunkPeak()
        {
            if (!Mute)
            {
                Task.Run(() =>
                {
                    System.Console.Beep(4000, 30);
                });
            }
        }

        private void PlayArmsPeak()
        {
            if (!Mute)
            {
                Task.Run(() =>
                {
                    System.Console.Beep(6000, 30);
                });
            }
        }

        private void PlaySegmentEnd()
        {
            if (!Mute)
            {
                Task.Run(() =>
                {
                    System.Console.Beep(1000, 15);
                    System.Console.Beep(1000, 15);
                });
            }
        }

        public bool Mute { get => mute; set => mute = value; }
        public ActionBlock<KleshnevData> Input { get => input; set => input = value; }
        public ActionBlock<List<SegmentHit>> InputSegmentHits { get => inputSegmentHits; set => inputSegmentHits = value; }
    }
}
