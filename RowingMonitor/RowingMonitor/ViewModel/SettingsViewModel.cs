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

        public SettingsViewModel()
        {
            Template = Properties.Settings.Default.Template;
            FootSpineBaseOffset = Properties.Settings.Default.FootSpineBaseOffset.ToString(
                CultureInfo.InvariantCulture.NumberFormat);
            MinSegmentTime = Properties.Settings.Default.MinSegmentTime.ToString(
                CultureInfo.InvariantCulture.NumberFormat);
            PeakDetectionWindow = Properties.Settings.Default.PeakDetectionWindow.ToString(
                CultureInfo.InvariantCulture.NumberFormat);
        }        

        public void SaveSettings()
        {
            Properties.Settings.Default.Template = Template;
            Properties.Settings.Default.FootSpineBaseOffset =
                float.Parse(FootSpineBaseOffset, CultureInfo.InvariantCulture.NumberFormat);
            Properties.Settings.Default.MinSegmentTime =
                double.Parse(MinSegmentTime, CultureInfo.InvariantCulture.NumberFormat);
            Properties.Settings.Default.PeakDetectionWindow =
                double.Parse(PeakDetectionWindow, CultureInfo.InvariantCulture.NumberFormat);

            Properties.Settings.Default.Save();
        }

        public string Template { get => template; set => template = value; }
        public string FootSpineBaseOffset { get => footSpineBaseOffset; set => footSpineBaseOffset = value; }
        public string MinSegmentTime { get => minSegmentTime; set => minSegmentTime = value; }
        public string PeakDetectionWindow { get => peakDetectionWindow; set => peakDetectionWindow = value; }
    }
}
