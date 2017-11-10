using RowingMonitor.Model.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace RowingMonitor.Model.Pipeline
{
    /// <summary>
    /// RowingSonification searches for peaks in the velocities of legs, 
    /// trunk and arms and plays a tune if they appear.
    /// 
    /// It also plays a double tone if a rowing segment ended to 
    /// represent the rhythm of the rower.
    /// </summary>
    public class RowingSonification
    {
        private ActionBlock<KleshnevData> input;
        private ActionBlock<List<SegmentHit>> inputSegmentHits;

        // data for peak detection
        private MeanWindowPeakDetector legsPeakDetector;
        private MeanWindowPeakDetector trunkPeakDetector;
        private MeanWindowPeakDetector armsPeakDetector;

        private long[] lastBounds;

        // sound player
        private SoundPlayer legsSoundPlayer;
        private SoundPlayer trunkSoundPlayer;
        private SoundPlayer armsSoundPlayer;
        private SoundPlayer segmentSoundPlayer;

        /// <summary>
        /// Create a new RowingSonification.
        /// </summary>
        public RowingSonification()
        {
            // create peak detectors
            legsPeakDetector = new MeanWindowPeakDetector(Properties.Settings.Default.PeakDetectionWindow);
            trunkPeakDetector = new MeanWindowPeakDetector(Properties.Settings.Default.PeakDetectionWindow);
            armsPeakDetector = new MeanWindowPeakDetector(Properties.Settings.Default.PeakDetectionWindow);

            // load sounds
            legsSoundPlayer = new SoundPlayer(Properties.Resources.d3);
            trunkSoundPlayer = new SoundPlayer(Properties.Resources.e3);
            armsSoundPlayer = new SoundPlayer(Properties.Resources.g3);
            segmentSoundPlayer = new SoundPlayer(Properties.Resources.c3_twice);

            try {
                legsSoundPlayer.Load();
                trunkSoundPlayer.Load();
                armsSoundPlayer.Load();
                segmentSoundPlayer.Load();
            }
            catch (Exception e) {
                Logger.Log(this.ToString(), e.Message);
            }

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
            if (!Properties.Settings.Default.Mute)
            {
                if (Properties.Settings.Default.PlayBeep) {
                    Task.Run(() =>
                    {
                        Console.Beep(2000, 30);
                    });
                }
                else {
                    legsSoundPlayer.Play();
                }                
            }            
        }

        private void PlayTrunkPeak()
        {
            if (!Properties.Settings.Default.Mute)
            {
                if (Properties.Settings.Default.PlayBeep) {
                    Task.Run(() =>
                    {
                        Console.Beep(4000, 30);
                    });
                }
                else {
                    trunkSoundPlayer.Play();
                }
                
            }
        }

        private void PlayArmsPeak()
        {
            if (!Properties.Settings.Default.Mute)
            {
                if (Properties.Settings.Default.PlayBeep) {
                    Task.Run(() =>
                    {
                        Console.Beep(6000, 30);
                    });
                }
                else {
                    armsSoundPlayer.Play();
                }
                
            }
        }

        private void PlaySegmentEnd()
        {
            if (!Properties.Settings.Default.Mute)
            {
                if (Properties.Settings.Default.PlayBeep) {
                    Task.Run(() =>
                    {
                        Console.Beep(1000, 15);
                        Console.Beep(1000, 15);
                    });
                }
                else {
                    segmentSoundPlayer.Play();
                }
                
            }
        }

        /// <summary>
        /// Close all sound player.
        /// </summary>
        public void Destroy()
        {
            legsSoundPlayer.Dispose();
            trunkSoundPlayer.Dispose();
            armsSoundPlayer.Dispose();
            segmentSoundPlayer.Dispose();
        }
        
        /// <summary>
        /// The ActionBlock that recieves the KleshnevData.
        /// </summary>
        public ActionBlock<KleshnevData> Input { get => input; set => input = value; }
        /// <summary>
        /// The ActionBlock that recieves the list of SegmentHit.
        /// </summary>
        public ActionBlock<List<SegmentHit>> InputSegmentHits { get => inputSegmentHits; set => inputSegmentHits = value; }
    }
}
