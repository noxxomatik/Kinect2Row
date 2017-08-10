using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RowingMonitor.Model.Util
{
    class PlotDataHandler
    {
    }

    public struct PlotData
    {
        private double x;
        private double y;
        private string annotation;
        private DataStreamType dataStreamType;
        private long index;

        public double X { get => x; set => x = value; }
        public double Y { get => y; set => y = value; }
        public string Annotation { get => annotation; set => annotation = value; }
        public DataStreamType DataStreamType { get => dataStreamType; set => dataStreamType = value; }
        public long Index { get => index; set => index = value; }
    }
}
