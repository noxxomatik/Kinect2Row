using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RowingMonitor.ViewModel
{
    class SettingsViewModel
    {
        private string template;
        private string footSpineBaseOffset;
        private string minSegmentTime;
        private string peakDetectionWindow;
        private string dTWStartMinimumPosition;
        private string dTWStartMaximumPosition;
        private string dTWMaxDistance;
        private bool playSounds;
        private bool playBeeps;

        public SettingsViewModel()
        {
            Template = Properties.Settings.Default.Template;
            FootSpineBaseOffset = Properties.Settings.Default.FootSpineBaseOffset.ToString(
                CultureInfo.CurrentUICulture.NumberFormat);
            MinSegmentTime = Properties.Settings.Default.MinSegmentTime.ToString(
                CultureInfo.CurrentUICulture.NumberFormat);
            PeakDetectionWindow = Properties.Settings.Default.PeakDetectionWindow.ToString(
                CultureInfo.CurrentUICulture.NumberFormat);
            DTWStartMinimumPosition = 
                Properties.Settings.Default.DTWStartMinimumPosition.ToString(CultureInfo.CurrentUICulture.NumberFormat);
            DTWStartMaximumPosition = 
                Properties.Settings.Default.DTWStartMaximumPosition.ToString(CultureInfo.CurrentUICulture.NumberFormat);
            DTWMaxDistance =
                Properties.Settings.Default.DTWMaxDistance.ToString(CultureInfo.CurrentUICulture.NumberFormat);
            PlaySounds = !Properties.Settings.Default.Mute;
            PlayBeeps = Properties.Settings.Default.PlayBeep;
        }        

        public void SaveSettings()
        {
            Properties.Settings.Default.Template = Template;
            Properties.Settings.Default.FootSpineBaseOffset =
                float.Parse(FootSpineBaseOffset, CultureInfo.CurrentUICulture.NumberFormat);
            Properties.Settings.Default.MinSegmentTime =
                double.Parse(MinSegmentTime, CultureInfo.CurrentUICulture.NumberFormat);
            Properties.Settings.Default.PeakDetectionWindow =
                double.Parse(PeakDetectionWindow, CultureInfo.CurrentUICulture.NumberFormat);
            Properties.Settings.Default.DTWStartMinimumPosition =
                float.Parse(DTWStartMinimumPosition, CultureInfo.CurrentUICulture.NumberFormat);
            Properties.Settings.Default.DTWStartMaximumPosition =
                float.Parse(DTWStartMaximumPosition, CultureInfo.CurrentUICulture.NumberFormat);
            Properties.Settings.Default.DTWMaxDistance =
                float.Parse(DTWMaxDistance, CultureInfo.CurrentUICulture.NumberFormat);
            Properties.Settings.Default.Mute = !PlaySounds;
            Properties.Settings.Default.PlayBeep = PlayBeeps;

            Properties.Settings.Default.Save();
        }

        public string Template { get => template; set => template = value; }
        public string FootSpineBaseOffset { get => footSpineBaseOffset; set => footSpineBaseOffset = value; }
        public string MinSegmentTime { get => minSegmentTime; set => minSegmentTime = value; }
        public string PeakDetectionWindow { get => peakDetectionWindow; set => peakDetectionWindow = value; }
        public string DTWStartMinimumPosition { get => dTWStartMinimumPosition; set => dTWStartMinimumPosition = value; }
        public string DTWStartMaximumPosition { get => dTWStartMaximumPosition; set => dTWStartMaximumPosition = value; }
        public string DTWMaxDistance { get => dTWMaxDistance; set => dTWMaxDistance = value; }
        public bool PlayBeeps { get => playBeeps; set => playBeeps = value; }
        public bool PlaySounds { get => playSounds; set => playSounds = value; }
    }
}
