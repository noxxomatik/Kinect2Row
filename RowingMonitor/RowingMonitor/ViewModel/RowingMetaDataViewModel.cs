using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RowingMonitor.Model.Util;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RowingMonitor.ViewModel
{
    class RowingMetaDataViewModel : INotifyPropertyChanged
    {
        private string frameIndex;
        private string sessionTime;
        private string strokeCount;
        private string strokeRate;
        private string catchFactor;
        private string rowingStyleFactor;
        private string strokeLength;
        private string meanStrokeLength;
        private string strokeTime;
        private string meanStrokeTime;
        private string seatTravelDistance;
        private string meanSeatTravelDistance;
        private string maxHandleVelocity;
        private string meanMaxHandleVelocity;
        private string maxLegsVelocity;
        private string meanMaxLegsVelocity;
        private string maxArmsVelocity;
        private string meanMaxArmsVelocity;
        private string maxTrunkVelocity;
        private string meanMaxTrunkVelocity;
        private string strokesPerMinute;
        private string trunkAngle;
        private string maxCatchTrunkAngle;
        private string maxFinishTrunkAngle;

        public event PropertyChangedEventHandler PropertyChanged;

        internal void Render(RowingMetaData metaData)
        {
            if (metaData.SegmentState != SegmentState.SegmentEnded) {
                FrameIndex = metaData.Index.ToString();
                SessionTime = (metaData.SessionTime / 60000).ToString("0.00");
                StrokeLength = metaData.StrokeLength.ToString("0.00");
                StrokeTime = (metaData.StrokeTime / 1000).ToString("0.00");
                SeatTravelDistance = metaData.SeatTravelDistance.ToString("0.00");
                MaxHandleVelocity = metaData.MaxHandleVelocity.ToString("0.00");
                MaxLegsVelocity = metaData.MaxLegsVelocity.ToString("0.00");
                MaxArmsVelocity = metaData.MaxArmsVelocity.ToString("0.00");
                MaxTrunkVelocity = metaData.MaxTrunkVelocity.ToString("0.00");
                TrunkAngle = Degrees.RadianToDegree(metaData.TrunkAngle).ToString("0.00");                
            }
            else {
                StrokeCount = metaData.StrokeCount.ToString();
                StrokeRate = metaData.StrokeRate.ToString("0.00");
                CatchFactor = metaData.CatchFactor.ToString("0.00");
                RowingStyleFactor = metaData.RowingStyleFactor.ToString("0.00");
                MeanStrokeLength = metaData.MeanStrokeLength.ToString("0.00");
                MeanStrokeTime = (metaData.MeanStrokeTime / 1000).ToString("0.00");
                MeanSeatTravelDistance = metaData.MeanSeatTravelDistance.ToString("0.00");
                MeanMaxHandleVelocity = metaData.MeanMaxHandleVelocity.ToString("0.00");
                MeanMaxLegsVelocity = metaData.MeanMaxLegsVelocity.ToString("0.00");
                MeanMaxArmsVelocity = metaData.MeanMaxArmsVelocity.ToString("0.00");
                MeanMaxTrunkVelocity = metaData.MeanMaxTrunkVelocity.ToString("0.00");
                StrokesPerMinute = metaData.StrokesPerMinute.ToString("0.00");
                MaxCatchTrunkAngle = Degrees.RadianToDegree(metaData.MaxCatchTrunkAngle).ToString("0.00");
                MaxFinishTrunkAngle = Degrees.RadianToDegree(metaData.MaxFinishTrunkAngle).ToString("0.00");
            }
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = "None")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string FrameIndex
        {
            get => frameIndex;
            set {
                frameIndex = value;
                OnPropertyChanged();
            }
        }
        public string SessionTime
        {
            get => sessionTime; set {
                sessionTime = value;
                OnPropertyChanged();
            }
        }
        public string StrokeCount
        {
            get => strokeCount; set {
                strokeCount = value;
                OnPropertyChanged();
            }
        }
        public string StrokeRate
        {
            get => strokeRate; set {
                strokeRate = value;
                OnPropertyChanged();
            }
        }
        public string CatchFactor
        {
            get => catchFactor; set {
                catchFactor = value;
                OnPropertyChanged();
            }
        }
        public string RowingStyleFactor
        {
            get => rowingStyleFactor; set {
                rowingStyleFactor = value;
                OnPropertyChanged();
            }
        }
        public string StrokeLength
        {
            get => strokeLength; set {
                strokeLength = value;
                OnPropertyChanged();
            }
        }
        public string MeanStrokeLength
        {
            get => meanStrokeLength; set {
                meanStrokeLength = value;
                OnPropertyChanged();
            }
        }
        public string StrokeTime
        {
            get => strokeTime; set {
                strokeTime = value;
                OnPropertyChanged();
            }
        }
        public string MeanStrokeTime
        {
            get => meanStrokeTime; set {
                meanStrokeTime = value;
                OnPropertyChanged();
            }
        }
        public string SeatTravelDistance
        {
            get => seatTravelDistance; set {
                seatTravelDistance = value;
                OnPropertyChanged();
            }
        }
        public string MeanSeatTravelDistance
        {
            get => meanSeatTravelDistance; set {
                meanSeatTravelDistance = value;
                OnPropertyChanged();
            }
        }
        public string MaxHandleVelocity
        {
            get => maxHandleVelocity; set {
                maxHandleVelocity = value;
                OnPropertyChanged();
            }
        }
        public string MeanMaxHandleVelocity
        {
            get => meanMaxHandleVelocity; set {
                meanMaxHandleVelocity = value;
                OnPropertyChanged();
            }
        }
        public string MaxLegsVelocity
        {
            get => maxLegsVelocity; set {
                maxLegsVelocity = value;
                OnPropertyChanged();
            }
        }
        public string MeanMaxLegsVelocity
        {
            get => meanMaxLegsVelocity; set {
                meanMaxLegsVelocity = value;
                OnPropertyChanged();
            }
        }
        public string MaxArmsVelocity
        {
            get => maxArmsVelocity; set {
                maxArmsVelocity = value;
                OnPropertyChanged();
            }
        }
        public string MeanMaxArmsVelocity
        {
            get => meanMaxArmsVelocity; set {
                meanMaxArmsVelocity = value;
                OnPropertyChanged();
            }
        }
        public string MaxTrunkVelocity
        {
            get => maxTrunkVelocity; set {
                maxTrunkVelocity = value;
                OnPropertyChanged();
            }
        }
        public string MeanMaxTrunkVelocity
        {
            get => meanMaxTrunkVelocity; set {
                meanMaxTrunkVelocity = value;
                OnPropertyChanged();
            }
        }
        public string StrokesPerMinute
        {
            get => strokesPerMinute; set {
                strokesPerMinute = value;
                OnPropertyChanged();
            }
        }
        public string TrunkAngle
        {
            get => trunkAngle; set {
                trunkAngle = value;
                OnPropertyChanged();
            }
        }
        public string MaxCatchTrunkAngle
        {
            get => maxCatchTrunkAngle; set {
                maxCatchTrunkAngle = value;
                OnPropertyChanged();
            }
        }
        public string MaxFinishTrunkAngle
        {
            get => maxFinishTrunkAngle; set {
                maxFinishTrunkAngle = value;
                OnPropertyChanged();
            }
        }
    }
}
