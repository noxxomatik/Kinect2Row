using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static RowingMonitor.Model.Pipeline.KleshnevVelocityCalculator;
using RowingMonitor.Model.Pipeline;

namespace RowingMonitor.Model
{
    /// <summary>
    /// Represents the arguments for a finished Kleshnev analysis.
    /// </summary>
    public class KleshnevEventArgs : EventArgs        
    {
        private List<KleshnevData> kleshnevData;

        public KleshnevEventArgs(List<KleshnevData> kleshnevData)
        {
            KleshnevData = kleshnevData;
        }

        internal List<KleshnevData> KleshnevData { get => kleshnevData; set => kleshnevData = value; }
    }
}
