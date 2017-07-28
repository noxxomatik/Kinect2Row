using RowingMonitor.Model.Pipeline;
using RowingMonitor.View;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace RowingMonitor.ViewModel
{
    class TrunkAngleViewModel : INotifyPropertyChanged
    {
        private double catchTrunkAngle;
        private double finishTrunkAngle;
        private double trunkAngle;

        private string catchTrunkAngleString;
        private string finishTrunkAngleString;
        private string trunkAngleString;        

        public TrunkAngleViewModel()
        {
        }

        public void Render(double catchTrunkAngle, double finishTrunkAngle, double trunkAngle)
        {
            CatchTrunkAngle = catchTrunkAngle;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CatchTrunkAngle"));
            FinishTrunkAngle = finishTrunkAngle;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("FinishTrunkAngle"));
            TrunkAngle = trunkAngle;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TrunkAngle"));

            CatchTrunkAngleString = catchTrunkAngle.ToString("0.0") + "°";
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CatchTrunkAngleString"));
            FinishTrunkAngleString = finishTrunkAngle.ToString("0.0") + "°";
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("FinishTrunkAngleString"));
            TrunkAngleString = trunkAngle.ToString("0.0") + "°";
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TrunkAngleString"));
        }

        public double CatchTrunkAngle { get => catchTrunkAngle; set => catchTrunkAngle = value; }
        public double FinishTrunkAngle { get => finishTrunkAngle; set => finishTrunkAngle = value; }
        public double TrunkAngle { get => trunkAngle; set => trunkAngle = value; }
        public string CatchTrunkAngleString { get => catchTrunkAngleString; set => catchTrunkAngleString = value; }
        public string FinishTrunkAngleString { get => finishTrunkAngleString; set => finishTrunkAngleString = value; }
        public string TrunkAngleString { get => trunkAngleString; set => trunkAngleString = value; }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
