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

        // data for peak detection
        private MeanWindowPeakDetector legsPeakDetector;
        private MeanWindowPeakDetector trunkPeakDetector;
        private MeanWindowPeakDetector armsPeakDetector;

        private long[] lastBounds;

        public RowingSonification()
        {
            legsPeakDetector = new MeanWindowPeakDetector(Properties.Settings.Default.PeakDetectionWindow);
            trunkPeakDetector = new MeanWindowPeakDetector(Properties.Settings.Default.PeakDetectionWindow);
            armsPeakDetector = new MeanWindowPeakDetector(Properties.Settings.Default.PeakDetectionWindow);

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
            double legsVelocity = kleshnevData.Velocities[KleshnevVelocityType.Legs];
            double trunkVelocity = kleshnevData.Velocities[KleshnevVelocityType.Trunk];
            double armsVelocity = (kleshnevData.Velocities[KleshnevVelocityType.ArmsLeft] 
                + kleshnevData.Velocities[KleshnevVelocityType.ArmsRight]) / 2;

            if (legsPeakDetector.HasPeak(kleshnevData.AbsTimestamp, legsVelocity)) {
                PlayLegPeak();
            }

            if (trunkPeakDetector.HasPeak(kleshnevData.AbsTimestamp, trunkVelocity)) {
                PlayTrunkPeak();
            }

            if(armsPeakDetector.HasPeak(kleshnevData.AbsTimestamp, armsVelocity)) {
                PlayArmsPeak();
            }
        }

        private void CheckForSegmentEnd(List<SegmentHit> hits)
        {
            long[] bounds = SegmentHitHandler.GetLastSegmentStartEnd(hits);
            if (SegmentHitHandler.IsSegmentValid(hits, bounds)) {
                if (lastBounds == null || (lastBounds[1] != bounds[1])) {
                    lastBounds = bounds;

                    legsPeakDetector.SegmentEnded();
                    trunkPeakDetector.SegmentEnded();
                    armsPeakDetector.SegmentEnded();

                    PlaySegmentEnd();
                }
            }            
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
